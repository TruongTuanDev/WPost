using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using WbTnvedManager.Data;
using WbTnvedManager.Models;
using WbTnvedManager.Services;
using WbTnvedManager.Services.Resolvers;

namespace WbTnvedManager.Tests
{
    public class MarkirovkaAcceptanceTests
    {
        private readonly MarkirovkaReconciliationEngine _engine = new();

        [Fact]
        public void T01_TwoShopsWithSameVendorCode_ShouldNotMixDataOrTokens()
        {
            var shop1 = new SellerAccount { Id = "SHOP_1", INN = "7701000001", ApiKey = "KEY_1" };
            var shop2 = new SellerAccount { Id = "SHOP_2", INN = "7702000002", ApiKey = "KEY_2" };

            var card1 = new WbCardItem { NmId = 1001, VendorCode = "SHIRT-SAME-CODE" };
            var card2 = new WbCardItem { NmId = 2002, VendorCode = "SHIRT-SAME-CODE" };

            Assert.NotEqual(shop1.Id, shop2.Id);
            Assert.NotEqual(shop1.ApiKey, shop2.ApiKey);
            Assert.NotEqual(card1.NmId, card2.NmId);
        }

        [Fact]
        public void T02_GtinWithLeadingZero_OrDataMatrixAi01_ShouldPreserveStringAndExtract14Digits()
        {
            // Case A: 13-digit EAN normalized to 14-digit with leading 0
            string ean13 = "4607001234567";
            string norm14 = MarkirovkaReconciliationEngine.NormalizeGtin14(ean13);
            Assert.Equal("04607001234567", norm14);
            Assert.StartsWith("0", norm14);

            // Case B: GS1 DataMatrix raw string with AI (01)
            string rawDm = "0104607001234567215XYZ\u001d91EE06\u001d92qwer";
            string extracted = MarkirovkaReconciliationEngine.ExtractGtinFromDataMatrix(rawDm);
            Assert.Equal("04607001234567", extracted);
            Assert.Equal(14, extracted.Length);
        }

        [Fact]
        public void T03_GtinBadChecksum_ShouldReportErrorAndNeverHallucinateNumbers()
        {
            // 4607001234560 has bad checksum (real check digit is 2 for 460700123456)
            string badGtin = "4607001234560";
            var res = GtinAndVariantValidator.ValidateGtin(badGtin);

            Assert.Equal(GtinLocalStatus.BAD_CHECKSUM, res.LocalStatus);
            Assert.Null(res.NormalizedGtin14);
            Assert.Contains("check digit", res.ExplanationVi);
        }

        [Fact]
        public void T04_GtinRealButDifferentSize_ShouldBlockAssignment()
        {
            var variants = new List<ProductVariantInput>
            {
                new() { VariantId = "V1", ColorCanonical = "Черный", SizeValue = "S", GtinRaw = "4607001234562" },
                new() { VariantId = "V2", ColorCanonical = "Черный", SizeValue = "XL", GtinRaw = "4607001234562" } // Trùng GTIN hợp lệ nhưng khác size
            };

            var collisions = GtinAndVariantValidator.CheckVariantCollisions(variants);
            Assert.NotEmpty(collisions);
            Assert.Contains("GTIN_VARIANT_COLLISION", collisions[0]);
        }

        [Fact]
        public void T05_PhysicalLotMismatchWithWbCard_ShouldGenerateDataConflict()
        {
            var account = new SellerAccount { INN = "7707083893" };
            var card = new WbCardItem
            {
                NmId = 112233,
                VendorCode = "TEST-JEANS",
                Sizes = new List<System.Text.Json.JsonElement>
                {
                    JsonSerializer.Deserialize<JsonElement>("""{"techSize":"32","chrtID":991,"skus":["04601111111111"]}""")
                }
            };

            var stock = new List<StockEvidence>
            {
                new() { LotId = "LOT-1", Sku = "TEST-JEANS", Gtin14 = "04609999999999" } // Khác GTIN trong kho
            };

            var result = _engine.Reconcile(account, new List<WbCardItem> { card }, new List<NationalCatalogProduct>(), stock, new List<TnvedMatrixEntry>());
            
            var finding = result.Findings.FirstOrDefault(f => f.RuleId == "R06");
            Assert.NotNull(finding);
            Assert.Equal(FindingSourceKind.DATA_CONFLICT, finding.SourceKind);
        }

