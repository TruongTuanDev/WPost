using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WbTnvedManager.Data;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class TnvedSelectorService
    {
        private readonly MatrixRepository _repository;

        public TnvedSelectorService(MatrixRepository repository)
        {
            _repository = repository;
        }

        public (string TnvedCode, string MatchReason) GetTnvedForAttributes(int subjectId, string gender, string material, string knitType = "")
        {
            var match = _repository.FindBestMatch(subjectId, gender, material, knitType);
            if (match != null)
            {
                return (match.TnvedCode, $"Khớp chính xác quy tắc Ma trận: ID {match.Id} [{match.SubjectName}] ({match.Gender}, {match.Material})");
            }

            // Fallback prefix heuristic
            var prefix = GetFallbackPrefix(subjectId, gender, material, knitType);
            if (!string.IsNullOrEmpty(prefix))
            {
                return (prefix, $"Khớp theo tiền tố phân loại HS ({prefix})");
            }

            return (string.Empty, "Không tìm thấy mã phù hợp trong Ma trận");
        }

        public string InferGender(string text, string currentGender = "")
        {
            if (!string.IsNullOrWhiteSpace(currentGender))
            {
                var norm = currentGender.Trim().ToLowerInvariant();
                if (norm.Contains("жен")) return "Женский";
                if (norm.Contains("муж")) return "Мужской";
                if (norm.Contains("дев")) return "Девочки";
                if (norm.Contains("мал")) return "Мальчики";
                if (norm.Contains("уни")) return "Унисекс";
            }

            var t = (text ?? "").ToLowerInvariant();
            if (t.Contains("девоч") || t.Contains("для девочек")) return "Девочки";
            if (t.Contains("мальчик") || t.Contains("для мальчиков")) return "Мальчики";
            if (t.Contains("женск") || t.Contains("для женщин") || t.Contains("жен")) return "Женский";
            if (t.Contains("мужск") || t.Contains("для мужчин") || t.Contains("муж")) return "Мужской";
            if (t.Contains("унисекс") || t.Contains("unisex")) return "Унисекс";

            return "Женский"; // Default fallback in fashion
        }

        public string InferMaterial(string text, string currentMaterial = "")
        {
            var combined = $"{currentMaterial} {text}".ToLowerInvariant();
            if (combined.Contains("хлоп") || combined.Contains("cotton") || combined.Contains("деним") || combined.Contains("джинс")) return "Хлопок";
            if (combined.Contains("полиэст") || combined.Contains("синтет") || combined.Contains("polyester") || combined.Contains("нейлон") || combined.Contains("акрил") || combined.Contains("лайкра")) return "Синтетика";
            if (combined.Contains("шерст") || combined.Contains("wool") || combined.Contains("кашемир")) return "Шерсть";
            if (combined.Contains("лен") || combined.Contains("flax") || combined.Contains("linen")) return "Лен";
            if (combined.Contains("шелк") || combined.Contains("silk")) return "Шелк";
            if (combined.Contains("вискоз") || combined.Contains("viscose")) return "Вискоза";
            if (combined.Contains("кож") || combined.Contains("leather") || combined.Contains("экокож")) return "Кожа";

            return "Хлопок"; // Default fallback
        }

        public string InferKnitType(string text, string currentKnit = "")
        {
            var combined = $"{currentKnit} {text}".ToLowerInvariant();
            if (combined.Contains("трикот") || combined.Contains("вязан") || combined.Contains("джерси") || combined.Contains("футер") || combined.Contains("кулир") || combined.Contains("knit"))
                return "Трикотаж";
            if (combined.Contains("ткань") || combined.Contains("ткан") || combined.Contains("деним") || combined.Contains("джинс") || combined.Contains("костюм") || combined.Contains("woven") ||
                combined.Contains("плать") || combined.Contains("брюк") || combined.Contains("юбк") || combined.Contains("рубаш") || combined.Contains("пальт") || combined.Contains("куртк"))
                return "Ткань";

            return "Трикотаж";
        }

        private string GetFallbackPrefix(int subjectId, string gender, string material, string knitType)
        {
            bool isFemale = gender == "Женский" || gender == "Девочки";
            bool isKnit = knitType == "Трикотаж";

            // Known general subject IDs
            return subjectId switch
            {
                105 => "6109100000", // T-shirt cotton
                240 => isFemale ? (isKnit ? "6104620000" : "6204623100") : (isKnit ? "6103420000" : "6203423100"),
                273 => isFemale ? "6204623100" : "6203423100", // Jeans
                156 => isKnit ? "6104420000" : "6204420000", // Dress
                157 => isKnit ? "6104520000" : "6204520000", // Skirt
                138 => "6205200000", // Shirt
                139 => isKnit ? "6106100000" : "6206300000", // Blouse
                248 => isFemale ? "6110209900" : "6110209100", // Hoodie
                217 => isFemale ? "6202400000" : "6201400000", // Jacket
                197 => "6115950000", // Socks
                _ => string.Empty
            };
        }

        public object BuildUploadByItemPayload(int subjectId, string vendorCode, string title, string description, string gender, string material, string tnvedCode, string brand = "NoBrand", decimal price = 1000)
        {
            return new
            {
                subjectID = subjectId,
                variants = new[]
                {
                    new
                    {
                        vendorCode = vendorCode,
                        title = title,
                        description = description,
                        brand = brand,
                        characteristics = new object[]
                        {
                            new { id = 5, name = "ТНВЭД", value = tnvedCode },
                            new { id = 8, name = "Пол", value = gender },
                            new { id = 10, name = "Состав", value = material }
                        },
                        sizes = new[]
                        {
                            new
                            {
                                techSize = "FreeSize",
                                wbSize = "",
                                price = price,
                                skus = new[] { Guid.NewGuid().ToString("N")[..12] }
                            }
                        }
                    }
                }
            };
        }
    }
}
