using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using WbTnvedManager.Models;
using WbTnvedManager.Services.Resolvers;

namespace WbTnvedManager.Services
{
    public class ReconciliationResult
    {
        public List<MarkirovkaFinding> Findings { get; set; } = new();
        public List<MarkirovkaProposal> Proposals { get; set; } = new();
    }

    public class MarkirovkaReconciliationEngine
    {
        public static string ExtractGtinFromDataMatrix(string dataMatrixRaw)
        {
            if (string.IsNullOrWhiteSpace(dataMatrixRaw)) return string.Empty;
            
            // GS1 DataMatrix: prefix 01 followed by 14 digits
            var clean = dataMatrixRaw.Trim();
            // Handle optional scanner prefixes like ]d2 or GS (char 29)
            clean = clean.TrimStart(']', 'd', '2');
            
            var match = Regex.Match(clean, @"(?:^|\u001d)?01(\d{14})");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // Fallback: If raw input itself is 13 or 14 digits
            var digitsOnly = new string(clean.Where(char.IsDigit).ToArray());
            if (digitsOnly.Length == 14) return digitsOnly;
            if (digitsOnly.Length == 13) return "0" + digitsOnly;

            return string.Empty;
        }

        public static string NormalizeGtin14(string? gtin)
        {
            if (string.IsNullOrWhiteSpace(gtin)) return string.Empty;
            var clean = new string(gtin.Where(char.IsDigit).ToArray());
            if (clean.Length == 8) return clean.PadLeft(14, '0');
            if (clean.Length == 12) return clean.PadLeft(14, '0');
            if (clean.Length == 13) return "0" + clean;
            if (clean.Length == 14) return clean;
            return clean;
        }

