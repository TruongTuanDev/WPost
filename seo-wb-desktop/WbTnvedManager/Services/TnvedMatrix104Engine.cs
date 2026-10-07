using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WbTnvedManager.Models;
using WbTnvedManager.Services.Resolvers;

namespace WbTnvedManager.Services
{
    public class MatrixSeedRow
    {
        public string rule_id { get; set; } = string.Empty;
        public string family { get; set; } = string.Empty;
        public string tnved10 { get; set; } = string.Empty;
        public string competition_group { get; set; } = string.Empty;
        public string tariff_gender { get; set; } = string.Empty;
        public string height_scope { get; set; } = string.Empty;
        public string construction_vi_ru { get; set; } = string.Empty;
        public string product_vi { get; set; } = string.Empty;
        public string material_condition_vi { get; set; } = string.Empty;
        public string conditions_vi { get; set; } = string.Empty;
        public string gate_profile { get; set; } = string.Empty;
        public string local_semantic_auto_policy { get; set; } = string.Empty;
        public string source_id { get; set; } = string.Empty;
        public int pdf_page_1_based { get; set; }
        public string? supporting_source_id { get; set; }
        public string? supporting_pdf_page_1_based { get; set; }
    }

    public class MatrixRoot
    {
        public string marker { get; set; } = string.Empty;
        public string schema_version { get; set; } = string.Empty;
        public string rulepack_version { get; set; } = string.Empty;
        public string reviewed_at { get; set; } = string.Empty;
        public string purpose { get; set; } = string.Empty;
        public bool production_executable { get; set; }
        public int row_count { get; set; }
        public string rows_canonical_sha256 { get; set; } = string.Empty;
        public List<MatrixSeedRow> rows { get; set; } = new();
    }

    public class RuleEvaluationOutput
    {
        public MatrixSeedRow Rule { get; set; } = new();
        public TriState Result { get; set; } = TriState.UNKNOWN;
        public List<string> FailureReasons { get; set; } = new();
        public List<string> MissingFields { get; set; } = new();
    }

    public class TnvedMatrix104Engine
    {
        public const string ExpectedSha256 = "7c35fb4ccda8e67aa984e9e0c0ec8736054fd26c69466150e18021e790193a76";
        public const string RulepackVersion = "TNVED-2026-10-07-v1";

        private readonly List<MatrixSeedRow> _rules = new();
        public IReadOnlyList<MatrixSeedRow> Rules => _rules;

        public bool IsInitialized { get; private set; }

        public TnvedMatrix104Engine()
        {
            Initialize();
        }

        public void Initialize()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var jsonPath = Path.Combine(baseDir, "Data", "matrix_104_v1.json");
            if (!File.Exists(jsonPath))
            {
                jsonPath = Path.Combine(baseDir, "..", "..", "..", "Data", "matrix_104_v1.json");
            }

            if (File.Exists(jsonPath))
            {
                var jsonText = File.ReadAllText(jsonPath);
                var root = JsonSerializer.Deserialize<MatrixRoot>(jsonText);
                if (root != null && root.rows != null)
                {
                    _rules.Clear();
                    _rules.AddRange(root.rows);
                    IsInitialized = true;
                }
            }
        }

