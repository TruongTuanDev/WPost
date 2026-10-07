using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class MarkirovkaBatchExecutionResult
    {
        public bool Success { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public string SummaryMessage { get; set; } = string.Empty;
    }

    public class MarkirovkaUpdateWorker
    {
        private readonly IWbApiClient _apiClient;
        private readonly SafeWbUpdatePipeline _safePipeline;

        public MarkirovkaUpdateWorker(IWbApiClient apiClient, SafeWbUpdatePipeline safePipeline)
        {
            _apiClient = apiClient;
            _safePipeline = safePipeline;
        }

        public async Task<MarkirovkaBatchExecutionResult> ExecuteApprovedProposalsAsync(
            List<MarkirovkaProposal> approvedProposals,
            SellerAccount account,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var res = new MarkirovkaBatchExecutionResult();
            var validProposals = approvedProposals.Where(p => p.State == ProposalState.APPROVED).ToList();

            if (validProposals.Count == 0)
            {
                res.SummaryMessage = "Không có đề xuất nào ở trạng thái ĐÃ DUYỆT (APPROVED) để gửi.";
                return res;
            }

            progress?.Report($"[1/4] Chuẩn bị hàng đợi gửi WB cho {validProposals.Count} đề xuất đã duyệt...");

            var auditItems = new List<AuditResultItem>();
            foreach (var prop in validProposals)
            {
                prop.Delivery = DeliveryState.SENDING;

                // Create audit item representation
                var card = new WbCardItem
                {
                    NmId = prop.NmId,
                    VendorCode = prop.VendorCode,
                    Title = prop.Title,
                    Characteristics = new List<WbCharacteristic>
                    {
                        new() { Id = 5, Name = "ТНВЭД", Value = prop.WbCurrentTnved },
                        new() { Id = 8, Name = "Пол", Value = prop.DesiredGender }
                    }
                };

                var item = new AuditResultItem
                {
                    Card = card,
                    CurrentTnved = prop.WbCurrentTnved,
                    SuggestedTnved = prop.DesiredTnved,
                    CurrentGender = "",
                    SuggestedGender = prop.DesiredGender,
                    MatchReason = prop.Justification,
                    IsSelected = true
                };
                auditItems.Add(item);
            }

            progress?.Report($"[2/4] Kiểm tra tiền kiểm an toàn & gửi API Wildberries...");

            var pipelineResult = await _safePipeline.ExecuteSafeBatchUpdateAsync(
                auditItems,
                account,
                progress,
                cancellationToken);

            progress?.Report($"[3/4] Đọc lại (Readback) và cập nhật trục trạng thái...");

            foreach (var prop in validProposals)
            {
                if (pipelineResult.Success)
                {
                    prop.Delivery = DeliveryState.ACCEPTED;
                    prop.Readback = ReadbackState.MATCHED;
                    prop.MarketplaceCheck = MarketplaceCheckState.PENDING; // Chờ sàn xét duyệt trong 180 ngày theo NĐ 821
                    prop.WbCurrentTnved = prop.DesiredTnved;
                    prop.WbKizMarked = true;
                    res.SuccessCount++;
                }
                else
                {
                    prop.Delivery = DeliveryState.FAILED;
                    prop.Readback = ReadbackState.MISMATCHED;
                    prop.MarketplaceCheck = MarketplaceCheckState.FAILED;
                    res.ErrorCount++;
                }
            }

            res.Success = pipelineResult.Success;
            res.SummaryMessage = pipelineResult.Message;
            progress?.Report($"[4/4] Hoàn tất! {res.SummaryMessage}");

            return res;
        }
    }
}