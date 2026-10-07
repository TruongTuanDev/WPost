using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WbTnvedManager.Data;
using WbTnvedManager.Models;
using WbTnvedManager.Services;

namespace WbTnvedManager.ViewModels
{
    public class DocumentsViewModel : ViewModelBase
    {
        private readonly IWbApiClient? _apiClient;
        private readonly ShopDocumentRepository _repository;
        private readonly ShopDocumentUpdateService _updateService;

        private ShopProfile? _selectedShop;
        private ShopDocumentPackage? _selectedPackage;

        // Form Fields
        private string _formPackageName = string.Empty;
        private string _formDocType = "Декларация соответствия";
        private string _formDocNumber = string.Empty;
        private DateTime _formStartDate = DateTime.Today;
        private DateTime _formEndDate = DateTime.Today.AddYears(3);
        private bool _formIsEndless = false;
        private DocumentApplyScope _formTargetScope = DocumentApplyScope.AllShop;
        private string _formScopeFilterValue = string.Empty;
        private bool _formAutoApplyOnCreate = false;

        private bool _replaceExisting = false;
        private bool _isProcessing = false;
        private int _progressPercent = 0;
        private string _progressStatusText = "Sẵn sàng";
        private string _previewSummaryText = string.Empty;
        private string _validationMessage = string.Empty;

        // Lists
        public ObservableCollection<ShopProfile> Shops { get; } = new();
        public ObservableCollection<ShopDocumentPackage> Packages { get; } = new();
        public ObservableCollection<CardDocumentReportItem> ReportItems { get; } = new();
        public ObservableCollection<string> DocTypes { get; } = new()
        {
            "Декларация соответствия",
            "Сертификат соответствия",
            "Отказное письмо",
            "Свидетельство о гос. регистрации"
        };

        public ObservableCollection<DocumentApplyScope> TargetScopes { get; } = new()
        {
            DocumentApplyScope.AllShop,
            DocumentApplyScope.SelectedCards,
            DocumentApplyScope.BySubject,
            DocumentApplyScope.ByTnved
        };

        // Cached Cards for Preview / Apply
        private List<WbCardItem> _cachedCards = new();

        public ShopProfile? SelectedShop
        {
            get => _selectedShop;
            set
            {
                if (SetProperty(ref _selectedShop, value) && value != null)
                {
                    LoadPackagesForShop(value.ShopId);
                }
            }
        }

        public ShopDocumentPackage? SelectedPackage
        {
            get => _selectedPackage;
            set
            {
                if (SetProperty(ref _selectedPackage, value) && value != null)
                {
                    LoadPackageToForm(value);
                }
            }
        }

        public string FormPackageName { get => _formPackageName; set => SetProperty(ref _formPackageName, value); }
        public string FormDocType { get => _formDocType; set => SetProperty(ref _formDocType, value); }
        public string FormDocNumber { get => _formDocNumber; set => SetProperty(ref _formDocNumber, value); }
        public DateTime FormStartDate { get => _formStartDate; set => SetProperty(ref _formStartDate, value); }
        public DateTime FormEndDate { get => _formEndDate; set => SetProperty(ref _formEndDate, value); }
        public bool FormIsEndless
        {
            get => _formIsEndless;
            set
            {
                if (SetProperty(ref _formIsEndless, value))
                {
                    OnPropertyChanged(nameof(IsEndDateEnabled));
                }
            }
        }
        public bool IsEndDateEnabled => !FormIsEndless;

        public DocumentApplyScope FormTargetScope
        {
            get => _formTargetScope;
            set
            {
                if (SetProperty(ref _formTargetScope, value))
                {
                    OnPropertyChanged(nameof(IsScopeFilterVisible));
                }
            }
        }

        public bool IsScopeFilterVisible => FormTargetScope == DocumentApplyScope.BySubject || FormTargetScope == DocumentApplyScope.ByTnved;
        public string FormScopeFilterValue { get => _formScopeFilterValue; set => SetProperty(ref _formScopeFilterValue, value); }
        public bool FormAutoApplyOnCreate { get => _formAutoApplyOnCreate; set => SetProperty(ref _formAutoApplyOnCreate, value); }

