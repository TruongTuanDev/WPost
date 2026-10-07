using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WbTnvedManager.Models;
using WbTnvedManager.Services.Resolvers;

namespace WbTnvedManager.Services
{
    public class CardAuditService
    {
        private readonly TnvedSelectorService _selector;
        private readonly RulesEngine _rulesEngine;
        private readonly ProductVariantAuditEngine _specAuditEngine;

        public CardAuditService(TnvedSelectorService selector, ProductVariantAuditEngine? specAuditEngine = null)
        {
            _selector = selector;
            _rulesEngine = new RulesEngine(selector);
            
            if (specAuditEngine != null)
            {
                _specAuditEngine = specAuditEngine;
            }
            else
            {
                var matrix104 = new TnvedMatrix104Engine();
                _specAuditEngine = new ProductVariantAuditEngine(matrix104);
            }
        }

        public List<AuditResultItem> AuditCards(IEnumerable<WbCardItem> cards)
        {
            var results = new List<AuditResultItem>();

            foreach (var card in cards)
            {
                var currentTnved = card.CurrentTnved?.Trim() ?? string.Empty;
                var currentGender = card.CurrentGender?.Trim() ?? string.Empty;
                var currentMaterial = card.CurrentMaterial?.Trim() ?? string.Empty;

                var textContext = $"{card.SubjectName} {card.Title} {card.Description} {currentMaterial}";
                var detectedGender = _selector.InferGender(textContext, currentGender);
                var detectedMaterial = _selector.InferMaterial(textContext, currentMaterial);
                var detectedKnit = _selector.InferKnitType(textContext);

                // 1. Construct Strongly-Typed ProductVariantInput for 104-rule Engine
                var variant = MapCardToVariant(card, textContext, detectedGender, detectedMaterial, detectedKnit);

                // 2. Execute 104-Rule Canonical Spec Audit
                var specResult = _specAuditEngine.AuditVariant(variant);

                // 3. Fallback Evaluation Context for RulesEngine (issues, readiness)
                var evaluationContext = new AuditEvaluationContext
                {
                    WbCard = card,
                    Account = new SellerAccount()
                };
                var report = _rulesEngine.Evaluate(evaluationContext);

                var item = new AuditResultItem
                {
                    Card = card,
                    CurrentTnved = currentTnved,
                    CurrentGender = currentGender,
                    DetectedMaterial = detectedMaterial,
                    Issues = report.Issues,
                    Readiness = report.Readiness,
                    SpecAuditDetails = specResult,
                    IsSelected = true
                };

                // 4. Synthesize Conclusions
                if (specResult.Checks.Classification == ClassificationStatus.MATCH)
                {
                    item.SuggestedTnved = specResult.Classification.CurrentTnved10 ?? currentTnved;
                    item.SuggestedGender = !string.IsNullOrEmpty(currentGender) ? currentGender : detectedGender;
                    item.MatchReason = specResult.Classification.ExplanationVi;
                    item.Status = AuditStatus.MatchOk;
                    item.StatusMessage = $"Mã TNVED và Giới tính đã khớp chuẩn ma trận 104 dòng: {specResult.Classification.ExplanationVi}";
                    item.IsSelected = false;
                }
                else if (specResult.Checks.Classification == ClassificationStatus.MISMATCH ||
                         specResult.Checks.Classification == ClassificationStatus.MISSING_CODE)
                {
                    var targetCode = specResult.Classification.CandidateTnved10.FirstOrDefault();
                    if (!string.IsNullOrEmpty(targetCode))
                    {
                        var resolvedTariffGender = TariffGenderResolver.Resolve(variant).TariffGender;
                        item.SuggestedTnved = targetCode;
                        item.SuggestedGender = MapTariffGenderToString(resolvedTariffGender, currentGender, detectedGender);
                        item.MatchReason = specResult.Classification.ExplanationVi;

                        bool tnvedMatches = !string.IsNullOrEmpty(currentTnved) && currentTnved.Equals(targetCode, StringComparison.OrdinalIgnoreCase);
                        bool genderMatches = !string.IsNullOrEmpty(currentGender) && currentGender.Equals(item.SuggestedGender, StringComparison.OrdinalIgnoreCase);

                        if (!tnvedMatches && !genderMatches)
                        {
                            item.Status = AuditStatus.BothMismatch;
                            item.StatusMessage = $"Sai cả TNVED ({currentTnved} -> {targetCode}) và Giới tính ({currentGender} -> {item.SuggestedGender})";
                        }
                        else if (!tnvedMatches)
                        {
                            item.Status = AuditStatus.TnvedMismatch;
                            item.StatusMessage = $"Lệch mã TNVED ({currentTnved} -> {targetCode}): {specResult.Classification.ExplanationVi}";
                        }
                        else
                        {
                            item.Status = AuditStatus.GenderMismatch;
                            item.StatusMessage = $"Thiếu hoặc lệch Giới tính ({currentGender} -> {item.SuggestedGender})";
                        }

                        item.IsSelected = true;
                    }
                    else
                    {
                        ApplyFallbackMatrix(item, card, currentTnved, currentGender, detectedGender, detectedMaterial, detectedKnit, textContext);
                    }
                }
                else if (specResult.Checks.Classification == ClassificationStatus.NOT_COVERED)
                {
                    // Commodity outside 104 lines: Fallback smoothly to SQLite matrix
                    ApplyFallbackMatrix(item, card, currentTnved, currentGender, detectedGender, detectedMaterial, detectedKnit, textContext);
                }
                else
                {
                    // NEEDS_DATA or AMBIGUOUS
                    item.Status = AuditStatus.NoMatrixMatch;
                    item.StatusMessage = specResult.Classification.ExplanationVi;
                    item.SuggestedGender = detectedGender;
                    item.IsSelected = false;
                }

                results.Add(item);
            }

            return results;
        }

