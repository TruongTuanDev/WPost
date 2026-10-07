using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WbTnvedManager.Models
{
    public enum FindingSourceKind
    {
        OFFICIAL_SOURCE_ERROR,     // Lỗi hoặc trạng thái thật do API nguồn trả về
        LOCAL_VALIDATION_FINDING,  // Kết quả bộ quy tắc nội bộ phát hiện
        DATA_CONFLICT,             // Mâu thuẫn giữa các nguồn / lô
        SOURCE_UNAVAILABLE         // Không đọc được nguồn, sai quyền, timeout
    }

    public enum FindingStatus
    {
        OPEN,
        IN_PROGRESS,
        RESOLVED,
        IGNORED_WITH_REASON
    }

    public enum ProposalState
    {
        DRAFT,
        NEEDS_EVIDENCE,
        READY,
        APPROVED,
        STALE,
        REJECTED
    }

    public enum DeliveryState
    {
        QUEUED,
        SENDING,
        ACCEPTED,
        UNKNOWN_OUTCOME,
        FAILED
    }

    public enum ReadbackState
    {
        UNVERIFIED,
        MATCHED,
        MISMATCHED,
        PARTIAL
    }

    public enum MarketplaceCheckState
    {
        UNKNOWN,
        PENDING,
        PASSED,
        FAILED,
        NOT_EXPOSED
    }

    /// <summary>
    /// Thẻ đăng ký trên National Catalog (Национальный Каталог - Честный ЗНАК)
    /// </summary>
    public class NationalCatalogProduct
    {
        public string ProviderRecordId { get; set; } = string.Empty;
        public string Gtin14 { get; set; } = string.Empty;
        public string OwnerInn { get; set; } = string.Empty;
        public string TitleRu { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string TnvedCode { get; set; } = string.Empty;
        public string Status { get; set; } = "Опубликована"; // Опубликована, На модерации, Черновик, Требует изменений
        public bool IsDraft { get; set; }
        public string RawMessageRu { get; set; } = string.Empty;
        public string? ModerationComment { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new();
        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Bằng chứng lô hàng thực tế (Physical Lot / Stock Evidence)
    /// </summary>
    public class StockEvidence
    {
        public string LotId { get; set; } = string.Empty;
        public string VariantId { get; set; } = string.Empty;
        public string Gtin14 { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? ColorRu { get; set; }
        public string? SizeValue { get; set; }
        public string LabelEvidence { get; set; } = string.Empty;
        public string? ScannedDataMatrixAi01 { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public string VerifiedBy { get; set; } = "Operator";
        public bool IsVerified { get; set; } = true;
    }

    /// <summary>
    /// Bản ghi phát hiện lỗi/sai lệch chuẩn hóa theo Section 06 & 11
    /// </summary>
    public class MarkirovkaFinding : INotifyPropertyChanged
    {
        private FindingStatus _status = FindingStatus.OPEN;

        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = "DEFAULT_TENANT";
        public string ShopId { get; set; } = "DEFAULT_SHOP";
        public string LegalEntityInn { get; set; } = string.Empty;
        public string RuleId { get; set; } = string.Empty; // R01 -> R16
        public string RuleTitle { get; set; } = string.Empty;
        public FindingSourceKind SourceKind { get; set; } = FindingSourceKind.LOCAL_VALIDATION_FINDING;
        public IssueSeverity Severity { get; set; } = IssueSeverity.BLOCK;
        
        public long NmId { get; set; }
        public string VendorCode { get; set; } = string.Empty;
        public string SizeTech { get; set; } = string.Empty;

        public string FieldPath { get; set; } = string.Empty;
        public string RawMessageRu { get; set; } = string.Empty;
        public string TranslatedExplanationVi { get; set; } = string.Empty;
        public string EvidenceSummary { get; set; } = string.Empty;

        public FindingStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
        public string Fingerprint { get; set; } = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Đề xuất sửa đổi đối chiếu 3 nguồn theo Section 08 & 12
    /// </summary>
    public class MarkirovkaProposal : INotifyPropertyChanged
    {
        private ProposalState _state = ProposalState.DRAFT;
        private DeliveryState _delivery = DeliveryState.QUEUED;
        private ReadbackState _readback = ReadbackState.UNVERIFIED;
        private MarketplaceCheckState _mpCheck = MarketplaceCheckState.UNKNOWN;
        private bool _isSelected = true;

        public string ProposalId { get; set; } = Guid.NewGuid().ToString("N");
        public int Version { get; set; } = 1;
        public string TenantId { get; set; } = "DEFAULT_TENANT";
        public string ShopId { get; set; } = "DEFAULT_SHOP";

        public long NmId { get; set; }
        public long? ChrtId { get; set; }
        public string VendorCode { get; set; } = string.Empty;
        public string TechSize { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        // Tri-source values
        public string PhysicalGtin { get; set; } = string.Empty;
        public string PhysicalSize { get; set; } = string.Empty;
        
        public string NkGtin { get; set; } = string.Empty;
        public string NkStatus { get; set; } = string.Empty;
        public string NkTnved { get; set; } = string.Empty;

        public string WbCurrentGtin { get; set; } = string.Empty;
        public string WbCurrentTnved { get; set; } = string.Empty;
        public string WbCurrentBarcode { get; set; } = string.Empty;
        public bool WbNeedKiz { get; set; } = true;
        public bool WbKizMarked { get; set; } = false;

        // Desired Patch
        public string DesiredGtin { get; set; } = string.Empty;
        public string DesiredTnved { get; set; } = string.Empty;
        public string DesiredGender { get; set; } = string.Empty;
        public bool DesiredKizMarked { get; set; } = true;
        public string Justification { get; set; } = string.Empty;
        public List<string> Blockers { get; set; } = new();

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public ProposalState State
        {
            get => _state;
            set { _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanApprove)); }
        }

        public DeliveryState Delivery
        {
            get => _delivery;
            set { _delivery = value; OnPropertyChanged(); }
        }

        public ReadbackState Readback
        {
            get => _readback;
            set { _readback = value; OnPropertyChanged(); }
        }

        public MarketplaceCheckState MarketplaceCheck
        {
            get => _mpCheck;
            set { _mpCheck = value; OnPropertyChanged(); }
        }

        public bool CanApprove => State == ProposalState.READY || State == ProposalState.DRAFT;
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime? ApprovedAt { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}