        public bool ReplaceExisting { get => _replaceExisting; set => SetProperty(ref _replaceExisting, value); }
        public bool IsProcessing { get => _isProcessing; set => SetProperty(ref _isProcessing, value); }
        public int ProgressPercent { get => _progressPercent; set => SetProperty(ref _progressPercent, value); }
        public string ProgressStatusText { get => _progressStatusText; set => SetProperty(ref _progressStatusText, value); }
        public string PreviewSummaryText { get => _previewSummaryText; set => SetProperty(ref _previewSummaryText, value); }
        public string ValidationMessage { get => _validationMessage; set => SetProperty(ref _validationMessage, value); }

        public int TotalReportsCount => ReportItems.Count;
        public int SuccessReportsCount => ReportItems.Count(r => r.WriteStatus == "Đã ghi trên WB");
        public int DuplicateReportsCount => ReportItems.Count(r => r.WriteStatus == "Đã có giấy tờ trùng");
        public int ErrorReportsCount => ReportItems.Count(r => r.WriteStatus == "Lỗi cập nhật" || r.WriteStatus == "Chưa xác nhận kết quả");

        public ICommand SavePackageCommand { get; }
        public ICommand NewPackageCommand { get; }
        public ICommand DeletePackageCommand { get; }
        public ICommand AddShopCommand { get; }
        public ICommand PreviewApplyCommand { get; }
        public ICommand ApplyToWbCommand { get; }
        public ICommand RetryFailedCardsCommand { get; }

        public DocumentsViewModel(IWbApiClient? apiClient = null, ShopDocumentRepository? repository = null)
        {
            _repository = repository ?? new ShopDocumentRepository();
            _apiClient = apiClient;
            _updateService = new ShopDocumentUpdateService(_apiClient ?? new WbApiClient(""), _repository);

            SavePackageCommand = new RelayCommand(SavePackage);
            NewPackageCommand = new RelayCommand(ResetForm);
            DeletePackageCommand = new RelayCommand(DeletePackage, () => SelectedPackage != null);
            AddShopCommand = new RelayCommand(AddNewShop);
            PreviewApplyCommand = new RelayCommand(async () => await PreviewApplyAsync(), () => !IsProcessing);
            ApplyToWbCommand = new RelayCommand(async () => await ApplyToWbAsync(), () => !IsProcessing && SelectedPackage != null);
            RetryFailedCardsCommand = new RelayCommand(async () => await RetryFailedCardsAsync(), () => !IsProcessing && ErrorReportsCount > 0);

            LoadShops();
        }

        public void LoadShops()
        {
            Shops.Clear();
            var list = _repository.GetShops();
            foreach (var s in list) Shops.Add(s);

            if (Shops.Count > 0)
            {
                SelectedShop = Shops.First();
            }
        }

        public void LoadPackagesForShop(string shopId)
        {
            Packages.Clear();
            var list = _repository.GetPackagesByShop(shopId);
            foreach (var p in list) Packages.Add(p);

            if (Packages.Count > 0)
            {
                SelectedPackage = Packages.First();
            }
            else
            {
                ResetForm();
            }
        }

        private void LoadPackageToForm(ShopDocumentPackage pkg)
        {
            FormPackageName = pkg.PackageName;
            FormDocType = pkg.DocType;
            FormDocNumber = pkg.DocNumber;
            FormStartDate = pkg.StartDate;
            FormEndDate = pkg.EndDate;
            FormIsEndless = pkg.IsEndless;
            FormTargetScope = pkg.TargetScope;
            FormScopeFilterValue = pkg.ScopeFilterValue;
            FormAutoApplyOnCreate = pkg.AutoApplyOnCreate;
            ValidationMessage = string.Empty;
        }

