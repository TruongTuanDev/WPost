using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WbTnvedManager.Models;
using WbTnvedManager.Services;

namespace WbTnvedManager.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly AppSettings _settings;
        private readonly IWbApiClient _apiClient;
        private readonly INationalCatalogConnector? _nkConnector;

        private string _apiKey;
        private string _contentBaseUrl;
        private string _legalEntityInn;
        private string _nationalCatalogApiKey;
        private string _nationalCatalogBaseUrl;
        private int _rateLimitDelayMs;
        private int _batchSize;
        private string _connectionStatus = "Chưa kiểm tra";
        private string _nkConnectionStatus = "Chưa kiểm tra";
        private bool _isTesting = false;
        private bool _isTestingNk = false;

        public string ApiKey { get => _apiKey; set => SetProperty(ref _apiKey, value); }
        public string ContentBaseUrl { get => _contentBaseUrl; set => SetProperty(ref _contentBaseUrl, value); }
        public string LegalEntityInn { get => _legalEntityInn; set => SetProperty(ref _legalEntityInn, value); }
        public string NationalCatalogApiKey { get => _nationalCatalogApiKey; set => SetProperty(ref _nationalCatalogApiKey, value); }
        public string NationalCatalogBaseUrl { get => _nationalCatalogBaseUrl; set => SetProperty(ref _nationalCatalogBaseUrl, value); }
        public int RateLimitDelayMs { get => _rateLimitDelayMs; set => SetProperty(ref _rateLimitDelayMs, value); }
        public int BatchSize { get => _batchSize; set => SetProperty(ref _batchSize, value); }
        public string ConnectionStatus { get => _connectionStatus; set => SetProperty(ref _connectionStatus, value); }
        public string NkConnectionStatus { get => _nkConnectionStatus; set => SetProperty(ref _nkConnectionStatus, value); }
        public bool IsTesting { get => _isTesting; set => SetProperty(ref _isTesting, value); }
        public bool IsTestingNk { get => _isTestingNk; set => SetProperty(ref _isTestingNk, value); }

        public ICommand SaveSettingsCommand { get; }
        public ICommand TestConnectionCommand { get; }
        public ICommand TestNkConnectionCommand { get; }

        public SettingsViewModel(AppSettings settings, IWbApiClient apiClient, INationalCatalogConnector? nkConnector = null)
        {
            _settings = settings;
            _apiClient = apiClient;
            _nkConnector = nkConnector;

            _apiKey = settings.ApiKey;
            _contentBaseUrl = settings.ContentBaseUrl;
            _legalEntityInn = settings.LegalEntityInn;
            _nationalCatalogApiKey = settings.NationalCatalogApiKey;
            _nationalCatalogBaseUrl = settings.NationalCatalogBaseUrl;
            _rateLimitDelayMs = settings.RateLimitDelayMs;
            _batchSize = settings.BatchSize;

            SaveSettingsCommand = new RelayCommand(SaveSettings);
            TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync());
            TestNkConnectionCommand = new RelayCommand(async () => await TestNkConnectionAsync());
        }

        public void SaveSettings()
        {
            _settings.ApiKey = ApiKey?.Trim() ?? string.Empty;
            _settings.ContentBaseUrl = ContentBaseUrl?.Trim() ?? "https://content-api.wildberries.ru";
            _settings.LegalEntityInn = LegalEntityInn?.Trim() ?? string.Empty;
            _settings.NationalCatalogApiKey = NationalCatalogApiKey?.Trim() ?? string.Empty;
            _settings.NationalCatalogBaseUrl = NationalCatalogBaseUrl?.Trim() ?? "https://api.catalog.crpt.ru";
            _settings.RateLimitDelayMs = RateLimitDelayMs;
            _settings.BatchSize = BatchSize;
            _settings.Save();

            _apiClient.UpdateConfiguration(_settings.ApiKey, _settings.ContentBaseUrl, _settings.RateLimitDelayMs);
            _nkConnector?.Configure(_settings.NationalCatalogApiKey, _settings.NationalCatalogBaseUrl);

            MessageBox.Show("Đã lưu cấu hình cài đặt thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async Task TestConnectionAsync()
        {
            IsTesting = true;
            ConnectionStatus = "Đang kết nối...";

            _apiClient.UpdateConfiguration(ApiKey?.Trim() ?? "", ContentBaseUrl?.Trim() ?? "", RateLimitDelayMs);
            bool ok = await _apiClient.TestConnectionAsync();

            IsTesting = false;
            if (ok)
            {
                ConnectionStatus = "✅ Kết nối API Wildberries THÀNH CÔNG!";
                MessageBox.Show("Kết nối API Wildberries thành công! Token hợp lệ.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ConnectionStatus = "❌ Kết nối thất bại. Vui lòng kiểm tra lại API Token.";
                MessageBox.Show("Không thể kết nối đến Wildberries API.\nVui lòng kiểm tra lại API Token hoặc quyền truy cập của Token.", "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task TestNkConnectionAsync()
        {
            if (_nkConnector == null) return;
            IsTestingNk = true;
            NkConnectionStatus = "Đang kết nối Национальный Каталог...";

            _nkConnector.Configure(NationalCatalogApiKey?.Trim() ?? "", NationalCatalogBaseUrl?.Trim() ?? "");
            bool ok = await _nkConnector.TestConnectionAsync(NationalCatalogApiKey?.Trim() ?? "", LegalEntityInn?.Trim() ?? "");

            IsTestingNk = false;
            if (ok)
            {
                NkConnectionStatus = "✅ Kết nối CRPT / National Catalog THÀNH CÔNG!";
                MessageBox.Show("Kết nối Национальный Каталог thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                NkConnectionStatus = "❌ Kết nối thất bại hoặc Token không hợp lệ.";
                MessageBox.Show("Không thể kết nối đến Национальный Каталог (CRPT).\nVui lòng kiểm tra API Key hoặc INN.", "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
