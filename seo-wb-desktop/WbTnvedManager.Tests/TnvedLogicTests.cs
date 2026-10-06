using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using WbTnvedManager.Data;
using WbTnvedManager.Models;
using WbTnvedManager.Services;

namespace WbTnvedManager.Tests
{
    public class TnvedLogicTests
    {
        private readonly string _testDbPath;
        private readonly MatrixRepository _repository;
        private readonly TnvedSelectorService _selector;

        public TnvedLogicTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_tnved_{Guid.NewGuid():N}.db");
            var connStr = $"Data Source={_testDbPath}";

            // Initialize test SQLite DB
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
        }

        [Fact]
        public void Database_ShouldHaveComprehensiveSeedData()
        {
            var all = _repository.GetAll();
            Assert.True(all.Count >= 20, $"Expected >= 20 seed items, but got {all.Count}");

            var tshirt = all.FirstOrDefault(x => x.SubjectId == 105 && x.Gender == "Женский" && x.Material == "Хлопок");
            Assert.NotNull(tshirt);
            Assert.Equal("6109100000", tshirt.TnvedCode);
        }

        [Theory]
        [InlineData(105, "Женский", "Хлопок", "Трикотаж", "6109100000")]
        [InlineData(273, "Мужской", "Хлопок", "Ткань", "6203423100")]
        [InlineData(273, "Женский", "Хлопок", "Ткань", "6204623100")]
        [InlineData(156, "Женский", "Хлопок", "Ткань", "6204420000")]
        [InlineData(248, "Мужской", "Хлопок", "Трикотаж", "6110209100")]
        public void ExactMatrixLookup_ShouldReturnCorrectTnved(int subjectId, string gender, string material, string knit, string expectedTnved)
        {
            var (code, reason) = _selector.GetTnvedForAttributes(subjectId, gender, material, knit);
            Assert.Equal(expectedTnved, code);
            Assert.Contains("Khớp", reason);
        }

        [Theory]
        [InlineData("Футболка оверсайз женская хлопок", "Женский", "Хлопок")]
        [InlineData("Джинсы прямые для мужчин плотный деним", "Мужской", "Хлопок")]
        [InlineData("Платье вечернее шелк женское", "Женский", "Шелк")]
        [InlineData("Худи для мальчиков с капюшоном", "Мальчики", "Хлопок")]
        [InlineData("Куртка мужская зимняя полиэстер", "Мужской", "Синтетика")]
        public void HeuristicInference_ShouldDetectGenderAndMaterial(string text, string expectedGender, string expectedMaterial)
        {
            var gender = _selector.InferGender(text);
            var material = _selector.InferMaterial(text);

            Assert.Equal(expectedGender, gender);
            Assert.Equal(expectedMaterial, material);
        }

