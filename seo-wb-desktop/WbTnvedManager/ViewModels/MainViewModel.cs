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
        private readonly TnvedMatrix104Engine _matrix104Engine;
        private readonly ProductVariantAuditEngine _specAuditEngine;
        private readonly CardAuditService _auditService;
        private readonly CardBulkUpdateService _bulkUpdateService;
        private readonly SafeWbUpdatePipeline _safePipeline;
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
        public MarkirovkaViewModel MarkirovkaVM { get; }
        public DocumentsViewModel DocumentsVM { get; }
        public SyncCenterViewModel SyncCenterVM { get; }
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
                    else if (value == 3) DocumentsVM.ApplyFilter();
                    else if (value == 4) SyncCenterVM.ApplyFilter();
                    else if (value == 5) MatrixManagerVM.RefreshList();
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
            var nkConnector = new NationalCatalogConnector();
            _selectorService = new TnvedSelectorService(_repository);
            _matrix104Engine = new TnvedMatrix104Engine();
            _specAuditEngine = new ProductVariantAuditEngine(_matrix104Engine);
            _auditService = new CardAuditService(_selectorService, _specAuditEngine);
            _bulkUpdateService = new CardBulkUpdateService(_apiClient);
            _safePipeline = new SafeWbUpdatePipeline(_apiClient, new RulesEngine(_selectorService));
            _errorTrackerService = new CardErrorTrackerService(_apiClient);
            _updateService = new AppUpdateService();

            // Initialize Child ViewModels
            CardBuilderVM = new CardBuilderViewModel(_repository, _selectorService, _apiClient);
            BulkAuditVM = new BulkAuditViewModel(_apiClient, _auditService, _bulkUpdateService, _errorTrackerService, _safePipeline);
            MarkirovkaVM = new MarkirovkaViewModel(_apiClient, nkConnector, _safePipeline, _repository, new SellerAccount { INN = "7707083893" });
            DocumentsVM = new DocumentsViewModel();
            SyncCenterVM = new SyncCenterViewModel();
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
                var result = await _updateService.CheckForUpdateAsync();
                _lastUpdateResult = result;
                if (result.HasUpdate)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        HasUpdateAvailable = true;
                        UpdateButtonText = $"⚡ Tải Bản Mới (v{result.LatestVersion})";
                    });
                }
            }
            catch { }
        }

        private async Task HandleUpdateActionAsync()
        {
            if (HasUpdateAvailable && _lastUpdateResult?.DownloadUrl != null)
            {
                IsDownloadingUpdate = true;
                UpdateButtonText = "⏳ Đang tải bản cập nhật...";
                var progress = new Progress<int>(p =>
                {
                    UpdateProgressPercent = p;
                    UpdateButtonText = $"⏳ Đang tải {p}%...";
                });

                bool ok = await _updateService.DownloadAndApplyUpdateAsync(_lastUpdateResult.DownloadUrl, progress);
                if (!ok)
                {
                    UpdateButtonText = "❌ Tải thất bại. Thử lại!";
                    IsDownloadingUpdate = false;
                }
            }
            else
            {
                IsCheckingUpdate = true;
                UpdateButtonText = "🔍 Đang kiểm tra...";
                var result = await _updateService.CheckForUpdateAsync();
                _lastUpdateResult = result;
                IsCheckingUpdate = false;

                if (result.HasUpdate)
                {
                    HasUpdateAvailable = true;
                    UpdateButtonText = $"⚡ Tải Bản Mới (v{result.LatestVersion})";
                }
                else
                {
                    HasUpdateAvailable = false;
                    UpdateButtonText = "✅ Đang là bản mới nhất";
                }
            }
        }
    }
}
