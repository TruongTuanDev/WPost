using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class NationalCatalogConnector : INationalCatalogConnector
    {
        private readonly HttpClient _httpClient;
        private string _apiKey = string.Empty;
        private string _baseUrl = "https://api.catalog.crpt.ru";

        public NationalCatalogConnector(HttpClient? httpClient = null, string apiKey = "", string baseUrl = "https://api.catalog.crpt.ru")
        {
            _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _apiKey = apiKey;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public void Configure(string apiKey, string baseUrl)
        {
            _apiKey = apiKey;
            if (!string.IsNullOrWhiteSpace(baseUrl)) _baseUrl = baseUrl.TrimEnd('/');
        }

        public async Task<bool> TestConnectionAsync(string apiKeyOrToken, string legalEntityInn, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(apiKeyOrToken)) return false;
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v3/categories");
                req.Headers.TryAddWithoutValidation("X-API-KEY", apiKeyOrToken);
                var res = await _httpClient.SendAsync(req, cancellationToken);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<NationalCatalogProduct>> GetProductsAsync(string legalEntityInn, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            // Boundary call to National Catalog v4/product-list & v3/feed-product
            var list = new List<NationalCatalogProduct>();
            if (string.IsNullOrWhiteSpace(_apiKey)) return list;

            try
            {
                var from = fromDate ?? DateTime.UtcNow.AddMonths(-6);
                var to = toDate ?? DateTime.UtcNow;
                var url = $"{_baseUrl}/v4/product-list?from_date={from:yyyy-MM-dd}&to_date={to:yyyy-MM-dd}&limit=100";
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.TryAddWithoutValidation("X-API-KEY", _apiKey);

                var res = await _httpClient.SendAsync(req, cancellationToken);
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("products", out var prodArray) && prodArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in prodArray.EnumerateArray())
                        {
                            var gtin = item.TryGetProperty("gtin", out var g) ? g.GetString() ?? "" : "";
                            var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                            var status = item.TryGetProperty("status", out var s) ? s.GetString() ?? "Опубликована" : "Опубликована";
                            var tnved = item.TryGetProperty("tnved", out var t) ? t.GetString() ?? "" : "";

                            list.Add(new NationalCatalogProduct
                            {
                                ProviderRecordId = gtin,
                                Gtin14 = gtin.Length == 13 ? "0" + gtin : gtin,
                                OwnerInn = legalEntityInn,
                                TitleRu = name,
                                Status = status,
                                TnvedCode = tnved,
                                IsDraft = status.Contains("Черновик", StringComparison.OrdinalIgnoreCase) || status.Contains("Требует", StringComparison.OrdinalIgnoreCase)
                            });
                        }
                    }
                }
            }
            catch { }

            return list;
        }

        public async Task<NationalCatalogProduct?> GetProductByGtinAsync(string gtin14, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(gtin14)) return null;
            try
            {
                var cleanGtin = gtin14.TrimStart('0');
                var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v3/product?gtin={cleanGtin}");
                req.Headers.TryAddWithoutValidation("X-API-KEY", _apiKey);
                var res = await _httpClient.SendAsync(req, cancellationToken);
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(json);
                    var name = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() ?? "Опубликована" : "Опубликована";
                    var tnved = doc.RootElement.TryGetProperty("tnved", out var t) ? t.GetString() ?? "" : "";

                    return new NationalCatalogProduct
                    {
                        ProviderRecordId = gtin14,
                        Gtin14 = gtin14.Length == 13 ? "0" + gtin14 : gtin14,
                        TitleRu = name,
                        Status = status,
                        TnvedCode = tnved,
                        IsDraft = status.Contains("Черновик", StringComparison.OrdinalIgnoreCase)
                    };
                }
            }
            catch { }
            return null;
        }

        public async Task<List<MarkirovkaFinding>> GetFeedStatusErrorsAsync(string feedId, CancellationToken cancellationToken = default)
        {
            var errors = new List<MarkirovkaFinding>();
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(feedId)) return errors;
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v3/feed-status?feed_id={feedId}");
                req.Headers.TryAddWithoutValidation("X-API-KEY", _apiKey);
                var res = await _httpClient.SendAsync(req, cancellationToken);
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("errors", out var errArray) && errArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var err in errArray.EnumerateArray())
                        {
                            var msg = err.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
                            var attr = err.TryGetProperty("attribute", out var a) ? a.GetString() ?? "" : "";
                            errors.Add(new MarkirovkaFinding
                            {
                                RuleId = "R09",
                                RuleTitle = "Lỗi kiểm duyệt thuộc tính National Catalog",
                                SourceKind = FindingSourceKind.OFFICIAL_SOURCE_ERROR,
                                Severity = IssueSeverity.BLOCK,
                                FieldPath = attr,
                                RawMessageRu = msg,
                                TranslatedExplanationVi = $"Hệ thống Национальный Каталог báo lỗi tại thuộc tính ''{attr}'': {msg}",
                                EvidenceSummary = $"Feed ID: {feedId}"
                            });
                        }
                    }
                }
            }
            catch { }
            return errors;
        }
    }
}