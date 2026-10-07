using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WbTnvedManager.Models;
using WbTnvedManager.Services;

namespace WbTnvedManager.ViewModels
{
    public class BulkAuditViewModel : ViewModelBase
    {
        private readonly IWbApiClient _apiClient;
        private readonly CardAuditService _auditService;
        private readonly CardBulkUpdateService _bulkUpdateService;
        private readonly CardErrorTrackerService _errorTracker;
        private readonly SafeWbUpdatePipeline? _safePipeline;

        private List<AuditResultItem> _allAuditResults = new();
        private ObservableCollection<AuditResultItem> _filteredResults = new();
        private ObservableCollection<WbCardErrorItem> _recentErrors = new();

        private string _searchKeyword = string.Empty;
        private string _selectedFilter = "NeedsFix"; // "All", "NeedsFix", "MatchOk", "NoMatrix"
        private string _statusMessage = "Sẵn sàng quét danh sách sản phẩm.";
        private bool _isBusy = false;
        private int _progressPercent = 0;
        private CancellationTokenSource? _cts;

        // Statistics
        private int _totalCardsCount = 0;
        private int _matchedOkCount = 0;
        private int _needsFixCount = 0;
        private int _noMatrixCount = 0;
        private int _selectedCount = 0;
        private AuditResultItem? _selectedAuditItem;

        public ObservableCollection<AuditResultItem> FilteredResults => _filteredResults;
        public ObservableCollection<WbCardErrorItem> RecentErrors => _recentErrors;

