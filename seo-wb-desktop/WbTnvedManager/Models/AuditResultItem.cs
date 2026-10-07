using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WbTnvedManager.Models
{
    public enum AuditStatus
    {
        MatchOk,            // Mã TNVED và Giới tính đều chuẩn
        TnvedMismatch,      // Mã TNVED khác mã chuẩn trong ma trận
        GenderMismatch,     // Giới tính bị thiếu hoặc sai
        BothMismatch,       // Cả TNVED và Giới tính đều sai/thiếu
        NoMatrixMatch,      // Chưa có rule trong ma trận (cần bổ sung ma trận)
        UpdatedSuccess,     // Đã cập nhật thành công lên sàn
        UpdateFailed        // Lỗi khi gửi API cập nhật
    }

    public class AuditResultItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private AuditStatus _status;
        private string _statusMessage = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public WbCardItem Card { get; set; } = new();

        public long NmId
        {
            get => Card.NmId;
            set { Card.NmId = value; OnPropertyChanged(); }
        }

        public string VendorCode
        {
            get => Card.VendorCode;
            set { Card.VendorCode = value; OnPropertyChanged(); }
        }

        public string Title
        {
            get => Card.Title;
            set { Card.Title = value; OnPropertyChanged(); }
        }

        public int SubjectId
        {
            get => Card.SubjectId;
            set { Card.SubjectId = value; OnPropertyChanged(); }
        }

        public string SubjectName
        {
            get => Card.SubjectName;
            set { Card.SubjectName = value; OnPropertyChanged(); }
        }

        // Current values from WB
        public string CurrentTnved { get; set; } = string.Empty;
        public string CurrentGender { get; set; } = string.Empty;
        public string DetectedMaterial { get; set; } = string.Empty;

        // Suggested standard values from Matrix
        public string SuggestedTnved { get; set; } = string.Empty;
        public string SuggestedGender { get; set; } = string.Empty;
        public string MatchReason { get; set; } = string.Empty;

        public System.Collections.Generic.List<IssueItem> Issues { get; set; } = new();
        public ProductReadinessSummary Readiness { get; set; } = new();
        public VariantAuditResult? SpecAuditDetails { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public AuditStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusBadge)); OnPropertyChanged(nameof(CanFix)); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string StatusBadge => Status switch
        {
            AuditStatus.MatchOk => "✅ Chuẩn",
            AuditStatus.TnvedMismatch => "⚠️ Sai TNVED",
            AuditStatus.GenderMismatch => "⚠️ Sai/Thiếu Giới tính",
            AuditStatus.BothMismatch => "❌ Sai TNVED & Giới tính",
            AuditStatus.NoMatrixMatch => "❓ Chưa có Ma trận",
            AuditStatus.UpdatedSuccess => "🎉 Đã sửa xong",
            AuditStatus.UpdateFailed => "⛔ Lỗi cập nhật",
            _ => "Unknown"
        };

        public bool CanFix => Status == AuditStatus.TnvedMismatch || 
                              Status == AuditStatus.GenderMismatch || 
                              Status == AuditStatus.BothMismatch ||
                              Status == AuditStatus.UpdateFailed;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