        private void ApplyFallbackMatrix(
            AuditResultItem item, 
            WbCardItem card, 
            string currentTnved, 
            string currentGender, 
            string detectedGender, 
            string detectedMaterial, 
            string detectedKnit, 
            string textContext)
        {
            var (suggestedTnved, matchReason) = _selector.GetTnvedForAttributes(
                card.SubjectId,
                detectedGender,
                detectedMaterial,
                detectedKnit,
                subjectName: card.SubjectName,
                title: card.Title,
                fullTextContext: textContext
            );

            item.SuggestedTnved = suggestedTnved;
            item.SuggestedGender = detectedGender;
            item.MatchReason = matchReason;

            if (string.IsNullOrEmpty(suggestedTnved))
            {
                item.Status = AuditStatus.NoMatrixMatch;
                item.StatusMessage = "Chưa có quy tắc tra cứu trong ma trận cho danh mục này.";
                item.IsSelected = false;
            }
            else
            {
                bool tnvedMatches = !string.IsNullOrEmpty(currentTnved) && currentTnved.Equals(suggestedTnved, StringComparison.OrdinalIgnoreCase);
                bool genderMatches = !string.IsNullOrEmpty(currentGender) && currentGender.Equals(detectedGender, StringComparison.OrdinalIgnoreCase);

                if (tnvedMatches && genderMatches)
                {
                    item.Status = AuditStatus.MatchOk;
                    item.StatusMessage = "Mã TNVED và Giới tính đã hoàn toàn chính xác.";
                    item.IsSelected = false;
                }
                else if (!tnvedMatches && !genderMatches)
                {
                    item.Status = AuditStatus.BothMismatch;
                    item.StatusMessage = $"Sai cả TNVED ({currentTnved} -> {suggestedTnved}) và Giới tính ({currentGender} -> {detectedGender})";
                    item.IsSelected = true;
                }
                else if (!tnvedMatches)
                {
                    item.Status = AuditStatus.TnvedMismatch;
                    item.StatusMessage = $"Lệch mã TNVED ({currentTnved} -> {suggestedTnved})";
                    item.IsSelected = true;
                }
                else
                {
                    item.Status = AuditStatus.GenderMismatch;
                    item.StatusMessage = $"Thiếu hoặc sai Giới tính ({currentGender} -> {detectedGender})";
                    item.IsSelected = true;
                }
            }
        }