        public AuditResultItem? SelectedAuditItem
        {
            get => _selectedAuditItem;
            set => SetProperty(ref _selectedAuditItem, value);
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set { if (SetProperty(ref _searchKeyword, value)) ApplyFilter(); }
        }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set { if (SetProperty(ref _selectedFilter, value)) ApplyFilter(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    (ScanCardsCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (BulkFixCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public int ProgressPercent
        {
            get => _progressPercent;
            set => SetProperty(ref _progressPercent, value);
        }

        public int TotalCardsCount { get => _totalCardsCount; set => SetProperty(ref _totalCardsCount, value); }
        public int MatchedOkCount { get => _matchedOkCount; set => SetProperty(ref _matchedOkCount, value); }
        public int NeedsFixCount { get => _needsFixCount; set => SetProperty(ref _needsFixCount, value); }
        public int NoMatrixCount { get => _noMatrixCount; set => SetProperty(ref _noMatrixCount, value); }
        public int SelectedCount { get => _selectedCount; set => SetProperty(ref _selectedCount, value); }

        public ICommand ScanCardsCommand { get; }
        public ICommand LoadSampleCardsCommand { get; }
        public ICommand BulkFixCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }
        public ICommand CheckErrorsCommand { get; }
        public ICommand ImportFileCommand { get; }
        public ICommand ExportReportCommand { get; }

        public BulkAuditViewModel(
            IWbApiClient apiClient,
            CardAuditService auditService,
            CardBulkUpdateService bulkUpdateService,
            CardErrorTrackerService errorTracker,
            SafeWbUpdatePipeline? safePipeline = null)
        {
            _apiClient = apiClient;
            _auditService = auditService;
            _bulkUpdateService = bulkUpdateService;
            _errorTracker = errorTracker;
            _safePipeline = safePipeline;

            ScanCardsCommand = new RelayCommand(async () => await ScanAndAuditAsync(), () => !IsBusy);
            LoadSampleCardsCommand = new RelayCommand(LoadSampleCards, () => !IsBusy);
            BulkFixCommand = new RelayCommand(async () => await ExecuteBulkFixAsync(), () => !IsBusy && _allAuditResults.Any(r => r.IsSelected && r.CanFix));
            CancelCommand = new RelayCommand(CancelOperation, () => IsBusy);
            SelectAllCommand = new RelayCommand(SelectAll);
            DeselectAllCommand = new RelayCommand(DeselectAll);
            CheckErrorsCommand = new RelayCommand(async () => await LoadErrorsAsync(), () => !IsBusy);
            ImportFileCommand = new RelayCommand(ImportFile, () => !IsBusy);
            ExportReportCommand = new RelayCommand(ExportReport, () => !IsBusy);

            // Auto-load sample cards on startup so UI is never blank
            LoadSampleCards();
        }

        private void ImportFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "File dữ liệu (*.csv;*.txt)|*.csv;*.txt|Tất cả file (*.*)|*.*",
                Title = "Chọn file danh mục sản phẩm (CSV)"
            };

            if (dialog.ShowDialog() == true)
            {
                var importResult = FileImportExportService.ImportFromDelimitedText(dialog.FileName);
                if (importResult.Success && importResult.ImportedCards.Count > 0)
                {
                    _allAuditResults = _auditService.AuditCards(importResult.ImportedCards);
                    ApplyFilter();
                    UpdateCounts();
                    StatusMessage = importResult.Message;
                    MessageBox.Show(importResult.Message + (importResult.Warnings.Count > 0 ? "\n\nCảnh báo:\n" + string.Join("\n", importResult.Warnings.Take(3)) : ""), "Nhập file thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(importResult.Message, "Lỗi nhập file", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ExportReport()
        {
            if (_allAuditResults.Count == 0)
            {
                MessageBox.Show("Chưa có dữ liệu để xuất báo cáo.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "File Excel CSV (*.csv)|*.csv",
                FileName = $"BaoCao_KiemTra_TNVED_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Lưu báo cáo kiểm tra"
            };

            if (dialog.ShowDialog() == true)
            {
                FileImportExportService.ExportAuditResultsToCsv(_allAuditResults, dialog.FileName);
                MessageBox.Show($"Đã xuất báo cáo thành công ra file:\n{dialog.FileName}", "Xuất báo cáo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void LoadSampleCards()
        {
            var sampleCards = new List<WbCardItem>
            {
                new()
                {
                    NmId = 184920192,
                    SubjectId = 105,
                    SubjectName = "Футболка",
                    VendorCode = "TSHIRT-FEMALE-01",
                    Title = "Футболка женская базовая оверсайз хлопок",
                    Characteristics = new List<WbCharacteristic>
                    {
                        new() { Id = 5, Name = "ТНВЭД", Value = "6403999600" }, // SAI: Gán mã giày dép
                        new() { Id = 8, Name = "Пол", Value = "Женский" },
                        new() { Id = 10, Name = "Состав", Value = "100% хлопок" }
                    }
                },
                new()
                {
                    NmId = 209148201,
                    SubjectId = 273,
                    SubjectName = "Джинсы",
                    VendorCode = "JEANS-MALE-BLACK",
                    Title = "Джинсы мужские классические плотные деним",
                    Characteristics = new List<WbCharacteristic>
                    {
                        new() { Id = 5, Name = "ТНВЭД", Value = "6204623100" }, // SAI: Gán mã quần nữ thay vì nam (6203423100)
                        new() { Id = 8, Name = "Пол", Value = "Мужской" },
                        new() { Id = 10, Name = "Состав", Value = "Хлопок 98%, эластан 2%" }
                    }
                },
                new()
                {
                    NmId = 195827103,
                    SubjectId = 156,
                    SubjectName = "Платье",
                    VendorCode = "DRESS-SILK-SUMMER",
                    Title = "Платье летнее женское шелковое вечернее",
                    Characteristics = new List<WbCharacteristic>
                    {
                        new() { Id = 5, Name = "ТНВЭД", Value = "0000000000" }, // SAI: Mã rác
                        new() { Id = 10, Name = "Состав", Value = "Шелк" }
                    }
                },
                new()
                {
                    NmId = 174920583,
                    SubjectId = 248,
                    SubjectName = "Худи",
                    VendorCode = "HOODIE-UNISEX-COTTON",
                    Title = "Худи оверсайз с начесом унисекс",
                    Characteristics = new List<WbCharacteristic>
                    {
                        new() { Id = 5, Name = "ТНВЭД", Value = "6110209900" }, // CHUẨN
                        new() { Id = 8, Name = "Пол", Value = "Унисекс" },
                        new() { Id = 10, Name = "Состав", Value = "Хлопок 80%, полиэстер 20%" }
                    }
                },
                new()
                {
                    NmId = 239105829,
                    SubjectId = 138,
                    SubjectName = "Рубашка",
                    VendorCode = "SHIRT-MEN-WHITE",
                    Title = "Рубашка мужская классическая белая",
                    Characteristics = new List<WbCharacteristic>
                    {
                        new() { Id = 5, Name = "ТНВЭД", Value = "6206300000" }, // SAI: Mã áo blouse nữ
                        new() { Id = 8, Name = "Пол", Value = "Мужской" },
                        new() { Id = 10, Name = "Состав", Value = "Хлопок" }
                    }
                },
                new()
                {
                    NmId = 284019284,
                    SubjectId = 217,
                    SubjectName = "Куртка",
                    VendorCode = "JACKET-LEATHER-MEN",
                    Title = "Куртка мужская кожаная демисезонная",
                    Characteristics = new List<WbCharacteristic>
                    {
                        new() { Id = 5, Name = "ТНВЭД", Value = "6201400000" }, // SAI: Gán mã vải tổng hợp thay vì mã da 4203100001
                        new() { Id = 8, Name = "Пол", Value = "Мужской" },
                        new() { Id = 10, Name = "Состав", Value = "Натуральная кожа" }
                    }
                }
            };

            _allAuditResults = _auditService.AuditCards(sampleCards);

            foreach (var r in _allAuditResults)
            {
                r.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(AuditResultItem.IsSelected))
                    {
                        UpdateCounts();
                        (BulkFixCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    }
                };
            }

            ApplyFilter();
            UpdateCounts();
            StatusMessage = $"Đã nạp {_allAuditResults.Count} thẻ sản phẩm mẫu. {NeedsFixCount} thẻ phát hiện sai mã cần sửa.";
        }

        public async Task ScanAndAuditAsync()
        {
            IsBusy = true;
            _cts = new CancellationTokenSource();
            ProgressPercent = 0;

            try
            {
                var progress = new Progress<string>(msg => StatusMessage = msg);
                var cards = await _apiClient.GetAllCardsAsync(progress, _cts.Token);

                StatusMessage = $"Đang đối chiếu {cards.Count} sản phẩm với Ma trận TNVED...";
                _allAuditResults = _auditService.AuditCards(cards);

                // Listen to selection changes for count
                foreach (var r in _allAuditResults)
                {
                    r.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(AuditResultItem.IsSelected))
                        {
                            UpdateCounts();
                            (BulkFixCommand as RelayCommand)?.RaiseCanExecuteChanged();
                        }
                    };
                }

                ApplyFilter();
                UpdateCounts();
                StatusMessage = $"Quét hoàn tất: {_allAuditResults.Count} thẻ sản phẩm. {NeedsFixCount} thẻ cần sửa đổi.";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Đã hủy thao tác quét.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi khi tải dữ liệu: {ex.Message}";
                MessageBox.Show($"Lỗi quét thẻ sản phẩm:\n{ex.Message}", "Lỗi API", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        public async Task ExecuteBulkFixAsync()
        {
            var selectedToFix = _allAuditResults.Where(r => r.IsSelected && r.CanFix).ToList();
            if (selectedToFix.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn ít nhất 1 sản phẩm bị sai để sửa đổi.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn cập nhật mã TNVED & Giới tính chuẩn cho {selectedToFix.Count} sản phẩm đã chọn trên Wildberries?\n\n" +
                "Toàn bộ dữ liệu gốc (ảnh, kích thước, SKUs, mô tả) sẽ được BẢO TOÀN NGUYÊN VẸN.",
                "Xác nhận cập nhật hàng loạt",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            _cts = new CancellationTokenSource();

            try
            {
                var progress = new Progress<string>(msg => StatusMessage = msg);
                int fixedCount = 0;

                if (_safePipeline != null)
                {
                    var pipelineResult = await _safePipeline.ExecuteSafeBatchUpdateAsync(
                        selectedToFix, 
                        new SellerAccount { Id = "DEFAULT_SELLER" }, 
                        progress, 
                        _cts.Token);
                    fixedCount = pipelineResult.SuccessCount;
                }
                else
                {
                    fixedCount = await _bulkUpdateService.ExecuteBulkFixAsync(selectedToFix, 50, progress, _cts.Token);
                }

                UpdateCounts();
                ApplyFilter();

                MessageBox.Show(
                    $"Đã cập nhật an toàn thành công cho {fixedCount}/{selectedToFix.Count} sản phẩm.\n" +
                    "Hệ thống đã xác minh bảo toàn 100% dữ liệu gốc (ảnh, kích thước, SKUs, mô tả).",
                    "Hoàn tất cập nhật an toàn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Auto refresh errors
                await LoadErrorsAsync();
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Đã dừng tiến trình cập nhật.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi cập nhật: {ex.Message}";
                MessageBox.Show($"Lỗi trong quá trình cập nhật:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        public async Task LoadErrorsAsync()
        {
            try
            {
                StatusMessage = "Đang kiểm tra báo cáo lỗi từ Wildberries...";
                var errors = await _errorTracker.FetchRecentErrorsAsync();
                RecentErrors.Clear();
                foreach (var err in errors)
                {
                    RecentErrors.Add(err);
                }
                StatusMessage = $"Đã tải {errors.Count} thông báo lỗi gần nhất từ sàn.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Không thể tải danh sách lỗi: {ex.Message}";
            }
        }

        private void ApplyFilter()
        {
            var query = _allAuditResults.AsEnumerable();

            // Status filter
            query = SelectedFilter switch
            {
                "NeedsFix" => query.Where(r => r.CanFix),
                "MatchOk" => query.Where(r => r.Status == AuditStatus.MatchOk || r.Status == AuditStatus.UpdatedSuccess),
                "NoMatrix" => query.Where(r => r.Status == AuditStatus.NoMatrixMatch),
                _ => query
            };

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                var kw = SearchKeyword.Trim().ToLowerInvariant();
                query = query.Where(r =>
                    r.VendorCode.ToLowerInvariant().Contains(kw) ||
                    r.Title.ToLowerInvariant().Contains(kw) ||
                    r.NmId.ToString().Contains(kw) ||
                    r.SubjectName.ToLowerInvariant().Contains(kw) ||
                    r.CurrentTnved.ToLowerInvariant().Contains(kw) ||
                    r.SuggestedTnved.ToLowerInvariant().Contains(kw));
            }

            _filteredResults.Clear();
            foreach (var item in query)
            {
                _filteredResults.Add(item);
            }
        }

        private void UpdateCounts()
        {
            TotalCardsCount = _allAuditResults.Count;
            MatchedOkCount = _allAuditResults.Count(r => r.Status == AuditStatus.MatchOk || r.Status == AuditStatus.UpdatedSuccess);
            NeedsFixCount = _allAuditResults.Count(r => r.CanFix);
            NoMatrixCount = _allAuditResults.Count(r => r.Status == AuditStatus.NoMatrixMatch);
            SelectedCount = _allAuditResults.Count(r => r.IsSelected && r.CanFix);
        }

        private void SelectAll()
        {
            foreach (var r in _filteredResults)
            {
                if (r.CanFix) r.IsSelected = true;
            }
            UpdateCounts();
        }

        private void DeselectAll()
        {
            foreach (var r in _filteredResults)
            {
                r.IsSelected = false;
            }
            UpdateCounts();
        }

        private void CancelOperation()
        {
            _cts?.Cancel();
        }
    }
}