        [Fact]
        public void AuditCards_ShouldDetectMismatchesAccurately()
        {
            var auditService = new CardAuditService(_selector);

            var sampleCards = new List<WbCardItem>
            {
                // Card 1: Correct TNVED (T-shirt female cotton)
                new()
                {
                    NmId = 100001,
                    VendorCode = "TSHIRT-OK",
                    SubjectId = 105,
                    SubjectName = "Футболка",
                    Title = "Футболка женская базовая",
                    Characteristics = new List<WbCharacteristic>
                    {
                        new() { Id = 5, Name = "ТНВЭД", Value = "6109100000" },
                        new() { Id = 8, Name = "Пол", Value = "Женский" },
                        new() { Id = 10, Name = "Состав", Value = "Хлопок" }
                    }
                },
                // Card 2: Wrong TNVED (Jeans has wrong shoes TNVED 6403)
                new()
                {
                    NmId = 100002,
                    VendorCode = "JEANS-WRONG",
                    SubjectId = 273,
                    SubjectName = "Джинсы",
                    Title = "Джинсы мужские синие",
                    Characteristics = new List<WbCharacteristic>
                    {
                        new() { Id = 5, Name = "ТНВЭД", Value = "6403999600" }, // WRONG
                        new() { Id = 8, Name = "Пол", Value = "Мужской" }
                    }
                },
                // Card 3: Missing Gender & Wrong TNVED
                new()
                {
                    NmId = 100003,
                    VendorCode = "DRESS-WRONG-GENDER",
                    SubjectId = 156,
                    SubjectName = "Платье",
                    Title = "Платье летнее хлопковое женское",
                    Characteristics = new List<WbCharacteristic>
                    {
                        new() { Id = 5, Name = "ТНВЭД", Value = "1111111111" } // WRONG & No Gender
                    }
                }
            };

            var auditResults = auditService.AuditCards(sampleCards);

            Assert.Equal(3, auditResults.Count);

            // Card 1 must be MatchOk
            Assert.Equal(AuditStatus.MatchOk, auditResults[0].Status);
            Assert.False(auditResults[0].CanFix);

            // Card 2 must be TnvedMismatch
            Assert.Equal(AuditStatus.TnvedMismatch, auditResults[1].Status);
            Assert.Equal("6203423100", auditResults[1].SuggestedTnved);
            Assert.True(auditResults[1].CanFix);

            // Card 3 must be BothMismatch or TnvedMismatch
            Assert.True(auditResults[2].CanFix);
            Assert.Equal("6204420000", auditResults[2].SuggestedTnved);
            Assert.Equal("Женский", auditResults[2].SuggestedGender);
        }

        [Fact]
        public void AuditCards_SportPants_ShouldResolveCorrectTnved()
        {
            var auditService = new CardAuditService(_selector);

            var sampleCards = new List<WbCardItem>
            {
                new()
                {
                    NmId = 100004,
                    VendorCode = "quần nike 6832 den",
                    SubjectId = 10099, // Unseed ID but subjectName matches
                    SubjectName = "Брюки спортивные",
                    Title = "Брюки спортивные мужские оверсайз хлопок",
                    Characteristics = new List<WbCharacteristic>
                    {
                        new() { Id = 8, Name = "Пол", Value = "Мужской" },
                        new() { Id = 10, Name = "Состав", Value = "Хлопок" }
                    }
                }
            };

            var auditResults = auditService.AuditCards(sampleCards);

            Assert.Single(auditResults);
            Assert.Equal("6103420000", auditResults[0].SuggestedTnved);
            Assert.True(auditResults[0].CanFix);
            Assert.NotEqual(AuditStatus.NoMatrixMatch, auditResults[0].Status);
        }

        [Fact]
        public void PrepareUpdatedCard_MustPreserveOriginalDataAndOnlyUpdateTnvedAndGender()
        {
            var original = new WbCardItem
            {
                NmId = 99887766,
                ImtId = 11223344,
                SubjectId = 273,
                SubjectName = "Джинсы",
                VendorCode = "JEANS-ORIGINAL-001",
                Brand = "MyExclusiveBrand",
                Title = "Джинсы премиум мужские",
                Description = "Mô tả nguyên bản cực kỳ quan trọng không được phép thay đổi.",
                Characteristics = new List<WbCharacteristic>
                {
                    new() { Id = 5, Name = "ТНВЭД", Value = "0000000000" },
                    new() { Id = 10, Name = "Состав", Value = "100% Хлопок" },
                    new() { Id = 99, Name = "Сезон", Value = "Круглогодичный" }
                }
            };

            var updated = CardBulkUpdateService.PrepareUpdatedCard(original, "6203423100", "Мужской");

            // Verify preservation
            Assert.Equal(original.NmId, updated.NmId);
            Assert.Equal(original.ImtId, updated.ImtId);
            Assert.Equal(original.SubjectId, updated.SubjectId);
            Assert.Equal(original.VendorCode, updated.VendorCode);
            Assert.Equal(original.Brand, updated.Brand);
            Assert.Equal(original.Title, updated.Title);
            Assert.Equal(original.Description, updated.Description);

            // Verify TNVED updated
            var tnved = updated.Characteristics.FirstOrDefault(c => c.Id == 5);
            Assert.NotNull(tnved);
            Assert.Equal("6203423100", tnved.Value);

            // Verify Gender added / updated
            var gender = updated.Characteristics.FirstOrDefault(c => c.Id == 8);
            Assert.NotNull(gender);
            Assert.Equal("Мужской", gender.Value);

            // Verify other characteristics preserved
            var season = updated.Characteristics.FirstOrDefault(c => c.Id == 99);
            Assert.NotNull(season);
            Assert.Equal("Круглогодичный", season.GetFormattedValue());
        }