        public RuleEvaluationOutput EvaluateRule(
            MatrixSeedRow rule,
            ProductVariantInput variant,
            TariffGender resolvedTariffGender,
            MaterialResolutionResult resolvedMaterial,
            HeightScopeKind resolvedHeightScope)
        {
            var output = new RuleEvaluationOutput { Rule = rule };

            // 1. Competition group check
            if (rule.competition_group == "HEADGEAR")
            {
                if (variant.ProductForm != null && variant.ProductForm != "BEANIE" && variant.ProductForm != "CAP" &&
                    variant.ProductForm != "UNKNOWN")
                {
                    output.Result = TriState.FALSE;
                    output.FailureReasons.Add("Không thuộc nhóm HEADGEAR");
                    return output;
                }
                if (variant.ProductForm == null && !string.IsNullOrEmpty(variant.TitleRaw) &&
                    !variant.TitleRaw.Contains("шапка", StringComparison.OrdinalIgnoreCase) &&
                    !variant.TitleRaw.Contains("кепка", StringComparison.OrdinalIgnoreCase) &&
                    !variant.TitleRaw.Contains("beanie", StringComparison.OrdinalIgnoreCase) &&
                    !variant.TitleRaw.Contains("mũ", StringComparison.OrdinalIgnoreCase) &&
                    !variant.TitleRaw.Contains("nón", StringComparison.OrdinalIgnoreCase))
                {
                    output.Result = TriState.FALSE;
                    output.FailureReasons.Add("Không phải mũ/nón");
                    return output;
                }
            }
            else if (rule.competition_group == "BAG_CONTAINER")
            {
                if (variant.ProductForm != null && variant.ProductForm != "HANDBAG" && variant.ProductForm != "BACKPACK" &&
                    variant.ProductForm != "TRAVEL_BAG" && variant.ProductForm != "SPORT_BAG" && variant.ProductForm != "UNKNOWN")
                {
                    output.Result = TriState.FALSE;
                    output.FailureReasons.Add("Không thuộc nhóm BAG_CONTAINER");
                    return output;
                }
                if (variant.ProductForm == null && !string.IsNullOrEmpty(variant.TitleRaw) &&
                    !variant.TitleRaw.Contains("сумка", StringComparison.OrdinalIgnoreCase) &&
                    !variant.TitleRaw.Contains("рюкзак", StringComparison.OrdinalIgnoreCase) &&
                    !variant.TitleRaw.Contains("bag", StringComparison.OrdinalIgnoreCase) &&
                    !variant.TitleRaw.Contains("túi", StringComparison.OrdinalIgnoreCase) &&
                    !variant.TitleRaw.Contains("ba lô", StringComparison.OrdinalIgnoreCase))
                {
                    output.Result = TriState.FALSE;
                    output.FailureReasons.Add("Không phải túi/balo");
                    return output;
                }
            }
            else if (rule.competition_group == "APPAREL_TEXTILE")
            {
                if (variant.ProductForm != null && (variant.ProductForm == "HANDBAG" || variant.ProductForm == "BACKPACK" ||
                    variant.ProductForm == "TRAVEL_BAG" || variant.ProductForm == "SPORT_BAG" ||
                    variant.ProductForm == "BEANIE" || variant.ProductForm == "CAP"))
                {
                    output.Result = TriState.FALSE;
                    output.FailureReasons.Add("Không thuộc nhóm APPAREL_TEXTILE");
                    return output;
                }
            }

            // 2. Height scope check
            if (rule.height_scope == "BABY_LE_86")
            {
                if (resolvedHeightScope == HeightScopeKind.NON_BABY)
                {
                    output.Result = TriState.FALSE;
                    output.FailureReasons.Add("Hàng dành cho người lớn hoặc trẻ > 86 cm (không thỏa BABY_LE_86)");
                    return output;
                }
                if (resolvedHeightScope == HeightScopeKind.UNKNOWN)
                {
                    output.Result = TriState.UNKNOWN;
                    output.MissingFields.Add("height_scope");
                    return output;
                }
            }
            else if (rule.height_scope == "NON_BABY")
            {
                if (resolvedHeightScope == HeightScopeKind.BABY_LE_86)
                {
                    output.Result = TriState.FALSE;
                    output.FailureReasons.Add("Hàng cho em bé <= 86 cm (phải đi nhánh em bé 6111/6209)");
                    return output;
                }
                if (resolvedHeightScope == HeightScopeKind.UNKNOWN && 
                    (variant.Audience == AudienceKind.CHILD || variant.Audience == AudienceKind.UNKNOWN))
                {
                    output.Result = TriState.UNKNOWN;
                    output.MissingFields.Add("height_scope");
                    return output;
                }
            }

            // 3. Construction check
            if (rule.family != "BRA_SINGLE" && rule.family != "BRA_BRIEF_SET" && 
                rule.competition_group != "BAG_CONTAINER" && rule.rule_id != "T098")
            {
                bool isRuleWoven = rule.construction_vi_ru.StartsWith("Dệt thoi", StringComparison.OrdinalIgnoreCase) ||
                                   rule.construction_vi_ru.Contains("нетрикотаж", StringComparison.OrdinalIgnoreCase) ||
                                   rule.construction_vi_ru.Contains("ткань", StringComparison.OrdinalIgnoreCase);

                bool isRuleKnit = !isRuleWoven && (
                                  rule.construction_vi_ru.StartsWith("Dệt kim", StringComparison.OrdinalIgnoreCase) ||
                                  rule.construction_vi_ru.Contains("трикотаж", StringComparison.OrdinalIgnoreCase));

                if (isRuleKnit)
                {
                    if (variant.Construction == ConstructionKind.WOVEN || variant.Construction == ConstructionKind.NONWOVEN)
                    {
                        output.Result = TriState.FALSE;
                        output.FailureReasons.Add("Không phải Dệt kim (Knit)");
                        return output;
                    }
                    if (variant.Construction == ConstructionKind.UNKNOWN)
                    {
                        output.Result = TriState.UNKNOWN;
                        output.MissingFields.Add("construction");
                        return output;
                    }
                }
                else if (isRuleWoven)
                {
                    if (variant.Construction == ConstructionKind.KNIT)
                    {
                        output.Result = TriState.FALSE;
                        output.FailureReasons.Add("Không phải Dệt thoi (Woven)");
                        return output;
                    }
                    if (variant.Construction == ConstructionKind.UNKNOWN)
                    {
                        output.Result = TriState.UNKNOWN;
                        output.MissingFields.Add("construction");
                        return output;
                    }
                }
            }

            // 4. Tariff Gender check
            if (rule.tariff_gender == "M")
            {
                if (resolvedTariffGender == TariffGender.F || resolvedTariffGender == TariffGender.F_FALLBACK)
                {
                    output.Result = TriState.FALSE;
                    output.FailureReasons.Add("Không thuộc nhánh Nam (M)");
                    return output;
                }
                if (resolvedTariffGender == TariffGender.UNKNOWN)
                {
                    output.Result = TriState.UNKNOWN;
                    output.MissingFields.Add("tariff_gender");
                    return output;
                }
            }
            else if (rule.tariff_gender == "F")
            {
                if (resolvedTariffGender == TariffGender.M)
                {
                    output.Result = TriState.FALSE;
                    output.FailureReasons.Add("Không thuộc nhánh Nữ (F)");
                    return output;
                }
                if (resolvedTariffGender == TariffGender.UNKNOWN)
                {
                    output.Result = TriState.UNKNOWN;
                    output.MissingFields.Add("tariff_gender");
                    return output;
                }
            }

            // 5. Gate Profile checks
            var gateRes = EvaluateGateProfile(rule.gate_profile, rule, variant, resolvedMaterial);
            if (gateRes != TriState.TRUE)
            {
                output.Result = gateRes;
                if (gateRes == TriState.FALSE) output.FailureReasons.Add($"Không thỏa Gate Profile: {rule.gate_profile}");
                else output.MissingFields.Add($"gate:{rule.gate_profile}");
                return output;
            }

            // 6. Material Condition check
            var matRes = EvaluateMaterialCondition(rule, variant, resolvedMaterial);
            if (matRes != TriState.TRUE)
            {
                output.Result = matRes;
                if (matRes == TriState.FALSE) output.FailureReasons.Add($"Không khớp điều kiện chất liệu: {rule.material_condition_vi}");
                else output.MissingFields.Add("material_condition");
                return output;
            }

            output.Result = TriState.TRUE;
            return output;
        }

