using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WbTnvedManager.Data;
using WbTnvedManager.Models;
using WbTnvedManager.Services;
using WbTnvedManager.ViewModels;
using Xunit;

namespace WbTnvedManager.Tests
{
    public class AcceptanceTests
    {
        [Theory]
        [InlineData("460123456789", false)]
        [InlineData("4601234567893", true)]
        [InlineData("04601234567893", true)]
        [InlineData("12345670", true)]
        [InlineData("4601234567891", false)]
        [InlineData("460123456789A", false)]
        [InlineData("", false)]
        public void AT01_AT02_GtinStructureValidation(string gtin, bool expectedValid)
        {
            var status = GtinValidator.ValidateStructure(gtin);
            bool isValid = status == GtinStructureStatus.PASS;
            Assert.Equal(expectedValid, isValid);
        }

        [Fact]
        public void AT03_GtinCanonical14DigitFormatting()
        {
            string gtin13 = "4601234567893";
            string? canonical = GtinValidator.ToCanonical14(gtin13);
            Assert.NotNull(canonical);
            Assert.Equal("04601234567893", canonical);
            Assert.Equal(14, canonical.Length);
        }

        [Fact]
        public void AT04_StrictMultiLayerCompositionValidation_PassesWhen100Percent()
        {
            var layers = new List<ProductComponent>
            {
                new ProductComponent
                {
                    Type = "OUTER",
                    Fibers = new() { new FiberComposition { NameRu = "Хлопок", Percentage = 95 }, new FiberComposition { NameRu = "Эластан", Percentage = 5 } }
                },
                new ProductComponent
                {
                    Type = "LINING",
                    Fibers = new() { new FiberComposition { NameRu = "Полиэстер", Percentage = 100 } }
                }
            };

            var results = CompositionValidator.ValidateAllComponents(layers);
            Assert.All(results, r => Assert.True(r.IsValid));
        }

        [Fact]
        public void AT05_CompositionValidation_RejectsIllegalSum()
        {
            var layers = new List<ProductComponent>
            {
                new ProductComponent
                {
                    Type = "OUTER",
                    Fibers = new() { new FiberComposition { NameRu = "Хлопок", Percentage = 80 }, new FiberComposition { NameRu = "Эластан", Percentage = 10 } }
                }
            };

            var results = CompositionValidator.ValidateAllComponents(layers);
            Assert.Contains(results, r => !r.IsValid && r.ErrorMessage.Contains("90%"));
        }

        [Fact]
        public void AT06_TnvedClassification_RejectsArtificialZeroPadding()
        {
            bool valid10 = TnvedClassificationEngine.IsValid10DigitFormat("6109100000");
            bool invalid4 = TnvedClassificationEngine.IsValid10DigitFormat("6109");

            Assert.True(valid10);
            Assert.False(invalid4);
        }

        [Fact]
        public void AT07_TnvedClassification_DetectsKnittedVsWovenContradiction()
        {
            var result = TnvedClassificationEngine.EvaluateClassification(
                "6204620000",
                "Брюки",
                "KNITTED",
                "FEMALE",
                170,
                "Хлопок");

            Assert.False(result.IsValid);
            Assert.Contains("Mâu thuẫn kết cấu", result.ReasonVi);
        }

        [Fact]
        public void AT08_TnvedClassification_EnforcesUnisexNote9FemaleBranch()
        {
            var result = TnvedClassificationEngine.EvaluateClassification(
                null,
                "Брюки",
                "WOVEN",
                "UNISEX",
                170,
                "Хлопок");

            Assert.NotNull(result.CandidateCode);
            Assert.StartsWith("6204", result.CandidateCode);
            Assert.Contains("Note 9", result.ReasonVi);
        }

        [Fact]
        public void AT09_TnvedClassification_BabyHeightUnder86cmRoutesToChildrenChapter()
        {
            var result = TnvedClassificationEngine.EvaluateClassification(
                null,
                "Футболка",
                "KNITTED",
                "GIRLS",
                80,
                "Хлопок");

            Assert.NotNull(result.CandidateCode);
            Assert.StartsWith("6111", result.CandidateCode);
            Assert.True(result.IsBabyBranch);
        }