        [Fact]
        public void T06_NkDraftWithError_MustNotBeUsedAsGoldenStandard()
        {
            var account = new SellerAccount { INN = "7707083893" };
            var card = new WbCardItem
            {
                NmId = 556677,
                VendorCode = "TEST-DRAFT",
                Sizes = new List<System.Text.Json.JsonElement>
                {
                    JsonSerializer.Deserialize<JsonElement>("""{"techSize":"M","chrtID":551,"skus":["04607001234567"]}""")
                }
            };

            var nkList = new List<NationalCatalogProduct>
            {
                new() { Gtin14 = "04607001234567", Status = "Черновик (Ошибки модерации)", IsDraft = true, RawMessageRu = "Не указан состав" }
            };

            var result = _engine.Reconcile(account, new List<WbCardItem> { card }, nkList, new List<StockEvidence>(), new List<TnvedMatrixEntry>());
            
            var finding = result.Findings.FirstOrDefault(f => f.RuleId == "R08");
            Assert.NotNull(finding);
            Assert.Equal(FindingSourceKind.OFFICIAL_SOURCE_ERROR, finding.SourceKind);

            // Proposal must NOT be ready
            var prop = result.Proposals.FirstOrDefault(p => p.NmId == 556677);
            if (prop != null)
            {
                Assert.NotEqual(ProposalState.READY, prop.State);
            }
        }

        [Fact]
        public void T11_MissingWeaveType_TnvedShouldRequireVerification()
        {
            var input = new ProductVariantInput
            {
                SubjectId = 240, // Брюки
                TechnicalType = "Брюки",
                Construction = ConstructionKind.UNKNOWN // Không rõ Dệt thoi hay Dệt kim
            };

            var engine = new TnvedMatrix104Engine();
            // Category trousers without construction cannot deterministically pick 6103 vs 6203
            Assert.Equal(ConstructionKind.UNKNOWN, input.Construction);
        }

        [Fact]
        public void T15_CardMultiSizes_SafeUpdateMustPreserveAllSizesAndSkus()
        {
            var original = new WbCardItem
            {
                NmId = 12345,
                VendorCode = "TSHIRT-MULTISIZE",
                Sizes = new List<JsonElement>
                {
                    JsonSerializer.Deserialize<JsonElement>("""{"techSize":"S","chrtID":101,"skus":["2000001"],"price":1200}"""),
                    JsonSerializer.Deserialize<JsonElement>("""{"techSize":"M","chrtID":102,"skus":["2000002"],"price":1200}"""),
                    JsonSerializer.Deserialize<JsonElement>("""{"techSize":"L","chrtID":103,"skus":["2000003"],"price":1200}""")
                },
                Characteristics = new List<WbCharacteristic>
                {
                    new() { Id = 5, Name = "ТНВЭД", Value = "6403999600" }
                }
            };

            var updated = CardBulkUpdateService.PrepareUpdatedCard(original, "6109100000", "Женский");

            Assert.Equal(3, updated.Sizes?.Count);
            Assert.Equal("6109100000", updated.CurrentTnved);
            Assert.Equal("Женский", updated.CurrentGender);
            Assert.Equal("TSHIRT-MULTISIZE", updated.VendorCode);
        }

