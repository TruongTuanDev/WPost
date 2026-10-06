using System;
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

        private object _currentView;
        private int _selectedTabIndex = 0;

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
                    // Refresh data on tab switches if needed
                    if (value == 0) CardBuilderVM.LoadSubjectsFromMatrix();
                    else if (value == 2) MatrixManagerVM.RefreshList();
                }
            }
        }

        public string AppVersion => "v1.0.0 (Self-Contained Single File)";

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

            // Initialize Child ViewModels
            CardBuilderVM = new CardBuilderViewModel(_repository, _selectorService);
            BulkAuditVM = new BulkAuditViewModel(_apiClient, _auditService, _bulkUpdateService, _errorTrackerService);
            MatrixManagerVM = new MatrixManagerViewModel(_repository, _apiClient);
            SettingsVM = new SettingsViewModel(_settings, _apiClient);

            _currentView = CardBuilderVM;
        }
    }
}
