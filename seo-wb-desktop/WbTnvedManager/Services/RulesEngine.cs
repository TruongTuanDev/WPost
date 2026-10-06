using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class AuditEvaluationContext
    {
        public SellerAccount Account { get; set; } = new();
        public WbCardItem WbCard { get; set; } = new();
        public ProductModel? Model { get; set; }
        public SellableVariant? Variant { get; set; }
        public List<ConformityDocument> Documents { get; set; } = new();
        public ExternalSnapshot? LatestSnapshot { get; set; }
        public bool IsOfflineAudit { get; set; }
    }

    public class EvaluationReport
    {
        public List<IssueItem> Issues { get; set; } = new();
        public ProductReadinessSummary Readiness { get; set; } = new();

        public bool HasBlockingIssues => Issues.Any(i => i.Severity == IssueSeverity.BLOCK);
        public int TotalIssues => Issues.Count;
        public int BlockCount => Issues.Count(i => i.Severity == IssueSeverity.BLOCK);
        public int ReviewCount => Issues.Count(i => i.Severity == IssueSeverity.MANUAL_REVIEW);
        public int WarningCount => Issues.Count(i => i.Severity == IssueSeverity.WARNING);
    }

    public class RulesEngine
    {
        private readonly TnvedSelectorService _selector;

        public RulesEngine(TnvedSelectorService selector)
        {
            _selector = selector;
        }

        public EvaluationReport Evaluate(AuditEvaluationContext ctx)
        {
            var report = new EvaluationReport();
            var issues = report.Issues;

            var card = ctx.WbCard;
            var currentTnved = card.CurrentTnved?.Trim() ?? string.Empty;
            var currentGender = card.CurrentGender?.Trim() ?? string.Empty;
            var currentMaterial = card.CurrentMaterial?.Trim() ?? string.Empty;
            var title = card.Title?.Trim() ?? string.Empty;
            var vendorCode = card.VendorCode?.Trim() ?? string.Empty;

            // -------------------------------------------------------------
            // R001: Evidence provenance / AI-only claims
            // -------------------------------------------------------------
            if (ctx.Model != null && ctx.Model.Components.Any(c => c.EvidenceState == VerificationState.UNVERIFIED))
            {
                issues.Add(new IssueItem
                {
                    RuleId = "R001",
                    RuleTitle = "Thành phần cấu tạo chưa được xác thực bằng chứng từ",
                    TargetEntityId = card.NmId.ToString(),
                    FieldPath = "product.components",
                    FieldNameVi = "Bằng chứng thành phần",
                    FieldNameRu = "Доказательства состава",
                    Severity = IssueSeverity.MANUAL_REVIEW,
                    ObservedValue = "UNVERIFIED",
                    ExpectedConstraint = "Nhãn gốc hoặc biên bản thử nghiệm",
                    VietnameseExplanation = "Thành phần vải được nhập tự động hoặc chưa được duyệt từ nhãn hàng thật. Cần đối chiếu chứng từ kiểm nghiệm.",
                    EvidenceSource = "Product Evidence",
                    SuggestedAction = SuggestedActionKind.REVIEW
                });
            }

            // -------------------------------------------------------------
            // R002: Context & Account Isolation
            // -------------------------------------------------------------
            if (string.IsNullOrEmpty(ctx.Account.SellerCountry) || ctx.Account.SellerCountry != "RU")
            {
                issues.Add(new IssueItem
                {
                    RuleId = "R002",
                    RuleTitle = "Xác định quốc gia và thị trường người bán",
                    TargetEntityId = card.NmId.ToString(),
                    FieldPath = "account.sellerCountry",
                    FieldNameVi = "Quốc gia tài khoản",
                    FieldNameRu = "Страна продавца",
                    Severity = IssueSeverity.MANUAL_REVIEW,
                    ObservedValue = ctx.Account.SellerCountry,
                    ExpectedConstraint = "RU",
                    VietnameseExplanation = "Chưa xác nhận tài khoản thuộc phạm vi người bán tại Nga (bán trong Nga) để áp dụng đúng quy chuẩn GTIN/ГИС МТ.",
                    EvidenceSource = "SellerAccount Profile",
                    SuggestedAction = SuggestedActionKind.REVIEW,
                    BlockedOperations = new List<OperationKind> { OperationKind.APPLY_WB_FIX, OperationKind.CREATE_WB_CONTENT }
                });
            }

            // -------------------------------------------------------------
            // R003: Category Schema & Required Fields
            // -------------------------------------------------------------
            if (card.SubjectId <= 0 && string.IsNullOrWhiteSpace(card.SubjectName))
            {
                issues.Add(new IssueItem
                {
                    RuleId = "R003",
                    RuleTitle = "Thiếu danh mục sản phẩm (SubjectID)",
                    TargetEntityId = card.NmId.ToString(),
                    FieldPath = "subjectID",
                    FieldNameVi = "Danh mục sản phẩm",
                    FieldNameRu = "Предмет / Категория",
                    Severity = IssueSeverity.BLOCK,
                    ObservedValue = card.SubjectId.ToString(),
                    ExpectedConstraint = "> 0",
                    VietnameseExplanation = "Thẻ sản phẩm thiếu danh mục hợp lệ trên sàn Wildberries.",
                    EvidenceSource = "WB API Schema",
                    SuggestedAction = SuggestedActionKind.REVIEW,
                    BlockedOperations = new List<OperationKind> { OperationKind.CREATE_WB_CONTENT, OperationKind.APPLY_WB_FIX }
                });
            }

            // -------------------------------------------------------------
            // R010: Full 10-digit TNVED Format
            // -------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(currentTnved) && !TnvedClassificationEngine.IsValid10DigitFormat(currentTnved))
            {
                issues.Add(new IssueItem
                {
                    RuleId = "R010",
                    RuleTitle = "Mã ТН ВЭД không đủ 10 chữ số",
                    TargetEntityId = card.NmId.ToString(),
                    FieldPath = "characteristics.ТНВЭД",
                    FieldNameVi = "Mã ТН ВЭД",
                    FieldNameRu = "Код ТН ВЭД",
                    Severity = IssueSeverity.BLOCK,
                    ObservedValue = currentTnved,
                    ExpectedConstraint = "10 chữ số chuẩn",
                    VietnameseExplanation = $"Mã ТН ВЭД '{currentTnved}' chỉ có {currentTnved.Length} chữ số. Thao tác trên sàn yêu cầu mã đầy đủ 10 chữ số (không tự động nối thêm số 0).",
                    RussianReason = "Код ТН ВЭД должен содержать ровно 10 знаков.",
                    EvidenceSource = "EAEU Customs Tariff",
                    SuggestedAction = SuggestedActionKind.FORMAT,
                    BlockedOperations = new List<OperationKind> { OperationKind.APPLY_WB_FIX, OperationKind.CREATE_WB_CONTENT }
                });
            }

            // -------------------------------------------------------------
            // R012 & R013: Knitted vs Woven & Chapter 61/62 consistency
            // -------------------------------------------------------------
            var detectedKnit = _selector.InferKnitState(title + " " + card.SubjectName) ?? "knit";
            var detectedFamily = _selector.InferFamily(card.SubjectName + " " + title);
            var (suggestedTnved, matchReason) = _selector.GetTnvedForAttributes(
                card.SubjectId, currentGender, currentMaterial, detectedKnit, card.SubjectName, title);

            if (!string.IsNullOrEmpty(currentTnved) && !string.IsNullOrEmpty(suggestedTnved) && currentTnved != suggestedTnved)
            {
                issues.Add(new IssueItem
                {
                    RuleId = "R013",
                    RuleTitle = "Mã ТН ВЭД không khớp phân loại nhóm hàng",
                    TargetEntityId = card.NmId.ToString(),
                    FieldPath = "characteristics.ТНВЭД",
                    FieldNameVi = "Mã ТН ВЭД",
                    FieldNameRu = "Код ТН ВЭД",
                    Severity = IssueSeverity.BLOCK,
                    ObservedValue = currentTnved,
                    ExpectedConstraint = suggestedTnved,
                    VietnameseExplanation = $"Mã hiện tại '{currentTnved}' không đúng phân loại cho '{card.SubjectName}'. Mã chuẩn xác là '{suggestedTnved}' ({matchReason}).",
                    RussianReason = $"Рекомендуемый код ТН ВЭД: {suggestedTnved}",
                    EvidenceSource = "Ma trận phân loại WB / EAEU",
                    SuggestedAction = SuggestedActionKind.COPY,
                    SuggestedValue = suggestedTnved,
                    BlockedOperations = new List<OperationKind> { OperationKind.APPLY_WB_FIX }
                });
            }

            // -------------------------------------------------------------
            // R015 & R016: Intended Audience & Gender & Unisex
            // -------------------------------------------------------------
            var detectedGender = _selector.InferGender(title + " " + card.SubjectName);
            if (string.IsNullOrWhiteSpace(currentGender) || currentGender.Equals("Детский", StringComparison.OrdinalIgnoreCase))
            {
                string recGender = string.IsNullOrEmpty(detectedGender) ? "Женский" : detectedGender;
                issues.Add(new IssueItem
                {
                    RuleId = "R015",
                    RuleTitle = "Thiếu hoặc sai trường Giới tính (Пол)",
                    TargetEntityId = card.NmId.ToString(),
                    FieldPath = "characteristics.Пол",
                    FieldNameVi = "Giới tính",
                    FieldNameRu = "Пол",
                    Severity = IssueSeverity.BLOCK,
                    ObservedValue = string.IsNullOrEmpty(currentGender) ? "(Trống)" : currentGender,
                    ExpectedConstraint = recGender,
                    VietnameseExplanation = currentGender.Equals("Детский", StringComparison.OrdinalIgnoreCase)
                        ? "Hệ thống Hải quan EAEU và Честный ЗНАК không chấp nhận 'Детский' chung chung mà bắt buộc phân rõ 'Девочки' (Bé gái) hoặc 'Мальчики' (Bé trai)."
                        : "Thẻ sản phẩm chưa có thuộc tính Giới tính bắt buộc theo chuẩn WB.",
                    RussianReason = $"Необходимо указать корректный пол: {recGender}",
                    EvidenceSource = "WB Enum / CRPT Markirovka",
                    SuggestedAction = SuggestedActionKind.COPY,
                    SuggestedValue = recGender,
                    BlockedOperations = new List<OperationKind> { OperationKind.APPLY_WB_FIX }
                });
            }

            // -------------------------------------------------------------
            // R020 & R021: Composition per component
            // -------------------------------------------------------------
            if (ctx.Model != null && ctx.Model.Components.Count > 0)
            {
                var compResults = CompositionValidator.ValidateAllComponents(ctx.Model.Components);
                foreach (var cRes in compResults)
                {
                    if (!cRes.IsValid)
                    {
                        issues.Add(new IssueItem
                        {
                            RuleId = "R020",
                            RuleTitle = "Tổng tỷ lệ thành phần vải khác 100%",
                            TargetEntityId = card.NmId.ToString(),
                            FieldPath = $"components.{cRes.ComponentType}",
                            FieldNameVi = $"Thành phần ({cRes.ComponentType})",
                            FieldNameRu = $"Состав ({cRes.ComponentType})",
                            Severity = IssueSeverity.BLOCK,
                            ObservedValue = $"{cRes.TotalPercentage}%",
                            ExpectedConstraint = "100%",
                            VietnameseExplanation = cRes.ErrorMessage,
                            EvidenceSource = "Nhãn thành phần thật",
                            SuggestedAction = SuggestedActionKind.REVIEW,
                            BlockedOperations = new List<OperationKind> { OperationKind.CREATE_WB_CONTENT, OperationKind.APPLY_WB_FIX }
                        });
                    }
                }
            }

            // -------------------------------------------------------------
            // R030 & R031: GTIN & Barcode Structure & Registration
            // -------------------------------------------------------------
            if (card.Sizes != null && card.Sizes.Count > 0)
            {
                foreach (var sizeElem in card.Sizes)
                {
                    string techSize = "Size";
                    if (sizeElem.ValueKind == JsonValueKind.Object && sizeElem.TryGetProperty("techSize", out var ts))
                    {
                        techSize = ts.GetString() ?? "Size";
                    }

                    if (sizeElem.ValueKind == JsonValueKind.Object && sizeElem.TryGetProperty("skus", out var skusElem) && skusElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var skuItem in skusElem.EnumerateArray())
                        {
                            var sku = skuItem.GetString() ?? string.Empty;
                            if (string.IsNullOrEmpty(sku)) continue;

                            var status = GtinValidator.ValidateStructure(sku);
                            if (status != GtinStructureStatus.PASS)
                            {
                                issues.Add(new IssueItem
                                {
                                    RuleId = "R030",
                                    RuleTitle = "Mã Barcode / GTIN sai cấu trúc GS1",
                                    TargetEntityId = $"{card.NmId}_{techSize}",
                                    FieldPath = $"sizes[{techSize}].skus",
                                    FieldNameVi = $"Mã Barcode ({techSize})",
                                    FieldNameRu = $"Баркод размера {techSize}",
                                    Severity = IssueSeverity.BLOCK,
                                    ObservedValue = sku,
                                    ExpectedConstraint = "EAN-13 / GTIN hợp lệ (8, 12, 13, 14 số)",
                                    VietnameseExplanation = $"Mã '{sku}' không hợp lệ về cấu trúc ({status}). Checksum hoặc định dạng số không đạt chuẩn GS1.",
                                    EvidenceSource = "GS1 Standard",
                                    SuggestedAction = SuggestedActionKind.FORMAT,
                                    BlockedOperations = new List<OperationKind> { OperationKind.APPLY_WB_FIX, OperationKind.CREATE_WB_CONTENT }
                                });
                            }
                        }
                    }
                }
            }

            // -------------------------------------------------------------
            // R040: Conformity Documents (DS / SS / SGR)
            // -------------------------------------------------------------
            if (ctx.Documents.Count == 0 && (card.SubjectId > 0 || !string.IsNullOrEmpty(card.SubjectName)))
            {
                issues.Add(new IssueItem
                {
                    RuleId = "R040",
                    RuleTitle = "Chưa liên kết hồ sơ công bố hợp quy (ДС/СС/СГР)",
                    TargetEntityId = card.NmId.ToString(),
                    FieldPath = "documents",
                    FieldNameVi = "Hồ sơ hợp quy",
                    FieldNameRu = "Разрешительные документы (РД)",
                    Severity = IssueSeverity.MANUAL_REVIEW,
                    ObservedValue = "(Chưa có)",
                    ExpectedConstraint = "ДС theo ТР ТС 017/2011 hoặc ТР ТС 007/2011",
                    VietnameseExplanation = "Sản phẩm dệt may thuộc diện bắt buộc có bản công bố hợp quy ДС hoặc chứng nhận СС khi lưu thông tại Nga.",
                    EvidenceSource = "ТР ТС 017/2011 / CRPT",
                    SuggestedAction = SuggestedActionKind.EXTERNAL
                });
            }

            // -------------------------------------------------------------
            // R060: Content Length Limits (Title <= 60 chars)
            // -------------------------------------------------------------
            if (!string.IsNullOrEmpty(title) && title.Length > 60)
            {
                issues.Add(new IssueItem
                {
                    RuleId = "R060",
                    RuleTitle = "Tiêu đề vượt quá giới hạn 60 ký tự",
                    TargetEntityId = card.NmId.ToString(),
                    FieldPath = "title",
                    FieldNameVi = "Tiêu đề sản phẩm",
                    FieldNameRu = "Наименование товара",
                    Severity = IssueSeverity.BLOCK,
                    ObservedValue = $"{title} ({title.Length} ký tự)",
                    ExpectedConstraint = "<= 60 ký tự",
                    VietnameseExplanation = $"Quy định sàn Wildberries giới hạn tiêu đề tối đa 60 ký tự. Tiêu đề hiện tại dài {title.Length} ký tự.",
                    RussianReason = "Длина наименования не должна превышать 60 символов.",
                    EvidenceSource = "WB Material: how-to-create-card",
                    SuggestedAction = SuggestedActionKind.FORMAT,
                    SuggestedValue = title.Substring(0, Math.Min(60, title.Length)).Trim(),
                    BlockedOperations = new List<OperationKind> { OperationKind.CREATE_WB_CONTENT }
                });
            }

            // -------------------------------------------------------------
            // R063: Missing Dimensions or Weight
            // -------------------------------------------------------------
            if (card.Dimensions.HasValue && card.Dimensions.Value.ValueKind == JsonValueKind.Object)
            {
                var dim = card.Dimensions.Value;
                int len = dim.TryGetProperty("length", out var l) ? l.GetInt32() : 0;
                int wid = dim.TryGetProperty("width", out var w) ? w.GetInt32() : 0;
                int hei = dim.TryGetProperty("height", out var h) ? h.GetInt32() : 0;

                if (len <= 0 || wid <= 0 || hei <= 0)
                {
                    issues.Add(new IssueItem
                    {
                        RuleId = "R063",
                        RuleTitle = "Kích thước kiện hàng thiếu hoặc bằng 0",
                        TargetEntityId = card.NmId.ToString(),
                        FieldPath = "dimensions",
                        FieldNameVi = "Kích thước 3 chiều",
                        FieldNameRu = "Габариты упаковки (ДхШхВ)",
                        Severity = IssueSeverity.WARNING,
                        ObservedValue = $"{len}x{wid}x{hei} cm",
                        ExpectedConstraint = "> 0 cm",
                        VietnameseExplanation = "Cần điền kích thước bao bì thực tế để sàn WB tính phí lưu kho và vận chuyển chính xác.",
                        EvidenceSource = "WB Logistics Policy",
                        SuggestedAction = SuggestedActionKind.FORMAT
                    });
                }
            }

            // -------------------------------------------------------------
            // Compute Readiness Summary
            // -------------------------------------------------------------
            report.Readiness.LocalContentReadiness = issues.Any(i => i.Severity == IssueSeverity.BLOCK && 
                (i.FieldPath.Contains("ТНВЭД") || i.FieldPath.Contains("Пол") || i.FieldPath.Contains("title")))
                ? ReadinessStatus.FAIL
                : ReadinessStatus.PASS;

            report.Readiness.IdentityReadiness = issues.Any(i => i.RuleId == "R030" || i.RuleId == "R032")
                ? ReadinessStatus.FAIL
                : ReadinessStatus.PASS;

            report.Readiness.DocumentReadiness = issues.Any(i => i.RuleId.StartsWith("R04"))
                ? ReadinessStatus.MANUAL_REVIEW
                : ReadinessStatus.PASS;

            report.Readiness.RemoteCardReadiness = ReadinessStatus.PASS;
            report.Readiness.PhysicalGoodsReadiness = ReadinessStatus.PENDING;
            report.Readiness.IntegrationReadiness = ReadinessStatus.PASS;

            return report;
        }
    }
}