        private void ResetForm()
        {
            SelectedPackage = null;
            FormPackageName = "Bộ hồ sơ mới";
            FormDocType = "Декларация соответствия";
            FormDocNumber = string.Empty;
            FormStartDate = DateTime.Today;
            FormEndDate = DateTime.Today.AddYears(3);
            FormIsEndless = false;
            FormTargetScope = DocumentApplyScope.AllShop;
            FormScopeFilterValue = string.Empty;
            FormAutoApplyOnCreate = false;
            ValidationMessage = string.Empty;
        }

        public bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(FormPackageName))
            {
                ValidationMessage = "⚠️ Vui lòng nhập tên bộ giấy tờ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(FormDocNumber))
            {
                ValidationMessage = "⚠️ Số giấy tờ (Номер) là bắt buộc. Không được để trống.";
                return false;
            }

            if (!FormIsEndless && FormEndDate < FormStartDate)
            {
                ValidationMessage = "❌ Ngày kết thúc hiệu lực không được nhỏ hơn ngày bắt đầu.";
                return false;
            }

            if (!FormIsEndless && FormEndDate < DateTime.Today)
            {
                ValidationMessage = "⚠️ Giấy tờ đã quá ngày hết hạn hiệu lực. Cần kiểm tra lại trước khi áp dụng.";
            }
            else
            {
                ValidationMessage = "✅ Thông tin hợp lệ.";
            }

            return true;
        }

        public void ApplyFilter()
        {
            // Refresh shop configurations when tab is navigated
            LoadShops();
        }

