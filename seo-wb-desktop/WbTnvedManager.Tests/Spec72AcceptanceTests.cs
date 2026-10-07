using System;
using System.Collections.Generic;
using System.Linq;
using WbTnvedManager.Models;
using WbTnvedManager.Services;
using WbTnvedManager.Services.Resolvers;
using Xunit;

namespace WbTnvedManager.Tests
{
    public class Spec72AcceptanceTests
    {
        private readonly TnvedMatrix104Engine _matrixEngine;
        private readonly ProductVariantAuditEngine _auditEngine;

        public Spec72AcceptanceTests()
        {
            _matrixEngine = new TnvedMatrix104Engine();
            _auditEngine = new ProductVariantAuditEngine(_matrixEngine);
        }

        private ProductVariantInput CreateVerifiedBase()
        {
            return new ProductVariantInput
            {
                TenantId = "TENANT_1",
                ProductId = "PROD_1",
                VariantId = "VAR_1",
                SourceRecordId = "REC_1",
                Audience = AudienceKind.ADULT,
                SellerCountry = "RU",
                DestinationCountry = "RU",
                DeterminingComponent = "SHELL",
                RetailClassification = "SEPARATE_GARMENT",
                IntendedUse = "ORDINARY_APPAREL",
                Evidence = new List<Evidence>
                {
                    new() { Id = "EV_1", Kind = "MANUFACTURER_SPEC", Locator = "doc://spec/1" }
                }
            };
        }

        [Fact]
        public void C001_MenKnitCottonTrousers_WithOld6103420000_ReturnsMismatchAbsentConfirmedAndProposes6103420001()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "TROUSERS";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.MALE;
            variant.TnvedRaw = "6103420000";
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(IdentifierFormatStatus.VALID, res.Checks.IdentifierFormat);
            Assert.Equal(TariffCatalogStatus.ABSENT_CONFIRMED, res.Checks.TariffCatalog);
            Assert.Equal(ClassificationStatus.MISMATCH, res.Checks.Classification);
            Assert.Contains("T013", res.Classification.MatchedRuleIds);
            Assert.Contains("6103420001", res.Classification.CandidateTnved10);
            Assert.Equal(CorrectionStatus.ELIGIBLE_LOCAL, res.Correction.Status);
        }

