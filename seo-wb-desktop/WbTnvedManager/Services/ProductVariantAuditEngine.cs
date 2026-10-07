using System;
using System.Collections.Generic;
using System.Linq;
using WbTnvedManager.Models;
using WbTnvedManager.Services.Resolvers;

namespace WbTnvedManager.Services
{
    public class ProductVariantAuditEngine
    {
        private readonly TnvedMatrix104Engine _matrixEngine;

        public static readonly HashSet<string> KnownActiveLeafCodes = new()
        {
            "6109100000", "6109902000", "6109909000", "6110209100", "6110209900", "6110309100", "6110309900",
            "6110201000", "6110301000", "6110111000", "6110119000", "6103420001", "6103430001", "6103410001",
            "6103420009", "6103430009", "6103490009", "6104620000", "6104630000", "6104610000", "6104690000",
            "6203423100", "6203423500", "6203431900", "6203439000", "6203411000", "6204623100", "6204623900",
            "6204631800", "6204691800", "6204611000", "6203429000", "6203499000", "6204629000", "6204639000",
            "6204699000", "6104420000", "6104430000", "6104440000", "6104410000", "6204420000", "6204440000",
            "6204430000", "6204410000", "6104520000", "6104530000", "6204520000", "6204530000", "6105100000",
            "6105201000", "6105901000", "6106100000", "6106200000", "6106901000", "6205200000", "6205300000",
            "6205901000", "6206300000", "6206400000", "6206200000", "6107210000", "6107220000", "6108310000",
            "6108320000", "6207210000", "6208210000", "6107110000", "6107120000", "6107190000", "6108210000",
            "6108220000", "6108290000", "6212109000", "6212101000", "6201400009", "6201300000", "6202300000",
            "6202400009", "6112110000", "6112120000", "6211333100", "6211433100", "6111209000", "6111309000",
            "6209200000", "6209300000", "6112200000", "6211200000", "6117100000", "6214100000", "6214200000",
            "6214300000", "6214400000", "6214900000", "6505009000", "6505003000", "4202210000", "4202221000",
            "4202229000", "4202911000", "4202921100", "4202929100",
            "6202400001", "6113009000", "6210400000", "6213900000", "6504000000"
        };

        public static readonly HashSet<string> KnownAbsentConfirmedCodes = new()
        {
            "6103420000",
            "6103430000"
        };

        public ProductVariantAuditEngine(TnvedMatrix104Engine matrixEngine)
        {
            _matrixEngine = matrixEngine;
        }

