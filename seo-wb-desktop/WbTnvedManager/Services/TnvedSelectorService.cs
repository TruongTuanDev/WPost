using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WbTnvedManager.Data;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class TnvedSelectionHint
    {
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public string? Category { get; set; }
        public string? Gender { get; set; }
        public string? Material { get; set; }
        public string? Search { get; set; }
        public string SourceText { get; set; } = string.Empty;
        public string? Family { get; set; }
        public string? Audience { get; set; }
        public string? KnitState { get; set; }
        public string? MaterialFamily { get; set; }
        public List<string> Reasons { get; set; } = new();
    }

    public class TnvedScoredItem
    {
        public string TnvedCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Score { get; set; }
        public List<string> Reasons { get; set; } = new();
    }

    public class TnvedSelectorService
    {
        private static readonly Regex TokenRegex = new(@"[^\w\dа-яА-ЯёЁ]+", RegexOptions.Compiled);
        private readonly MatrixRepository _repository;

        public TnvedSelectorService(MatrixRepository repository)
        {
            _repository = repository;
        }

        public (string TnvedCode, string MatchReason) GetTnvedForAttributes(int subjectId, string gender, string material, string knitType = "")
        {
            // 1. Exact Database Matrix Match
            var match = _repository.FindBestMatch(subjectId, gender, material, knitType);
            if (match != null && !string.IsNullOrWhiteSpace(match.TnvedCode))
            {
                return (match.TnvedCode, $"Khớp chính xác quy tắc Ma trận: ID {match.Id} [{match.SubjectName}] ({match.Gender}, {match.Material})");
            }

            // 2. Advanced Heuristic Algorithm from backend FashionTnvedSelector
            var textContext = $"{gender} {material} {knitType}";
            var hint = BuildHint(subjectId, textContext, gender: gender, material: material);
            var expectedPrefixes = GetExpectedPrefixes(hint);

            if (expectedPrefixes.Count > 0)
            {
                var candidatePrefix = expectedPrefixes[0];
                var dbCandidate = _repository.Search(candidatePrefix).FirstOrDefault();
                if (dbCandidate != null)
                {
                    return (dbCandidate.TnvedCode, $"Khớp quy tắc phân loại nhóm {candidatePrefix} ({dbCandidate.Description})");
                }
                return (candidatePrefix.PadRight(10, '0'), $"Khớp theo tiền tố phân loại HS ({candidatePrefix})");
            }

            return (string.Empty, "Không tìm thấy mã phù hợp trong Ma trận");
        }

        public TnvedSelectionHint BuildHint(
            int subjectId,
            string sourceText,
            string? subjectName = null,
            string? gender = null,
            string? material = null)
        {
            var family = InferFamily(sourceText);
            var audience = InferAudience(gender ?? sourceText);
            var knitState = InferKnitState(sourceText, family);
            var materialFamily = InferMaterialFamily(material ?? sourceText);

            var reasons = new List<string>();
            if (!string.IsNullOrEmpty(family)) reasons.Add($"family={family}");
            if (!string.IsNullOrEmpty(audience)) reasons.Add($"audience={audience}");
            if (!string.IsNullOrEmpty(knitState)) reasons.Add($"knit={knitState}");
            if (!string.IsNullOrEmpty(materialFamily)) reasons.Add($"material_family={materialFamily}");

            return new TnvedSelectionHint
            {
                SubjectId = subjectId,
                SubjectName = subjectName,
                Gender = gender,
                Material = material,
                SourceText = sourceText,
                Family = family,
                Audience = audience,
                KnitState = knitState,
                MaterialFamily = materialFamily,
                Reasons = reasons
            };
        }

        public List<string> GetExpectedPrefixes(TnvedSelectionHint hint)
        {
            var audience = hint.Audience;
            var knit = hint.KnitState;
            var family = hint.Family;
            if (string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(knit) || string.IsNullOrEmpty(family))
                return new List<string>();

            var material = hint.MaterialFamily;
            bool isFemaleSide = audience is "female" or "girls";
            bool isMaleSide = audience is "male" or "boys";

            if (family is "pants" or "jeans" or "shorts")
            {
                if (knit == "knit")
                {
                    if (isFemaleSide) return TrouserMaterialPrefixes("61046", material);
                    if (isMaleSide) return TrouserMaterialPrefixes("61034", material);
                }
                if (isFemaleSide) return TrouserMaterialPrefixes("62046", material);
                if (isMaleSide) return TrouserMaterialPrefixes("62034", material);
            }

            if (family == "skirt" && isFemaleSide)
                return knit == "knit" ? new List<string> { "61045" } : new List<string> { "62045" };

            if (family == "dress" && isFemaleSide)
                return knit == "knit" ? new List<string> { "61044" } : new List<string> { "62044" };

            if (family == "tshirt")
                return new List<string> { "6109" };

            if (family is "shirt" or "blouse")
            {
                if (isFemaleSide) return knit == "knit" ? new List<string> { "6106" } : new List<string> { "6206" };
                if (isMaleSide) return knit == "knit" ? new List<string> { "6105" } : new List<string> { "6205" };
            }

            if (family is "jacket" or "coat")
            {
                if (isFemaleSide) return knit == "knit" ? new List<string> { "6102" } : new List<string> { "6202" };
                if (isMaleSide) return knit == "knit" ? new List<string> { "6101" } : new List<string> { "6201" };
            }

            return new List<string>();
        }

        private static List<string> TrouserMaterialPrefixes(string @base, string? materialFamily)
        {
            var suffix = materialFamily switch
            {
                "wool" => "1",
                "cotton" => "2",
                "flax" => "2",
                "synthetic" => "3",
                _ => null
            };

            if (!string.IsNullOrEmpty(suffix))
                return new List<string> { $"{@base}{suffix}", @base };

            return new List<string> { @base };
        }

        public string? InferFamily(string value)
        {
            var text = Normalize(value);
            if (text.Contains("джинс") || text.Contains("jeans") || text.Contains("denim")) return "jeans";
            if (text.Contains("брюк") || text.Contains("брюки") || text.Contains("trousers") || text.Contains("pants")) return "pants";
            if (text.Contains("шорт") || text.Contains("shorts")) return "shorts";
            if (text.Contains("юбк") || text.Contains("skirt")) return "skirt";
            if (text.Contains("плать") || text.Contains("dress")) return "dress";
            if (text.Contains("блуз") || text.Contains("blouse")) return "blouse";
            if (text.Contains("рубаш") || text.Contains("shirt") || text.Contains("сорочк")) return "shirt";
            if (text.Contains("куртк") || text.Contains("жакет") || text.Contains("jacket")) return "jacket";
            if (text.Contains("пальт") || text.Contains("coat")) return "coat";
            if (text.Contains("футболк") || text.Contains("t-shirt") || text.Contains("tshirt") || text.Contains("майк")) return "tshirt";
            return null;
        }

        public string? InferAudience(string value)
        {
            var text = Normalize(value);
            if (text.Contains("девоч") || text.Contains("girls")) return "girls";
            if (text.Contains("мальч") || text.Contains("boys")) return "boys";
            if (text.Contains("женск") || text.Contains("жен") || text.Contains("women") || text.Contains("female")) return "female";
            if (text.Contains("мужск") || text.Contains("men") || text.Contains("male") || text.Contains("муж")) return "male";
            if (text.Contains("unisex") || text.Contains("унисекс")) return "unisex";
            return null;
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

            var aud = InferAudience(text);
            return aud switch
            {
                "girls" => "Девочки",
                "boys" => "Мальчики",
                "male" => "Мужской",
                "female" => "Женский",
                "unisex" => "Унисекс",
                _ => "Женский"
            };
        }

        public string? InferKnitState(string value, string? family = null)
        {
            var text = Normalize(value);
            if (text.Contains("трикот") || text.Contains("вязан") || text.Contains("jersey") || text.Contains("knit") || text.Contains("футер")) return "knit";
            if (text.Contains("деним") || text.Contains("джинс") || text.Contains("лен") || text.Contains("linen") || text.Contains("woven") || text.Contains("ткан") || text.Contains("костюм")) return "woven";
            if (family is "pants" or "jeans" or "shorts" or "skirt" or "dress" or "shirt" or "blouse" or "jacket" or "coat") return "woven";
            return "knit";
        }

        public string InferKnitType(string text, string currentKnit = "")
        {
            var state = InferKnitState($"{currentKnit} {text}", InferFamily(text));
            return state == "knit" ? "Трикотаж" : "Ткань";
        }

        public string? InferMaterialFamily(string value)
        {
            var text = Normalize(value);
            if (text.Contains("хлоп") || text.Contains("cotton") || text.Contains("деним") || text.Contains("джинс")) return "cotton";
            if (text.Contains("полиэстер") || text.Contains("синтет") || text.Contains("synthetic") || text.Contains("polyester") || text.Contains("viscose") || text.Contains("вискоз")) return "synthetic";
            if (text.Contains("шерст") || text.Contains("wool") || text.Contains("кашемир")) return "wool";
            if (text.Contains("лен") || text.Contains("linen") || text.Contains("flax")) return "flax";
            if (text.Contains("шелк") || text.Contains("silk")) return "silk";
            if (text.Contains("кожа") || text.Contains("leather")) return "leather";
            return null;
        }

        public string InferMaterial(string text, string currentMaterial = "")
        {
            var mat = InferMaterialFamily($"{currentMaterial} {text}");
            return mat switch
            {
                "cotton" => "Хлопок",
                "synthetic" => "Синтетика",
                "wool" => "Шерсть",
                "flax" => "Лen",
                "silk" => "Шелк",
                "leather" => "Кожа",
                _ => "Хлопок"
            };
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var norm = value.ToLowerInvariant().Replace("ё", "е");
            norm = TokenRegex.Replace(norm, " ");
            return string.Join(" ", norm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public object BuildUploadByItemPayload(
            int subjectId,
            string vendorCode,
            string title,
            string description,
            string gender,
            string material,
            string tnvedCode,
            string brand = "NoBrand",
            string color = "Черный",
            int length = 30,
            int width = 25,
            int height = 5,
            double weightBrutto = 0.5,
            List<ProductSizeItem>? sizeList = null)
        {
            var charcs = new List<object>();

            // ID 5: TNVED
            if (!string.IsNullOrWhiteSpace(tnvedCode))
            {
                charcs.Add(new { id = 5, name = "ТНВЭД", value = tnvedCode.Trim() });
            }

            // ID 8: Gender (array of strings according to WB schema)
            if (!string.IsNullOrWhiteSpace(gender))
            {
                charcs.Add(new { id = 8, name = "Пол", value = new[] { gender.Trim() } });
            }

            // Material / Composition
            if (!string.IsNullOrWhiteSpace(material))
            {
                charcs.Add(new { id = 14177451, name = "Состав", value = material.Trim() });
            }

            // Color
            if (!string.IsNullOrWhiteSpace(color))
            {
                charcs.Add(new { id = 1000, name = "Цвет", value = new[] { color.Trim() } });
            }

            // Sizes
            var sizesPayload = new List<object>();
            if (sizeList != null && sizeList.Count > 0)
            {
                foreach (var s in sizeList)
                {
                    var skusList = new List<string>();
                    if (!string.IsNullOrWhiteSpace(s.Barcode))
                    {
                        skusList.Add(s.Barcode.Trim());
                    }

                    sizesPayload.Add(new
                    {
                        techSize = string.IsNullOrWhiteSpace(s.TechSize) ? "0" : s.TechSize.Trim(),
                        wbSize = string.IsNullOrWhiteSpace(s.WbSize) ? (string.IsNullOrWhiteSpace(s.TechSize) ? "0" : s.TechSize.Trim()) : s.WbSize.Trim(),
                        price = (int)s.Price,
                        skus = skusList.ToArray()
                    });
                }
            }
            else
            {
                sizesPayload.Add(new
                {
                    techSize = "0",
                    wbSize = "0",
                    price = 1000,
                    skus = Array.Empty<string>()
                });
            }

            return new[]
            {
                new
                {
                    subjectID = subjectId,
                    variants = new[]
                    {
                        new
                        {
                            vendorCode = string.IsNullOrWhiteSpace(vendorCode) ? $"PROD-{DateTime.Now:yyyyMMddHHmmss}" : vendorCode.Trim(),
                            title = string.IsNullOrWhiteSpace(title) ? "Товар Wildberries" : title.Trim(),
                            description = description?.Trim() ?? string.Empty,
                            brand = string.IsNullOrWhiteSpace(brand) ? "Нет бренда" : brand.Trim(),
                            dimensions = new
                            {
                                length = Math.Max(1, length),
                                width = Math.Max(1, width),
                                height = Math.Max(1, height),
                                weightBrutto = Math.Max(0.01, weightBrutto)
                            },
                            characteristics = charcs.ToArray(),
                            sizes = sizesPayload.ToArray()
                        }
                    }
                }
            };
        }
    }
}
