using System;
using System.Collections.Generic;
using System.Linq;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class TnvedEvaluationResult
    {
        public bool IsValid { get; set; } = true;
        public string? ResolvedCode { get; set; }
        public string ReasonVi { get; set; } = string.Empty;
        public string ReasonRu { get; set; } = string.Empty;
        public bool NeedsReview { get; set; }
        public string? CandidateCode { get; set; }
        public string Chapter { get; set; } = string.Empty; // 61, 62, etc.
        public bool IsBabyBranch { get; set; }
    }

    public static class TnvedClassificationEngine
    {
        private static readonly HashSet<string> ValidTariffPrefixes = new()
        {
            "6101", "6102", "6103", "6104", "6105", "6106", "6107", "6108", "6109", "6110", "6111", "6112", "6115",
            "6201", "6202", "6203", "6204", "6205", "6206", "6207", "6208", "6209", "6211", "6212",
            "6401", "6402", "6403", "6404", "6405", "6505", "4202", "4203"
        };

        /// <summary>
        /// Validates if a TNVED code has exactly 10 ASCII digits.
        /// Rule R010: Full code must be 10 digits; do not pad zeros artificially.
        /// </summary>
        public static bool IsValid10DigitFormat(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            var clean = code.Trim().Replace(" ", "");
            return clean.Length == 10 && clean.All(char.IsDigit);
        }

        /// <summary>
        /// Evaluates product classification according to EAEU customs rules (Chapter 61/62 notes, Unisex Note 9, Baby height <= 86cm).
        /// </summary>
        public static TnvedEvaluationResult EvaluateClassification(
            string? currentCode,
            string productType,
            string construction, // KNITTED, WOVEN, UNKNOWN
            string audience,     // MALE, FEMALE, UNISEX, BOYS, GIRLS, BABY
            decimal? heightCm,
            string? materialFamily)
        {
            var result = new TnvedEvaluationResult();

            // 1. Format check
            if (!string.IsNullOrWhiteSpace(currentCode) && !IsValid10DigitFormat(currentCode))
            {
                result.IsValid = false;
                result.NeedsReview = true;
                result.ReasonVi = $"Mã ТН ВЭД '{currentCode}' không đủ 10 chữ số. Luật hải quan EAEU yêu cầu mã đầy đủ 10 chữ số (không tự động nối thêm số 0).";
                result.ReasonRu = "Код ТН ВЭД должен содержать ровно 10 знаков.";
                return result;
            }

            // 2. Baby check (Height <= 86cm -> Chapter 6111 for knit / 6209 for woven)
            if (audience.Equals("BABY", StringComparison.OrdinalIgnoreCase) || (heightCm.HasValue && heightCm.Value <= 86m))
            {
                result.IsBabyBranch = true;
                if (construction.Equals("KNITTED", StringComparison.OrdinalIgnoreCase))
                {
                    result.CandidateCode = "6111209000";
                    result.ReasonVi = "Sản phẩm dành cho trẻ sơ sinh / chiều cao ≤ 86cm dệt kim thuộc nhóm ТН ВЭД 6111.";
                }
                else
                {
                    result.CandidateCode = "6209200000";
                    result.ReasonVi = "Sản phẩm dành cho trẻ sơ sinh / chiều cao ≤ 86cm dệt thoi thuộc nhóm ТН ВЭД 6209.";
                }
                result.ResolvedCode = result.CandidateCode;
                return result;
            }

            // 3. Chapter 61 (Knitted) vs Chapter 62 (Woven)
            bool isKnitted = construction.Equals("KNITTED", StringComparison.OrdinalIgnoreCase);
            bool isWoven = construction.Equals("WOVEN", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(currentCode))
            {
                var clean = currentCode.Trim().Replace(" ", "");
                if (clean.StartsWith("61") && isWoven)
                {
                    result.IsValid = false;
                    result.NeedsReview = true;
                    result.ReasonVi = $"Mâu thuẫn kết cấu: Vải được xác nhận là Dệt thoi (Woven) nhưng mã ТН ВЭД {clean} thuộc Chương 61 (Dệt kim - Knitted).";
                    return result;
                }
                if (clean.StartsWith("62") && isKnitted)
                {
                    result.IsValid = false;
                    result.NeedsReview = true;
                    result.ReasonVi = $"Mâu thuẫn kết cấu: Vải được xác nhận là Dệt kim (Knitted) nhưng mã ТН ВЭД {clean} thuộc Chương 62 (Dệt thoi - Woven).";
                    return result;
                }
            }

            // 4. Note 9 EAEU: Unisex rule
            if (audience.Equals("UNISEX", StringComparison.OrdinalIgnoreCase))
            {
                result.CandidateCode = isWoven ? "6204620000" : "6104620000";
                result.ReasonVi = "Theo Note 9 (Ghi chú 9 Chương 61/62 EAEU): Hàng Unisex không phân định rõ kiểu cắt nam/nữ được phân loại theo biểu thuế nhánh Nữ/Bé gái (6104 / 6204).";
                result.ResolvedCode = result.CandidateCode;
                result.IsValid = true;
                return result;
            }

            result.IsValid = true;
            result.ResolvedCode = currentCode;
            return result;
        }
    }
}
