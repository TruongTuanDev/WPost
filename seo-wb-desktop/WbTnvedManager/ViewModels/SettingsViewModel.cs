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
        private readonly CryptoProCertificateService _certService;

        private string _apiKey;
        private string _contentBaseUrl;
        private string _legalEntityInn;
        private string _nationalCatalogApiKey;
        private string _nationalCatalogBaseUrl;
        private string _omsId;
        private string _omsConnection;
        private string _kizReleaseMethod;
        private string _digitalSignatureInfo;
        private string _digitalSignatureStatus;
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
        public string OmsId { get => _omsId; set => SetProperty(ref _omsId, value); }
        public string OmsConnection { get => _omsConnection; set => SetProperty(ref _omsConnection, value); }
        public string KizReleaseMethod { get => _kizReleaseMethod; set => SetProperty(ref _kizReleaseMethod, value); }
        public string DigitalSignatureInfo { get => _digitalSignatureInfo; set => SetProperty(ref _digitalSignatureInfo, value); }
        public string DigitalSignatureStatus { get => _digitalSignatureStatus; set => SetProperty(ref _digitalSignatureStatus, value); }
        public int RateLimitDelayMs { get => _rateLimitDelayMs; set => SetProperty(ref _rateLimitDelayMs, value); }
        public int BatchSize { get => _batchSize; set => SetProperty(ref _batchSize, value); }
        public string ConnectionStatus { get => _connectionStatus; set => SetProperty(ref _connectionStatus, value); }
        public string NkConnectionStatus { get => _nkConnectionStatus; set => SetProperty(ref _nkConnectionStatus, value); }
        public bool IsTesting { get => _isTesting; set => SetProperty(ref _isTesting, value); }
        public bool IsTestingNk { get => _isTestingNk; set => SetProperty(ref _isTestingNk, value); }

        public System.Collections.ObjectModel.ObservableCollection<string> KizReleaseMethods { get; } = new()
        {
            "Sản xuất tại Nga",
            "Nhập khẩu vào Nga",
            "Uỷ quyền từ một người khác",
            "Tồn kho / Перемаркировка"
        };

        public ICommand SaveSettingsCommand { get; }
        public ICommand TestConnectionCommand { get; }
        public ICommand TestNkConnectionCommand { get; }
        public ICommand CheckDigitalSignatureCommand { get; }

        public SettingsViewModel(AppSettings settings, IWbApiClient apiClient, INationalCatalogConnector? nkConnector = null, CryptoProCertificateService? certService = null)
        {
            _settings = settings;
            _apiClient = apiClient;
            _nkConnector = nkConnector;
            _certService = certService ?? new CryptoProCertificateService();

            _apiKey = settings.ApiKey;
            _contentBaseUrl = settings.ContentBaseUrl;
            _legalEntityInn = string.IsNullOrWhiteSpace(settings.LegalEntityInn) ? "622903986965" : settings.LegalEntityInn;
            _nationalCatalogApiKey = settings.NationalCatalogApiKey;
            _nationalCatalogBaseUrl = settings.NationalCatalogBaseUrl;

            _omsId = string.IsNullOrWhiteSpace(settings.OmsId) ? "ede7e333-ee03-426b-868b-b18c84d08e1e" : settings.OmsId;
            _omsConnection = string.IsNullOrWhiteSpace(settings.OmsConnection) ? "4491fc8a-63bf-4df3-a277-a16f4b989cde" : settings.OmsConnection;
            _kizReleaseMethod = string.IsNullOrWhiteSpace(settings.KizReleaseMethod) ? "Sản xuất tại Nga" : settings.KizReleaseMethod;
            _digitalSignatureInfo = string.IsNullOrWhiteSpace(settings.DigitalSignatureInfo) 
                ? "4f9f19abc66f38829ca6ca9e50b191dd7d1f3d91 / INN 622903986965 / Hết hạn: 26.04.2027" 
                : settings.DigitalSignatureInfo;
            _digitalSignatureStatus = string.IsNullOrWhiteSpace(settings.DigitalSignatureStatus) ? "VERIFIED" : settings.DigitalSignatureStatus;

            _rateLimitDelayMs = settings.RateLimitDelayMs;
            _batchSize = settings.BatchSize;

            SaveSettingsCommand = new RelayCommand(SaveSettings);
            TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync());
            TestNkConnectionCommand = new RelayCommand(async () => await TestNkConnectionAsync());
            CheckDigitalSignatureCommand = new RelayCommand(CheckDigitalSignature);
        }

        public void SaveSettings()
        {
            _settings.ApiKey = ApiKey?.Trim() ?? string.Empty;
            _settings.ContentBaseUrl = ContentBaseUrl?.Trim() ?? "https://content-api.wildberries.ru";
            _settings.LegalEntityInn = LegalEntityInn?.Trim() ?? string.Empty;
            _settings.NationalCatalogApiKey = NationalCatalogApiKey?.Trim() ?? string.Empty;
            _settings.NationalCatalogBaseUrl = NationalCatalogBaseUrl?.Trim() ?? "https://api.catalog.crpt.ru";
            _settings.OmsId = OmsId?.Trim() ?? string.Empty;
            _settings.OmsConnection = OmsConnection?.Trim() ?? string.Empty;
            _settings.KizReleaseMethod = KizReleaseMethod ?? "Sản xuất tại Nga";
            _settings.DigitalSignatureInfo = DigitalSignatureInfo?.Trim() ?? string.Empty;
            _settings.DigitalSignatureStatus = DigitalSignatureStatus;
            _settings.RateLimitDelayMs = RateLimitDelayMs;
            _settings.BatchSize = BatchSize;
            _settings.Save();

            _apiClient.UpdateConfiguration(_settings.ApiKey, _settings.ContentBaseUrl, _settings.RateLimitDelayMs);
            _nkConnector?.Configure(_settings.NationalCatalogApiKey, _settings.NationalCatalogBaseUrl);

            if (Application.Current != null)
            {
                MessageBox.Show("Đã lưu cấu hình cài đặt thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void CheckDigitalSignature()
        {
            var result = _certService.VerifySignature(DigitalSignatureInfo, LegalEntityInn);
            DigitalSignatureInfo = result.FormattedInfo;
            DigitalSignatureStatus = result.StatusText;
            if (!string.IsNullOrEmpty(result.ExtractedInn))
            {
                LegalEntityInn = result.ExtractedInn;
            }

            if (Application.Current != null)
            {
                if (result.IsValid)
                {
                    MessageBox.Show($"Xác thực chữ ký số thành công!\n\n{result.FormattedInfo}\n\nTrạng thái: VERIFIED", "Chữ ký số hợp lệ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Không thể xác thực chữ ký số:\n{result.StatusText}", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
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