        [Fact]
        public void AT10_ShopDocuments_ShopIsolationAndPackageManagement()
        {
            DatabaseInitializer.InitializeDatabase();
            var repo = new ShopDocumentRepository();
            var vm = new DocumentsViewModel(null, repo);

            // Verify initial shops
            Assert.True(vm.Shops.Count >= 2);

            // Select Shop 1
            vm.SelectedShop = vm.Shops.First(s => s.ShopId == "SHOP_01");
            int shop1PackagesCount = vm.Packages.Count;
            Assert.True(shop1PackagesCount >= 1);

            // Select Shop 2
            vm.SelectedShop = vm.Shops.First(s => s.ShopId == "SHOP_02");
            // Verify Shop 2 isolation
            Assert.All(vm.Packages, p => Assert.Equal("SHOP_02", p.ShopId));

            // Create new package for Shop 2
            vm.FormPackageName = "Hồ sơ Váy Nữ Shop 2";
            vm.FormDocType = "Декларация соответствия";
            vm.FormDocNumber = "ЕАЭС N RU Д-RU.РА02.В.99999/26";
            vm.FormStartDate = new DateTime(2026, 1, 1);
            vm.FormEndDate = new DateTime(2029, 1, 1);
            vm.FormIsEndless = false;
            vm.FormTargetScope = DocumentApplyScope.AllShop;
            vm.FormAutoApplyOnCreate = true;

            Assert.True(vm.ValidateForm());
            vm.SavePackage();

            // Verify package was saved for Shop 2 only
            var shop2Packages = repo.GetPackagesByShop("SHOP_02");
            Assert.Contains(shop2Packages, p => p.DocNumber == "ЕАЭС N RU Д-RU.РА02.В.99999/26");

            var shop1Packages = repo.GetPackagesByShop("SHOP_01");
            Assert.DoesNotContain(shop1Packages, p => p.DocNumber == "ЕАЭС N RU Д-RU.РА02.В.99999/26");
        }

        [Fact]
        public void AT10_B_ShopDocuments_DuplicateDetectionAndPreservation()
        {
            var updateService = new ShopDocumentUpdateService(new WbApiClient(""), new ShopDocumentRepository());

            var pkg = new ShopDocumentPackage
            {
                DocType = "Декларация соответствия",
                DocNumber = "ЕАЭС N RU Д-RU.РА01.В.77777/26",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2029, 1, 1),
                IsEndless = false
            };

            // 1. Card without documents
            var cardEmpty = new WbCardItem
            {
                NmId = 1001,
                VendorCode = "DRESS-01",
                Title = "Платье летнее",
                Description = "Описание платья",
                Brand = "MyBrand"
            };

            var (dup1, conf1) = ShopDocumentUpdateService.CheckExistingDocument(cardEmpty, pkg);
            Assert.False(dup1);
            Assert.False(conf1);

            // 2. Card already has exact same document
            var cardWithSameDoc = new WbCardItem
            {
                NmId = 1002,
                VendorCode = "DRESS-02",
                Documents = new WbCardDocumentsContainer
                {
                    Items = new List<WbCardDocumentItem>
                    {
                        new WbCardDocumentItem
                        {
                            Type = "Декларация соответствия",
                            Number = "ЕАЭС N RU Д-RU.РА01.В.77777/26",
                            StartDate = "01.01.2026",
                            EndDate = "01.01.2029"
                        }
                    }
                }
            };

            var (dup2, conf2) = ShopDocumentUpdateService.CheckExistingDocument(cardWithSameDoc, pkg);
            Assert.True(dup2);
            Assert.False(conf2);

