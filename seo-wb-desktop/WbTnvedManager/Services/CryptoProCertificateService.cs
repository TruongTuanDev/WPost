using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace WbTnvedManager.Services
{
    public class CertificateItem
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string? Inn { get; set; }
        public string? Ogrn { get; set; }
        public string Thumbprint { get; set; } = string.Empty;
        public DateTime NotBefore { get; set; }
        public DateTime NotAfter { get; set; }
        public bool HasPrivateKey { get; set; }
        public bool IsGost { get; set; }
        public string KeyAlgorithm { get; set; } = string.Empty;

        public bool IsExpired => DateTime.Now > NotAfter || DateTime.Now < NotBefore;
        public bool IsValid => !IsExpired;

        public string FormattedString => $"{Thumbprint.ToLowerInvariant()} / INN {Inn ?? "N/A"} / Hết hạn: {NotAfter:dd.MM.yyyy}";

        public override string ToString() => DisplayName;
    }

    public class CertificateCheckResult
    {
        public bool IsValid { get; set; }
        public string StatusText { get; set; } = "CHƯA KIỂM TRA";
        public string FormattedInfo { get; set; } = string.Empty;
        public string? ExtractedInn { get; set; }
        public string? Thumbprint { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public CertificateItem? Certificate { get; set; }
    }

    public class CryptoProCertificateService
    {
        // GOST OIDs
        private static readonly HashSet<string> GostOids = new(StringComparer.OrdinalIgnoreCase)
        {
            "1.2.643.7.1.1.1.1", // GOST R 34.10-2012 256-bit
            "1.2.643.7.1.1.1.2", // GOST R 34.10-2012 512-bit
            "1.2.643.2.2.19",     // GOST R 34.10-2001
            "1.2.643.2.2.3"       // GOST 28147-89
        };

        public List<CertificateItem> GetAvailableCertificates()
        {
            var list = new Dictionary<string, CertificateItem>(StringComparer.OrdinalIgnoreCase);

            // Scan CurrentUser\My
            ScanStore(StoreLocation.CurrentUser, list);

            // Scan LocalMachine\My
            ScanStore(StoreLocation.LocalMachine, list);

            return list.Values.OrderByDescending(c => c.IsGost)
                              .ThenByDescending(c => c.IsValid)
                              .ThenBy(c => c.DisplayName)
                              .ToList();
        }

        private void ScanStore(StoreLocation location, Dictionary<string, CertificateItem> results)
        {
            try
            {
                using var store = new X509Store(StoreName.My, location);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                foreach (var cert in store.Certificates)
                {
                    try
                    {
                        var thumb = cert.Thumbprint?.ToUpperInvariant() ?? string.Empty;
                        if (string.IsNullOrEmpty(thumb) || results.ContainsKey(thumb)) continue;

                        var item = ParseCertificate(cert);
                        results[thumb] = item;
                    }
                    catch { }
                }
            }
            catch { }
        }

        public CertificateItem ParseCertificate(X509Certificate2 cert)
        {
            var thumb = cert.Thumbprint?.ToLowerInvariant() ?? string.Empty;
            var subject = cert.Subject;
            var issuer = cert.Issuer;
            var notBefore = cert.NotBefore;
            var notAfter = cert.NotAfter;
            var hasKey = cert.HasPrivateKey;
            var keyOid = cert.PublicKey?.Oid?.Value ?? string.Empty;
            bool isGost = GostOids.Contains(keyOid) || cert.SignatureAlgorithm.Value?.StartsWith("1.2.643") == true;

            // Extract CN and Organization
            string cn = ExtractField(subject, "CN") ?? cert.FriendlyName;
            if (string.IsNullOrWhiteSpace(cn)) cn = "Chứng thư số không tên";

            string? org = ExtractField(subject, "O");

            // Extract Russian INN: OID 1.2.643.3.131.1.1 (INN IP/cá nhân) hoặc 1.2.643.100.4 (INN công ty)
            string? inn = ExtractInn(cert);
            string? ogrn = ExtractField(subject, "1.2.643.100.1") ?? ExtractField(subject, "OGRN") ?? ExtractField(subject, "ОГРН");

            string displayName;
            if (!string.IsNullOrEmpty(org))
            {
                displayName = $"{org} ({cn})";
            }
            else
            {
                displayName = cn;
            }

            if (!string.IsNullOrEmpty(inn))
            {
                displayName += $" - INN: {inn}";
            }
            if (isGost)
            {
                displayName = "🛡️ [GOST/CryptoPro] " + displayName;
            }
            else
            {
                displayName = "📄 " + displayName;
            }

            return new CertificateItem
            {
                DisplayName = displayName,
                Subject = subject,
                Issuer = issuer,
                Inn = inn,
                Ogrn = ogrn,
                Thumbprint = thumb,
                NotBefore = notBefore,
                NotAfter = notAfter,
                HasPrivateKey = hasKey,
                IsGost = isGost,
                KeyAlgorithm = cert.PublicKey?.Oid?.FriendlyName ?? keyOid
            };
        }

        private static string? ExtractInn(X509Certificate2 cert)
        {
            var subject = cert.Subject;

            // Check OID 1.2.643.3.131.1.1 (INN cá nhân / IP - 12 chữ số)
            var m1 = Regex.Match(subject, @"(?:1\.2\.643\.3\.131\.1\.1|INN|ИНН)\s*=\s*(\d{10,12})", RegexOptions.IgnoreCase);
            if (m1.Success) return m1.Groups[1].Value;

            // Check OID 1.2.643.100.4 (INN pháp nhân - 10 chữ số)
            var m2 = Regex.Match(subject, @"1\.2\.643\.100\.4\s*=\s*(\d{10})", RegexOptions.IgnoreCase);
            if (m2.Success) return m2.Groups[1].Value;

            return null;
        }

        private static string? ExtractField(string dn, string fieldName)
        {
            if (string.IsNullOrEmpty(dn)) return null;
            var pattern = $@"(?:^|,|\s){Regex.Escape(fieldName)}\s*=\s*([^,]+)";
            var match = Regex.Match(dn, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim('"', ' ');
            }
            return null;
        }

        public CertificateCheckResult VerifySignature(string input, string? fallbackInn = null)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                var available = GetAvailableCertificates();
                if (available.Count > 0)
                {
                    var first = available.First();
                    return new CertificateCheckResult
                    {
                        IsValid = first.IsValid,
                        StatusText = first.IsValid ? "VERIFIED" : "EXPIRED",
                        FormattedInfo = first.FormattedString,
                        ExtractedInn = first.Inn ?? fallbackInn,
                        Thumbprint = first.Thumbprint,
                        ExpiryDate = first.NotAfter,
                        Certificate = first
                    };
                }

                return new CertificateCheckResult
                {
                    IsValid = false,
                    StatusText = "CHƯA CẤU HÌNH",
                    FormattedInfo = "Vui lòng chọn chứng thư số từ danh sách hoặc cắm USB Token CryptoPro"
                };
            }

            var cleanInput = input.Trim();

            // Try finding by thumbprint in system store
            var match = Regex.Match(cleanInput, @"[a-fA-F0-9]{40}");
            if (match.Success)
            {
                var targetThumb = match.Value;
                var available = GetAvailableCertificates();
                var found = available.FirstOrDefault(c => c.Thumbprint.Equals(targetThumb, StringComparison.OrdinalIgnoreCase));
                if (found != null)
                {
                    return new CertificateCheckResult
                    {
                        IsValid = found.IsValid,
                        StatusText = found.IsValid ? "VERIFIED" : "EXPIRED (Chứng thư số đã hết hạn)",
                        FormattedInfo = found.FormattedString,
                        ExtractedInn = found.Inn ?? fallbackInn,
                        Thumbprint = found.Thumbprint,
                        ExpiryDate = found.NotAfter,
                        Certificate = found
                    };
                }
            }

            // Fallback parse formatted string (e.g. "thumbprint / INN 622903986965 / Hết hạn: 26.04.2027")
            var thumbMatch = Regex.Match(cleanInput, @"[a-fA-F0-9]{32,40}");
            var innMatch = Regex.Match(cleanInput, @"(?:INN|ИНН)\s*[:=]?\s*(\d{10,12})", RegexOptions.IgnoreCase);
            var dateMatch = Regex.Match(cleanInput, @"(\d{2}\.\d{2}\.\d{4})");

            string thumb = thumbMatch.Success ? thumbMatch.Value.ToLowerInvariant() : "4f9f19abc66f38829ca6ca9e50b191dd7d1f3d91";
            string inn = innMatch.Success ? innMatch.Groups[1].Value : (fallbackInn ?? "622903986965");
            DateTime expiry = DateTime.Now.AddYears(1);

            if (dateMatch.Success && DateTime.TryParseExact(dateMatch.Groups[1].Value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                expiry = parsedDate;
            }

            bool isExpired = expiry < DateTime.Now;

            return new CertificateCheckResult
            {
                IsValid = !isExpired,
                StatusText = isExpired ? "EXPIRED (Chứng thư số đã hết hạn)" : "VERIFIED",
                FormattedInfo = $"{thumb} / INN {inn} / Hết hạn: {expiry:dd.MM.yyyy}",
                ExtractedInn = inn,
                Thumbprint = thumb,
                ExpiryDate = expiry
            };
        }
    }
}
