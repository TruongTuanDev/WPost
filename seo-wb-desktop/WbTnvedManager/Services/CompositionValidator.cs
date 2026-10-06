using System;
using System.Collections.Generic;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class CompositionValidationResult
    {
        public bool IsValid { get; set; } = true;
        public decimal TotalPercentage { get; set; }
        public string ComponentType { get; set; } = "OUTER";
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsMissingData { get; set; }
    }

    public static class CompositionValidator
    {
        /// <summary>
        /// Validates composition for a specific component (Outer, Lining, Filling).
        /// Rule R020: Total must be 100%. No auto-fudging or re-distributing.
        /// Rule R021: Separate components instead of mixing into a 200% total.
        /// </summary>
        public static CompositionValidationResult ValidateComponent(ProductComponent component)
        {
            var result = new CompositionValidationResult
            {
                ComponentType = component.Type
            };

            if (component.Fibers == null || component.Fibers.Count == 0)
            {
                result.IsValid = false;
                result.IsMissingData = true;
                result.ErrorMessage = $"Chưa có dữ liệu thành phần vải cho bộ phận {component.Type}.";
                return result;
            }

            decimal sum = 0;
            foreach (var fiber in component.Fibers)
            {
                if (fiber.Percentage < 0 || fiber.Percentage > 100)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Tỷ lệ sợi '{fiber.NameRu}' ({fiber.Percentage}%) không hợp lệ (phải từ 0-100%).";
                    return result;
                }
                sum += fiber.Percentage;
            }

            result.TotalPercentage = sum;

            if (sum != 100m)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Tổng tỷ lệ thành phần của bộ phận '{component.Type}' là {sum}%, không bằng 100%. Cần kiểm tra lại nhãn gốc thay vì tự điều chỉnh tỷ lệ.";
            }

            return result;
        }

        /// <summary>
        /// Validates all components of a product.
        /// </summary>
        public static List<CompositionValidationResult> ValidateAllComponents(List<ProductComponent> components)
        {
            var list = new List<CompositionValidationResult>();
            if (components == null || components.Count == 0)
            {
                list.Add(new CompositionValidationResult
                {
                    IsValid = false,
                    IsMissingData = true,
                    ErrorMessage = "Sản phẩm chưa khai báo thành phần cấu tạo (Состав)."
                });
                return list;
            }

            foreach (var comp in components)
            {
                list.Add(ValidateComponent(comp));
            }

            return list;
        }
    }
}