        public void SavePackage()
        {
            if (SelectedShop == null)
            {
                if (Application.Current != null)
                    MessageBox.Show("Vui lòng chọn hoặc thêm shop trước khi lưu.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateForm())
            {
                if (Application.Current != null)
                    MessageBox.Show(ValidationMessage, "Lỗi Nhập Liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cleanDocNum = FormDocNumber.Trim();
            var targetPkg = SelectedPackage ?? new ShopDocumentPackage
            {
                Id = "PKG_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                ShopId = SelectedShop.ShopId
            };

            targetPkg.ShopId = SelectedShop.ShopId;
            targetPkg.PackageName = FormPackageName.Trim();
            targetPkg.DocType = FormDocType;
            targetPkg.DocNumber = cleanDocNum;
            targetPkg.StartDate = FormStartDate;
            targetPkg.EndDate = FormEndDate;
            targetPkg.IsEndless = FormIsEndless;
            targetPkg.TargetScope = FormTargetScope;
            targetPkg.ScopeFilterValue = FormScopeFilterValue?.Trim() ?? string.Empty;
            targetPkg.AutoApplyOnCreate = FormAutoApplyOnCreate;

            _repository.SavePackage(targetPkg);

            if (FormAutoApplyOnCreate)
            {
                SelectedShop.AutoApplyOnCreate = true;
                _repository.SaveShop(SelectedShop);
            }

            LoadPackagesForShop(SelectedShop.ShopId);
            SelectedPackage = Packages.FirstOrDefault(p => p.Id == targetPkg.Id);

            if (Application.Current != null)
            {
                MessageBox.Show($"Đã lưu bộ giấy tờ '{targetPkg.PackageName}' cho shop '{SelectedShop.ShopName}' thành công!", "Lưu Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void DeletePackage()
        {
            if (SelectedPackage == null) return;
            if (Application.Current != null)
            {
                var confirm = MessageBox.Show($"Bạn có chắc chắn muốn xóa bộ giấy tờ '{SelectedPackage.PackageName}'?", "Xác Nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;
            }

            _repository.DeletePackage(SelectedPackage.Id);
            if (SelectedShop != null) LoadPackagesForShop(SelectedShop.ShopId);
        }

        public void AddNewShop()
        {
            string newId = "SHOP_" + DateTime.Now.ToString("yyMMddHHmmss");
            var newShop = new ShopProfile
            {
                ShopId = newId,
                ShopName = $"Shop mới {Shops.Count + 1}",
                Inn = "7707083893"
            };

            _repository.SaveShop(newShop);
            Shops.Add(newShop);
            SelectedShop = newShop;
        }

        public async Task PreviewApplyAsync()
        {
            if (SelectedShop == null || SelectedPackage == null)
            {
                if (Application.Current != null)
                    MessageBox.Show("Vui lòng chọn Shop và Bộ giấy tờ trước khi xem trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsProcessing = true;
            ProgressStatusText = "⏳ Đang tải toàn bộ danh sách thẻ của shop từ Wildberries...";
            ProgressPercent = 15;

            try
            {
                await EnsureCardsLoadedAsync();

                ProgressStatusText = "🔍 Đang đối soát và tính toán phạm vi áp dụng...";
                ProgressPercent = 60;

                var summary = _updateService.PreviewApply(_cachedCards, SelectedPackage, ReplaceExisting);
                PreviewSummaryText = $"📊 KẾT QUẢ XEM TRƯỚC: Tổng cộng {summary.TotalCards} thẻ | Sẽ cập nhật: {summary.WillUpdateCount} thẻ | Đã có giấy tờ trùng: {summary.AlreadyHasDuplicateCount} thẻ | Cần đối chiếu (khác ngày): {summary.NeedsAttentionCount} thẻ";

                ProgressPercent = 100;
                ProgressStatusText = "Hoàn tất xem trước.";

                if (Application.Current != null)
                {
                    MessageBox.Show(PreviewSummaryText, "Kết Quả Xem Trước", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ProgressStatusText = $"Lỗi: {ex.Message}";
                if (Application.Current != null)
                    MessageBox.Show($"Lỗi tải thẻ từ Wildberries: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public async Task ApplyToWbAsync()
        {
            if (SelectedShop == null || SelectedPackage == null)
            {
                if (Application.Current != null)
                    MessageBox.Show("Vui lòng chọn Shop và Bộ giấy tờ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Application.Current != null)
            {
                var confirm = MessageBox.Show(
                    $"Bạn có chắc chắn muốn áp dụng bộ giấy tờ '{SelectedPackage.DocNumber}' ({SelectedPackage.DocType}) lên các thẻ sản phẩm của shop '{SelectedShop.ShopName}'?\n\nPhạm vi: {SelectedPackage.ScopeDescription}\nChế độ: {(ReplaceExisting ? "Thay thế giấy tờ" : "Bổ sung giấy tờ (Chống trùng)")}",
                    "Xác Nhận Áp Dụng Lên Wildberries",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;
            }

            IsProcessing = true;
            ProgressPercent = 5;
            ProgressStatusText = "⏳ Đang tải đầy đủ thẻ sản phẩm qua các trang...";

            try
            {
                await EnsureCardsLoadedAsync();

                var scopedCards = _cachedCards.Where(c => ShopDocumentUpdateService.IsCardInScope(c, SelectedPackage, null)).ToList();

                ProgressStatusText = $"Đang cập nhật {scopedCards.Count} thẻ sản phẩm...";

                var progress = new Progress<(int Current, int Total, string StatusMessage)>(p =>
                {
                    ProgressPercent = (int)((double)p.Current / Math.Max(1, p.Total) * 100);
                    ProgressStatusText = $"[{ProgressPercent}%] {p.StatusMessage}";
                });

                var reports = await _updateService.ExecuteApplyAsync(
                    scopedCards,
                    SelectedShop,
                    SelectedPackage,
                    ReplaceExisting,
                    batchSize: 50,
                    progress: progress);

                ReportItems.Clear();
                foreach (var r in reports) ReportItems.Add(r);

                NotifyCounters();
                ProgressStatusText = $"✅ Hoàn tất! Đã ghi trên WB: {SuccessReportsCount} thẻ | Đã có trùng: {DuplicateReportsCount} thẻ | Lỗi: {ErrorReportsCount} thẻ";

                if (Application.Current != null)
                {
                    MessageBox.Show(ProgressStatusText, "Kết Quả Áp Dụng", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ProgressStatusText = $"Lỗi: {ex.Message}";
                if (Application.Current != null)
                    MessageBox.Show($"Lỗi áp dụng giấy tờ: {ex.Message}", "Lỗi Cập Nhật", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public async Task RetryFailedCardsAsync()
        {
            var failedReports = ReportItems.Where(r => r.WriteStatus == "Lỗi cập nhật" || r.WriteStatus == "Chưa xác nhận kết quả").ToList();
            if (failedReports.Count == 0) return;

            if (SelectedShop == null && Shops.Count > 0) SelectedShop = Shops.First();
            if (SelectedPackage == null && Packages.Count > 0) SelectedPackage = Packages.First();
            if (SelectedShop == null || SelectedPackage == null) return;

            var failedNmIds = failedReports.Select(r => r.NmId).ToHashSet();
            if (_cachedCards.Count == 0)
            {
                await EnsureCardsLoadedAsync();
            }

            var failedCards = _cachedCards.Where(c => failedNmIds.Contains(c.NmId)).ToList();
            if (failedCards.Count == 0)
            {
                failedCards = failedReports.Select(r => new WbCardItem
                {
                    NmId = r.NmId,
                    VendorCode = r.VendorCode,
                    Title = r.Title
                }).ToList();
            }

            IsProcessing = true;
            ProgressStatusText = $"Đang thử lại {failedCards.Count} thẻ bị lỗi...";

            try
            {
                var progress = new Progress<(int Current, int Total, string StatusMessage)>(p =>
                {
                    ProgressPercent = (int)((double)p.Current / Math.Max(1, p.Total) * 100);
                    ProgressStatusText = $"Thử lại: {p.StatusMessage}";
                });

                var newReports = await _updateService.ExecuteApplyAsync(
                    failedCards,
                    SelectedShop,
                    SelectedPackage,
                    ReplaceExisting,
                    batchSize: 50,
                    progress: progress);

                foreach (var nr in newReports)
                {
                    var exist = ReportItems.FirstOrDefault(r => r.NmId == nr.NmId);
                    if (exist != null)
                    {
                        exist.WriteStatus = nr.WriteStatus;
                        exist.WbCheckStatus = nr.WbCheckStatus;
                        exist.ErrorMessage = nr.ErrorMessage;
                    }
                }

                NotifyCounters();
                ProgressStatusText = $"Đã hoàn tất thử lại! Thẻ lỗi còn lại: {ErrorReportsCount}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task EnsureCardsLoadedAsync()
        {
            if (_cachedCards.Count > 0) return;

            if (_apiClient != null && _apiClient.HasApiKey)
            {
                _cachedCards = await _apiClient.GetAllCardsAsync();
            }

            if (_cachedCards.Count == 0)
            {
                // Fallback to local SQLite card cache if available
                var cacheRepo = new WbCardCacheRepository();
                var cachedResults = cacheRepo.LoadCachedAuditResults();
                if (cachedResults.Count > 0)
                {
                    _cachedCards = cachedResults.Select(c => c.Card).ToList();
                }
            }

            if (_cachedCards.Count == 0)
            {
                // Seed sample cards if completely offline for demonstration
                _cachedCards = GenerateMockCards();
            }
        }

        private List<WbCardItem> GenerateMockCards()
        {
            var list = new List<WbCardItem>();
            for (int i = 1; i <= 25; i++)
            {
                list.Add(new WbCardItem
                {
                    NmId = 180000000 + i,
                    VendorCode = $"MOD-TSHIRT-{i:D3}",
                    Title = $"Áo thun phong cách Nga cao cấp {i}",
                    SubjectId = 105,
                    SubjectName = "Футболка",
                    Characteristics = new()
                    {
                        new WbCharacteristic { Id = 5, Name = "ТНВЭД", Value = "6109100000" },
                        new WbCharacteristic { Id = 8, Name = "Пол", Value = "Мужской" }
                    }
                });
            }
            return list;
        }

        private void NotifyCounters()
        {
            OnPropertyChanged(nameof(TotalReportsCount));
            OnPropertyChanged(nameof(SuccessReportsCount));
            OnPropertyChanged(nameof(DuplicateReportsCount));
            OnPropertyChanged(nameof(ErrorReportsCount));
        }
    }
}