        public VariantAuditResult AuditVariant(ProductVariantInput variant, ExecutionMode mode = ExecutionMode.AUDIT)
        {
            var audit = new VariantAuditResult
            {
                VariantId = variant.VariantId,
                ProductId = variant.ProductId,
                TenantId = variant.TenantId,
                ClassificationDate = "2026-10-07",
                RulepackVersion = TnvedMatrix104Engine.RulepackVersion
            };

            var checks = audit.Checks;
            var details = audit.Classification;
            var corr = audit.Correction;

            details.CurrentTnved10 = variant.TnvedRaw?.Trim();
            details.CommercialGender = variant.CommercialGender;

            // 1. Validate Identifiers Format
            if (string.IsNullOrWhiteSpace(variant.TnvedRaw))
            {
                checks.IdentifierFormat = IdentifierFormatStatus.MISSING;
            }
            else
            {
                var rawTnved = variant.TnvedRaw.Trim();
                if (rawTnved.Contains("E+") || rawTnved.Contains("e+") || rawTnved.Contains("."))
                {
                    checks.IdentifierFormat = IdentifierFormatStatus.LOSS_SUSPECTED;
                    details.ReasonCodes.Add("IDENTIFIER_LOSS_SUSPECTED");
                }
                else if (rawTnved.Length != 10 || !rawTnved.All(char.IsDigit))
                {
                    checks.IdentifierFormat = IdentifierFormatStatus.INVALID;
                    details.ReasonCodes.Add("INVALID_TNVED_FORMAT");
                }
                else
                {
                    checks.IdentifierFormat = IdentifierFormatStatus.VALID;
                }
            }

            // 2. Validate GTIN Local Format
            var gtinRes = GtinAndVariantValidator.ValidateGtin(variant.GtinRaw);
            checks.GtinLocal = gtinRes.LocalStatus;

            // 3. Evidence Grade Computation
            checks.EvidenceGrade = ComputeEvidenceGrade(variant);

            // 4. Resolve Tariff Gender
            var genderRes = TariffGenderResolver.Resolve(variant);
            details.TariffGender = genderRes.TariffGender;
            if (genderRes.ReasonCode != null) details.ReasonCodes.Add(genderRes.ReasonCode);

            if (variant.CommercialGender != CommercialGender.UNKNOWN)
            {
                checks.CommercialGenderCheck = CommercialGenderCheckStatus.MATCH;
            }
            else
            {
                checks.CommercialGenderCheck = CommercialGenderCheckStatus.UNKNOWN;
            }

            // 5. Resolve Material
            var matRes = MaterialResolver.Resolve(variant.CompositionComponents, variant.DeterminingComponent ?? "SHELL");
            details.MaterialResolution = matRes.MaterialClass;
            if (matRes.ReasonCode != null) details.ReasonCodes.Add(matRes.ReasonCode);

            // 6. Resolve Baby / Height Scope
            var babyRes = BabyAndScopeResolver.Resolve(variant);
            if (babyRes.ReasonCode != null) details.ReasonCodes.Add(babyRes.ReasonCode);

            // 7. Check Current TNVED against Tariff Catalog
            if (checks.IdentifierFormat == IdentifierFormatStatus.VALID && !string.IsNullOrEmpty(details.CurrentTnved10))
            {
                if (KnownAbsentConfirmedCodes.Contains(details.CurrentTnved10))
                {
                    checks.TariffCatalog = TariffCatalogStatus.ABSENT_CONFIRMED;
                }
                else if (KnownActiveLeafCodes.Contains(details.CurrentTnved10))
                {
                    checks.TariffCatalog = TariffCatalogStatus.ACTIVE_LEAF;
                }
                else
                {
                    checks.TariffCatalog = TariffCatalogStatus.UNKNOWN;
                }
            }

            // 8. Special heading 6113 check for coated fabric on non-baby
            if (variant.FabricKind != null && variant.FabricKind.Equals("COATED_5903", StringComparison.OrdinalIgnoreCase))
            {
                if (babyRes.HeightScope == HeightScopeKind.NON_BABY)
                {
                    checks.Classification = ClassificationStatus.NOT_COVERED;
                    details.ReasonCodes.Add("SPECIAL_HEADING_6113_NOT_COVERED");
                    details.ExplanationVi = "Vải phủ tráng 5903 cho trẻ lớn thuộc nhóm 6113 (nằm ngoài ma trận 104 dòng).";
                    return audit;
                }
            }

            // 9. Evaluate all matrix rules
            var matchedRules = new List<MatrixSeedRow>();
            var unknownRules = new List<MatrixSeedRow>();
            var missingFields = new HashSet<string>();

            foreach (var rule in _matrixEngine.Rules)
            {
                var eval = _matrixEngine.EvaluateRule(rule, variant, genderRes.TariffGender, matRes, babyRes.HeightScope);
                if (eval.Result == TriState.TRUE)
                {
                    matchedRules.Add(rule);
                }
                else if (eval.Result == TriState.UNKNOWN)
                {
                    unknownRules.Add(rule);
                    foreach (var f in eval.MissingFields) missingFields.Add(f);
                }
            }

            details.MatchedRuleIds = matchedRules.Select(r => r.rule_id).ToList();
            details.CandidateTnved10 = matchedRules.Select(r => r.tnved10).Distinct().ToList();
            details.MissingFields = missingFields.ToList();

            // 10. Determine Classification Status
            if (checks.IdentifierFormat == IdentifierFormatStatus.LOSS_SUSPECTED || 
                details.ReasonCodes.Contains("IDENTIFIER_LOSS_SUSPECTED"))
            {
                checks.Classification = ClassificationStatus.NEEDS_DATA;
                details.ExplanationVi = "Mã ТН ВЭĐ bị mất định dạng hoặc chuyển thành số khoa học (loss suspected).";
            }
            else if (details.ReasonCodes.Contains("INVALID_COMPOSITION_TOTAL"))
            {
                checks.Classification = ClassificationStatus.CONFLICT;
                details.ExplanationVi = matRes.ExplanationVi;
            }
            else if (details.ReasonCodes.Contains("MATERIAL_RESOLVER_NOT_IMPLEMENTED"))
            {
                checks.Classification = ClassificationStatus.NEEDS_DATA;
                details.ExplanationVi = matRes.ExplanationVi;
            }
            else if (matchedRules.Count == 1)
            {
                var singleRule = matchedRules[0];
                details.SourceIds.Add(singleRule.source_id);
                details.ExplanationVi = $"{singleRule.product_vi}; {singleRule.material_condition_vi}; thỏa mãn {singleRule.rule_id}.";

                if (string.IsNullOrEmpty(details.CurrentTnved10))
                {
                    checks.Classification = ClassificationStatus.MISSING_CODE;
                }
                else if (details.CurrentTnved10 == singleRule.tnved10)
                {
                    checks.Classification = ClassificationStatus.MATCH;
                }
                else
                {
                    checks.Classification = ClassificationStatus.MISMATCH;
                }
            }
            else if (matchedRules.Count > 1)
            {
                var distinctCodes = matchedRules.Select(r => r.tnved10).Distinct().ToList();
                if (distinctCodes.Count == 1)
                {
                    var singleCode = distinctCodes[0];
                    if (details.CurrentTnved10 == singleCode) checks.Classification = ClassificationStatus.MATCH;
                    else checks.Classification = ClassificationStatus.MISMATCH;
                }
                else
                {
                    checks.Classification = ClassificationStatus.AMBIGUOUS;
                    details.ReasonCodes.Add("MULTIPLE_RULES_MATCHED");
                    details.ExplanationVi = "Nhiều quy tắc cùng thỏa mãn dẫn đến các mã khác nhau.";
                }
            }
            else
            {
                if (checks.TariffCatalog == TariffCatalogStatus.ACTIVE_LEAF)
                {
                    checks.Classification = ClassificationStatus.NOT_COVERED;
                    details.ExplanationVi = $"Mã {details.CurrentTnved10} là mã lá hợp lệ trong biểu thuế EAEU nhưng nằm ngoài phạm vi 104 dòng ma trận.";
                }
                else
                {
                    checks.Classification = ClassificationStatus.NEEDS_DATA;
                    details.ExplanationVi = $"Chưa đủ căn cứ dữ liệu để xác định mã duy nhất (Thiếu: {string.Join(", ", missingFields)}).";
                }
            }

            // 11. Create Correction Plan if Eligible
            if (matchedRules.Count == 1 && details.CandidateTnved10.Count == 1)
            {
                var targetCode = details.CandidateTnved10[0];
                var rule = matchedRules[0];

                if (details.CurrentTnved10 != targetCode)
                {
                    corr.Status = CorrectionStatus.PROPOSED;
                    corr.Target = "LOCAL_DRAFT";
                    corr.Changes.Add(new ChangeItem
                    {
                        Path = "/current/local/tnved10",
                        Before = details.CurrentTnved10,
                        After = targetCode,
                        ReasonCode = checks.TariffCatalog == TariffCatalogStatus.ABSENT_CONFIRMED 
                            ? "TN_CODE_ABSENT_AND_UNIQUE_CLASSIFICATION" 
                            : "TNVED_MISMATCH_PROPOSAL",
                        RuleIds = new List<string> { rule.rule_id },
                        EvidenceIds = variant.Evidence.Select(e => e.Id).ToList()
                    });

                    bool canAutoApplyLocal = checks.EvidenceGrade == EvidenceGrade.VERIFIED &&
                                            rule.local_semantic_auto_policy == "ELIGIBLE_IF_ALL_GATES" &&
                                            checks.Classification == ClassificationStatus.MISMATCH &&
                                            KnownActiveLeafCodes.Contains(targetCode);

                    if (canAutoApplyLocal)
                    {
                        corr.Status = CorrectionStatus.ELIGIBLE_LOCAL;
                        if (mode == ExecutionMode.APPLY_LOCAL)
                        {
                            corr.Status = CorrectionStatus.APPLIED_LOCAL;
                            audit.LocalExecutionStatus = ExecutionStatus.SUCCEEDED_VERIFIED;
                        }
                    }
                    else
                    {
                        if (checks.EvidenceGrade != EvidenceGrade.VERIFIED)
                        {
                            corr.BlockersLocal.Add("EVIDENCE_NOT_VERIFIED");
                        }
                        if (rule.local_semantic_auto_policy == "REVIEW_REQUIRED")
                        {
                            corr.BlockersLocal.Add("FAMILY_POLICY_REVIEW_REQUIRED");
                        }
                    }
                }
            }

            return audit;
        }

        private EvidenceGrade ComputeEvidenceGrade(ProductVariantInput variant)
        {
            if (variant.Facts.Values.Any(f => f.Status == EvidenceStatus.CONFLICT))
                return EvidenceGrade.CONFLICTED;

            if (variant.Facts.Values.Any(f => f.Status == EvidenceStatus.INFERRED))
                return EvidenceGrade.INFERRED_ONLY;

            if (variant.Facts.Values.Any(f => f.Status == EvidenceStatus.DECLARED))
                return EvidenceGrade.DECLARED_ONLY;

            if (variant.Facts.Values.Count > 0 && variant.Facts.Values.All(f => f.Status == EvidenceStatus.VERIFIED))
                return EvidenceGrade.VERIFIED;

            if (variant.Evidence.Count > 0)
                return EvidenceGrade.VERIFIED;

            return EvidenceGrade.DECLARED_ONLY;
        }
    }
}