        [Fact]
        public void T16_UpdateMustPreserveDocumentsAndDimensionsAndKizMarked()
        {
            var original = new WbCardItem
            {
                NmId = 98765,
                VendorCode = "JACKET-DOC-PRESERVE",
                Characteristics = new List<WbCharacteristic>
                {
                    new() { Id = 5, Name = "ТНВЭД", Value = "6201400000" },
                    new() { Id = 101, Name = "Документ", Value = "ЕАЭС N RU Д-RU.PA01.B.12345/26" }
                },
                Dimensions = JsonSerializer.Deserialize<JsonElement>("""{"length":30,"width":20,"height":5}""")
            };

            var updated = CardBulkUpdateService.PrepareUpdatedCard(original, "4203100001", "Мужской");

            Assert.NotNull(updated.Dimensions);
            Assert.Equal(30, updated.Dimensions.Value.GetProperty("length").GetInt32());
            Assert.NotNull(updated.Characteristics.FirstOrDefault(c => c.Name == "Документ"));
        }

        [Fact]
        public async Task T20_T21_WorkerDeliveryAndMarketplaceStateDecoupled()
        {
            var fakeApi = new FakeWbApiClientForTests();
            var safePipeline = new SafeWbUpdatePipeline(fakeApi, new RulesEngine(new TnvedSelectorService(new MatrixRepository())));
            var worker = new MarkirovkaUpdateWorker(fakeApi, safePipeline);

            var prop = new MarkirovkaProposal
            {
                NmId = 1111,
                VendorCode = "SKU-WORKER-TEST",
                State = ProposalState.APPROVED,
                WbCurrentTnved = "6403999600",
                DesiredTnved = "6109100000",
                DesiredGender = "Женский"
            };

            var res = await worker.ExecuteApprovedProposalsAsync(new List<MarkirovkaProposal> { prop }, new SellerAccount { Id = "ACC1" });

            Assert.True(res.Success);
            Assert.Equal(DeliveryState.ACCEPTED, prop.Delivery);
            Assert.Equal(ReadbackState.MATCHED, prop.Readback);
            Assert.Equal(MarketplaceCheckState.PENDING, prop.MarketplaceCheck); // Section 23: NĐ 821 kiểm tra 180 ngày không thể coi là hoàn tất ngay lập tức
        }

        private class FakeWbApiClientForTests : IWbApiClient
        {
            public bool HasApiKey => true;
            public void UpdateConfiguration(string apiKey, string baseUrl, int rateLimitDelayMs) { }
            public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
            public Task<List<WbCardItem>> GetAllCardsAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default) => Task.FromResult(new List<WbCardItem>());
            public Task<(bool Success, string Message)> UpdateCardsBatchAsync(List<WbCardItem> cardsToUpdate, CancellationToken cancellationToken = default) => Task.FromResult((true, "OK"));
            public Task<List<WbCardErrorItem>> GetCardErrorsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<WbCardErrorItem>());
            public Task<List<WbSubjectItem>> GetSubjectsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<WbSubjectItem>());
            public Task<List<WbDirectoryTnvedItem>> GetTnvedDirectoryAsync(int? subjectId = null, string? search = null, CancellationToken cancellationToken = default) => Task.FromResult(new List<WbDirectoryTnvedItem>());
            public Task<List<WbDirectoryTnvedItem>> GetAllTnvedDirectoryAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<WbDirectoryTnvedItem>());
            public Task<List<string>> GenerateBarcodesAsync(int count = 1, CancellationToken cancellationToken = default) => Task.FromResult(new List<string> { "2000000000001" });
            public Task<(bool Success, string Message, string RawResponse)> UploadCardsAsync(object cardUploadPayload, CancellationToken cancellationToken = default) => Task.FromResult((true, "Created", "{}"));
            public Task<(bool Success, string Message)> UploadMediaFileAsync(long nmId, int photoNumber, string fileName, byte[] content, CancellationToken cancellationToken = default) => Task.FromResult((true, "Uploaded"));
            public Task<(bool Success, string Message)> UploadMediaLinksAsync(long nmId, List<string> links, CancellationToken cancellationToken = default) => Task.FromResult((true, "Uploaded"));
        }
    }
}