        private ProductVariantInput MapCardToVariant(
            WbCardItem card, 
            string textContext, 
            string detectedGender, 
            string detectedMaterial, 
            string detectedKnit)
        {
            var lowerText = textContext.ToLowerInvariant();
            var lowerSub = (card.SubjectName ?? string.Empty).ToLowerInvariant();
            var lowerTitle = (card.Title ?? string.Empty).ToLowerInvariant();

            // 1. Gender & Audience mapping
            CommercialGender commGender = CommercialGender.UNKNOWN;
            AudienceKind audience = AudienceKind.ADULT;
            string g = (!string.IsNullOrWhiteSpace(card.CurrentGender) ? card.CurrentGender : detectedGender).Trim().ToLowerInvariant();
            if (g.Contains("мальчик") || g.Contains("boy"))
            {
                commGender = CommercialGender.MALE;
                audience = AudienceKind.CHILD;
            }
            else if (g.Contains("девочк") || g.Contains("girl"))
            {
                commGender = CommercialGender.FEMALE;
                audience = AudienceKind.CHILD;
            }
            else if (g.Contains("муж") || g.Contains("men") || g.Contains("male"))
            {
                commGender = CommercialGender.MALE;
            }
            else if (g.Contains("жен") || g.Contains("women") || g.Contains("female"))
            {
                commGender = CommercialGender.FEMALE;
            }
            else if (g.Contains("унисекс") || g.Contains("unisex"))
            {
                commGender = CommercialGender.UNISEX;
            }

            if (audience != AudienceKind.CHILD && (lowerText.Contains("детск") || lowerText.Contains("для детей") || lowerText.Contains("малыш") || lowerText.Contains("ребен")))
            {
                audience = AudienceKind.CHILD;
            }

            // 3. Construction mapping
            ConstructionKind construction = ConstructionKind.UNKNOWN;
            if (lowerText.Contains("трикотаж") || lowerText.Contains("вязаный") || lowerText.Contains("вязан") || lowerText.Contains("knit") || detectedKnit == "Трикотаж")
            {
                construction = ConstructionKind.KNIT;
            }
            else if (lowerText.Contains("ткань") || lowerText.Contains("тканый") || lowerText.Contains("нетрикотаж") || lowerText.Contains("деним") || lowerText.Contains("джинс") || detectedKnit == "Ткань")
            {
                construction = ConstructionKind.WOVEN;
            }
            else
            {
                // Default based on commodity subject
                if (lowerSub.Contains("футболк") || lowerSub.Contains("худи") || lowerSub.Contains("свитшот") || 
                    lowerSub.Contains("брюки спортивные") || lowerSub.Contains("джоггер") || lowerSub.Contains("леггинс") || 
                    lowerSub.Contains("свитер") || lowerSub.Contains("джемпер") || lowerSub.Contains("майка"))
                {
                    construction = ConstructionKind.KNIT;
                }
                else if (lowerSub.Contains("джинсы") || lowerSub.Contains("рубашка") || lowerSub.Contains("пальто") || lowerSub.Contains("куртка"))
                {
                    construction = ConstructionKind.WOVEN;
                }
            }

            // 4. ProductForm mapping
            string? productForm = null;
            if (lowerSub.Contains("футболк") || lowerSub.Contains("майк") || lowerSub.Contains("топ") || lowerSub.Contains("лонгслив") || lowerSub.Contains("боди") || lowerTitle.Contains("футболк"))
                productForm = "TSHIRT";
            else if (lowerSub.Contains("худи") || lowerTitle.Contains("худи"))
                productForm = "HOODIE";
            else if (lowerSub.Contains("свитшот") || lowerSub.Contains("толстовк") || lowerTitle.Contains("свитшот") || lowerTitle.Contains("толстовк"))
                productForm = "SWEATSHIRT";
            else if (lowerSub.Contains("джемпер") || lowerSub.Contains("свитер") || lowerSub.Contains("водолазк") || lowerSub.Contains("кардиган") || lowerSub.Contains("пуловер"))
                productForm = "SWEATER";
            else if (lowerSub.Contains("брюки") || lowerSub.Contains("штаны") || lowerSub.Contains("джоггер") || lowerSub.Contains("леггинс") || lowerSub.Contains("тайтс"))
                productForm = "TROUSERS";
            else if (lowerSub.Contains("джинс") || lowerTitle.Contains("джинс"))
                productForm = "JEANS";
            else if (lowerSub.Contains("шорты") || lowerTitle.Contains("шорты"))
                productForm = "SHORTS";
            else if (lowerSub.Contains("платье") || lowerSub.Contains("сарафан") || lowerTitle.Contains("платье"))
                productForm = "DRESS";
            else if (lowerSub.Contains("юбка") || lowerTitle.Contains("юбка"))
                productForm = "SKIRT";
            else if (lowerSub.Contains("рубашк") || lowerTitle.Contains("рубашк"))
                productForm = "SHIRT";
            else if (lowerSub.Contains("блузк") || lowerTitle.Contains("блузк"))
                productForm = "BLOUSE";
            else if (lowerSub.Contains("куртк") || lowerSub.Contains("ветровк") || lowerSub.Contains("пуховик") || lowerSub.Contains("жилет"))
                productForm = "JACKET";
            else if (lowerSub.Contains("пальто"))
                productForm = "COAT";
            else if (lowerSub.Contains("бюстгальтер"))
                productForm = "BRA";
            else if (lowerSub.Contains("трусы"))
                productForm = "BRIEFS";
            else if (lowerSub.Contains("пижам") || lowerSub.Contains("халат"))
                productForm = "PAJAMAS";
            else if (lowerSub.Contains("шапк"))
                productForm = "BEANIE";
            else if (lowerSub.Contains("кепк") || lowerSub.Contains("бейсболк"))
                productForm = "CAP";
            else if (lowerSub.Contains("сумк"))
                productForm = "HANDBAG";
            else if (lowerSub.Contains("рюкзак"))
                productForm = "BACKPACK";
            else if (lowerSub.Contains("кроссовк") || lowerSub.Contains("кеды"))
                productForm = "SNEAKER";
            else if (lowerSub.Contains("носки") || lowerSub.Contains("колготк"))
                productForm = "SOCKS";

            // 5. Composition mapping
            var fibers = ParseComposition(card.CurrentMaterial, lowerText, detectedMaterial);

            // 6. FabricKind
            string? fabricKind = null;
            if (lowerText.Contains("деним") || lowerText.Contains("джинс") || lowerText.Contains("denim"))
            {
                fabricKind = "DENIM";
            }

            var variant = new ProductVariantInput
            {
                TenantId = "DEFAULT_TENANT",
                ProductId = card.ImtId > 0 ? card.ImtId.ToString() : card.NmId.ToString(),
                VariantId = card.NmId.ToString(),
                SourceRecordId = card.NmId.ToString(),
                TitleRaw = card.Title,
                TnvedRaw = card.CurrentTnved,
                CommercialGender = commGender,
                Audience = audience,
                Construction = construction,
                ProductForm = productForm,
                FabricKind = fabricKind,
                DeterminingComponent = "SHELL",
                RetailClassification = "SEPARATE_GARMENT",
                IntendedUse = "ORDINARY_APPAREL",
                CompositionComponents = fibers,
                Evidence = new List<Evidence>
                {
                    new() { Id = $"EV_WB_{card.NmId}", Kind = "WB_CARD_DECLARED", Locator = $"wb://cards/{card.NmId}" }
                }
            };

            return variant;
        }

