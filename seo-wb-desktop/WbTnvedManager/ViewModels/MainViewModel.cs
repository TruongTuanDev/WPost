using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WbTnvedManager.Data;
using WbTnvedManager.Models;
using WbTnvedManager.Services;

namespace WbTnvedManager.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AppSettings _settings;
        private readonly MatrixRepository _repository;
        private readonly IWbApiClient _apiClient;
        private readonly TnvedSelectorService _selectorService;
        private readonly CardAuditService _auditService;
        private readonly CardBulkUpdateService _bulkUpdateService;
        private readonly CardErrorTrackerService _errorTrackerService;
        private readonly AppUpdateService _updateService;

        private object _currentView;
        private int _selectedTabIndex = 0;

        private bool _hasUpdateAvailable = false;
        private bool _isCheckingUpdate = false;
        private bool _isDownloadingUpdate = false;
        private int _updateProgressPercent = 0;
        private string _updateButtonText = "🔄 Kiểm Tra Cập Nhật";
        private UpdateCheckResult? _lastUpdateResult;

        public CardBuilderViewModel CardBuilderVM { get; }
        public BulkAuditViewModel BulkAuditVM { get; }
        public MatrixManagerViewModel MatrixManagerVM { get; }
        public SettingsViewModel SettingsVM { get; }

        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    if (value == 0) CardBuilderVM.LoadSubjectsFromMatrix();
                    else if (value == 2) MatrixManagerVM.RefreshList();
                }
            }
        }

        public string AppVersion => AppUpdateService.CurrentVersion;

        public bool HasUpdateAvailable { get => _hasUpdateAvailable; set => SetProperty(ref _hasUpdateAvailable, value); }
        public bool IsCheckingUpdate { get => _isCheckingUpdate; set => SetProperty(ref _isCheckingUpdate, value); }
        public bool IsDownloadingUpdate { get => _isDownloadingUpdate; set => SetProperty(ref _isDownloadingUpdate, value); }
        public int UpdateProgressPercent { get => _updateProgressPercent; set => SetProperty(ref _updateProgressPercent, value); }
        public string UpdateButtonText { get => _updateButtonText; set => SetProperty(ref _updateButtonText, value); }

        public ICommand CheckOrApplyUpdateCommand { get; }

        public MainViewModel()
        {
            // Initialize Database
            DatabaseInitializer.InitializeDatabase();

            // Load Settings
            _settings = AppSettings.Load();

            // Initialize Services
            _repository = new MatrixRepository();
            _apiClient = new WbApiClient(_settings.ApiKey, _settings.ContentBaseUrl, _settings.RateLimitDelayMs);
            _selectorService = new TnvedSelectorService(_repository);
            _auditService = new CardAuditService(_selectorService);
            _bulkUpdateService = new CardBulkUpdateService(_apiClient);
            _errorTrackerService = new CardErrorTrackerService(_apiClient);
            _updateService = new AppUpdateService();

            // Initialize Child ViewModels
            CardBuilderVM = new CardBuilderViewModel(_repository, _selectorService);
            BulkAuditVM = new BulkAuditViewModel(_apiClient, _auditService, _bulkUpdateService, _errorTrackerService);
            MatrixManagerVM = new MatrixManagerViewModel(_repository, _apiClient);
            SettingsVM = new SettingsViewModel(_settings, _apiClient);

            _currentView = CardBuilderVM;

            CheckOrApplyUpdateCommand = new RelayCommand(async () => await HandleUpdateActionAsync(), () => !IsDownloadingUpdate);

            // Auto-check for updates in background on launch
            _ = Task.Run(async () => await SilentCheckUpdateAsync());
        }

        private async Task SilentCheckUpdateAsync()
        {
            try
            {
                var check = await _updateService.CheckForUpdateAsync();
                _lastUpdateResult = check;
                if (check.HasUpdate)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        HasUpdateAvailable = true;
                        UpdateButtonText = $"🚀 Có Bản Mới {check.LatestVersion} - Bấm Để Cập Nhật!";
                    });
                }
            }
            catch
            {
                // Ignore background check failure
            }
        }

        private async Task HandleUpdateActionAsync()
        {
            if (HasUpdateAvailable && _lastUpdateResult != null && !string.IsNullOrEmpty(_lastUpdateResult.DownloadUrl))
            {
                var confirm = MessageBox.Show(
                    $"Bạn có muốn tự động tải và cập nhật lên phiên bản {_lastUpdateResult.LatestVersion} ngay bây giờ?\n\n" +
                    "Ứng dụng sẽ tự động tải file mới và khởi động lại phiên bản mới.",
                    "Xác nhận cập nhật",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (confirm != MessageBoxResult.Yes) return;

                IsDownloadingUpdate = true;
                UpdateProgressPercent = 0;
                UpdateButtonText = "⏳ Đang tải bản mới (0%)...";

                var progress = new Progress<int>(percent =>
                {
                    UpdateProgressPercent = percent;
                    UpdateButtonText = $"⏳ Đang tải bản mới ({percent}%)...";
                });

                bool ok = await _updateService.DownloadAndApplyUpdateAsync(_lastUpdateResult.DownloadUrl, progress);
                if (!ok)
                {
                    IsDownloadingUpdate = false;
                    UpdateButtonText = $"🚀 Có Bản Mới {_lastUpdateResult.LatestVersion} - Bấm Để Cập Nhật!";
                    MessageBox.Show("Không thể tải bản cập nhật tự động. Vui lòng kiểm tra kết nối mạng.", "Lỗi cập nhật", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                IsCheckingUpdate = true;
                UpdateButtonText = "🔍 Đang kiểm tra...";

                var check = await _updateService.CheckForUpdateAsync();
                _lastUpdateResult = check;
                IsCheckingUpdate = false;

                if (check.HasUpdate)
                {
                    HasUpdateAvailable = true;
                    UpdateButtonText = $"🚀 Có Bản Mới {check.LatestVersion} - Bấm Để Cập Nhật!";
                    MessageBox.Show($"Đã tìm thấy phiên bản mới: {check.LatestVersion}\n\nHãy nhấn lại nút cập nhật để nâng cấp tự động!", "Bản cập nhật mới", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    HasUpdateAvailable = false;
                    UpdateButtonText = $"✅ {AppVersion} (Đang là bản mới nhất)";
                    MessageBox.Show($"Bạn đang sử dụng phiên bản mới nhất: {AppVersion}", "Đã cập nhật", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}
