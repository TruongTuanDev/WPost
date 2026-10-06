using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class CardBulkUpdateService
    {
        private readonly IWbApiClient _apiClient;

        public CardBulkUpdateService(IWbApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<int> ExecuteBulkFixAsync(
            List<AuditResultItem> itemsToFix,
            int batchSize = 50,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (itemsToFix == null || itemsToFix.Count == 0) return 0;

            int totalSuccess = 0;
            int total = itemsToFix.Count;
            int currentBatchIndex = 0;

            for (int i = 0; i < total; i += batchSize)
            {
                if (cancellationToken.IsCancellationRequested) break;

                currentBatchIndex++;
                var batchItems = itemsToFix.Skip(i).Take(batchSize).ToList();
                progress?.Report($"Đang cập nhật nhóm {currentBatchIndex} ({i + 1}-{Math.Min(i + batchSize, total)} / {total} sản phẩm)...");

                var preparedCards = new List<WbCardItem>();
                foreach (var auditItem in batchItems)
                {
                    var updatedCard = PrepareUpdatedCard(auditItem.Card, auditItem.SuggestedTnved, auditItem.SuggestedGender);
                    preparedCards.Add(updatedCard);
                }

                var (success, message) = await _apiClient.UpdateCardsBatchAsync(preparedCards, cancellationToken);

                foreach (var auditItem in batchItems)
                {
                    if (success)
                    {
                        auditItem.Status = AuditStatus.UpdatedSuccess;
                        auditItem.StatusMessage = "Cập nhật thành công lên Wildberries.";
                        auditItem.IsSelected = false;
                        totalSuccess++;
                    }
                    else
                    {
                        auditItem.Status = AuditStatus.UpdateFailed;
                        auditItem.StatusMessage = $"Lỗi: {message}";
                    }
                }
            }

            progress?.Report($"Đã hoàn tất đồng bộ! Thành công: {totalSuccess}/{total} sản phẩm.");
            return totalSuccess;
        }

        public static WbCardItem PrepareUpdatedCard(WbCardItem original, string newTnved, string newGender)
        {
            // Serialize and deserialize to ensure deep clone without touching original memory
            var json = JsonSerializer.Serialize(original);
            var cloned = JsonSerializer.Deserialize<WbCardItem>(json) ?? new WbCardItem();

            // Guarantee characteristics list is initialized
            cloned.Characteristics ??= new List<WbCharacteristic>();

            // 1. Update or Add TNVED (id: 5)
            var tnvedCharc = cloned.Characteristics.FirstOrDefault(c => 
                c.Id == 5 || (c.Name != null && c.Name.Equals("ТНВЭД", StringComparison.OrdinalIgnoreCase)));
            
            if (tnvedCharc != null)
            {
                tnvedCharc.Value = newTnved;
                if (tnvedCharc.Id == 0) tnvedCharc.Id = 5;
            }
            else
            {
                cloned.Characteristics.Add(new WbCharacteristic
                {
                    Id = 5,
                    Name = "ТНВЭД",
                    Value = newTnved
                });
            }

            // 2. Update or Add Gender (id: 8)
            if (!string.IsNullOrWhiteSpace(newGender))
            {
                var genderCharc = cloned.Characteristics.FirstOrDefault(c => 
                    c.Id == 8 || (c.Name != null && c.Name.Equals("Пол", StringComparison.OrdinalIgnoreCase)));

                if (genderCharc != null)
                {
                    // If original was array of strings, keep as array or string
                    if (genderCharc.Value is JsonElement elem && elem.ValueKind == JsonValueKind.Array)
                    {
                        genderCharc.Value = new[] { newGender };
                    }
                    else
                    {
                        genderCharc.Value = newGender;
                    }
                    if (genderCharc.Id == 0) genderCharc.Id = 8;
                }
                else
                {
                    cloned.Characteristics.Add(new WbCharacteristic
                    {
                        Id = 8,
                        Name = "Пол",
                        Value = newGender
                    });
                }
            }

            return cloned;
        }
    }
}