        private static List<CompositionComponent> ParseComposition(string? currentMaterial, string lowerText, string detectedMaterial)
        {
            var list = new List<CompositionComponent>();
            var matStr = currentMaterial ?? string.Empty;

            // Check percentage patterns: e.g. "Хлопок 80%, полиэстер 20%" or "80% хлопок, 20% полиэстер"
            var pctMatches = Regex.Matches(matStr, @"(\d+)\s*%\s*([a-zA-Zа-яА-ЯёЁ]+)|([a-zA-Zа-яА-ЯёЁ]+)\s*(\d+)\s*%");
            if (pctMatches.Count > 0)
            {
                foreach (Match m in pctMatches)
                {
                    string pctStr = !string.IsNullOrEmpty(m.Groups[1].Value) ? m.Groups[1].Value : m.Groups[4].Value;
                    string nameStr = !string.IsNullOrEmpty(m.Groups[2].Value) ? m.Groups[2].Value : m.Groups[3].Value;

                    if (decimal.TryParse(pctStr, out decimal pct) && !string.IsNullOrWhiteSpace(nameStr))
                    {
                        var normName = NormalizeFiberName(nameStr);
                        list.Add(new CompositionComponent { FiberName = normName, Percentage = pct });
                    }
                }
            }

            if (list.Count == 0)
            {
                // Single material fallback
                string source = !string.IsNullOrWhiteSpace(matStr) ? matStr.ToLowerInvariant() : detectedMaterial.ToLowerInvariant();
                if (source.Contains("хлопок") || source.Contains("cotton"))
                    list.Add(new CompositionComponent { FiberName = "Cotton", Percentage = 100m });
                else if (source.Contains("синтетика") || source.Contains("полиэстер") || source.Contains("эластан") || source.Contains("полиамид"))
                    list.Add(new CompositionComponent { FiberName = "Polyester", Percentage = 100m });
                else if (source.Contains("шерсть") || source.Contains("wool") || source.Contains("кашемир"))
                    list.Add(new CompositionComponent { FiberName = "Wool", Percentage = 100m });
                else if (source.Contains("шелк") || source.Contains("silk"))
                    list.Add(new CompositionComponent { FiberName = "Silk", Percentage = 100m });
                else if (source.Contains("вискоза") || source.Contains("viscose"))
                    list.Add(new CompositionComponent { FiberName = "Viscose", Percentage = 100m });
                else if (source.Contains("лен") || source.Contains("linen") || source.Contains("flax"))
                    list.Add(new CompositionComponent { FiberName = "Flax", Percentage = 100m });
                else if (source.Contains("кожа") || source.Contains("leather"))
                    list.Add(new CompositionComponent { FiberName = "Leather", Percentage = 100m });
                else
                    list.Add(new CompositionComponent { FiberName = "Cotton", Percentage = 100m });
            }

            return list;
        }