        private TriState EvaluateGateProfile(string gate, MatrixSeedRow rule, ProductVariantInput variant, MaterialResolutionResult material)
        {
            var form = variant.ProductForm?.ToUpperInvariant();

            switch (gate)
            {
                case "TSHIRT":
                    if (form != null && form != "TSHIRT") return TriState.FALSE;
                    if (form == null && string.IsNullOrEmpty(variant.TitleRaw)) return TriState.UNKNOWN;
                    if (form == null && !string.IsNullOrEmpty(variant.TitleRaw) && 
                        !variant.TitleRaw.Contains("t-shirt", StringComparison.OrdinalIgnoreCase) && 
                        !variant.TitleRaw.Contains("футболка", StringComparison.OrdinalIgnoreCase) &&
                        !variant.TitleRaw.Contains("áo phông", StringComparison.OrdinalIgnoreCase) &&
                        !variant.TitleRaw.Contains("áo thun", StringComparison.OrdinalIgnoreCase))
                        return TriState.FALSE;
                    return TriState.TRUE;

                case "KNIT_TOP_REGULAR":
                    if (form != null && form != "HOODIE" && form != "SWEATSHIRT" && form != "SWEATER" && form != "CARDIGAN")
                    {
                        if (form == "TSHIRT" || form == "JACKET" || form == "TROUSERS" || form == "DRESS" || form == "SHORTS" ||
                            form == "BRA" || form == "BRA_BRIEF_SET" || form == "SCARF" || form == "BEANIE" || form == "CAP")
                            return TriState.FALSE;
                    }
                    if (form == null && string.IsNullOrEmpty(variant.TitleRaw)) return TriState.UNKNOWN;
                    if (variant.IsFineKnitDensity) return TriState.FALSE;
                    if (material.MaterialClass == "WOOL" && variant.WeightGrams >= 600m) return TriState.FALSE;
                    return TriState.TRUE;

                case "KNIT_TOP_FINE_NECK":
                    if (!variant.IsFineKnitDensity) return TriState.FALSE;
                    return TriState.TRUE;

                case "WOOL_TOP_REGULAR":
                    if (material.MaterialClass != "WOOL" && material.MaterialClass != "CASHMERE") return TriState.FALSE;
                    if (variant.WeightGrams >= 600m) return TriState.FALSE;
                    return TriState.TRUE;

                case "WOOL_PULLOVER_HEAVY":
                    if (material.MaterialClass != "WOOL") return TriState.FALSE;
                    if (!variant.WeightGrams.HasValue || variant.WeightGrams.Value < 600m) return TriState.FALSE;
                    return TriState.TRUE;

                case "KNIT_BOTTOM":
                    if (form != null && form != "TROUSERS" && form != "SHORTS" && form != "BREECHES" && form != "LEGGINGS" && form != "JOGGERS")
                    {
                        return TriState.FALSE;
                    }
                    if (form == null && !string.IsNullOrEmpty(variant.TitleRaw) &&
                        !variant.TitleRaw.Contains("jogger", StringComparison.OrdinalIgnoreCase) &&
                        !variant.TitleRaw.Contains("брюки", StringComparison.OrdinalIgnoreCase) &&
                        !variant.TitleRaw.Contains("штаны", StringComparison.OrdinalIgnoreCase) &&
                        !variant.TitleRaw.Contains("quần", StringComparison.OrdinalIgnoreCase))
                        return TriState.FALSE;

                    if (rule.rule_id == "T013" || rule.rule_id == "T014" || rule.rule_id == "T015")
                    {
                        if (form == "SHORTS") return TriState.FALSE;
                    }
                    if (rule.rule_id == "T016" || rule.rule_id == "T017" || rule.rule_id == "T018")
                    {
                        if (form == "TROUSERS" || form == "LEGGINGS" || form == "JOGGERS") return TriState.FALSE;
                    }
                    return TriState.TRUE;

                case "WOVEN_TROUSER":
                    if (form != null && form != "TROUSERS" && form != "JEANS" && form != "BREECHES") return TriState.FALSE;
                    if (rule.rule_id == "T023" || rule.rule_id == "T028")
                    {
                        if (variant.FabricKind != null && !variant.FabricKind.Equals("DENIM", StringComparison.OrdinalIgnoreCase))
                            return TriState.FALSE;
                    }
                    else if (rule.rule_id == "T024" || rule.rule_id == "T029")
                    {
                        if (variant.FabricKind != null && variant.FabricKind.Equals("DENIM", StringComparison.OrdinalIgnoreCase))
                            return TriState.FALSE;
                    }
                    return TriState.TRUE;

                case "WOVEN_SHORT":
                    if (form != null && form != "SHORTS") return TriState.FALSE;
                    return TriState.TRUE;

                case "DRESS":
                    if (form != null && form != "DRESS") return TriState.FALSE;
                    return TriState.TRUE;

                case "SKIRT":
                    if (form != null && form != "SKIRT") return TriState.FALSE;
                    return TriState.TRUE;

                case "KNIT_SHIRT":
                    if (form != null && form != "SHIRT" && form != "POLO" && form != "BLOUSE") return TriState.FALSE;
                    if (!variant.IsShirtKnitDensity) return TriState.FALSE;
                    return TriState.TRUE;

                case "WOVEN_SHIRT":
                    if (form != null && form != "SHIRT" && form != "BLOUSE") return TriState.FALSE;
                    return TriState.TRUE;

                case "SLEEPWEAR":
                    if (form != null && form != "PYJAMA" && form != "NIGHTWEAR" && form != "NIGHTGOWN") return TriState.FALSE;
                    return TriState.TRUE;

                case "KNIT_UNDERPANTS":
                    if (form != null && form != "UNDERPANTS" && form != "BRIEFS" && form != "BOXERS") return TriState.FALSE;
                    return TriState.TRUE;

                case "BRA_SINGLE":
                    if (form != null && form != "BRA") return TriState.FALSE;
                    if (form == null && !string.IsNullOrEmpty(variant.TitleRaw) &&
                        !variant.TitleRaw.Contains("бюстгальтер", StringComparison.OrdinalIgnoreCase) &&
                        !variant.TitleRaw.Contains("bra", StringComparison.OrdinalIgnoreCase) &&
                        !variant.TitleRaw.Contains("áo ngực", StringComparison.OrdinalIgnoreCase))
                        return TriState.FALSE;
                    if (variant.IsBraBriefSet) return TriState.FALSE;
                    return TriState.TRUE;

                case "BRA_BRIEF_SET":
                    if (!variant.IsBraBriefSet) return TriState.FALSE;
                    return TriState.TRUE;

                case "WOVEN_OUTER_JACKET":
                    if (form != null && form != "JACKET" && form != "COAT" && form != "ANORAK" && form != "WINDCHEATER") return TriState.FALSE;
                    return TriState.TRUE;

                case "KNIT_TRACKSUIT":
                    if (form != null && form != "TRACKSUIT") return TriState.FALSE;
                    if (variant.RetailClassification != null && variant.RetailClassification.Equals("UNRESOLVED_SET", StringComparison.OrdinalIgnoreCase))
                        return TriState.FALSE;
                    return TriState.TRUE;

                case "WOVEN_TRACKSUIT_LINED":
                    if (form != null && form != "TRACKSUIT") return TriState.FALSE;
                    if (!variant.IsTracksuitLinedSameMaterial) return TriState.FALSE;
                    return TriState.TRUE;

                case "BABY_APPAREL":
                    if (form != null && (form == "BRA" || form == "BRA_BRIEF_SET" || form == "HANDBAG" ||
                        form == "BACKPACK" || form == "TRAVEL_BAG" || form == "SPORT_BAG" || form == "SCARF" ||
                        form == "BEANIE" || form == "CAP"))
                    {
                        return TriState.FALSE;
                    }
                    return TriState.TRUE;

                case "SKI_SUIT":
                    if (!variant.IsSkiSuit) return TriState.FALSE;
                    return TriState.TRUE;

                case "SCARF":
                    if (form != null && form != "SCARF" && form != "SHAWL") return TriState.FALSE;
                    if (variant.IsScarfSquareLe60cm) return TriState.FALSE;
                    return TriState.TRUE;

                case "BEANIE":
                    if (form != null && form != "BEANIE") return TriState.FALSE;
                    if (variant.HasVisor) return TriState.FALSE;
                    return TriState.TRUE;

                case "TEXTILE_VISOR_CAP":
                    if (form != null && form != "CAP") return TriState.FALSE;
                    if (!variant.HasVisor) return TriState.FALSE;
                    if (variant.IsBraidedOrStripFormed) return TriState.FALSE;
                    return TriState.TRUE;

                case "HANDBAG":
                    if (form != null && form != "HANDBAG") return TriState.FALSE;
                    if (rule.rule_id == "T099" && variant.BagVisibleSurface != null && variant.BagVisibleSurface != "LEATHER") return TriState.FALSE;
                    if (rule.rule_id == "T100" && variant.BagVisibleSurface != null && variant.BagVisibleSurface != "PLASTIC_SHEET") return TriState.FALSE;
                    if (rule.rule_id == "T101" && variant.BagVisibleSurface != null && variant.BagVisibleSurface != "TEXTILE") return TriState.FALSE;
                    if (string.IsNullOrEmpty(variant.BagVisibleSurface)) return TriState.UNKNOWN;
                    return TriState.TRUE;

                case "BACKPACK_TRAVEL_SPORT_BAG":
                    if (form != null && form != "BACKPACK" && form != "TRAVEL_BAG" && form != "SPORT_BAG") return TriState.FALSE;
                    if (rule.rule_id == "T102" && variant.BagVisibleSurface != null && variant.BagVisibleSurface != "LEATHER") return TriState.FALSE;
                    if (rule.rule_id == "T103" && variant.BagVisibleSurface != null && variant.BagVisibleSurface != "PLASTIC_SHEET") return TriState.FALSE;
                    if (rule.rule_id == "T104" && variant.BagVisibleSurface != null && variant.BagVisibleSurface != "TEXTILE") return TriState.FALSE;
                    if (string.IsNullOrEmpty(variant.BagVisibleSurface)) return TriState.UNKNOWN;
                    return TriState.TRUE;

                default:
                    return TriState.TRUE;
            }
        }

