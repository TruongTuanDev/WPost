using System;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services.Resolvers
{
    public class TariffGenderResolutionResult
    {
        public TariffGender TariffGender { get; set; } = TariffGender.UNKNOWN;
        public CommercialGender CommercialGender { get; set; } = CommercialGender.UNKNOWN;
        public string ExplanationVi { get; set; } = string.Empty;
        public string? ReasonCode { get; set; }
    }

    public static class TariffGenderResolver
    {
        public static TariffGenderResolutionResult Resolve(ProductVariantInput variant)
        {
            var res = new TariffGenderResolutionResult
            {
                CommercialGender = variant.CommercialGender
            };

            // 1. If audience or cut is explicitly Boys / Girls / Male / Female
            if (variant.Audience == AudienceKind.CHILD)
            {
                if (variant.CutGender != null)
                {
                    if (variant.CutGender.Equals("MALE", StringComparison.OrdinalIgnoreCase) || 
                        variant.CutGender.Equals("BOYS", StringComparison.OrdinalIgnoreCase))
                    {
                        res.TariffGender = TariffGender.M;
                        res.ExplanationVi = "Kiểu cắt rõ cho bé trai.";
                        return res;
                    }
                    if (variant.CutGender.Equals("FEMALE", StringComparison.OrdinalIgnoreCase) || 
                        variant.CutGender.Equals("GIRLS", StringComparison.OrdinalIgnoreCase))
                    {
                        res.TariffGender = TariffGender.F;
                        res.ExplanationVi = "Kiểu cắt rõ cho bé gái.";
                        return res;
                    }
                }

                if (variant.CommercialGender == CommercialGender.MALE)
                {
                    res.TariffGender = TariffGender.M;
                    res.ExplanationVi = "Đối tượng thương mại xác định là Bé trai.";
                    return res;
                }
                if (variant.CommercialGender == CommercialGender.FEMALE)
                {
                    res.TariffGender = TariffGender.F;
                    res.ExplanationVi = "Đối tượng thương mại xác định là Bé gái.";
                    return res;
                }
            }

            // 2. Cut gender for adults
            if (!string.IsNullOrEmpty(variant.CutGender))
            {
                if (variant.CutGender.Equals("MALE", StringComparison.OrdinalIgnoreCase))
                {
                    res.TariffGender = TariffGender.M;
                    res.ExplanationVi = "Kiểu cắt rõ cho nam.";
                    return res;
                }
                if (variant.CutGender.Equals("FEMALE", StringComparison.OrdinalIgnoreCase))
                {
                    res.TariffGender = TariffGender.F;
                    res.ExplanationVi = "Kiểu cắt rõ cho nữ.";
                    return res;
                }
            }

            // 3. Commercial gender MALE / FEMALE
            if (variant.CommercialGender == CommercialGender.MALE)
            {
                res.TariffGender = TariffGender.M;
                res.ExplanationVi = "Hồ sơ thương mại xác định cho Nam.";
                return res;
            }
            if (variant.CommercialGender == CommercialGender.FEMALE)
            {
                res.TariffGender = TariffGender.F;
                res.ExplanationVi = "Hồ sơ thương mại xác định cho Nữ.";
                return res;
            }

            // 4. Unisex / Indistinguishable cut: Check front fastening (Note 9 Ch 61/62)
            if (variant.CommercialGender == CommercialGender.UNISEX || 
                (variant.CutGender != null && variant.CutGender.Equals("INDISTINGUISHABLE", StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.IsNullOrEmpty(variant.FrontFastening))
                {
                    if (variant.FrontFastening.Equals("LEFT_TO_RIGHT", StringComparison.OrdinalIgnoreCase))
                    {
                        res.TariffGender = TariffGender.M;
                        res.ExplanationVi = "Hàng unisex/không phân biệt cắt may có cài trước từ trái qua phải (nhánh Nam theo Chú giải 9).";
                        return res;
                    }
                    if (variant.FrontFastening.Equals("RIGHT_TO_LEFT", StringComparison.OrdinalIgnoreCase))
                    {
                        res.TariffGender = TariffGender.F;
                        res.ExplanationVi = "Hàng unisex/không phân biệt cắt may có cài trước từ phải qua trái (nhánh Nữ theo Chú giải 9).";
                        return res;
                    }
                }

                // If fully verified that design cannot be distinguished and fastening does not resolve
                if (variant.CutGender != null && variant.CutGender.Equals("INDISTINGUISHABLE", StringComparison.OrdinalIgnoreCase))
                {
                    res.TariffGender = TariffGender.F_FALLBACK;
                    res.ExplanationVi = "Thiết kế unisex không phân định nam/nữ: Áp dụng Chú giải 9 phân loại theo nhánh Nữ (F_FALLBACK). Giữ nguyên commercial_gender = UNISEX.";
                    return res;
                }

                // If it's only a marketing label without verified cut or fastening evidence
                res.TariffGender = TariffGender.UNKNOWN;
                res.ReasonCode = "NEEDS_DATA";
                res.ExplanationVi = "Chỉ có nhãn marketing unisex; chưa có bằng chứng kiểu cắt/cách đóng để xác định nhánh thuế quan.";
                return res;
            }

            res.TariffGender = TariffGender.UNKNOWN;
            res.ReasonCode = "NEEDS_DATA";
            res.ExplanationVi = "Chưa có thông tin giới tính.";
            return res;
        }
    }
}
