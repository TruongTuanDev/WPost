using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace WbTnvedManager.Models
{
    public enum DocumentApplyScope
    {
        AllShop,        // Toàn bộ thẻ của shop
        SelectedCards,  // Các thẻ đang chọn
        BySubject,      // Theo nhóm sản phẩm / Danh mục WB
        ByTnved         // Theo mã ТН ВЭД
    }

    public class ShopProfile : INotifyPropertyChanged
    {
        private string _shopId = string.Empty;
        private string _shopName = string.Empty;
        private string _inn = string.Empty;
        private string _apiKey = string.Empty;
        private bool _autoApplyOnCreate = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string ShopId { get => _shopId; set => SetProperty(ref _shopId, value); }
        public string ShopName { get => _shopName; set => SetProperty(ref _shopName, value); }
        public string Inn { get => _inn; set => SetProperty(ref _inn, value); }
        public string ApiKey { get => _apiKey; set => SetProperty(ref _apiKey, value); }
        public bool AutoApplyOnCreate { get => _autoApplyOnCreate; set => SetProperty(ref _autoApplyOnCreate, value); }

        public string DisplayName => string.IsNullOrWhiteSpace(Inn) ? ShopName : $"{ShopName} (INN: {Inn})";

        public override string ToString() => DisplayName;

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class ShopDocumentPackage : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _shopId = string.Empty;
        private string _packageName = "Bộ giấy tờ mặc định";
        private string _docType = "Декларация соответствия";
        private string _docNumber = string.Empty;
        private DateTime _startDate = DateTime.Today;
        private DateTime _endDate = DateTime.Today.AddYears(3);
        private bool _isEndless = false;
        private DocumentApplyScope _targetScope = DocumentApplyScope.AllShop;
        private string _scopeFilterValue = string.Empty;
        private bool _autoApplyOnCreate = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Id { get => _id; set => SetProperty(ref _id, value); }
        public string ShopId { get => _shopId; set => SetProperty(ref _shopId, value); }
        public string PackageName { get => _packageName; set => SetProperty(ref _packageName, value); }
        public string DocType { get => _docType; set => SetProperty(ref _docType, value); }
        public string DocNumber { get => _docNumber; set => SetProperty(ref _docNumber, value); }
        public DateTime StartDate { get => _startDate; set => SetProperty(ref _startDate, value); }
        public DateTime EndDate { get => _endDate; set => SetProperty(ref _endDate, value); }
        public bool IsEndless { get => _isEndless; set => SetProperty(ref _isEndless, value); }
        public DocumentApplyScope TargetScope { get => _targetScope; set => SetProperty(ref _targetScope, value); }
        public string ScopeFilterValue { get => _scopeFilterValue; set => SetProperty(ref _scopeFilterValue, value); }
        public bool AutoApplyOnCreate { get => _autoApplyOnCreate; set => SetProperty(ref _autoApplyOnCreate, value); }

        public string FormattedValidity => IsEndless 
            ? $"Từ {StartDate:dd.MM.yyyy} (Бессрочно / Không thời hạn)" 
            : $"Từ {StartDate:dd.MM.yyyy} đến {EndDate:dd.MM.yyyy}";

        public string ScopeDescription => TargetScope switch
        {
            DocumentApplyScope.AllShop => "Toàn bộ thẻ của shop",
            DocumentApplyScope.SelectedCards => "Các thẻ được chọn",
            DocumentApplyScope.BySubject => $"Theo danh mục ({ScopeFilterValue})",
            DocumentApplyScope.ByTnved => $"Theo ТН ВЭД ({ScopeFilterValue})",
            _ => "Toàn bộ shop"
        };

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class CardDocumentReportItem : INotifyPropertyChanged
    {
        private long _nmId;
        private string _vendorCode = string.Empty;
        private string _title = string.Empty;
        private string _subjectName = string.Empty;
        private string _currentDocInfo = "Chưa có";
        private string _appliedDocNumber = string.Empty;
        private string _writeStatus = "Chờ gửi";
        private string _wbCheckStatus = "Chưa có thông tin kiểm tra từ WB";
        private string _errorMessage = string.Empty;
        private bool _isSelected = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public long NmId { get => _nmId; set => SetProperty(ref _nmId, value); }
        public string VendorCode { get => _vendorCode; set => SetProperty(ref _vendorCode, value); }
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string SubjectName { get => _subjectName; set => SetProperty(ref _subjectName, value); }
        public string CurrentDocInfo { get => _currentDocInfo; set => SetProperty(ref _currentDocInfo, value); }
        public string AppliedDocNumber { get => _appliedDocNumber; set => SetProperty(ref _appliedDocNumber, value); }
        public string WriteStatus { get => _writeStatus; set => SetProperty(ref _writeStatus, value); }
        public string WbCheckStatus { get => _wbCheckStatus; set => SetProperty(ref _wbCheckStatus, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

        public string CardUrl => $"https://www.wildberries.ru/catalog/{NmId}/detail.aspx";

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class DocumentPreviewSummary
    {
        public int TotalCards { get; set; }
        public int WillUpdateCount { get; set; }
        public int AlreadyHasDuplicateCount { get; set; }
        public int NeedsAttentionCount { get; set; }
        public int ErrorCount { get; set; }
    }
}
