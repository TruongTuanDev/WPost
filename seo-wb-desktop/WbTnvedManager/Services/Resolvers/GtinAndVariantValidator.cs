using System;
using System.Collections.Generic;
using System.Linq;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services.Resolvers
{
    public class GtinValidationResult
    {
        public GtinLocalStatus LocalStatus { get; set; } = GtinLocalStatus.MISSING;
        public string? NormalizedGtin14 { get; set; }
        public string? ReasonCode { get; set; }
        public string ExplanationVi { get; set; } = string.Empty;
    }

    public static class GtinAndVariantValidator
    {
        public static GtinValidationResult ValidateGtin(string? gtin)
        {
            var res = new GtinValidationResult();

            if (string.IsNullOrWhiteSpace(gtin))
            {
                res.LocalStatus = GtinLocalStatus.MISSING;
                res.ExplanationVi = "Thiếu mã GTIN.";
                return res;
            }

            var clean = gtin.Trim().Replace(" ", "");

            if (!clean.All(char.IsDigit))
            {
                res.LocalStatus = GtinLocalStatus.INVALID_FORMAT;
                res.ReasonCode = "NON_DIGIT_CHARACTERS";
                res.ExplanationVi = $"Mã GTIN '{clean}' chứa ký tự không phải chữ số.";
                return res;
            }

            if (clean.Length == 12)
            {
                res.LocalStatus = GtinLocalStatus.UNSUPPORTED_FORMAT;
                res.ReasonCode = "UNSUPPORTED_FORMAT";
                res.ExplanationVi = "GTIN-12 không thuộc profile tiếp nhận 13/14 của hệ thống.";
                return res;
            }

            if (clean.Length != 13 && clean.Length != 14)
            {
                res.LocalStatus = GtinLocalStatus.INVALID_FORMAT;
                res.ReasonCode = "INVALID_LENGTH";
                res.ExplanationVi = $"Độ dài GTIN ({clean.Length}) không hợp lệ (yêu cầu GTIN-13 hoặc GTIN-14).";
                return res;
            }

            // Checksum validation
            int checkDigit = int.Parse(clean.Substring(clean.Length - 1, 1));
            string payload = clean.Substring(0, clean.Length - 1);

            int sum = 0;
            int weight = 3;
            for (int i = payload.Length - 1; i >= 0; i--)
            {
                int digit = payload[i] - '0';
                sum += digit * weight;
                weight = (weight == 3) ? 1 : 3;
            }

            int expectedCheckDigit = (10 - (sum % 10)) % 10;
            if (checkDigit != expectedCheckDigit)
            {
                res.LocalStatus = GtinLocalStatus.BAD_CHECKSUM;
                res.ReasonCode = "BAD_CHECKSUM";
                res.ExplanationVi = $"Sai chữ số kiểm tra (check digit={checkDigit}, kỳ vọng={expectedCheckDigit}).";
                return res;
            }

            res.LocalStatus = GtinLocalStatus.CHECKSUM_OK;
            res.NormalizedGtin14 = (clean.Length == 13) ? ("0" + clean) : clean;
            res.ExplanationVi = $"GTIN hợp lệ ({res.NormalizedGtin14}).";
            return res;
        }

        public static List<string> CheckVariantCollisions(List<ProductVariantInput> variants)
        {
            var collisions = new List<string>();
            var gtinToVariants = new Dictionary<string, List<ProductVariantInput>>();

            foreach (var v in variants)
            {
                if (string.IsNullOrWhiteSpace(v.GtinRaw)) continue;
                var gRes = ValidateGtin(v.GtinRaw);
                if (gRes.NormalizedGtin14 != null)
                {
                    if (!gtinToVariants.TryGetValue(gRes.NormalizedGtin14, out var list))
                    {
                        list = new List<ProductVariantInput>();
                        gtinToVariants[gRes.NormalizedGtin14] = list;
                    }
                    list.Add(v);
                }
            }

            foreach (var kvp in gtinToVariants)
            {
                var list = kvp.Value;
                if (list.Count > 1)
                {
                    // Check if they represent distinct commercial items (different color or size)
                    var distinctColors = list.Select(x => x.ColorCanonical ?? x.ColorRaw ?? "").Distinct().Count();
                    var distinctSizes = list.Select(x => x.SizeValue ?? "").Distinct().Count();

                    if (distinctColors > 1 || distinctSizes > 1)
                    {
                        collisions.Add($"GTIN_VARIANT_COLLISION: GTIN {kvp.Key} bị dùng trùng cho các biến thể khác màu/size ({list.Count} biến thể).");
                    }
                }
            }

            return collisions;
        }
    }
}
