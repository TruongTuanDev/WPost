using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WbTnvedManager.Models
{
    public enum EvidenceStatus
    {
        VERIFIED,
        DECLARED,
        INFERRED,
        CONFLICT
    }

    public enum TriState
    {
        FALSE = 0,
        TRUE = 1,
        UNKNOWN = 2
    }

    public enum CommercialGender
    {
        MALE,
        FEMALE,
        UNISEX,
        UNKNOWN
    }

    public enum TariffGender
    {
        M,
        F,
        F_FALLBACK,
        ANY,
        UNKNOWN
    }

    public enum ConstructionKind
    {
        KNIT,
        WOVEN,
        NONWOVEN,
        MIXED,
        UNKNOWN
    }

    public enum AudienceKind
    {
        ADULT,
        CHILD,
        INFANT,
        UNKNOWN
    }

    public enum HeightScopeKind
    {
        NON_BABY,
        BABY_LE_86,
        NOT_APPLICABLE,
        UNKNOWN
    }

    public enum IdentifierFormatStatus
    {
        VALID,
        INVALID,
        MISSING,
        LOSS_SUSPECTED
    }

    public enum TariffCatalogStatus
    {
        ACTIVE_LEAF,
        NOT_A_LEAF,
        ABSENT_CONFIRMED,
        HISTORICAL,
        UNKNOWN
    }

    public enum ClassificationStatus
    {
        MATCH,
        MISMATCH,
        MISSING_CODE,
        NEEDS_DATA,
        NOT_COVERED,
        AMBIGUOUS,
        CONFLICT,
        RULE_ERROR
    }

    public enum EvidenceGrade
    {
        VERIFIED,
        DECLARED_ONLY,
        INFERRED_ONLY,
        CONFLICTED
    }

    public enum CommercialGenderCheckStatus
    {
        MATCH,
        MISMATCH,
        UNKNOWN,
        CONFLICT,
        NOT_APPLICABLE
    }

    public enum WbCompatibilityStatus
    {
        ALLOWED,
        DISALLOWED,
        UNKNOWN,
        NOT_CHECKED
    }

    public enum NcConsistencyStatus
    {
        MATCH,
        MISMATCH,
        UNKNOWN,
        NOT_CHECKED
    }

    public enum GtinLocalStatus
    {
        FORMAT_OK,
        CHECKSUM_OK,
        INVALID_FORMAT,
        BAD_CHECKSUM,
        UNSUPPORTED_FORMAT,
        MISSING
    }

    public enum GtinRegistryStatus
    {
        CONFIRMED,
        NOT_FOUND_CONFIRMED,
        INACTIVE_CONFIRMED,
        UNKNOWN,
        NOT_CHECKED
    }

    public enum MarkingScopeStatus
    {
        REQUIRED,
        NOT_REQUIRED_CONFIRMED,
        UNKNOWN,
        OUTSIDE_CONTEXT
    }

    public enum ApplicabilityStatus
    {
        APPLICABLE,
        OUTSIDE_CONTEXT,
        UNKNOWN
    }

    public enum CorrectionStatus
    {
        NONE,
        NORMALIZE_ONLY,
        PROPOSED,
        ELIGIBLE_LOCAL,
        BLOCKED,
        APPLIED_LOCAL,
        PENDING_EXTERNAL,
        VERIFIED_EXTERNAL,
        FAILED_EXTERNAL,
        OUTCOME_UNKNOWN,
        PARTIALLY_SYNCED
    }

    public enum ExecutionStatus
    {
        NOT_SCHEDULED,
        PENDING,
        IN_PROGRESS,
        SUCCEEDED_VERIFIED,
        FAILED,
        OUTCOME_UNKNOWN
    }

    public enum SyncStatus
    {
        NOT_REQUESTED,
        PENDING,
        IN_SYNC,
        PARTIALLY_SYNCED,
        FAILED,
        OUTCOME_UNKNOWN
    }

    public enum ExecutionMode
    {
        AUDIT,
        PROPOSE,
        APPLY_LOCAL,
        SYNC_AUTHORIZED
    }

    public enum LocalSemanticAutoPolicy
    {
        ELIGIBLE_IF_ALL_GATES,
        REVIEW_REQUIRED
    }

    public class Fact<T>
    {
        public T? Value { get; set; }
        public EvidenceStatus Status { get; set; } = EvidenceStatus.DECLARED;
        public List<string> EvidenceIds { get; set; } = new();
        public string ExtractionMethod { get; set; } = "MANUAL";
        public string? VerifiedBy { get; set; }
        public string? VerifiedAt { get; set; }
        public List<string> InputFactIds { get; set; } = new();
        public string? ResolverVersion { get; set; }
        public Dictionary<string, object>? DerivationTrace { get; set; }
    }

    public class Evidence
    {
        public string Id { get; set; } = string.Empty;
        public string Kind { get; set; } = "MANUFACTURER_SPEC";
        public string Locator { get; set; } = string.Empty;
        public string? Sha256 { get; set; }
        public string? IssuedAt { get; set; }
        public string ObservedAt { get; set; } = DateTime.UtcNow.ToString("o");
        public List<string> ProductScope { get; set; } = new();
    }

    public class CompositionComponent
    {
        public string FiberName { get; set; } = string.Empty;
        public decimal Percentage { get; set; }
        public string? ComponentPart { get; set; } = "SHELL"; // "SHELL", "LINING", "PADDING"
    }

    public class ProductVariantInput
    {
        public string SchemaVersion { get; set; } = "1.0";
        public string TenantId { get; set; } = "DEFAULT_TENANT";
        public string ProductId { get; set; } = string.Empty;
        public string VariantId { get; set; } = string.Empty;
        public string SourceRecordId { get; set; } = string.Empty;

        public string? Brand { get; set; }
        public string? ManufacturerModel { get; set; }
        public string? TitleRaw { get; set; }
        public string? TechnicalType { get; set; }

        public string? SellerCountry { get; set; } = "RU";
        public string? DestinationCountry { get; set; } = "RU";
        public string? NmId { get; set; }
        public string? ChrtId { get; set; }
        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }

        public CommercialGender CommercialGender { get; set; } = CommercialGender.UNKNOWN;
        public AudienceKind Audience { get; set; } = AudienceKind.UNKNOWN;
        public string? CutGender { get; set; }
        public string? FrontFastening { get; set; }

        public ConstructionKind Construction { get; set; } = ConstructionKind.UNKNOWN;
        public string? ProductForm { get; set; }
        public string? FabricKind { get; set; }
        public string? IntendedUse { get; set; } = "ORDINARY_APPAREL";
        public string? RetailClassification { get; set; } = "SEPARATE_GARMENT";

        public List<CompositionComponent> CompositionComponents { get; set; } = new();
        public string? DeterminingComponent { get; set; } = "SHELL";
        public bool IsFineKnitDensity { get; set; }
        public bool IsShirtKnitDensity { get; set; }
        public decimal? WeightGrams { get; set; }
        public bool HasVisor { get; set; }
        public string? BagVisibleSurface { get; set; }
        public bool IsBraSingle { get; set; }
        public bool IsBraBriefSet { get; set; }
        public bool IsTracksuitLinedSameMaterial { get; set; }
        public bool IsSkiSuit { get; set; }
        public bool IsScarfSquareLe60cm { get; set; }
        public bool IsBraidedOrStripFormed { get; set; }

        public string? ColorRaw { get; set; }
        public string? ColorCanonical { get; set; }
        public string? SizeSystem { get; set; }
        public string? SizeValue { get; set; }
        public string? SizeMeasureType { get; set; }
        public decimal? HeightMinCm { get; set; }
        public decimal? HeightMaxCm { get; set; }

        public string? TnvedRaw { get; set; }
        public string? GtinRaw { get; set; }
        public string? BarcodeRaw { get; set; }

        public string? NcCardId { get; set; }
        public string? NcGtin { get; set; }
        public string? NcStatus { get; set; }
        public string? NcDescriptionType { get; set; } = "FULL_DESCRIPTION";

        public List<Evidence> Evidence { get; set; } = new();
        public Dictionary<string, Fact<object>> Facts { get; set; } = new();
        public Dictionary<string, object> Raw { get; set; } = new();

        public bool IsVerifiedFact(string key)
        {
            if (Facts.TryGetValue(key, out var fact))
            {
                return fact.Status == EvidenceStatus.VERIFIED && fact.Value != null;
            }
            return false;
        }
    }

    public class ChecksSummary
    {
        public IdentifierFormatStatus IdentifierFormat { get; set; } = IdentifierFormatStatus.VALID;
        public TariffCatalogStatus TariffCatalog { get; set; } = TariffCatalogStatus.UNKNOWN;
        public ClassificationStatus Classification { get; set; } = ClassificationStatus.NEEDS_DATA;
        public EvidenceGrade EvidenceGrade { get; set; } = EvidenceGrade.DECLARED_ONLY;
        public CommercialGenderCheckStatus CommercialGenderCheck { get; set; } = CommercialGenderCheckStatus.UNKNOWN;
        public WbCompatibilityStatus WbCompatibility { get; set; } = WbCompatibilityStatus.NOT_CHECKED;
        public NcConsistencyStatus NcConsistency { get; set; } = NcConsistencyStatus.NOT_CHECKED;
        public GtinLocalStatus GtinLocal { get; set; } = GtinLocalStatus.MISSING;
        public GtinRegistryStatus GtinRegistry { get; set; } = GtinRegistryStatus.NOT_CHECKED;
        public MarkingScopeStatus MarkingScope { get; set; } = MarkingScopeStatus.UNKNOWN;
        public ApplicabilityStatus Applicability { get; set; } = ApplicabilityStatus.APPLICABLE;
    }

    public class ClassificationResultDetails
    {
        public string? CurrentTnved10 { get; set; }
        public List<string> CandidateTnved10 { get; set; } = new();
        public List<string> MatchedRuleIds { get; set; } = new();
        public TariffGender TariffGender { get; set; } = TariffGender.UNKNOWN;
        public CommercialGender CommercialGender { get; set; } = CommercialGender.UNKNOWN;
        public string MaterialResolution { get; set; } = string.Empty;
        public List<string> MissingFields { get; set; } = new();
        public List<string> SourceIds { get; set; } = new();
        public string ExplanationVi { get; set; } = string.Empty;
        public List<string> ReasonCodes { get; set; } = new();
    }

    public class ChangeItem
    {
        public string Path { get; set; } = string.Empty;
        public object? Before { get; set; }
        public object? After { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
        public List<string> RuleIds { get; set; } = new();
        public List<string> EvidenceIds { get; set; } = new();
    }

    public class CorrectionPlan
    {
        public CorrectionStatus Status { get; set; } = CorrectionStatus.NONE;
        public string Target { get; set; } = "LOCAL_DRAFT";
        public List<ChangeItem> Changes { get; set; } = new();
        public bool ExternalAllowed { get; set; }
        public List<string> BlockersExternal { get; set; } = new();
        public List<string> BlockersLocal { get; set; } = new();
    }

    public class VariantAuditResult
    {
        public string SchemaVersion { get; set; } = "1.0";
        public string VariantId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ClassificationDate { get; set; } = "2026-10-07";
        public string RulepackVersion { get; set; } = "TNVED-2026-10-07-v1";
        public ChecksSummary Checks { get; set; } = new();
        public ClassificationResultDetails Classification { get; set; } = new();
        public CorrectionPlan Correction { get; set; } = new();
        public ExecutionStatus LocalExecutionStatus { get; set; } = ExecutionStatus.NOT_SCHEDULED;
        public ExecutionStatus WbExecutionStatus { get; set; } = ExecutionStatus.NOT_SCHEDULED;
        public ExecutionStatus NcExecutionStatus { get; set; } = ExecutionStatus.NOT_SCHEDULED;
        public SyncStatus SyncStatus { get; set; } = SyncStatus.NOT_REQUESTED;
    }
}
