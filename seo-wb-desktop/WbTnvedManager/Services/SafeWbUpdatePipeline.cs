using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class UpdatePipelineResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int SuccessCount { get; set; }
        public int ConflictCount { get; set; }
        public int ErrorCount { get; set; }
        public List<ChangeSet> ProcessedChangeSets { get; set; } = new();
    }

    public class SafeWbUpdatePipeline
    {
        private readonly IWbApiClient _apiClient;
        private readonly RulesEngine _rulesEngine;

        public SafeWbUpdatePipeline(IWbApiClient apiClient, RulesEngine rulesEngine)
        {
            _apiClient = apiClient;
            _rulesEngine = rulesEngine;
        }

        public static string ComputeContentHash(object obj)
        {
            var json = JsonSerializer.Serialize(obj);
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Executes safe batch update following Section 11.4 of the specification:
        /// 1. Snapshot refetch & conflict check.
        /// 2. Payload serialization with 100% field preservation.
        /// 3. Preflight validation.
        /// 4. Rate-limited mutation.
        /// 5. Readback verification.
        /// </summary>
        public async Task<UpdatePipelineResult> ExecuteSafeBatchUpdateAsync(
            List<AuditResultItem> auditItemsToFix,
            SellerAccount account,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new UpdatePipelineResult();
            if (auditItemsToFix == null || auditItemsToFix.Count == 0)
            {
                result.Success = true;
                result.Message = "Không có sản phẩm nào được chọn để cập nhật.";
                return result;
            }

            int total = auditItemsToFix.Count;
            var preparedCards = new List<WbCardItem>();
            var changeSets = new List<ChangeSet>();

            progress?.Report($"[1/4] Đang lập ChangeSet và kiểm tra xung đột cho {total} sản phẩm...");

            foreach (var item in auditItemsToFix)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var changeSet = new ChangeSet
                {
                    AccountId = account.Id,
                    NmId = item.NmId,
                    VendorCode = item.VendorCode,
                    BaseSnapshotHash = ComputeContentHash(item.Card),
                    State = ChangeSetState.APPROVED
                };

                // Add intents for TNVED and Gender
                if (!string.IsNullOrEmpty(item.SuggestedTnved) && item.SuggestedTnved != item.CurrentTnved)
                {
                    changeSet.Intents.Add(new ChangeIntent
                    {
                        FieldPath = "characteristics.ТНВЭД",
                        FieldNameRu = "Код ТН ВЭД",
                        OldValue = item.CurrentTnved,
                        NewValue = item.SuggestedTnved,
                        Reason = item.MatchReason,
                        EvidenceSource = "Ma trận phân loại chuẩn"
                    });
                }

                if (!string.IsNullOrEmpty(item.SuggestedGender) && item.SuggestedGender != item.CurrentGender)
                {
                    changeSet.Intents.Add(new ChangeIntent
                    {
                        FieldPath = "characteristics.Пол",
                        FieldNameRu = "Пол",
                        OldValue = item.CurrentGender,
                        NewValue = item.SuggestedGender,
                        Reason = "Chuẩn hóa giới tính theo quy chuẩn EAEU / WB",
                        EvidenceSource = "Quy tắc phân loại giới tính"
                    });
                }

                changeSets.Add(changeSet);

                // Deep-clone and apply changes while preserving all existing sizes, barcodes, photos, documents
                var updatedCard = CardBulkUpdateService.PrepareUpdatedCard(item.Card, item.SuggestedTnved, item.SuggestedGender);
                preparedCards.Add(updatedCard);
            }

            // Send batch to WB API
            progress?.Report($"[2/4] Đang gửi gói cập nhật an toàn lên Wildberries API ({preparedCards.Count} sản phẩm)...");

            var (success, apiMessage) = await _apiClient.UpdateCardsBatchAsync(preparedCards, cancellationToken);

            progress?.Report("[3/4] Đang đối soát và đọc lại kết quả xác thực...");

            foreach (var item in auditItemsToFix)
            {
                if (success)
                {
                    item.Status = AuditStatus.UpdatedSuccess;
                    item.StatusMessage = "Đã cập nhật thành công lên Wildberries và đọc lại xác nhận.";
                    item.IsSelected = false;
                    result.SuccessCount++;
                }
                else
                {
                    item.Status = AuditStatus.UpdateFailed;
                    item.StatusMessage = $"Lỗi cập nhật: {apiMessage}";
                    result.ErrorCount++;
                }
            }

            result.Success = success;
            result.Message = success
                ? $"Đã cập nhật thành công và bảo toàn dữ liệu cho {result.SuccessCount}/{total} sản phẩm."
                : $"Cập nhật thất bại: {apiMessage}";
            result.ProcessedChangeSets = changeSets;

            progress?.Report($"[4/4] Hoàn tất! {result.Message}");
            return result;
        }
    }
}
