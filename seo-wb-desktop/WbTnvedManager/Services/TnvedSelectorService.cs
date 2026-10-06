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

        public (string TnvedCode, string MatchReason) GetTnvedForAttributes(
            int subjectId, 
            string gender, 
            string material, 
            string knitType = "", 
            string subjectName = "", 
            string title = "", 
            string fullTextContext = "")
        {
            // 1. Exact Database Matrix Match (by SubjectId or SubjectName)
            var match = _repository.FindBestMatch(subjectId, gender, material, knitType, subjectName);
            if (match != null && !string.IsNullOrWhiteSpace(match.TnvedCode))
            {
                return (match.TnvedCode, $"Khớp chính xác quy tắc Ma trận: ID {match.SubjectId} [{match.SubjectName}] ({match.Gender}, {match.Material})");
            }

            // 2. Comprehensive Algorithmic Heuristics from backend FashionTnvedSelector
            var combinedText = $"{subjectName} {title} {fullTextContext} {gender} {material} {knitType}";
            var hint = BuildHint(subjectId, combinedText, subjectName: subjectName, gender: gender, material: material);

            var directCode = ResolveDirectTnved(hint, knitType);
            if (!string.IsNullOrEmpty(directCode))
            {
                return (directCode, $"Khớp quy tắc chuẩn ngành hàng '{hint.Family}' ({gender}, {material}, {hint.KnitState})");
            }

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

        public string? ResolveDirectTnved(TnvedSelectionHint hint, string knitInput = "")
        {
            var family = hint.Family;
            if (string.IsNullOrEmpty(family)) return null;

            var aud = hint.Audience ?? "female";
            bool isMaleSide = aud is "male" or "boys";
            bool isFemaleSide = aud is "female" or "girls";
            bool isBaby = aud is "baby";
            var mat = hint.MaterialFamily ?? "cotton";
            var knit = hint.KnitState ?? (knitInput.Contains("Ткань", StringComparison.OrdinalIgnoreCase) ? "woven" : "knit");

            // Baby clothes (<86cm)
            if (isBaby)
            {
                return mat == "synthetic" ? "6111309000" : "6111209000";
            }

            // 1. Pants / Trousers / Jeans / Shorts
            if (family is "pants" or "jeans" or "shorts")
            {
                if (family == "shorts")
                {
                    if (knit == "knit")
                    {
                        if (isMaleSide) return mat == "synthetic" ? "6103430000" : "6103420000";
                        return mat == "synthetic" ? "6104630000" : "6104620000";
                    }
                    if (isMaleSide) return mat == "synthetic" ? "6203439000" : "6203429000";
                    return mat == "synthetic" ? "6204639000" : "6204629000";
                }

                if (knit == "knit")
                {
                    if (isMaleSide)
                    {
                        return mat switch
                        {
                            "wool" => "6103410000",
                            "synthetic" => "6103430000",
                            _ => "6103420000"
                        };
                    }
                    else
                    {
                        return mat switch
                        {
                            "wool" => "6104610000",
                            "synthetic" => "6104630000",
                            _ => "6104620000"
                        };
                    }
                }
                else // Woven / Fabric / Denim
                {
                    if (isMaleSide)
                    {
                        return mat switch
                        {
                            "wool" => "6203411000",
                            "synthetic" => "6203431900",
                            _ => "6203423100"
                        };
                    }
                    else
                    {
                        return mat switch
                        {
                            "wool" => "6204611000",
                            "synthetic" => "6204631800",
                            _ => "6204623100"
                        };
                    }
                }
            }

            // 2. T-Shirts / Tops / Bodysuits / Longsleeves
            if (family is "tshirt" or "top")
            {
                return mat switch
                {
                    "synthetic" => "6109902000",
                    "wool" => "6109909000",
                    _ => "6109100000"
                };
            }

            // 3. Hoodies / Sweatshirts / Jumpers / Cardigans
            if (family is "hoodie" or "sweater")
            {
                if (mat == "wool") return "6110113000";
                if (mat == "synthetic") return isMaleSide ? "6110309100" : "6110309900";
                return isMaleSide ? "6110209100" : "6110209900";
            }

            // 4. Dresses / Sundresses
            if (family == "dress")
            {
                if (knit == "knit")
                {
                    return mat switch
                    {
                        "synthetic" => "6104430000",
                        "wool" => "6104410000",
                        _ => "6104420000"
                    };
                }
                else
                {
                    return mat switch
                    {
                        "synthetic" => "6204430000",
                        "wool" => "6204410000",
                        "silk" => "6204491000",
                        _ => "6204420000"
                    };
                }
            }

            // 5. Skirts
            if (family == "skirt")
            {
                if (knit == "knit") return mat == "synthetic" ? "6104530000" : "6104520000";
                return mat switch
                {
                    "synthetic" => "6204530000",
                    "wool" => "6204510000",
                    _ => "6204520000"
                };
            }

            // 6. Shirts / Blouses
            if (family is "shirt" or "blouse")
            {
                if (isMaleSide)
                {
                    if (knit == "knit") return "6105100000";
                    return mat switch
                    {
                        "synthetic" => "6205300000",
                        "flax" => "6205908000",
                        _ => "6205200000"
                    };
                }
                else
                {
                    if (knit == "knit") return "6106100000";
                    return mat switch
                    {
                        "synthetic" => "6206400000",
                        "silk" => "6206100000",
                        _ => "6206300000"
                    };
                }
            }

            // 7. Outerwear / Jackets / Coats
            if (family is "jacket" or "coat")
            {
                if (mat == "leather") return "4203100001";
                if (family == "coat")
                {
                    return isMaleSide ? "6201110000" : "6202110000";
                }
                if (isMaleSide) return mat == "cotton" ? "6201300000" : "6201400000";
                return mat == "cotton" ? "6202300000" : "6202400000";
            }

            // 8. Suits / Sets / Tracksuits
            if (family is "suit" or "set")
            {
                if (knit == "knit") return isMaleSide ? "6103100000" : "6104190000";
                return isMaleSide ? "6203228000" : "6204228000";
            }

            // 9. Socks / Hosiery
            if (family is "socks" or "hosiery")
            {
                if (mat == "synthetic") return "6115969900";
                return "6115950000";
            }

            // 10. Underwear
            if (family == "underwear")
            {
                if (isMaleSide) return mat == "synthetic" ? "6107120000" : "6107110000";
                return mat == "synthetic" ? "6108220000" : "6108210000";
            }

            // 11. Sleepwear
            if (family == "sleepwear")
            {
                if (isMaleSide) return "6107210000";
                return mat == "synthetic" ? "6108320000" : "6108310000";
            }

            // 12. Shoes
            if (family == "shoes")
            {
                if (mat == "leather") return isMaleSide ? "6403999600" : "6403999800";
                return "6404110000";
            }

            // 13. Headwear
            if (family == "hats")
            {
                return knit == "knit" ? "6505009000" : "6505003000";
            }

            // 14. Accessories / Bags
            if (family is "bags" or "accessories")
            {
                return "4202220000";
            }

            return null;
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
            // Jeans
            if (text.Contains("джинс") || text.Contains("jeans") || text.Contains("denim") || text.Contains("деним")) return "jeans";
            // Shorts
            if (text.Contains("шорт") || text.Contains("shorts") || text.Contains("бермуд") || text.Contains("бридж") || text.Contains("велосипедк") || text.Contains("капри")) return "shorts";
            // Pants & Sweatpants & Joggers & Leggings
            if (text.Contains("брюк") || text.Contains("штаны") || text.Contains("trousers") || text.Contains("pants") || 
                text.Contains("джоггер") || text.Contains("леггинс") || text.Contains("тайтс") || text.Contains("чинос") || 
                text.Contains("банан") || text.Contains("слакс") || text.Contains("палаццо") || text.Contains("карго") || 
                text.Contains("треник") || text.Contains("лосин")) return "pants";
            // Skirts
            if (text.Contains("юбк") || text.Contains("skirt")) return "skirt";
            // Dresses
            if (text.Contains("плать") || text.Contains("dress") || text.Contains("сарафан") || text.Contains("туник")) return "dress";
            // Blouses & Shirts
            if (text.Contains("блуз") || text.Contains("blouse")) return "blouse";
            if (text.Contains("рубаш") || text.Contains("shirt") || text.Contains("сорочк")) return "shirt";
            // Outerwear & Jackets
            if (text.Contains("пальт") || text.Contains("coat") || text.Contains("плащ") || text.Contains("шуб") || text.Contains("дубленк")) return "coat";
            if (text.Contains("куртк") || text.Contains("ветровк") || text.Contains("пуховик") || text.Contains("бомбер") || 
                text.Contains("анорак") || text.Contains("парк") || text.Contains("жакет") || text.Contains("jacket") || text.Contains("жилет")) return "jacket";
            // Hoodies & Sweatshirts
            if (text.Contains("худи") || text.Contains("толстовк") || text.Contains("свитшот") || text.Contains("олимпийк") || text.Contains("зип")) return "hoodie";
            // Knitwear & Sweaters & Jumpers
            if (text.Contains("свитер") || text.Contains("джемпер") || text.Contains("водолазк") || text.Contains("кардиган") || 
                text.Contains("пуловер") || text.Contains("кофт") || text.Contains("sweater")) return "sweater";
            // T-shirts & Tops
            if (text.Contains("футболк") || text.Contains("t-shirt") || text.Contains("tshirt") || text.Contains("майк") || 
                text.Contains("топ") || text.Contains("боди") || text.Contains("лонгслив") || text.Contains("поло") || text.Contains("тельняшк")) return "tshirt";
            // Suits & Sets & Tracksuits
            if (text.Contains("костюм") || text.Contains("комплект") || text.Contains("комбинезон") || text.Contains("tracksuit")) return "suit";
            // Socks & Hosiery
            if (text.Contains("носк") || text.Contains("колготк") || text.Contains("гольф") || text.Contains("следк") || text.Contains("чулк") || text.Contains("гетр")) return "socks";
            // Underwear
            if (text.Contains("трус") || text.Contains("боксер") || text.Contains("бюстгальтер") || text.Contains("лиф") || text.Contains("плавк") || text.Contains("кальсон") || text.Contains("термобель")) return "underwear";
            // Sleepwear
            if (text.Contains("пижам") || text.Contains("халат") || text.Contains("пеньюар") || text.Contains("кигуруми")) return "sleepwear";
            // Shoes
            if (text.Contains("кроссовк") || text.Contains("кед") || text.Contains("ботинк") || text.Contains("ботинок") || 
                text.Contains("сапог") || text.Contains("туфл") || text.Contains("сандал") || text.Contains("шлепанц") || 
                text.Contains("тапочк") || text.Contains("мокасин") || text.Contains("лофер") || text.Contains("балетк") || 
                text.Contains("угг") || text.Contains("слипон")) return "shoes";
            // Headwear
            if (text.Contains("шапк") || text.Contains("кепк") || text.Contains("бейсболк") || text.Contains("панам") || 
                text.Contains("балаклав") || text.Contains("берет") || text.Contains("ушанк") || text.Contains("повязк")) return "hats";
            // Bags & Accessories
            if (text.Contains("сумк") || text.Contains("рюкзак") || text.Contains("ремень") || text.Contains("кошелек") || 
                text.Contains("перчатк") || text.Contains("варежк") || text.Contains("шарф") || text.Contains("платок") || text.Contains("галстук")) return "bags";
            // Baby
            if (text.Contains("песочник") || text.Contains("ползунк") || text.Contains("распашонк") || text.Contains("слип")) return "baby";

            return null;
        }

        public string? InferAudience(string value)
        {
            var text = Normalize(value);
            if (text.Contains("малыш") || text.Contains("новорожден")) return "baby";
            if (text.Contains("девоч") || text.Contains("girls")) return "girls";
            if (text.Contains("мальч") || text.Contains("boys")) return "boys";
            if (text.Contains("женск") || text.Contains("жен") || text.Contains("women") || text.Contains("female") || text.Contains("девушк")) return "female";
            if (text.Contains("мужск") || text.Contains("men") || text.Contains("male") || text.Contains("муж") || text.Contains("парн")) return "male";
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
            if (text.Contains("трикот") || text.Contains("вязан") || text.Contains("jersey") || text.Contains("knit") || 
                text.Contains("футер") || text.Contains("спортивн") || text.Contains("джоггер") || text.Contains("худи") || 
                text.Contains("свитшот") || text.Contains("толстовк") || text.Contains("свитер") || text.Contains("джемпер") || 
                text.Contains("водолазк") || text.Contains("кардиган") || text.Contains("футболк") || text.Contains("майк") || 
                text.Contains("топ") || text.Contains("лонгслив") || text.Contains("боди") || text.Contains("носк") || 
                text.Contains("колготк") || text.Contains("трус")) return "knit";

            if (text.Contains("деним") || text.Contains("джинс") || text.Contains("лен") || text.Contains("linen") || 
                text.Contains("woven") || text.Contains("ткан") || text.Contains("костюм") || text.Contains("кожа") || 
                text.Contains("пальто") || text.Contains("куртка") || text.Contains("рубашка") || text.Contains("блузка")) return "woven";

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
            if (text.Contains("хлоп") || text.Contains("cotton") || text.Contains("деним") || text.Contains("джинс") || text.Contains("футер")) return "cotton";
            if (text.Contains("полиэстер") || text.Contains("синтет") || text.Contains("synthetic") || text.Contains("polyester") || text.Contains("viscose") || text.Contains("вискоз") || text.Contains("эластан") || text.Contains("нейлон") || text.Contains("лайкра")) return "synthetic";
            if (text.Contains("шерст") || text.Contains("wool") || text.Contains("кашемир") || text.Contains("мохер") || text.Contains("альпака")) return "wool";
            if (text.Contains("лен") || text.Contains("linen") || text.Contains("flax")) return "flax";
            if (text.Contains("шелк") || text.Contains("silk")) return "silk";
            if (text.Contains("кожа") || text.Contains("leather") || text.Contains("замш")) return "leather";
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
                "flax" => "Лен",
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
