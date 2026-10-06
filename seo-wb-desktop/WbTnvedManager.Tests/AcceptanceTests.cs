using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using WbTnvedManager.Data;
using WbTnvedManager.Models;
using WbTnvedManager.Services;

namespace WbTnvedManager.Tests
{
    public class AcceptanceTests
    {
        private readonly RulesEngine _rulesEngine;
        private readonly TnvedSelectorService _selector;
        private readonly MatrixRepository _repository;

        public AcceptanceTests()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_spec_{Guid.NewGuid():N}.db");
            var connStr = $"Data Source={dbPath}";

            using var conn = new Microsoft.Data.Sqlite.SqliteConnection(connStr);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE TnvedMatrix (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SubjectId INTEGER NOT NULL,
                    SubjectName TEXT NOT NULL,
                    Gender TEXT NOT NULL,
                    Material TEXT NOT NULL,
                    KnitType TEXT DEFAULT '',
                    TnvedCode TEXT NOT NULL,
                    Description TEXT DEFAULT '',
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );
            ";
            cmd.ExecuteNonQuery();

            _repository = new MatrixRepository(connStr);
            _repository.BulkInsertOrUpdate(DatabaseInitializer.GetDefaultSeeds());
            _selector = new TnvedSelectorService(_repository);
            _rulesEngine = new RulesEngine(_selector);
        }

        // -------------------------------------------------------------
        // AT01: Only 4-digit TNVED (e.g. 6103) -> No zero-padding, requires review
        // -------------------------------------------------------------
        [Fact]
        public void AT01_FourDigitTnved_MustNotAutoPadZeros_AndMustRequireReview()
        {
            var card = new WbCardItem
            {
                NmId = 5001,
                SubjectId = 105,
                SubjectName = "Футболка",
                Characteristics = new List<WbCharacteristic>
                {
                    new() { Id = 5, Name = "ТНВЭД", Value = "6103" } // Only 4 digits
                }
            };

            var context = new AuditEvaluationContext { WbCard = card };
            var report = _rulesEngine.Evaluate(context);

            var r010Issue = report.Issues.FirstOrDefault(i => i.RuleId == "R010");
            Assert.NotNull(r010Issue);
            Assert.Equal(IssueSeverity.BLOCK, r010Issue.Severity);
            Assert.Contains("mã đầy đủ 10 chữ số", r010Issue.VietnameseExplanation);
        }

        // -------------------------------------------------------------
        // AT03: Chapter 61 vs Chapter 62 Knitted/Woven Contradiction
        // -------------------------------------------------------------
        [Fact]
        public void AT03_KnittedVsWovenContradiction_MustBeFlagged()
        {
            var eval = TnvedClassificationEngine.EvaluateClassification(
                currentCode: "6103420000", // Chapter 61 (Knitted)
                productType: "Брюки",
                construction: "WOVEN",     // Woven!
                audience: "MALE",
                heightCm: null,
                materialFamily: "cotton"
            );

            Assert.False(eval.IsValid);
            Assert.True(eval.NeedsReview);
            Assert.Contains("Mâu thuẫn kết cấu", eval.ReasonVi);
        }

        // -------------------------------------------------------------
        // AT05: Baby sizing (Height <= 86cm) -> Chapter 6111 / 6209
        // -------------------------------------------------------------
        [Fact]
        public void AT05_BabyHeightUnder86cm_MustMapToBabyBranch()
        {
            var eval = TnvedClassificationEngine.EvaluateClassification(
                currentCode: null,
                productType: "Боди",
                construction: "KNITTED",
                audience: "BABY",
                heightCm: 80m,
                materialFamily: "cotton"
            );

            Assert.True(eval.IsBabyBranch);
            Assert.Equal("6111209000", eval.CandidateCode);
        }

        // -------------------------------------------------------------
        // AT07: Fabric composition 75+25+5 = 105% -> Must report error without auto-fudging
        // -------------------------------------------------------------
        [Fact]
        public void AT07_CompositionOver100_MustBeFlaggedAsInvalid()
        {
            var component = new ProductComponent
            {
                Type = "OUTER",
                Fibers = new List<FiberComposition>
                {
                    new() { NameRu = "хлопок", Percentage = 75 },
                    new() { NameRu = "полиэстер", Percentage = 25 },
                    new() { NameRu = "эластан", Percentage = 5 }
                }
            };

            var res = CompositionValidator.ValidateComponent(component);

            Assert.False(res.IsValid);
            Assert.Equal(105m, res.TotalPercentage);
            Assert.Contains("105%", res.ErrorMessage);
        }