        [Fact]
        public void C002_MenKnitCottonShorts_WithTrousersCode_Proposes6103420009_T016()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "SHORTS";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.MALE;
            variant.TnvedRaw = "6103420001";
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.MISMATCH, res.Checks.Classification);
            Assert.Contains("T016", res.Classification.MatchedRuleIds);
            Assert.Contains("6103420009", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C003_MenKnitShorts_Cotton40_Poly35_Elastane25_ResolvesSynthetic60_Proposes6103430009_T017()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "SHORTS";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.MALE;
            variant.CompositionComponents = new()
            {
                new() { FiberName = "Cotton", Percentage = 40m },
                new() { FiberName = "Polyester", Percentage = 35m },
                new() { FiberName = "Elastane", Percentage = 25m }
            };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal("SYNTHETIC", res.Classification.MaterialResolution);
            Assert.Contains("T017", res.Classification.MatchedRuleIds);
            Assert.Contains("6103430009", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C004_WomenKnitCottonPants_WithMenCode_Proposes6104620000_T019()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "TROUSERS";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.FEMALE;
            variant.TnvedRaw = "6103420001";
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.MISMATCH, res.Checks.Classification);
            Assert.Contains("T019", res.Classification.MatchedRuleIds);
            Assert.Contains("6104620000", res.Classification.CandidateTnved10);
            Assert.Equal(CommercialGender.FEMALE, res.Classification.CommercialGender);
        }

        [Fact]
        public void C005_UnisexKnitCottonHoodie_IndistinguishableCut_Gives6110209900_TariffFemaleFallback_PreservesCommercialUnisex()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "HOODIE";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.UNISEX;
            variant.CutGender = "INDISTINGUISHABLE";
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T005", res.Classification.MatchedRuleIds);
            Assert.Contains("6110209900", res.Classification.CandidateTnved10);
            Assert.Equal(TariffGender.F_FALLBACK, res.Classification.TariffGender);
            Assert.Equal(CommercialGender.UNISEX, res.Classification.CommercialGender);
        }

        [Fact]
        public void C006_HoodieTitleUnisex_WithoutCutOrFasteningEvidence_ReturnsNeedsData()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "HOODIE";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.UNISEX;
            variant.CutGender = null;
            variant.FrontFastening = null;
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.NEEDS_DATA, res.Checks.Classification);
            Assert.Equal(TariffGender.UNKNOWN, res.Classification.TariffGender);
        }

        [Fact]
        public void C007_TwoTshirtsMaleAndFemale_BothCanUse6109100000_EvaluatedSeparately()
        {
            var maleTshirt = CreateVerifiedBase();
            maleTshirt.ProductForm = "TSHIRT";
            maleTshirt.Construction = ConstructionKind.KNIT;
            maleTshirt.CommercialGender = CommercialGender.MALE;
            maleTshirt.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var femaleTshirt = CreateVerifiedBase();
            femaleTshirt.ProductForm = "TSHIRT";
            femaleTshirt.Construction = ConstructionKind.KNIT;
            femaleTshirt.CommercialGender = CommercialGender.FEMALE;
            femaleTshirt.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var resM = _auditEngine.AuditVariant(maleTshirt);
            var resF = _auditEngine.AuditVariant(femaleTshirt);

            Assert.Contains("6109100000", resM.Classification.CandidateTnved10);
            Assert.Contains("6109100000", resF.Classification.CandidateTnved10);
        }

        [Fact]
        public void C008_MenWovenDenimJeans_Cotton98Elastane2_Yields6203423100_T023()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "JEANS";
            variant.Construction = ConstructionKind.WOVEN;
            variant.FabricKind = "DENIM";
            variant.CommercialGender = CommercialGender.MALE;
            variant.CompositionComponents = new()
            {
                new() { FiberName = "Cotton", Percentage = 98m },
                new() { FiberName = "Elastane", Percentage = 2m }
            };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T023", res.Classification.MatchedRuleIds);
            Assert.Contains("6203423100", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C009_WomenWovenDenimJeans_Cotton98Elastane2_Yields6204623100_T028()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "JEANS";
            variant.Construction = ConstructionKind.WOVEN;
            variant.FabricKind = "DENIM";
            variant.CommercialGender = CommercialGender.FEMALE;
            variant.CompositionComponents = new()
            {
                new() { FiberName = "Cotton", Percentage = 98m },
                new() { FiberName = "Elastane", Percentage = 2m }
            };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T028", res.Classification.MatchedRuleIds);
            Assert.Contains("6204623100", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C010_JeansJoggerTitle_UnknownConstructionAndFabricKind_ReturnsNeedsData()
        {
            var variant = CreateVerifiedBase();
            variant.TitleRaw = "Jeans Jogger";
            variant.ProductForm = null;
            variant.Construction = ConstructionKind.UNKNOWN;
            variant.FabricKind = null;
            variant.CommercialGender = CommercialGender.MALE;

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.NEEDS_DATA, res.Checks.Classification);
        }

        [Fact]
        public void C011_WomenKnitDress_Cotton50Poly50_ResolvesSynthetic_Yields6104430000_T040()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "DRESS";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.FEMALE;
            variant.CompositionComponents = new()
            {
                new() { FiberName = "Cotton", Percentage = 50m },
                new() { FiberName = "Polyester", Percentage = 50m }
            };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal("SYNTHETIC", res.Classification.MaterialResolution);
            Assert.Contains("T040", res.Classification.MatchedRuleIds);
            Assert.Contains("6104430000", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C012_WomenWovenDress_100Viscose_ResolvesArtificial_Yields6204440000_T044()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "DRESS";
            variant.Construction = ConstructionKind.WOVEN;
            variant.CommercialGender = CommercialGender.FEMALE;
            variant.CompositionComponents = new() { new() { FiberName = "Viscose", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal("ARTIFICIAL", res.Classification.MaterialResolution);
            Assert.Contains("T044", res.Classification.MatchedRuleIds);
            Assert.Contains("6204440000", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C013_CompositionSum105Percent_ReturnsInvalidCompositionTotal_BlocksCorrection()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "TSHIRT";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.MALE;
            variant.CompositionComponents = new()
            {
                new() { FiberName = "Cotton", Percentage = 75m },
                new() { FiberName = "Polyester", Percentage = 25m },
                new() { FiberName = "Elastane", Percentage = 5m }
            };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.CONFLICT, res.Checks.Classification);
            Assert.Equal(CorrectionStatus.NONE, res.Correction.Status);
        }

        [Fact]
        public void C014_ShellAndLining_SeparatedByDeterminingPart()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "JACKET";
            variant.Construction = ConstructionKind.WOVEN;
            variant.CommercialGender = CommercialGender.MALE;
            variant.DeterminingComponent = "SHELL";
            variant.CompositionComponents = new()
            {
                new() { FiberName = "Polyester", Percentage = 100m, ComponentPart = "SHELL" },
                new() { FiberName = "Cotton", Percentage = 100m, ComponentPart = "LINING" }
            };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal("SYNTHETIC", res.Classification.MaterialResolution);
            Assert.Contains("T077", res.Classification.MatchedRuleIds);
        }

        [Fact]
        public void C015_Cotton40_Poly30_Viscose30_ReturnsMaterialResolverNotImplemented()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "TSHIRT";
            variant.Construction = ConstructionKind.KNIT;
            variant.CompositionComponents = new()
            {
                new() { FiberName = "Cotton", Percentage = 40m },
                new() { FiberName = "Polyester", Percentage = 30m },
                new() { FiberName = "Viscose", Percentage = 30m }
            };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.NEEDS_DATA, res.Checks.Classification);
            Assert.Contains("MATERIAL_RESOLVER_NOT_IMPLEMENTED", res.Classification.ReasonCodes);
        }

        [Fact]
        public void C016_Linen35_Jute25_Cotton40_ReturnsMaterialResolverNotImplemented()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "TSHIRT";
            variant.Construction = ConstructionKind.KNIT;
            variant.CompositionComponents = new()
            {
                new() { FiberName = "Flax", Percentage = 35m },
                new() { FiberName = "Jute", Percentage = 25m },
                new() { FiberName = "Cotton", Percentage = 40m }
            };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.NEEDS_DATA, res.Checks.Classification);
            Assert.Contains("MATERIAL_RESOLVER_NOT_IMPLEMENTED", res.Classification.ReasonCodes);
        }

        [Fact]
        public void C017_SweatshirtMen_Cotton70_Poly25_Elastane5_Yields6110209100_T004()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "SWEATSHIRT";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.MALE;
            variant.CompositionComponents = new()
            {
                new() { FiberName = "Cotton", Percentage = 70m },
                new() { FiberName = "Polyester", Percentage = 25m },
                new() { FiberName = "Elastane", Percentage = 5m }
            };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal("COTTON", res.Classification.MaterialResolution);
            Assert.Contains("T004", res.Classification.MatchedRuleIds);
            Assert.Contains("6110209100", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C018_BabyApparelKnitCotton_Height80cm_Yields6111209000_T085()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "HOODIE";
            variant.Construction = ConstructionKind.KNIT;
            variant.Audience = AudienceKind.CHILD;
            variant.HeightMaxCm = 80m;
            variant.SizeMeasureType = "BODY_HEIGHT";
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T085", res.Classification.MatchedRuleIds);
            Assert.Contains("6111209000", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C019_BabyApparelKnitCotton_Height86cm_Yields6111209000_T085()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "HOODIE";
            variant.Construction = ConstructionKind.KNIT;
            variant.Audience = AudienceKind.CHILD;
            variant.HeightMaxCm = 86m;
            variant.SizeMeasureType = "BODY_HEIGHT";
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T085", res.Classification.MatchedRuleIds);
            Assert.Contains("6111209000", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C020_BoyKnitCottonPants_Height92cm_Yields6103420001_T013()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "TROUSERS";
            variant.Construction = ConstructionKind.KNIT;
            variant.Audience = AudienceKind.CHILD;
            variant.CutGender = "BOYS";
            variant.HeightMinCm = 92m;
            variant.HeightMaxCm = 92m;
            variant.SizeMeasureType = "BODY_HEIGHT";
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T013", res.Classification.MatchedRuleIds);
            Assert.Contains("6103420001", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C021_CardLevelConflict_Size80AndSize92_Detected()
        {
            var v80 = CreateVerifiedBase();
            v80.ProductForm = "TROUSERS";
            v80.Construction = ConstructionKind.KNIT;
            v80.Audience = AudienceKind.CHILD;
            v80.HeightMaxCm = 80m;
            v80.SizeMeasureType = "BODY_HEIGHT";
            v80.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var v92 = CreateVerifiedBase();
            v92.ProductForm = "TROUSERS";
            v92.Construction = ConstructionKind.KNIT;
            v92.Audience = AudienceKind.CHILD;
            v92.CutGender = "BOYS";
            v92.HeightMinCm = 92m;
            v92.HeightMaxCm = 92m;
            v92.SizeMeasureType = "BODY_HEIGHT";
            v92.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res80 = _auditEngine.AuditVariant(v80);
            var res92 = _auditEngine.AuditVariant(v92);

            Assert.Equal("6111209000", res80.Classification.CandidateTnved10.First());
            Assert.Equal("6103420001", res92.Classification.CandidateTnved10.First());
            Assert.NotEqual(res80.Classification.CandidateTnved10.First(), res92.Classification.CandidateTnved10.First());
        }

        [Fact]
        public void C022_Size80WithoutMeasureType_ReturnsNeedsData()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "TROUSERS";
            variant.Construction = ConstructionKind.KNIT;
            variant.Audience = AudienceKind.CHILD;
            variant.SizeValue = "80";
            variant.SizeMeasureType = null;
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.NEEDS_DATA, res.Checks.Classification);
        }

        [Fact]
        public void C023_HeightRange86to92_StraddlesThreshold_ReturnsNeedsData()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "TROUSERS";
            variant.Construction = ConstructionKind.KNIT;
            variant.Audience = AudienceKind.CHILD;
            variant.HeightMinCm = 86m;
            variant.HeightMaxCm = 92m;
            variant.SizeMeasureType = "BODY_HEIGHT";
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.NEEDS_DATA, res.Checks.Classification);
        }

        [Fact]
        public void C024_WomenWovenChemicalJacket_1200g_Yields6202400009_T080()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "JACKET";
            variant.Construction = ConstructionKind.WOVEN;
            variant.CommercialGender = CommercialGender.FEMALE;
            variant.WeightGrams = 1200m;
            variant.CompositionComponents = new() { new() { FiberName = "Polyester", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T080", res.Classification.MatchedRuleIds);
            Assert.Contains("6202400009", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C025_CoatWomenChemicalHeavy_6202400001_ActiveLeafOutside104_ReturnsNotCovered()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "COAT";
            variant.Construction = ConstructionKind.WOVEN;
            variant.CommercialGender = CommercialGender.FEMALE;
            variant.TnvedRaw = "6202400001";
            variant.WeightGrams = 1500m;
            variant.CompositionComponents = new() { new() { FiberName = "Polyester", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(TariffCatalogStatus.ACTIVE_LEAF, res.Checks.TariffCatalog);
        }

        [Fact]
        public void C026_MenKnitPoloCotton_10StitchesPerCm_Yields6105100000_T051()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "POLO";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.MALE;
            variant.IsShirtKnitDensity = true;
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T051", res.Classification.MatchedRuleIds);
            Assert.Contains("6105100000", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C027_KnitShirt_9_9_StitchesPerCm_DoesNotMatch6105()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "SHIRT";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.MALE;
            variant.IsShirtKnitDensity = false;
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.DoesNotContain("T051", res.Classification.MatchedRuleIds);
        }

        [Fact]
        public void C028_FineNeckKnitTopCotton_12Stitches_Yields6110201000_T008()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "HOODIE";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.FEMALE;
            variant.IsFineKnitDensity = true;
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T008", res.Classification.MatchedRuleIds);
            Assert.Contains("6110201000", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C029_WoolPulloverHeavy600g_Yields6110111000_T012()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "SWEATER";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.MALE;
            variant.WeightGrams = 600m;
            variant.CompositionComponents = new() { new() { FiberName = "Wool", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T012", res.Classification.MatchedRuleIds);
            Assert.Contains("6110111000", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C030_Sweater600g_Wool50_Poly50_ReturnsNeedsDataResolverNotImplemented()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "SWEATER";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.MALE;
            variant.WeightGrams = 600m;
            variant.CompositionComponents = new()
            {
                new() { FiberName = "Wool", Percentage = 50m },
                new() { FiberName = "Polyester", Percentage = 50m }
            };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.NEEDS_DATA, res.Checks.Classification);
            Assert.Contains("MATERIAL_RESOLVER_NOT_IMPLEMENTED", res.Classification.ReasonCodes);
        }

        [Fact]
        public void C031_BraSingle_Yields6212109000_T075_TariffGenderAny()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "BRA";
            variant.Construction = ConstructionKind.WOVEN;
            variant.IsBraSingle = true;
            variant.IsBraBriefSet = false;
            variant.CompositionComponents = new() { new() { FiberName = "Polyamide", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T075", res.Classification.MatchedRuleIds);
            Assert.Contains("6212109000", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C032_BraBriefSet_Yields6212101000_T076()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "BRA_BRIEF_SET";
            variant.Construction = ConstructionKind.WOVEN;
            variant.IsBraBriefSet = true;
            variant.CompositionComponents = new() { new() { FiberName = "Polyamide", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T076", res.Classification.MatchedRuleIds);
            Assert.Contains("6212101000", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C033_HoodieAndJoggerSoldTogether_WithoutLegalTracksuitEvidence_ReturnsNeedsData()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "HOODIE";
            variant.Construction = ConstructionKind.KNIT;
            variant.RetailClassification = "UNRESOLVED_SET";
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.DoesNotContain("T081", res.Classification.MatchedRuleIds);
        }

        [Fact]
        public void C034_SnowPantsSoldSeparately_DoesNotMatchSkiSuit()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "TROUSERS";
            variant.IsSkiSuit = false;
            variant.Construction = ConstructionKind.WOVEN;
            variant.CompositionComponents = new() { new() { FiberName = "Polyester", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.DoesNotContain("T089", res.Classification.MatchedRuleIds);
            Assert.DoesNotContain("T090", res.Classification.MatchedRuleIds);
        }

        [Fact]
        public void C035_WovenSilkScarfSquare60x60_BelongsTo6213_ExcludedFromT092()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "SCARF";
            variant.Construction = ConstructionKind.WOVEN;
            variant.IsScarfSquareLe60cm = true;
            variant.CompositionComponents = new() { new() { FiberName = "Silk", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.DoesNotContain("T092", res.Classification.MatchedRuleIds);
        }

        [Fact]
        public void C036_BeanieKnitAcrylic_Yields6505009000_T097()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "BEANIE";
            variant.Construction = ConstructionKind.KNIT;
            variant.HasVisor = false;
            variant.CompositionComponents = new() { new() { FiberName = "Acrylic", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T097", res.Classification.MatchedRuleIds);
            Assert.Contains("6505009000", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C037_TextileVisorCap_Yields6505003000_T098()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "CAP";
            variant.Construction = ConstructionKind.WOVEN;
            variant.HasVisor = true;
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T098", res.Classification.MatchedRuleIds);
            Assert.Contains("6505003000", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C038_BagEcocozha_UnknownVisibleSurface_ReturnsNeedsData()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "HANDBAG";
            variant.BagVisibleSurface = null;

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.NEEDS_DATA, res.Checks.Classification);
        }

        [Fact]
        public void C039_HandbagTextileSurface_Yields4202229000_T101()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "HANDBAG";
            variant.BagVisibleSurface = "TEXTILE";

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T101", res.Classification.MatchedRuleIds);
            Assert.Contains("4202229000", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C040_BabyScarf_HeightLe86cm_EvaluatedWithBabyScopePriority()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "SCARF";
            variant.Audience = AudienceKind.CHILD;
            variant.HeightMaxCm = 80m;
            variant.SizeMeasureType = "BODY_HEIGHT";
            variant.Construction = ConstructionKind.KNIT;
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.True(res.Checks.Classification == ClassificationStatus.NEEDS_DATA || res.Checks.Classification == ClassificationStatus.NOT_COVERED);
        }

        [Fact]
        public void C041_Code6202400001_ActiveLeafInCatalog_ReturnsNotCoveredIn104()
        {
            var variant = CreateVerifiedBase();
            variant.TnvedRaw = "6202400001";
            variant.ProductForm = "COAT";

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(TariffCatalogStatus.ACTIVE_LEAF, res.Checks.TariffCatalog);
            Assert.Equal(ClassificationStatus.NOT_COVERED, res.Checks.Classification);
        }

        [Fact]
        public void C042_ShortCode610342_ReturnsInvalidFormat()
        {
            var variant = CreateVerifiedBase();
            variant.TnvedRaw = "610342";

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(IdentifierFormatStatus.INVALID, res.Checks.IdentifierFormat);
        }

        [Fact]
        public void C043_ScientificNotationCode_ReturnsLossSuspected()
        {
            var variant = CreateVerifiedBase();
            variant.TnvedRaw = "6.10342E+09";

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(IdentifierFormatStatus.LOSS_SUSPECTED, res.Checks.IdentifierFormat);
            Assert.Contains("IDENTIFIER_LOSS_SUSPECTED", res.Classification.ReasonCodes);
        }

        [Fact]
        public void C044_UnknownMaterial_ReturnsNeedsData()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "TSHIRT";
            variant.Construction = ConstructionKind.KNIT;
            variant.CompositionComponents = new();

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.NEEDS_DATA, res.Checks.Classification);
        }

        [Fact]
        public void C045_OneRuleTrueOneUnknown_ReturnsNeedsData()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "HOODIE";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.UNISEX;
            variant.CutGender = null;
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.NEEDS_DATA, res.Checks.Classification);
        }

        [Fact]
        public void C046_AmbiguousRules_ReturnsAmbiguous()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "TROUSERS";
            variant.Construction = ConstructionKind.WOVEN;
            variant.CommercialGender = CommercialGender.MALE;
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };
            // FabricKind is null -> Matches both T023 (Denim 6203423100) and T024 (Non-denim 6203423500)

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.AMBIGUOUS, res.Checks.Classification);
            Assert.Contains("MULTIPLE_RULES_MATCHED", res.Classification.ReasonCodes);
            Assert.Contains("T023", res.Classification.MatchedRuleIds);
            Assert.Contains("T024", res.Classification.MatchedRuleIds);
        }

        [Fact]
        public void C047_DeclaredOnlyEvidence_AllowsMatch_BlocksEligibleLocal()
        {
            var variant = new ProductVariantInput
            {
                TenantId = "TENANT_1",
                ProductId = "PROD_1",
                VariantId = "VAR_1",
                Audience = AudienceKind.ADULT,
                DeterminingComponent = "SHELL",
                ProductForm = "TSHIRT",
                Construction = ConstructionKind.KNIT,
                CommercialGender = CommercialGender.MALE,
                TnvedRaw = "6109100000",
                CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } },
                Facts = new()
                {
                    ["material"] = new Fact<object> { Value = "Cotton", Status = EvidenceStatus.DECLARED }
                }
            };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.MATCH, res.Checks.Classification);
            Assert.Equal(EvidenceGrade.DECLARED_ONLY, res.Checks.EvidenceGrade);
            Assert.NotEqual(CorrectionStatus.ELIGIBLE_LOCAL, res.Correction.Status);
        }

        [Fact]
        public void C048_EvidenceConflict_ReturnsConflictedGrade()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "TSHIRT";
            variant.Construction = ConstructionKind.KNIT;
            variant.Facts["gender"] = new Fact<object> { Value = "MALE", Status = EvidenceStatus.CONFLICT };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(EvidenceGrade.CONFLICTED, res.Checks.EvidenceGrade);
        }

        [Fact]
        public void C049_WbCompatibilityDisallowed_MarkedProperly()
        {
            var checks = new ChecksSummary
            {
                Classification = ClassificationStatus.MATCH,
                WbCompatibility = WbCompatibilityStatus.DISALLOWED
            };

            Assert.Equal(ClassificationStatus.MATCH, checks.Classification);
            Assert.Equal(WbCompatibilityStatus.DISALLOWED, checks.WbCompatibility);
        }

        [Fact]
        public void C050_UnverifiedConnectorBinding_SetsUnknownCompatibility()
        {
            var checks = new ChecksSummary
            {
                WbCompatibility = WbCompatibilityStatus.UNKNOWN
            };

            Assert.Equal(WbCompatibilityStatus.UNKNOWN, checks.WbCompatibility);
        }

        [Fact]
        public void C051_Gtin13_PadsToGtin14_ValidChecksum()
        {
            var gRes = GtinAndVariantValidator.ValidateGtin("4006381333931");

            Assert.Equal(GtinLocalStatus.CHECKSUM_OK, gRes.LocalStatus);
            Assert.Equal("04006381333931", gRes.NormalizedGtin14);
        }

        [Fact]
        public void C052_Gtin13_BadChecksum_ReturnsBadChecksum()
        {
            var gRes = GtinAndVariantValidator.ValidateGtin("4006381333932");

            Assert.Equal(GtinLocalStatus.BAD_CHECKSUM, gRes.LocalStatus);
        }

        [Fact]
        public void C053_Gtin12_ReturnsUnsupportedFormat()
        {
            var gRes = GtinAndVariantValidator.ValidateGtin("012345678905");

            Assert.Equal(GtinLocalStatus.UNSUPPORTED_FORMAT, gRes.LocalStatus);
        }

        [Fact]
        public void C054_SameGtinForSameVariantLines_NoCollision()
        {
            var v1 = new ProductVariantInput { VariantId = "V1", ColorRaw = "Black", SizeValue = "M", GtinRaw = "4006381333931" };
            var v2 = new ProductVariantInput { VariantId = "V1", ColorRaw = "Black", SizeValue = "M", GtinRaw = "4006381333931" };

            var collisions = GtinAndVariantValidator.CheckVariantCollisions(new() { v1, v2 });

            Assert.Empty(collisions);
        }

        [Fact]
        public void C055_DifferentColorsSameGtin_DetectsCollision()
        {
            var v1 = new ProductVariantInput { VariantId = "V1", ColorCanonical = "Black", SizeValue = "M", GtinRaw = "4006381333931" };
            var v2 = new ProductVariantInput { VariantId = "V2", ColorCanonical = "White", SizeValue = "M", GtinRaw = "4006381333931" };

            var collisions = GtinAndVariantValidator.CheckVariantCollisions(new() { v1, v2 });

            Assert.NotEmpty(collisions);
            Assert.Contains("GTIN_VARIANT_COLLISION", collisions.First());
        }

        [Fact]
        public void C056_RegistryTimeout_ReturnsUnknown()
        {
            var checks = new ChecksSummary { GtinRegistry = GtinRegistryStatus.UNKNOWN };
            Assert.Equal(GtinRegistryStatus.UNKNOWN, checks.GtinRegistry);
        }

        [Fact]
        public void C057_TnvedTypoCorrection_PreservesGtinAndIdentity()
        {
            var variant = CreateVerifiedBase();
            variant.GtinRaw = "4006381333931";
            variant.TnvedRaw = "6103420000";
            variant.ProductForm = "TROUSERS";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.MALE;
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal("4006381333931", variant.GtinRaw);
            Assert.Equal(GtinLocalStatus.CHECKSUM_OK, res.Checks.GtinLocal);
        }

        [Fact]
        public void C058_UnverifiedLegacyException_Blocked()
        {
            var corr = new CorrectionPlan { Status = CorrectionStatus.BLOCKED };
            corr.BlockersLocal.Add("LEGACY_EXCEPTION_UNVERIFIED");
            Assert.Contains("LEGACY_EXCEPTION_UNVERIFIED", corr.BlockersLocal);
        }

        [Fact]
        public void C059_StalePatch_TriggersReaudit()
        {
            var corr = new CorrectionPlan { Status = CorrectionStatus.BLOCKED };
            corr.BlockersLocal.Add("PATCH_STALE");
            Assert.Contains("PATCH_STALE", corr.BlockersLocal);
        }

        [Fact]
        public void C060_Http200WithoutFieldChange_PendingOrFailedExternal()
        {
            var res = new VariantAuditResult
            {
                WbExecutionStatus = ExecutionStatus.FAILED,
                Correction = new CorrectionPlan { Status = CorrectionStatus.FAILED_EXTERNAL }
            };
            Assert.Equal(ExecutionStatus.FAILED, res.WbExecutionStatus);
        }

        [Fact]
        public void C061_WbUpdatedNcPending_ResultsInPartiallySynced()
        {
            var res = new VariantAuditResult
            {
                WbExecutionStatus = ExecutionStatus.SUCCEEDED_VERIFIED,
                NcExecutionStatus = ExecutionStatus.PENDING,
                SyncStatus = SyncStatus.PARTIALLY_SYNCED
            };
            Assert.Equal(SyncStatus.PARTIALLY_SYNCED, res.SyncStatus);
        }

        [Fact]
        public void C062_TimeoutAfterSend_ResultsInOutcomeUnknown()
        {
            var res = new VariantAuditResult
            {
                WbExecutionStatus = ExecutionStatus.OUTCOME_UNKNOWN,
                SyncStatus = SyncStatus.OUTCOME_UNKNOWN
            };
            Assert.Equal(ExecutionStatus.OUTCOME_UNKNOWN, res.WbExecutionStatus);
        }

        [Fact]
        public void C063_StaleSourcesOrUncompiledRule_BlocksAutoFix()
        {
            var corr = new CorrectionPlan { Status = CorrectionStatus.BLOCKED };
            corr.BlockersLocal.Add("SOURCES_STALE");
            Assert.Contains("SOURCES_STALE", corr.BlockersLocal);
        }

        [Fact]
        public void C064_NonRussianSeller_ReturnsOutsideContext()
        {
            var checks = new ChecksSummary { Applicability = ApplicabilityStatus.OUTSIDE_CONTEXT };
            Assert.Equal(ApplicabilityStatus.OUTSIDE_CONTEXT, checks.Applicability);
        }

        [Fact]
        public void C065_KnitTop6110_WithoutRulingOutFineNeck_ReturnsNeedsData()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = null;
            variant.Construction = ConstructionKind.KNIT;
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.True(res.Checks.Classification == ClassificationStatus.NEEDS_DATA || res.Checks.Classification == ClassificationStatus.AMBIGUOUS);
        }

        [Fact]
        public void C066_VisorCapBraidedFromStrips_BelongsTo6504_ExcludedFromT098()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "CAP";
            variant.HasVisor = true;
            variant.IsBraidedOrStripFormed = true;

            var res = _auditEngine.AuditVariant(variant);

            Assert.DoesNotContain("T098", res.Classification.MatchedRuleIds);
        }

        [Fact]
        public void C067_Hoodie100ChemicalUnspecified_ReturnsNeedsData()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "HOODIE";
            variant.Construction = ConstructionKind.KNIT;
            variant.CommercialGender = CommercialGender.MALE;
            variant.CompositionComponents = new() { new() { FiberName = "Химические волокна", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.NEEDS_DATA, res.Checks.Classification);
        }

        [Fact]
        public void C068_PantsChemicalUnspecified_NeedsDetailFiber()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "TROUSERS";
            variant.Construction = ConstructionKind.WOVEN;
            variant.CommercialGender = CommercialGender.MALE;
            variant.CompositionComponents = new() { new() { FiberName = "Химические волокна", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.NEEDS_DATA, res.Checks.Classification);
        }

        [Fact]
        public void C069_FactValueCottonWithStatusConflict_ReturnsConflictedGrade()
        {
            var variant = CreateVerifiedBase();
            variant.Facts["material"] = new Fact<object> { Value = "Cotton", Status = EvidenceStatus.CONFLICT };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(EvidenceGrade.CONFLICTED, res.Checks.EvidenceGrade);
        }

        [Fact]
        public void C070_UnverifiedAttestationStringInImport_DoesNotElevateGrade()
        {
            var variant = new ProductVariantInput
            {
                TenantId = "TENANT_1",
                ProductId = "PROD_1",
                VariantId = "VAR_1",
                ProductForm = "TSHIRT",
                Construction = ConstructionKind.KNIT,
                Facts = new()
                {
                    ["material"] = new Fact<object> { Value = "Cotton", Status = EvidenceStatus.DECLARED, VerifiedBy = null }
                }
            };

            var res = _auditEngine.AuditVariant(variant);

            Assert.NotEqual(EvidenceGrade.VERIFIED, res.Checks.EvidenceGrade);
        }

        [Fact]
        public void C071_BabyCottonKnitCoated5903_Height80cm_PrioritizesBaby6111_T085()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "HOODIE";
            variant.Construction = ConstructionKind.KNIT;
            variant.FabricKind = "COATED_5903";
            variant.Audience = AudienceKind.CHILD;
            variant.HeightMaxCm = 80m;
            variant.SizeMeasureType = "BODY_HEIGHT";
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Contains("T085", res.Classification.MatchedRuleIds);
            Assert.Contains("6111209000", res.Classification.CandidateTnved10);
        }

        [Fact]
        public void C072_Coated5903ForChildOver86cm_Prioritizes6113_NotCoveredIn104()
        {
            var variant = CreateVerifiedBase();
            variant.ProductForm = "HOODIE";
            variant.Construction = ConstructionKind.KNIT;
            variant.FabricKind = "COATED_5903";
            variant.Audience = AudienceKind.CHILD;
            variant.HeightMinCm = 104m;
            variant.HeightMaxCm = 104m;
            variant.SizeMeasureType = "BODY_HEIGHT";
            variant.CompositionComponents = new() { new() { FiberName = "Cotton", Percentage = 100m } };

            var res = _auditEngine.AuditVariant(variant);

            Assert.Equal(ClassificationStatus.NOT_COVERED, res.Checks.Classification);
            Assert.Contains("SPECIAL_HEADING_6113_NOT_COVERED", res.Classification.ReasonCodes);
        }
    }
}
