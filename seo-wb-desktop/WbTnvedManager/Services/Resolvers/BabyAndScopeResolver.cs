using System;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services.Resolvers
{
    public class BabyAndScopeResolutionResult
    {
        public HeightScopeKind HeightScope { get; set; } = HeightScopeKind.UNKNOWN;
        public bool IsBabyEligible { get; set; }
        public string ExplanationVi { get; set; } = string.Empty;
        public string? ReasonCode { get; set; }
    }

    public static class BabyAndScopeResolver
    {
        public static BabyAndScopeResolutionResult Resolve(ProductVariantInput variant)
        {
            var res = new BabyAndScopeResolutionResult();

            // 1. Non-apparel (Headgear, Bags) -> NOT_APPLICABLE
            if (variant.ProductForm != null && (
                variant.ProductForm.Equals("BEANIE", StringComparison.OrdinalIgnoreCase) ||
                variant.ProductForm.Equals("CAP", StringComparison.OrdinalIgnoreCase) ||
                variant.ProductForm.Equals("HANDBAG", StringComparison.OrdinalIgnoreCase) ||
                variant.ProductForm.Equals("BACKPACK", StringComparison.OrdinalIgnoreCase) ||
                variant.ProductForm.Equals("BRA", StringComparison.OrdinalIgnoreCase) ||
                variant.ProductForm.Equals("BRA_BRIEF_SET", StringComparison.OrdinalIgnoreCase)))
            {
                res.HeightScope = HeightScopeKind.NOT_APPLICABLE;
                res.IsBabyEligible = false;
                res.ExplanationVi = "Không áp dụng quy tắc chiều cao em bé cho nhóm hàng này.";
                return res;
            }

            // 2. Adult -> NON_BABY
            if (variant.Audience == AudienceKind.ADULT)
            {
                res.HeightScope = HeightScopeKind.NON_BABY;
                res.IsBabyEligible = false;
                res.ExplanationVi = "Sản phẩm dành cho người lớn (NON_BABY).";
                return res;
            }

            // 3. Child / Infant with explicit body height
            if (variant.HeightMaxCm.HasValue)
            {
                decimal maxH = variant.HeightMaxCm.Value;
                decimal minH = variant.HeightMinCm ?? maxH;

                if (minH > 86m)
                {
                    res.HeightScope = HeightScopeKind.NON_BABY;
                    res.IsBabyEligible = false;
                    res.ExplanationVi = $"Chiều cao trẻ {minH}-{maxH} cm > 86 cm (NON_BABY).";
                    return res;
                }

                if (maxH <= 86m)
                {
                    res.HeightScope = HeightScopeKind.BABY_LE_86;
                    res.IsBabyEligible = true;
                    res.ExplanationVi = $"Chiều cao trẻ {minH}-{maxH} cm <= 86 cm (Ưu tiên nhóm Em bé 6111/6209).";
                    return res;
                }

                // Range straddles 86cm (e.g. 86-92cm)
                res.HeightScope = HeightScopeKind.UNKNOWN;
                res.ReasonCode = "NEEDS_DATA";
                res.ExplanationVi = $"Dải chiều cao {minH}-{maxH} cm vắt qua ngưỡng 86 cm; cần xác minh phân loại từng size.";
                return res;
            }

            // 4. Size value given without verified body height measure type
            if (!string.IsNullOrEmpty(variant.SizeValue))
            {
                if (variant.SizeMeasureType == null || !variant.SizeMeasureType.Equals("BODY_HEIGHT", StringComparison.OrdinalIgnoreCase))
                {
                    res.HeightScope = HeightScopeKind.UNKNOWN;
                    res.ReasonCode = "NEEDS_DATA";
                    res.ExplanationVi = $"Cột size ghi '{variant.SizeValue}' nhưng chưa xác minh đây là chiều cao cơ thể hay vòng ngực/vòng đầu (size_measure_type).";
                    return res;
                }
            }

            // 5. Infant audience without explicit height
            if (variant.Audience == AudienceKind.INFANT)
            {
                res.HeightScope = HeightScopeKind.BABY_LE_86;
                res.IsBabyEligible = true;
                res.ExplanationVi = "Đối tượng sơ sinh (Infant) thuộc nhóm em bé <= 86 cm.";
                return res;
            }

            res.HeightScope = HeightScopeKind.UNKNOWN;
            res.ReasonCode = "NEEDS_DATA";
            res.ExplanationVi = "Chưa đủ dữ liệu chiều cao để phân định nhóm em bé hay trẻ lớn.";
            return res;
        }
    }
}
