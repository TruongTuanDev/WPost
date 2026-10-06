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

        private string _apiKey;
        private string _contentBaseUrl;
        private int _rateLimitDelayMs;
        private int _batchSize;
        private string _connectionStatus = "Chưa kiểm tra";
        private bool _isTesting = false;

        public string ApiKey { get => _apiKey; set => SetProperty(ref _apiKey, value); }
        public string ContentBaseUrl { get => _contentBaseUrl; set => SetProperty(ref _contentBaseUrl, value); }
        public int RateLimitDelayMs { get => _rateLimitDelayMs; set => SetProperty(ref _rateLimitDelayMs, value); }
        public int BatchSize { get => _batchSize; set => SetProperty(ref _batchSize, value); }
        public string ConnectionStatus { get => _connectionStatus; set => SetProperty(ref _connectionStatus, value); }
        public bool IsTesting { get => _isTesting; set => SetProperty(ref _isTesting, value); }

        public ICommand SaveSettingsCommand { get; }
        public ICommand TestConnectionCommand { get; }

        public SettingsViewModel(AppSettings settings, IWbApiClient apiClient)
        {
            _settings = settings;
            _apiClient = apiClient;

            _apiKey = settings.ApiKey;
            _contentBaseUrl = settings.ContentBaseUrl;
            _rateLimitDelayMs = settings.RateLimitDelayMs;
            _batchSize = settings.BatchSize;

            SaveSettingsCommand = new RelayCommand(SaveSettings);
            TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync());
        }

        public void SaveSettings()
        {
            _settings.ApiKey = ApiKey?.Trim() ?? string.Empty;
            _settings.ContentBaseUrl = ContentBaseUrl?.Trim() ?? "https://content-api.wildberries.ru";
            _settings.RateLimitDelayMs = RateLimitDelayMs;
            _settings.BatchSize = BatchSize;
            _settings.Save();

            _apiClient.UpdateConfiguration(_settings.ApiKey, _settings.ContentBaseUrl, _settings.RateLimitDelayMs);
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
    }
}