        // -------------------------------------------------------------
        // AT08: Outer 100% and Lining 100% stored separately -> Must be valid
        // -------------------------------------------------------------
        [Fact]
        public void AT08_OuterAndLiningStoredSeparately_MustBeValid()
        {
            var components = new List<ProductComponent>
            {
                new()
                {
                    Type = "OUTER",
                    Fibers = new List<FiberComposition> { new() { NameRu = "полиэстер", Percentage = 100 } }
                },
                new()
                {
                    Type = "LINING",
                    Fibers = new List<FiberComposition> { new() { NameRu = "полиэстер", Percentage = 100 } }
                }
            };

            var results = CompositionValidator.ValidateAllComponents(components);

            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.True(r.IsValid));
        }

        // -------------------------------------------------------------
        // AT13 & AT14: GS1 GTIN-13 Checksum Validation
        // -------------------------------------------------------------
        [Theory]
        [InlineData("6291041500213", GtinStructureStatus.PASS)]
        [InlineData("4607009520018", GtinStructureStatus.PASS)]
        [InlineData("6291041500214", GtinStructureStatus.FAIL_CHECK_DIGIT)] // Wrong check digit
        [InlineData("46O7009520018", GtinStructureStatus.FAIL_NON_NUMERIC)] // Cyrillic/Latin 'O'
        [InlineData("12345", GtinStructureStatus.FAIL_FORMAT)]             // Too short
        public void AT13_AT14_GtinValidation_MustAccuratelyValidateGs1Format(string gtin, GtinStructureStatus expectedStatus)
        {
            var status = GtinValidator.ValidateStructure(gtin);
            Assert.Equal(expectedStatus, status);
        }

        // -------------------------------------------------------------
        // AT15: GTIN-13 vs 14-digit Canonical Identity Equality
        // -------------------------------------------------------------
        [Fact]
        public void AT15_Gtin13AndCanonical14_MustShareSameIdentity()
        {
            string gtin13 = "4607009520018";
            string gtin14 = "04607009520018";

            Assert.True(GtinValidator.AreSameIdentity(gtin13, gtin14));
            Assert.Equal("04607009520018", GtinValidator.ToCanonical14(gtin13));
        }

        // -------------------------------------------------------------
        // AT35: Full Update Preservation (Preserves all other fields)
        // -------------------------------------------------------------
        [Fact]
        public void AT35_SafeUpdate_MustPreserveAllExistingCardData()
        {
            var original = new WbCardItem
            {
                NmId = 99887766,
                SubjectId = 273,
                SubjectName = "Джинсы",
                VendorCode = "PRESERVE-001",
                Title = "Джинсы премиум мужские",
                Description = "Mô tả nguyên bản cực kỳ quan trọng.",
                Dimensions = JsonSerializer.SerializeToElement(new { length = 30, width = 20, height = 5 }),
                Photos = new List<JsonElement> { JsonSerializer.SerializeToElement(new { big = "https://images.wbstatic.net/test.jpg" }) },
                Sizes = new List<JsonElement>
                {
                    JsonSerializer.SerializeToElement(new { techSize = "32", wbSize = "48", skus = new[] { "2000000000001" }, price = 2500 }),
                    JsonSerializer.SerializeToElement(new { techSize = "34", wbSize = "50", skus = new[] { "2000000000002" }, price = 2500 })
                },
                Characteristics = new List<WbCharacteristic>
                {
                    new() { Id = 5, Name = "ТНВЭД", Value = "0000000000" },
                    new() { Id = 8, Name = "Пол", Value = "Детский" },
                    new() { Id = 99, Name = "Сезон", Value = "Всесезонный" }
                }
            };

            var updated = CardBulkUpdateService.PrepareUpdatedCard(original, "6203423100", "Мужской");

            // Verify preservation of core metadata
            Assert.Equal(original.NmId, updated.NmId);
            Assert.Equal(original.VendorCode, updated.VendorCode);
            Assert.Equal(original.Title, updated.Title);
            Assert.Equal(original.Description, updated.Description);
            Assert.Equal(2, updated.Sizes.Count);
            Assert.Equal(30, updated.Dimensions.Value.GetProperty("length").GetInt32());

            // Verify TNVED & Gender updated
            var tnvedCharc = updated.Characteristics.First(c => c.Id == 5);
            Assert.Equal("6203423100", tnvedCharc.Value);

            var genderCharc = updated.Characteristics.First(c => c.Id == 8);
            Assert.Equal("Мужской", genderCharc.Value);

            // Verify third characteristic untouched
            var seasonCharc = updated.Characteristics.First(c => c.Id == 99);
            Assert.Equal("Всесезонный", seasonCharc.GetFormattedValue());
        }

        // -------------------------------------------------------------
        // AT57: Unisex Classification under EAEU Note 9
        // -------------------------------------------------------------
        [Fact]
        public void AT57_UnisexClassification_FollowsNote9Eaeu()
        {
            var eval = TnvedClassificationEngine.EvaluateClassification(
                currentCode: "6104620000",
                productType: "Худи",
                construction: "KNITTED",
                audience: "UNISEX",
                heightCm: null,
                materialFamily: "cotton"
            );

            Assert.True(eval.IsValid);
            Assert.Contains("Ghi chú 9", eval.ReasonVi);
        }
    }
}