        private TriState EvaluateMaterialCondition(MatrixSeedRow rule, ProductVariantInput variant, MaterialResolutionResult material)
        {
            if (rule.competition_group == "BAG_CONTAINER")
            {
                if (rule.rule_id == "T099" || rule.rule_id == "T102") return variant.BagVisibleSurface == "LEATHER" ? TriState.TRUE : TriState.FALSE;
                if (rule.rule_id == "T100" || rule.rule_id == "T103") return variant.BagVisibleSurface == "PLASTIC_SHEET" ? TriState.TRUE : TriState.FALSE;
                if (rule.rule_id == "T101" || rule.rule_id == "T104") return variant.BagVisibleSurface == "TEXTILE" ? TriState.TRUE : TriState.FALSE;
                return TriState.UNKNOWN;
            }

            if (rule.rule_id == "T098")
            {
                // Headgear visor cap: accepts any textile material
                return TriState.TRUE;
            }

            if (rule.rule_id == "T097" || rule.rule_id == "T098" || rule.material_condition_vi.Contains("không tách theo sợi", StringComparison.OrdinalIgnoreCase) || rule.material_condition_vi.Contains("không tách cotton", StringComparison.OrdinalIgnoreCase))
            {
                return TriState.TRUE;
            }

            if (!material.IsValid)
            {
                return TriState.UNKNOWN;
            }

            var matClass = material.MaterialClass;

            // 1. COTTON rules
            if (rule.rule_id == "T001" || rule.rule_id == "T004" || rule.rule_id == "T005" || rule.rule_id == "T008" ||
                rule.rule_id == "T013" || rule.rule_id == "T016" || rule.rule_id == "T019" || rule.rule_id == "T023" ||
                rule.rule_id == "T024" || rule.rule_id == "T028" || rule.rule_id == "T029" || rule.rule_id == "T033" || 
                rule.rule_id == "T036" || rule.rule_id == "T039" || rule.rule_id == "T043" || rule.rule_id == "T047" || 
                rule.rule_id == "T049" || rule.rule_id == "T051" || rule.rule_id == "T054" || rule.rule_id == "T057" || 
                rule.rule_id == "T060" || rule.rule_id == "T063" || rule.rule_id == "T065" || rule.rule_id == "T067" || 
                rule.rule_id == "T068" || rule.rule_id == "T069" || rule.rule_id == "T072" || rule.rule_id == "T078" || 
                rule.rule_id == "T079" || rule.rule_id == "T081" || rule.rule_id == "T085" || rule.rule_id == "T087")
            {
                return matClass == "COTTON" ? TriState.TRUE : TriState.FALSE;
            }

            // 2. SYNTHETIC only rules
            if (rule.rule_id == "T025" || rule.rule_id == "T030" || rule.rule_id == "T040" || rule.rule_id == "T045" ||
                rule.rule_id == "T048" || rule.rule_id == "T050" || rule.rule_id == "T052" || rule.rule_id == "T082" ||
                rule.rule_id == "T086" || rule.rule_id == "T088" || rule.rule_id == "T094")
            {
                return matClass == "SYNTHETIC" ? TriState.TRUE : TriState.FALSE;
            }

            // 3. ARTIFICIAL only rules
            if (rule.rule_id == "T026" || rule.rule_id == "T031" || rule.rule_id == "T041" || rule.rule_id == "T044" || rule.rule_id == "T095")
            {
                return matClass == "ARTIFICIAL" ? TriState.TRUE : TriState.FALSE;
            }

            // 4. CHEMICAL (SYNTHETIC or ARTIFICIAL) rules
            if (rule.rule_id == "T002" || rule.rule_id == "T006" || rule.rule_id == "T007" || rule.rule_id == "T009" ||
                rule.rule_id == "T014" || rule.rule_id == "T017" || rule.rule_id == "T020" || rule.rule_id == "T034" ||
                rule.rule_id == "T037" || rule.rule_id == "T055" || rule.rule_id == "T058" || rule.rule_id == "T061" ||
                rule.rule_id == "T064" || rule.rule_id == "T066" || rule.rule_id == "T070" || rule.rule_id == "T073" ||
                rule.rule_id == "T077" || rule.rule_id == "T080" || rule.rule_id == "T083" || rule.rule_id == "T084")
            {
                return (matClass == "SYNTHETIC" || matClass == "ARTIFICIAL") ? TriState.TRUE : TriState.FALSE;
            }

            // 5. WOOL / CASHMERE rules
            if (rule.rule_id == "T010" || rule.rule_id == "T011" || rule.rule_id == "T012" || rule.rule_id == "T015" ||
                rule.rule_id == "T021" || rule.rule_id == "T027" || rule.rule_id == "T032" || rule.rule_id == "T042" ||
                rule.rule_id == "T046" || rule.rule_id == "T053" || rule.rule_id == "T056" || rule.rule_id == "T062" ||
                rule.rule_id == "T093")
            {
                return (matClass == "WOOL" || matClass == "CASHMERE") ? TriState.TRUE : TriState.FALSE;
            }

            // 6. SILK rules
            if (rule.rule_id == "T092")
            {
                return matClass == "SILK" ? TriState.TRUE : TriState.FALSE;
            }

            // 7. FLAX / RAMIE rules
            if (rule.rule_id == "T059")
            {
                return (matClass == "FLAX" || matClass == "RAMIE") ? TriState.TRUE : TriState.FALSE;
            }

            // 8. OTHER material rules
            if (rule.rule_id == "T003" || rule.rule_id == "T018" || rule.rule_id == "T022" || rule.rule_id == "T035" ||
                rule.rule_id == "T038" || rule.rule_id == "T071" || rule.rule_id == "T074" || rule.rule_id == "T096")
            {
                return (matClass == "FLAX" || matClass == "RAMIE" || matClass == "SILK" || matClass == "OTHER") ? TriState.TRUE : TriState.FALSE;
            }

            return TriState.FALSE;
        }
    }
}
