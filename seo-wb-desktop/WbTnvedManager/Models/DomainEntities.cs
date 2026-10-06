using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace WbTnvedManager.Models
{
    public enum IssueSeverity
    {
        BLOCK,          // B: Lỗi đã đủ căn cứ, chặn thao tác cụ thể
        MANUAL_REVIEW,  // M: Chưa đủ dữ liệu hoặc cần quyết định nghiệp vụ
        WARNING         // W: Khuyến nghị/rủi ro không chặn thao tác
    }

    public enum SuggestedActionKind
    {
        FORMAT,         // Chuẩn hóa bản nháp không thay danh tính
        COPY,           // Lấy giá trị từ nguồn đã xác minh cho đúng hàng
        REVIEW,         // Cần người có trách nhiệm duyệt
        EXTERNAL        // Cần xử lý qua cổng chính thức bên ngoài
    }

    public enum OperationKind
    {
        SAVE_LOCAL_DRAFT,
        EXPORT_AUDIT,
        APPLY_WB_FIX,
        CREATE_WB_CONTENT,
        UPDATE_NK,
        SIGN_NK,
        ACTIVATE_SALES_OR_STOCK,
        ISSUE_OR_CIRCULATE_CODES
    }

    public enum ReadinessStatus
    {
        PASS,
        FAIL,
        MANUAL_REVIEW,
        PENDING,
        NOT_APPLICABLE
    }

    public enum ChangeSetState
    {
        DRAFT,
        VALIDATED,
        READY_FOR_REVIEW,
        APPROVED,
        PREFLIGHT,
        APPLYING,
        VERIFYING,
        COMPLETED,
        PARTIALLY_COMPLETED,
        FAILED,
        NEEDS_RECONCILIATION,
        CONFLICT,
        BLOCKED_CONTRACT,
        BLOCKED_POLICY,
        CANCELED_BEFORE_SEND
    }

    public enum VerificationState
    {
        UNVERIFIED,
        VERIFIED,
        DISPUTED,
        OBSOLETE
    }

    public enum GtinStructureStatus
    {
        UNKNOWN,
        PASS,
        FAIL_FORMAT,
        FAIL_CHECK_DIGIT,
        FAIL_NON_NUMERIC
    }

    public enum ConformityDocType
    {
        DS,     // Декларация о соответствии (ТР ТС 017/2011, etc.)
        SS,     // Сертификат соответствия
        SGR,    // Свидетельство о государственной регистрации (ТР ТС 007/2011, etc.)
        EXEMPTION_LETTER // Thư từ chối chứng nhận / Miễn
    }

    public class SellerAccount : INotifyPropertyChanged
    {
        public string Id { get; set; } = "ACC_MAIN";
        public string OrganizationId { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = "WB Seller RU";
        public string WbSellerId { get; set; } = string.Empty;
        public string INN { get; set; } = string.Empty;
        public string SellerCountry { get; set; } = "RU";
        public string TargetMarket { get; set; } = "RU";
        public List<string> Roles { get; set; } = new() { "SELLER", "IMPORTER" };
        public string GtinAllocationMaster { get; set; } = "GS1_RUS"; // GS1_RUS, NK, INTERNATIONAL
        public string ApiKey { get; set; } = string.Empty;
        public string ContentBaseUrl { get; set; } = "https://content-api.wildberries.ru";
        public bool IsProductionWriteEnabled { get; set; } = true;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public class ProductModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ManufacturerModel { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty; // e.g. "Брюки", "Худи", "Платье"
        public string TechnicalNameRu { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Construction { get; set; } = "KNITTED"; // KNITTED, WOVEN, OTHER, UNKNOWN
        public string IntendedAudience { get; set; } = "UNISEX"; // MALE, FEMALE, UNISEX, BOYS, GIRLS, BABY
        public string AgeGroup { get; set; } = "ADULT"; // ADULT, CHILDREN, BABY
        public string? CountryOfManufacture { get; set; }
        public List<ProductComponent> Components { get; set; } = new();
        public List<string> EvidenceRefs { get; set; } = new();
    }

    public class ProductComponent
    {
        public string Type { get; set; } = "OUTER"; // OUTER (Vải ngoài), LINING (Lớp lót), FILLING (Ruột/bông)
        public List<FiberComposition> Fibers { get; set; } = new();
        public VerificationState EvidenceState { get; set; } = VerificationState.UNVERIFIED;

        public decimal TotalPercentage
        {
            get
            {
                decimal sum = 0;
                foreach (var f in Fibers) sum += f.Percentage;
                return sum;
            }
        }
    }

    public class FiberComposition
    {
        public string NameRu { get; set; } = string.Empty; // e.g. "хлопок", "полиэстер", "эластан"
        public decimal Percentage { get; set; }
    }

    public class SellableVariant
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ModelId { get; set; } = string.Empty;
        public string VendorCode { get; set; } = string.Empty;
        public string ColorRu { get; set; } = string.Empty;
        public string SizeSystem { get; set; } = "INTERNATIONAL"; // INTERNATIONAL, RU, EU, US
        public string SizeValue { get; set; } = string.Empty;     // S, M, L, 42, 44, 46
        public string? RussianSize { get; set; }                 // 44, 46, 48
        public decimal? HeightCm { get; set; }                   // e.g. 86cm for baby classification
        public GtinIdentity Gtin { get; set; } = new();
        public string BarcodeLogistics { get; set; } = string.Empty;
        public decimal PriceRub { get; set; }
    }

    public class GtinIdentity
    {
        public string Raw { get; set; } = string.Empty;
        public string Canonical14 { get; set; } = string.Empty;
        public int OriginalLength => Raw?.Length ?? 0;
        public GtinStructureStatus StructureStatus { get; set; } = GtinStructureStatus.UNKNOWN;
        public VerificationState RegistrationStatus { get; set; } = VerificationState.UNVERIFIED;
        public VerificationState IdentityMatchStatus { get; set; } = VerificationState.UNVERIFIED;
        public string RegisteredOwner { get; set; } = string.Empty;
        public string Source { get; set; } = "INPUT"; // GS1, NK, BARCODE_SCAN, INPUT
    }

    public class ConformityDocument
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public ConformityDocType Type { get; set; } = ConformityDocType.DS;
        public string ExactNumber { get; set; } = string.Empty;
        public string? TradeName { get; set; }
        public string? Applicant { get; set; }
        public string? Manufacturer { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsEndless { get; set; }
        public string TechnicalRegulation { get; set; } = "ТР ТС 017/2011"; // ТР ТС 017/2011, ТР ТС 007/2011
        public List<string> CoveredTnvedPrefixes { get; set; } = new();
        public List<string> CoveredModels { get; set; } = new();
        public string? RegistryStatus { get; set; } // VALID, SUSPENDED, CANCELED
        public VerificationState VerificationState { get; set; } = VerificationState.UNVERIFIED;
    }

    public class ExternalSnapshot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string System { get; set; } = "WB"; // WB, NK, GS1, REGISTRY
        public string AccountId { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
        public string RawPayload { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public string? ETag { get; set; }
    }

    public class IssueItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string RuleId { get; set; } = string.Empty;
        public string RuleTitle { get; set; } = string.Empty;
        public string TargetEntityId { get; set; } = string.Empty;
        public string FieldPath { get; set; } = string.Empty;
        public string FieldNameVi { get; set; } = string.Empty;
        public string FieldNameRu { get; set; } = string.Empty;
        public IssueSeverity Severity { get; set; } = IssueSeverity.BLOCK;
        public string ObservedValue { get; set; } = string.Empty;
        public string ExpectedConstraint { get; set; } = string.Empty;
        public string VietnameseExplanation { get; set; } = string.Empty;
        public string RussianReason { get; set; } = string.Empty;
        public string EvidenceSource { get; set; } = string.Empty;
        public SuggestedActionKind SuggestedAction { get; set; } = SuggestedActionKind.REVIEW;
        public string SuggestedValue { get; set; } = string.Empty;
        public List<OperationKind> BlockedOperations { get; set; } = new();
    }

    public class ProductReadinessSummary
    {
        public ReadinessStatus LocalContentReadiness { get; set; } = ReadinessStatus.PENDING;
        public ReadinessStatus IdentityReadiness { get; set; } = ReadinessStatus.PENDING;
        public ReadinessStatus DocumentReadiness { get; set; } = ReadinessStatus.PENDING;
        public ReadinessStatus RemoteCardReadiness { get; set; } = ReadinessStatus.PENDING;
        public ReadinessStatus PhysicalGoodsReadiness { get; set; } = ReadinessStatus.PENDING;
        public ReadinessStatus IntegrationReadiness { get; set; } = ReadinessStatus.PASS;

        public bool CanApplyWbFix => LocalContentReadiness != ReadinessStatus.FAIL &&
                                     IntegrationReadiness == ReadinessStatus.PASS;
    }

    public class ChangeIntent
    {
        public string FieldPath { get; set; } = string.Empty;
        public string FieldNameRu { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string EvidenceSource { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = true;
    }

    public class ChangeSet
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string AccountId { get; set; } = string.Empty;
        public long NmId { get; set; }
        public string VendorCode { get; set; } = string.Empty;
        public string BaseSnapshotHash { get; set; } = string.Empty;
        public List<ChangeIntent> Intents { get; set; } = new();
        public ChangeSetState State { get; set; } = ChangeSetState.DRAFT;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public string? ConflictMessage { get; set; }
    }
}