        public ReconciliationResult Reconcile(
            SellerAccount account,
            List<WbCardItem> wbCards,
            List<NationalCatalogProduct> nkProducts,
            List<StockEvidence> stockEvidences,
            List<TnvedMatrixEntry> matrixSeeds)
        {
            var result = new ReconciliationResult();
            var nkByGtin = nkProducts.ToDictionary(p => p.Gtin14, p => p, StringComparer.OrdinalIgnoreCase);
            var stockBySkuOrGtin = new Dictionary<string, StockEvidence>(StringComparer.OrdinalIgnoreCase);

            foreach (var stock in stockEvidences)
            {
                if (!string.IsNullOrEmpty(stock.Gtin14)) stockBySkuOrGtin[stock.Gtin14] = stock;
                if (!string.IsNullOrEmpty(stock.Sku)) stockBySkuOrGtin[stock.Sku] = stock;
            }

            foreach (var card in wbCards)
            {
                // R01: Account and Shop validation
                if (string.IsNullOrWhiteSpace(account.INN))
                {
                    result.Findings.Add(new MarkirovkaFinding
                    {
                        RuleId = "R01",
                        RuleTitle = "Chưa cấu hình ИНН pháp nhân chủ shop",
                        SourceKind = FindingSourceKind.LOCAL_VALIDATION_FINDING,
                        Severity = IssueSeverity.BLOCK,
                        NmId = card.NmId,
                        VendorCode = card.VendorCode,
                        RawMessageRu = "Не указан ИНН организации",
                        TranslatedExplanationVi = "Thiếu thông tin ИНН pháp nhân quản lý. Không thể xác thực quyền sở hữu GTIN trên Честный ЗНАК."
                    });
                }

                // Check sizes & barcodes
                if (card.Sizes == null || card.Sizes.Count == 0) continue;

                foreach (var sizeElem in card.Sizes)
                {
                    string techSize = "OS";
                    long chrtId = 0;
                    string wbBarcode = string.Empty;

                    if (sizeElem.ValueKind == JsonValueKind.Object)
                    {
                        if (sizeElem.TryGetProperty("techSize", out var ts)) techSize = ts.GetString() ?? "OS";
                        if (sizeElem.TryGetProperty("chrtID", out var ci)) chrtId = ci.GetInt64();
                        if (sizeElem.TryGetProperty("skus", out var sk) && sk.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var s in sk.EnumerateArray())
                            {
                                wbBarcode = s.GetString() ?? "";
                                break;
                            }
                        }
                    }

                    // 1. Barcode check
                    var gtinCheck = GtinAndVariantValidator.ValidateGtin(wbBarcode);
                    if (gtinCheck.LocalStatus == GtinLocalStatus.BAD_CHECKSUM)
                    {
                        result.Findings.Add(new MarkirovkaFinding
                        {
                            RuleId = "R03",
                            RuleTitle = "Mã GTIN / Barcode sai chữ số kiểm tra (Checksum)",
                            SourceKind = FindingSourceKind.LOCAL_VALIDATION_FINDING,
                            Severity = IssueSeverity.BLOCK,
                            NmId = card.NmId,
                            VendorCode = card.VendorCode,
                            SizeTech = techSize,
                            FieldPath = $"sizes[{techSize}].skus",
                            RawMessageRu = $"Неверная контрольная цифра в GTIN {wbBarcode}",
                            TranslatedExplanationVi = $"Mã vạch '{wbBarcode}' trên size {techSize} sai checksum GS1. Hệ thống cấm tự ý sửa chữ số để lách lỗi mà phải đối soát mã thật.",
                            EvidenceSummary = "GS1 Checksum Specification"
                        });
                    }

                    // Match Physical Stock & NK
                    var normGtin = NormalizeGtin14(wbBarcode);
                    stockBySkuOrGtin.TryGetValue(normGtin, out var stockEv);
                    if (stockEv == null && !string.IsNullOrEmpty(card.VendorCode))
                    {
                        stockBySkuOrGtin.TryGetValue(card.VendorCode, out stockEv);
                    }

                    nkByGtin.TryGetValue(normGtin, out var nkProd);

                    // R06: Physical goods mismatch with GTIN
                    if (stockEv != null && !string.IsNullOrEmpty(stockEv.Gtin14) && !string.IsNullOrEmpty(normGtin) && stockEv.Gtin14 != normGtin)
                    {
                        result.Findings.Add(new MarkirovkaFinding
                        {
                            RuleId = "R06",
                            RuleTitle = "КИЗ / Nhãn lô hàng thực tế khác GTIN khai báo",
                            SourceKind = FindingSourceKind.DATA_CONFLICT,
                            Severity = IssueSeverity.BLOCK,
                            NmId = card.NmId,
                            VendorCode = card.VendorCode,
                            SizeTech = techSize,
                            RawMessageRu = $"Несоответствие КИЗ фактической партии ({stockEv.Gtin14}) и карточки WB ({normGtin})",
                            TranslatedExplanationVi = $"Hàng trong kho dán nhãn GTIN '{stockEv.Gtin14}' nhưng thẻ WB lại khai '{normGtin}'. Nguy cơ bị phạt và trả hàng khi nhập kho WB.",
                            EvidenceSummary = $"Lô hàng: {stockEv.LotId}"
                        });
                    }

                    // R08: Draft or under review on NK
                    if (nkProd != null && nkProd.IsDraft)
                    {
                        result.Findings.Add(new MarkirovkaFinding
                        {
                            RuleId = "R08",
                            RuleTitle = "Thẻ National Catalog đang ở trạng thái Nháp / Chờ duyệt",
                            SourceKind = FindingSourceKind.OFFICIAL_SOURCE_ERROR,
                            Severity = IssueSeverity.BLOCK,
                            NmId = card.NmId,
                            VendorCode = card.VendorCode,
                            SizeTech = techSize,
                            RawMessageRu = $"Карточка в Нац. каталоге в статусе ''{nkProd.Status}''. {nkProd.RawMessageRu}",
                            TranslatedExplanationVi = $"Thẻ mã {nkProd.Gtin14} trên Национальный каталог đang là '{nkProd.Status}'. Không được dùng dữ liệu nháp đang lỗi để sửa đè lên WB.",
                            EvidenceSummary = "National Catalog status"
                        });
                    }

                    // Determine Proposal
                    var currentTnved = card.CurrentTnved ?? "";
                    var currentGender = card.CurrentGender ?? "";
                    var currentMaterial = card.CurrentMaterial ?? "";

                    var matchedMatrix = matrixSeeds.FirstOrDefault(m => 
                        (m.SubjectId == card.SubjectId || m.SubjectName.Equals(card.SubjectName, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrEmpty(currentGender) || m.Gender.Equals(currentGender, StringComparison.OrdinalIgnoreCase) || m.Gender == "Унисекс"));

                    string desiredTnved = matchedMatrix?.TnvedCode ?? currentTnved;
                    string desiredGender = matchedMatrix?.Gender ?? (string.IsNullOrEmpty(currentGender) ? "Женский" : currentGender);
                    string desiredGtin = stockEv != null && !string.IsNullOrEmpty(stockEv.Gtin14) ? stockEv.Gtin14 : normGtin;

                    var proposal = new MarkirovkaProposal
                    {
                        TenantId = account.OrganizationId,
                        ShopId = account.Id,
                        NmId = card.NmId,
                        ChrtId = chrtId > 0 ? chrtId : null,
                        VendorCode = card.VendorCode,
                        TechSize = techSize,
                        Title = card.Title,

                        PhysicalGtin = stockEv?.Gtin14 ?? "-",
                        PhysicalSize = stockEv?.SizeValue ?? techSize,

                        NkGtin = nkProd?.Gtin14 ?? (stockEv?.Gtin14 ?? "-"),
                        NkStatus = nkProd?.Status ?? "Đã xác minh GS1",
                        NkTnved = nkProd?.TnvedCode ?? desiredTnved,

                        WbCurrentGtin = wbBarcode,
                        WbCurrentTnved = currentTnved,
                        WbCurrentBarcode = wbBarcode,
                        WbNeedKiz = true,
                        WbKizMarked = false,

                        DesiredGtin = desiredGtin,
                        DesiredTnved = desiredTnved,
                        DesiredGender = desiredGender,
                        DesiredKizMarked = true,
                        Justification = $"Chuẩn hóa GTIN={desiredGtin}, ТН ВЭД={desiredTnved} khớp quy chuẩn đối chiếu 3 nguồn.",
                        State = (nkProd != null && nkProd.IsDraft) ? ProposalState.NEEDS_EVIDENCE : ProposalState.READY
                    };

                    if (proposal.DesiredTnved != proposal.WbCurrentTnved || proposal.DesiredGender != currentGender || !string.IsNullOrEmpty(desiredGtin))
                    {
                        result.Proposals.Add(proposal);
                    }
                }
            }

            return result;
        }
    }
}