            // 3. Card with same document number but conflict date
            var cardWithConflictDate = new WbCardItem
            {
                NmId = 1003,
                VendorCode = "DRESS-03",
                Documents = new WbCardDocumentsContainer
                {
                    Items = new List<WbCardDocumentItem>
                    {
                        new WbCardDocumentItem
                        {
                            Type = "Декларация соответствия",
                            Number = "ЕАЭС N RU Д-RU.РА01.В.77777/26",
                            StartDate = "01.01.2024",
                            EndDate = "01.01.2025"
                        }
                    }
                }
            };

            var (dup3, conf3) = ShopDocumentUpdateService.CheckExistingDocument(cardWithConflictDate, pkg);
            Assert.False(dup3);
            Assert.True(conf3);

            // 4. Verify 100% data preservation
            var updatedPayload = updateService.PrepareCardWithDocument(cardEmpty, pkg, replaceExisting: false);
            Assert.Equal(cardEmpty.NmId, updatedPayload.NmId);
            Assert.Equal(cardEmpty.VendorCode, updatedPayload.VendorCode);
            Assert.Equal(cardEmpty.Title, updatedPayload.Title);
            Assert.Equal(cardEmpty.Description, updatedPayload.Description);
            Assert.Equal(cardEmpty.Brand, updatedPayload.Brand);
            Assert.NotNull(updatedPayload.Documents);
            Assert.Single(updatedPayload.Documents.Items);
            Assert.Equal("ЕАЭС N RU Д-RU.РА01.В.77777/26", updatedPayload.Documents.Items[0].Number);
        }

        [Fact]
        public void AT10_C_CardBuilder_AutoApplyDocumentOnCreate()
        {
            DatabaseInitializer.InitializeDatabase();
            var docRepo = new ShopDocumentRepository();
            var repo = new MatrixRepository();
            var selector = new TnvedSelectorService(repo);

            var vm = new CardBuilderViewModel(repo, selector, null, docRepo);

            // Should load shops and default auto-apply package
            Assert.NotNull(vm.SelectedShop);
            Assert.NotNull(vm.SelectedDocumentPackage);
            Assert.NotEmpty(vm.SelectedDocumentNumber);
            Assert.Contains(vm.SelectedDocumentNumber, vm.GeneratedPayloadJson);
            Assert.Contains("documents", vm.GeneratedPayloadJson);
        }

        [Fact]
        public async Task AT10_D_ShopDocument_RetryFailedCardsOnly()
        {
            DatabaseInitializer.InitializeDatabase();
            var docRepo = new ShopDocumentRepository();
            var mockApi = new MockApiForDocTests();
            var vm = new DocumentsViewModel(mockApi, docRepo);

            // Seed reports: 1 success, 1 duplicate, 1 error
            vm.ReportItems.Clear();
            vm.ReportItems.Add(new CardDocumentReportItem
            {
                NmId = 1,
                VendorCode = "CARD-01",
                WriteStatus = "Đã ghi trên WB",
                WbCheckStatus = "WB chấp nhận"
            });
            vm.ReportItems.Add(new CardDocumentReportItem
            {
                NmId = 2,
                VendorCode = "CARD-02",
                WriteStatus = "Đã có giấy tờ trùng",
                WbCheckStatus = "WB chấp nhận"
            });
            vm.ReportItems.Add(new CardDocumentReportItem
            {
                NmId = 3,
                VendorCode = "CARD-03",
                WriteStatus = "Lỗi cập nhật",
                ErrorMessage = "WB API 500 error"
            });

            Assert.Equal(1, vm.ErrorReportsCount);

            // Retry should only reprocess error card 3
            await vm.RetryFailedCardsAsync();
            var card3 = vm.ReportItems.First(r => r.NmId == 3);
            Assert.Equal("Đã ghi trên WB", card3.WriteStatus);

            // Other cards remain untouched
            Assert.Equal("Đã ghi trên WB", vm.ReportItems.First(r => r.NmId == 1).WriteStatus);
            Assert.Equal("Đã có giấy tờ trùng", vm.ReportItems.First(r => r.NmId == 2).WriteStatus);
        }

