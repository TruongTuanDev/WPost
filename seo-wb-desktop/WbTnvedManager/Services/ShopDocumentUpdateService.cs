using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WbTnvedManager.Data;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class ShopDocumentUpdateService
    {
        private readonly IWbApiClient _apiClient;
        private readonly ShopDocumentRepository _repository;

        public ShopDocumentUpdateService(IWbApiClient apiClient, ShopDocumentRepository? repository = null)
        {
            _apiClient = apiClient;
            _repository = repository ?? new ShopDocumentRepository();
        }

        public DocumentPreviewSummary PreviewApply(
            List<WbCardItem> allCards,
            ShopDocumentPackage package,
            bool replaceExisting = false,
            List<long>? selectedNmIds = null)
        {
            var summary = new DocumentPreviewSummary();
            if (allCards == null || allCards.Count == 0 || package == null)
            {
                return summary;
            }

            var cleanTargetNum = package.DocNumber?.Trim() ?? string.Empty;
            var targetType = package.DocType?.Trim() ?? "Декларация соответствия";

            foreach (var card in allCards)
            {
                if (!IsCardInScope(card, package, selectedNmIds))
                {
                    continue;
                }

                summary.TotalCards++;
                var (hasExactDuplicate, hasConflictingDate) = CheckExistingDocument(card, targetType, cleanTargetNum, package);

                if (hasExactDuplicate && !replaceExisting)
                {
                    summary.AlreadyHasDuplicateCount++;
                }
                else if (hasConflictingDate && !replaceExisting)
                {
                    summary.NeedsAttentionCount++;
                }
                else
                {
                    summary.WillUpdateCount++;
                }
            }

            return summary;
        }

        public async Task<List<CardDocumentReportItem>> ExecuteApplyAsync(
            List<WbCardItem> cardsToProcess,
            ShopProfile shop,
            ShopDocumentPackage package,
            bool replaceExisting = false,
            int batchSize = 50,
            IProgress<(int Current, int Total, string StatusMessage)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var reports = new List<CardDocumentReportItem>();
            if (cardsToProcess == null || cardsToProcess.Count == 0 || package == null)
            {
                return reports;
            }

            var cleanDocNumber = package.DocNumber?.Trim() ?? string.Empty;
            var docType = package.DocType?.Trim() ?? "Декларация соответствия";

            // Initial report creation
            foreach (var card in cardsToProcess)
            {
                var curDocStr = ExtractCurrentDocSummary(card);
                reports.Add(new CardDocumentReportItem
                {
                    NmId = card.NmId,
                    VendorCode = card.VendorCode,
                    Title = card.Title,
                    SubjectName = card.SubjectName,
                    CurrentDocInfo = curDocStr,
                    AppliedDocNumber = cleanDocNumber,
                    WriteStatus = "Chờ gửi",
                    WbCheckStatus = "Chưa có thông tin kiểm tra từ WB"
                });
            }

            int total = cardsToProcess.Count;
            int processed = 0;

            for (int i = 0; i < total; i += batchSize)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var batchCards = cardsToProcess.Skip(i).Take(batchSize).ToList();
                var batchReports = reports.Skip(i).Take(batchSize).ToList();

                var cardsToUpdateWb = new List<WbCardItem>();
                var correspondingReports = new List<CardDocumentReportItem>();

                for (int b = 0; b < batchCards.Count; b++)
                {
                    var card = batchCards[b];
                    var report = batchReports[b];

                    report.WriteStatus = "Đang gửi";

                    var (hasExactDuplicate, hasConflictingDate) = CheckExistingDocument(card, docType, cleanDocNumber, package);

                    if (hasExactDuplicate && !replaceExisting)
                    {
                        report.WriteStatus = "Đã có giấy tờ trùng";
                        report.ErrorMessage = "WB đã có cùng loại và số giấy tờ này. Đã bỏ qua thêm trùng.";
                        _repository.SaveLog(shop.ShopId, card.NmId, card.VendorCode, cleanDocNumber, report.WriteStatus, report.WbCheckStatus, report.ErrorMessage);
                        continue;
                    }

                    if (hasConflictingDate && !replaceExisting)
                    {
                        report.WriteStatus = "Cần bổ sung cấu hình";
                        report.ErrorMessage = "Trùng số giấy tờ nhưng khác ngày hiệu lực. Hãy chọn chế độ thay thế để cập nhật.";
                        _repository.SaveLog(shop.ShopId, card.NmId, card.VendorCode, cleanDocNumber, report.WriteStatus, report.WbCheckStatus, report.ErrorMessage);
                        continue;
                    }

                    // Prepare updated card preserving all other data
                    var updatedCard = PrepareCardWithDocument(card, package, replaceExisting);
                    cardsToUpdateWb.Add(updatedCard);
                    correspondingReports.Add(report);
                }

                if (cardsToUpdateWb.Count > 0)
                {
                    var (success, apiMsg) = await _apiClient.UpdateCardsBatchAsync(cardsToUpdateWb, cancellationToken);

                    for (int k = 0; k < correspondingReports.Count; k++)
                    {
                        var report = correspondingReports[k];
                        var updatedCard = cardsToUpdateWb[k];

                        if (success)
                        {
                            report.WriteStatus = "Đã ghi trên WB";
                            report.WbCheckStatus = updatedCard.Documents?.OverallVerdict ?? "WB đang kiểm tra";
                            report.ErrorMessage = string.Empty;
                        }
                        else
                        {
                            report.WriteStatus = "Lỗi cập nhật";
                            report.ErrorMessage = apiMsg;
                        }

                        _repository.SaveLog(shop.ShopId, report.NmId, report.VendorCode, cleanDocNumber, report.WriteStatus, report.WbCheckStatus, report.ErrorMessage);
                    }
                }

                processed += batchCards.Count;
                progress?.Report((processed, total, $"Đã xử lý {processed}/{total} thẻ sản phẩm"));
            }

            return reports;
        }

        public WbCardItem PrepareCardWithDocument(WbCardItem original, ShopDocumentPackage package, bool replaceExisting)
        {
            var json = JsonSerializer.Serialize(original);
            var cloned = JsonSerializer.Deserialize<WbCardItem>(json) ?? new WbCardItem();

            cloned.Documents ??= new WbCardDocumentsContainer();
            cloned.Documents.Items ??= new List<WbCardDocumentItem>();

            var cleanDocNumber = package.DocNumber?.Trim() ?? string.Empty;
            var docType = package.DocType?.Trim() ?? "Декларация соответствия";

            if (replaceExisting)
            {
                cloned.Documents.Items.RemoveAll(d => 
                    d.Number.Equals(cleanDocNumber, StringComparison.OrdinalIgnoreCase) && 
                    d.Type.Equals(docType, StringComparison.OrdinalIgnoreCase));
            }

            var newDoc = new WbCardDocumentItem
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                Type = docType,
                Number = cleanDocNumber,
                StartDate = package.StartDate.ToString("dd.MM.yyyy"),
                EndDate = package.IsEndless ? null : package.EndDate.ToString("dd.MM.yyyy"),
                IsEndless = package.IsEndless,
                Verdict = "Checking"
            };

            cloned.Documents.Items.Add(newDoc);
            cloned.Documents.ExcludeDocuments = false;

            return cloned;
        }

        public static (bool HasExactDuplicate, bool HasConflictingDate) CheckExistingDocument(
            WbCardItem card,
            ShopDocumentPackage target)
        {
            return CheckExistingDocument(card, target.DocType, target.DocNumber, target);
        }

        public static (bool HasExactDuplicate, bool HasConflictingDate) CheckExistingDocument(
            WbCardItem card,
            string docType,
            string docNumber,
            ShopDocumentPackage target)
        {
            if (card.Documents?.Items == null || card.Documents.Items.Count == 0)
            {
                return (false, false);
            }

            var cleanNum = docNumber.Trim();
            foreach (var d in card.Documents.Items)
            {
                if (string.Equals(d.Number?.Trim(), cleanNum, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(d.Type?.Trim(), docType.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    bool sameEndless = d.IsEndless == target.IsEndless;
                    bool sameStart = string.Equals(d.StartDate?.Trim(), target.StartDate.ToString("dd.MM.yyyy"), StringComparison.OrdinalIgnoreCase);
                    bool sameEnd = target.IsEndless || string.Equals(d.EndDate?.Trim(), target.EndDate.ToString("dd.MM.yyyy"), StringComparison.OrdinalIgnoreCase);

                    if (sameEndless && sameStart && sameEnd)
                    {
                        return (true, false);
                    }
                    return (false, true); // Same number/type but dates conflict
                }
            }

            return (false, false);
        }

        private static string ExtractCurrentDocSummary(WbCardItem card)
        {
            if (card.Documents?.Items == null || card.Documents.Items.Count == 0)
            {
                return "Chưa có";
            }

            var items = card.Documents.Items.Select(d => $"{d.Type}: {d.Number}").ToList();
            return string.Join("; ", items);
        }

        public static bool IsCardInScope(WbCardItem card, ShopDocumentPackage package, List<long>? selectedNmIds)
        {
            if (package == null) return false;

            switch (package.TargetScope)
            {
                case DocumentApplyScope.AllShop:
                    return true;

                case DocumentApplyScope.SelectedCards:
                    return selectedNmIds != null && selectedNmIds.Contains(card.NmId);

                case DocumentApplyScope.BySubject:
                    if (string.IsNullOrWhiteSpace(package.ScopeFilterValue)) return true;
                    var subFilters = package.ScopeFilterValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                    return subFilters.Any(sf => 
                        card.SubjectId.ToString() == sf || 
                        card.SubjectName.Contains(sf, StringComparison.OrdinalIgnoreCase));

                case DocumentApplyScope.ByTnved:
                    if (string.IsNullOrWhiteSpace(package.ScopeFilterValue)) return true;
                    var tnvedFilters = package.ScopeFilterValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                    var cardTnved = card.CurrentTnved ?? "";
                    return tnvedFilters.Any(tf => cardTnved.StartsWith(tf, StringComparison.OrdinalIgnoreCase));

                default:
                    return true;
            }
        }
    }
}