        [Fact]
        public async Task RateLimiter_ShouldThrottleConsecutiveRequests()
        {
            var limiter = new RateLimiter(300);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            await limiter.ExecuteAsync(async () => await Task.Delay(10));
            await limiter.ExecuteAsync(async () => await Task.Delay(10));

            sw.Stop();
            Assert.True(sw.ElapsedMilliseconds >= 280, $"Expected >= 280ms delay, but took {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void BuildUploadByItemPayload_ShouldFormatCorrectWildberriesSchema()
        {
            var sizes = new List<ProductSizeItem>
            {
                new() { TechSize = "S", WbSize = "42", Price = 1500, Barcode = "2000000000001" },
                new() { TechSize = "M", WbSize = "44", Price = 1600, Barcode = "2000000000002" }
            };

            var payload = _selector.BuildUploadByItemPayload(
                subjectId: 105,
                vendorCode: "TSHIRT-TEST-001",
                title: "Футболка женская оверсайz",
                description: "Mô tả chất lượng cao",
                gender: "Женский",
                material: "Хлопок",
                tnvedCode: "6109100000",
                brand: "WB Fashion",
                color: "Черный",
                length: 30,
                width: 25,
                height: 5,
                weightBrutto: 0.5,
                sizeList: sizes
            );

            var json = JsonSerializer.Serialize(payload);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal(JsonValueKind.Array, root.ValueKind);
            var firstGroup = root[0];
            Assert.Equal(105, firstGroup.GetProperty("subjectID").GetInt32());

            var variants = firstGroup.GetProperty("variants");
            Assert.Equal(1, variants.GetArrayLength());
            var variant = variants[0];

            Assert.Equal("TSHIRT-TEST-001", variant.GetProperty("vendorCode").GetString());
            Assert.Equal("WB Fashion", variant.GetProperty("brand").GetString());

            var dims = variant.GetProperty("dimensions");
            Assert.Equal(30, dims.GetProperty("length").GetInt32());
            Assert.Equal(25, dims.GetProperty("width").GetInt32());
            Assert.Equal(5, dims.GetProperty("height").GetInt32());

            var charcs = variant.GetProperty("characteristics");
            Assert.True(charcs.GetArrayLength() >= 3);

            var variantSizes = variant.GetProperty("sizes");
            Assert.Equal(2, variantSizes.GetArrayLength());
            Assert.Equal("S", variantSizes[0].GetProperty("techSize").GetString());
            Assert.Equal("2000000000001", variantSizes[0].GetProperty("skus")[0].GetString());
        }

        [Fact]
        public void ProductPhotoItem_ShouldDetectLocalFileAndDisplayName()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var photo = new ProductPhotoItem
                {
                    FilePath = tempFile,
                    OrderIndex = 1
                };

                Assert.True(photo.IsLocalFile);
                Assert.Equal(Path.GetFileName(tempFile), photo.DisplayName);

                var urlPhoto = new ProductPhotoItem
                {
                    Url = "https://images.wbstatic.net/test.jpg",
                    OrderIndex = 2
                };

                Assert.False(urlPhoto.IsLocalFile);
                Assert.StartsWith("https://images.wbstatic.net/", urlPhoto.DisplayName);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}