        [Fact]
        public void AT11_12StepWizard_PreflightCheckFlagsMissingBarcodes()
        {
            DatabaseInitializer.InitializeDatabase();
            var repo = new MatrixRepository();
            var selector = new TnvedSelectorService(repo);
            var vm = new CardBuilderViewModel(repo, selector, null);

            vm.Sizes.Clear();
            vm.Sizes.Add(new ViewModels.ProductSizeItem { TechSize = "S", RussianSize = "42-44", Sku = "" });
            vm.RunPreflightCheck();

            Assert.Contains(vm.PreflightIssues, i => i.Severity == IssueSeverity.BLOCK && i.RuleId == "R012");
        }

        [Fact]
        public void AT12_FileImportExportService_PreservesLeadingZerosOnDelimitedFile()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"test_import_{Guid.NewGuid():N}.csv");
            try
            {
                string csvContent = "vendorCode,title,subjectName,tnved,gender,material,barcode,price\n" +
                                    "TEST-001,Футболка женская,Футболки,0101210000,Женский,Хлопок,04601234567893,1200\n" +
                                    "=cmd|' /C calc'!A0,Брюки,Брюки,6109100000,Мужской,Хлопок,04601234567893,1500";
                File.WriteAllText(tempFile, csvContent, System.Text.Encoding.UTF8);

                var result = FileImportExportService.ImportFromDelimitedText(tempFile);
                Assert.True(result.Success);
                Assert.Equal(2, result.ImportedCards.Count);

                // Leading zero preservation
                Assert.Equal("0101210000", result.ImportedCards[0].CurrentTnved);

                // Formula injection defense
                Assert.False(result.ImportedCards[1].VendorCode.StartsWith("="));
                Assert.StartsWith("'", result.ImportedCards[1].VendorCode);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }

    public class MockApiForDocTests : IWbApiClient
    {
        public bool HasApiKey => true;
        public void UpdateConfiguration(string apiKey, string baseUrl, int rateLimitDelayMs) { }
        public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<List<WbCardItem>> GetAllCardsAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            var list = new List<WbCardItem>
            {
                new WbCardItem { NmId = 1, VendorCode = "CARD-01", Title = "Áo Thun" },
                new WbCardItem { NmId = 2, VendorCode = "CARD-02", Title = "Quần Jean" },
                new WbCardItem { NmId = 3, VendorCode = "CARD-03", Title = "Váy Nữ" }
            };
            return Task.FromResult(list);
        }
        public Task<(bool Success, string Message)> UpdateCardsBatchAsync(List<WbCardItem> cardsToUpdate, CancellationToken cancellationToken = default) => Task.FromResult((true, "Updated"));
        public Task<List<WbCardErrorItem>> GetCardErrorsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<WbCardErrorItem>());
        public Task<List<Models.WbSubjectItem>> GetSubjectsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<Models.WbSubjectItem>());
        public Task<List<WbDirectoryTnvedItem>> GetTnvedDirectoryAsync(int? subjectId = null, string? search = null, CancellationToken cancellationToken = default) => Task.FromResult(new List<WbDirectoryTnvedItem>());
        public Task<List<WbDirectoryTnvedItem>> GetAllTnvedDirectoryAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<WbDirectoryTnvedItem>());
        public Task<List<string>> GenerateBarcodesAsync(int count = 1, CancellationToken cancellationToken = default) => Task.FromResult(new List<string> { "2000000000001" });
        public Task<(bool Success, string Message, string RawResponse)> UploadCardsAsync(object cardUploadPayload, CancellationToken cancellationToken = default) => Task.FromResult((true, "Created", "{}"));
        public Task<(bool Success, string Message)> UploadMediaFileAsync(long nmId, int photoNumber, string fileName, byte[] content, CancellationToken cancellationToken = default) => Task.FromResult((true, "Uploaded"));
        public Task<(bool Success, string Message)> UploadMediaLinksAsync(long nmId, List<string> links, CancellationToken cancellationToken = default) => Task.FromResult((true, "Uploaded"));
    }
}