        private static string NormalizeFiberName(string rawName)
        {
            var lower = rawName.Trim().ToLowerInvariant();
            if (lower.Contains("хлопок") || lower.Contains("cotton")) return "Cotton";
            if (lower.Contains("полиэстер") || lower.Contains("polyester")) return "Polyester";
            if (lower.Contains("синтетик") || lower.Contains("эластан") || lower.Contains("полиамид") || lower.Contains("нейлон")) return "Synthetic";
            if (lower.Contains("шерсть") || lower.Contains("wool") || lower.Contains("кашемир")) return "Wool";
            if (lower.Contains("шелк") || lower.Contains("silk")) return "Silk";
            if (lower.Contains("вискоз") || lower.Contains("viscose")) return "Viscose";
            if (lower.Contains("лен") || lower.Contains("linen") || lower.Contains("flax")) return "Flax";
            if (lower.Contains("кожа") || lower.Contains("leather")) return "Leather";
            return "Cotton";
        }

        private static string MapTariffGenderToString(TariffGender tariffGender, string currentGender, string fallbackDetectedGender)
        {
            if (tariffGender == TariffGender.M) return "Мужской";
            if (tariffGender == TariffGender.F || tariffGender == TariffGender.F_FALLBACK)
            {
                // If commercial gender was Unisex, keep Unisex commercially so we don't inappropriately rename Unisex to Female!
                if (currentGender.Equals("Унисекс", StringComparison.OrdinalIgnoreCase) ||
                    fallbackDetectedGender.Equals("Унисекс", StringComparison.OrdinalIgnoreCase))
                {
                    return "Унисекс";
                }
                return "Женский";
            }
            return !string.IsNullOrEmpty(currentGender) ? currentGender : fallbackDetectedGender;
        }
    }
}
