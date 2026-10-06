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
        public void AT10_ConformityDocuments_ValidationAndExpiryCheck()
        {
            var vm = new DocumentsViewModel();
            Assert.True(vm.Documents.Count >= 3);

            // Test expired document
            var expiredDoc = new ConformityDocument
            {
                Id = "DOC-EXP",
                Type = ConformityDocType.DS,
                ExactNumber = "EXP-999",
                StartDate = DateTime.Today.AddYears(-3),
                EndDate = DateTime.Today.AddDays(-1),
                IsEndless = false
            };
            vm.Documents.Add(expiredDoc);
            vm.StatusFilter = "Đã hết hạn";
            Assert.Contains(vm.FilteredDocuments, d => d.ExactNumber == "EXP-999");
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
}
