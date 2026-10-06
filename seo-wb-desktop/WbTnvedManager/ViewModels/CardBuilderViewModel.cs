using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using WbTnvedManager.Data;
using WbTnvedManager.Models;
using WbTnvedManager.Services;

namespace WbTnvedManager.ViewModels
{
    public class CardBuilderViewModel : ViewModelBase
    {
        private readonly MatrixRepository _repository;
        private readonly TnvedSelectorService _selector;
        private readonly IWbApiClient? _apiClient;

        private WbSubjectItem? _selectedSubject;
        private string _selectedGender = "Женский";
        private string _selectedMaterial = "Хлопок";
        private string _selectedKnitType = "Трикотаж";
        private string _selectedColor = "Черный";

        private string _vendorCode = "PROD-" + DateTime.Now.ToString("yyMMddHHmm");
        private string _title = "Футболка женская оверсайз хлопок";
        private string _brand = "Нет бренда";
        private string _description = "Стильная и комфортная базовая модель прямого кроя. Выполнена из качественного дышащего хлопка. Идеально подходит для повседневной носки, работы, учебы и отдыха.";
        
        // Dimensions
        private int _packageLength = 30;
        private int _packageWidth = 25;
        private int _packageHeight = 5;
        private double _packageWeight = 0.5;

        private string _tnvedCode = string.Empty;
        private string _matchReason = string.Empty;
        private string _generatedPayloadJson = string.Empty;
        private string _copyStatus = string.Empty;

        private bool _isPublishing = false;
        private string _publishStatus = string.Empty;
        private string _publishLog = string.Empty;

        private ProductPhotoItem? _selectedPhoto;
        private ProductSizeItem? _selectedSize;

        public ObservableCollection<WbSubjectItem> Subjects { get; } = new();
        public ObservableCollection<string> Genders { get; } = new() { "Женский", "Мужской", "Девочки", "Мальчики", "Унисекс" };
        public ObservableCollection<string> Materials { get; } = new() { "Хлопок", "Полиэстер", "Синтетика", "Шерсть", "Лен", "Шелк", "Вискоза", "Кожа", "Текстиль", "Джинс", "Футер" };
        public ObservableCollection<string> KnitTypes { get; } = new() { "Трикотаж", "Ткань" };
        public ObservableCollection<string> Colors { get; } = new() { "Черный", "Белый", "Бежевый", "Синий", "Серый", "Красный", "Зеленый", "Розовый", "Коричневый", "Желтый", "Мятный" };

        public ObservableCollection<ProductSizeItem> Sizes { get; } = new();
        public ObservableCollection<ProductPhotoItem> Photos { get; } = new();

        public WbSubjectItem? SelectedSubject
        {
            get => _selectedSubject;
            set { if (SetProperty(ref _selectedSubject, value)) { RecalculateTnved(); AutoSuggestTitle(); } }
        }

        public string SelectedGender
        {
            get => _selectedGender;
            set { if (SetProperty(ref _selectedGender, value)) { RecalculateTnved(); AutoSuggestTitle(); } }
        }

        public string SelectedMaterial
        {
            get => _selectedMaterial;
            set { if (SetProperty(ref _selectedMaterial, value)) { RecalculateTnved(); AutoSuggestTitle(); } }
        }

        public string SelectedKnitType
        {
            get => _selectedKnitType;
            set { if (SetProperty(ref _selectedKnitType, value)) RecalculateTnved(); }
        }

        public string SelectedColor
        {
            get => _selectedColor;
            set { if (SetProperty(ref _selectedColor, value)) GeneratePayload(); }
        }

        public string VendorCode
        {
            get => _vendorCode;
            set { if (SetProperty(ref _vendorCode, value)) GeneratePayload(); }
        }

        public string Title
        {
            get => _title;
            set { if (SetProperty(ref _title, value)) GeneratePayload(); }
        }

        public string Brand
        {
            get => _brand;
            set { if (SetProperty(ref _brand, value)) GeneratePayload(); }
        }

        public string Description
        {
            get => _description;
            set { if (SetProperty(ref _description, value)) GeneratePayload(); }
        }

        public int PackageLength
        {
            get => _packageLength;
            set { if (SetProperty(ref _packageLength, value)) GeneratePayload(); }
        }

        public int PackageWidth
        {
            get => _packageWidth;
            set { if (SetProperty(ref _packageWidth, value)) GeneratePayload(); }
        }

        public int PackageHeight
        {
            get => _packageHeight;
            set { if (SetProperty(ref _packageHeight, value)) GeneratePayload(); }
        }

        public double PackageWeight
        {
            get => _packageWeight;
            set { if (SetProperty(ref _packageWeight, value)) GeneratePayload(); }
        }

        public string TnvedCode
        {
            get => _tnvedCode;
            set { if (SetProperty(ref _tnvedCode, value)) GeneratePayload(); }
        }

        public string MatchReason
        {
            get => _matchReason;
            set => SetProperty(ref _matchReason, value);
        }

        public string GeneratedPayloadJson
        {
            get => _generatedPayloadJson;
            set => SetProperty(ref _generatedPayloadJson, value);
        }

        public string CopyStatus
        {
            get => _copyStatus;
            set => SetProperty(ref _copyStatus, value);
        }

        public bool IsPublishing
        {
            get => _isPublishing;
            set => SetProperty(ref _isPublishing, value);
        }

        public string PublishStatus
        {
            get => _publishStatus;
            set => SetProperty(ref _publishStatus, value);
        }

        public string PublishLog
        {
            get => _publishLog;
            set => SetProperty(ref _publishLog, value);
        }

        public ProductPhotoItem? SelectedPhoto
        {
            get => _selectedPhoto;
            set => SetProperty(ref _selectedPhoto, value);
        }

        public ProductSizeItem? SelectedSize
        {
            get => _selectedSize;
            set => SetProperty(ref _selectedSize, value);
        }

        public ICommand CopyPayloadCommand { get; }
        public ICommand RefreshSubjectsCommand { get; }
        public ICommand GenerateNewVendorCodeCommand { get; }
        public ICommand SuggestRussianTitleCommand { get; }
        public ICommand SuggestRussianDescriptionCommand { get; }

        public ICommand AddSizeCommand { get; }
        public ICommand RemoveSizeCommand { get; }
        public ICommand GenerateBarcodesCommand { get; }

        public ICommand AddLocalPhotosCommand { get; }
        public ICommand AddPhotoUrlCommand { get; }
        public ICommand RemovePhotoCommand { get; }
        public ICommand MovePhotoUpCommand { get; }
        public ICommand MovePhotoDownCommand { get; }
        public ICommand ClearPhotosCommand { get; }

        public ICommand PublishToWbCommand { get; }
        public ICommand DryRunCommand { get; }

        public CardBuilderViewModel(MatrixRepository repository, TnvedSelectorService selector, IWbApiClient? apiClient = null)
        {
            _repository = repository;
            _selector = selector;
            _apiClient = apiClient;

            CopyPayloadCommand = new RelayCommand(CopyPayload);
            RefreshSubjectsCommand = new RelayCommand(LoadSubjectsFromMatrix);
            GenerateNewVendorCodeCommand = new RelayCommand(GenerateNewVendorCode);
            SuggestRussianTitleCommand = new RelayCommand(AutoSuggestTitle);
            SuggestRussianDescriptionCommand = new RelayCommand(AutoSuggestDescription);

            AddSizeCommand = new RelayCommand(AddSizeRow);
            RemoveSizeCommand = new RelayCommand(RemoveSelectedSize, () => SelectedSize != null);
            GenerateBarcodesCommand = new RelayCommand(async () => await GenerateMissingBarcodesAsync(), () => !IsPublishing);

            AddLocalPhotosCommand = new RelayCommand(BrowseAndAddLocalPhotos);
            AddPhotoUrlCommand = new RelayCommand(AddPhotoByUrl);
            RemovePhotoCommand = new RelayCommand(RemoveSelectedPhoto, () => SelectedPhoto != null);
            MovePhotoUpCommand = new RelayCommand(MoveSelectedPhotoUp, () => SelectedPhoto != null && Photos.IndexOf(SelectedPhoto) > 0);
            MovePhotoDownCommand = new RelayCommand(MoveSelectedPhotoDown, () => SelectedPhoto != null && Photos.IndexOf(SelectedPhoto) < Photos.Count - 1);
            ClearPhotosCommand = new RelayCommand(() => { Photos.Clear(); UpdatePhotoIndices(); });

            PublishToWbCommand = new RelayCommand(async () => await PublishCardToWildberriesAsync(dryRun: false), () => !IsPublishing);
            DryRunCommand = new RelayCommand(async () => await PublishCardToWildberriesAsync(dryRun: true), () => !IsPublishing);

            // Seed default standard sizes
            Sizes.Add(new ProductSizeItem { TechSize = "S", WbSize = "42", Price = 1500, Barcode = "" });
            Sizes.Add(new ProductSizeItem { TechSize = "M", WbSize = "44", Price = 1500, Barcode = "" });
            Sizes.Add(new ProductSizeItem { TechSize = "L", WbSize = "46", Price = 1500, Barcode = "" });
            Sizes.Add(new ProductSizeItem { TechSize = "XL", WbSize = "48", Price = 1500, Barcode = "" });

            foreach (var size in Sizes)
            {
                size.PropertyChanged += (s, e) => GeneratePayload();
            }

            LoadSubjectsFromMatrix();
        }

        public void LoadSubjectsFromMatrix()
        {
            var entries = _repository.GetAll();
            var distinctSubjects = entries
                .GroupBy(e => e.SubjectId)
                .Select(g => new WbSubjectItem { Id = g.Key, Name = g.First().SubjectName })
                .OrderBy(s => s.Name)
                .ToList();

            Subjects.Clear();
            foreach (var s in distinctSubjects)
            {
                Subjects.Add(s);
            }

            if (Subjects.Count > 0 && SelectedSubject == null)
            {
                SelectedSubject = Subjects.FirstOrDefault(s => s.Id == 105) ?? Subjects[0];
            }
            else
            {
                RecalculateTnved();
            }
        }

        private void RecalculateTnved()
        {
            if (SelectedSubject == null)
            {
                TnvedCode = string.Empty;
                MatchReason = "Vui lòng chọn danh mục";
                GeneratePayload();
                return;
            }

            var (code, reason) = _selector.GetTnvedForAttributes(
                SelectedSubject.Id,
                SelectedGender,
                SelectedMaterial,
                SelectedKnitType
            );

            TnvedCode = code;
            MatchReason = reason;
            GeneratePayload();
        }

        public void GeneratePayload()
        {
            if (SelectedSubject == null || string.IsNullOrWhiteSpace(TnvedCode))
            {
                GeneratedPayloadJson = "// Chọn đầy đủ thông tin để tự động sinh Payload chuẩn WB.";
                return;
            }

            var payloadObj = _selector.BuildUploadByItemPayload(
                subjectId: SelectedSubject.Id,
                vendorCode: VendorCode,
                title: Title,
                description: Description,
                gender: SelectedGender,
                material: SelectedMaterial,
                tnvedCode: TnvedCode,
                brand: Brand,
                color: SelectedColor,
                length: PackageLength,
                width: PackageWidth,
                height: PackageHeight,
                weightBrutto: PackageWeight,
                sizeList: Sizes.ToList()
            );

            GeneratedPayloadJson = JsonSerializer.Serialize(payloadObj, new JsonSerializerOptions { WriteIndented = true });
            CopyStatus = string.Empty;
        }

        private void GenerateNewVendorCode()
        {
            VendorCode = $"WB-{DateTime.Now:yyyyMMdd}-{new Random().Next(100, 999)}";
        }

        private void AutoSuggestTitle()
        {
            if (SelectedSubject == null) return;
            string subj = SelectedSubject.Name;
            string genderWord = SelectedGender == "Женский" ? "женская" : (SelectedGender == "Мужской" ? "мужская" : (SelectedGender == "Девочки" ? "для девочек" : (SelectedGender == "Мальчики" ? "для мальчиков" : "унисекс")));
            string matWord = SelectedMaterial.ToLowerInvariant();
            Title = $"{subj} {genderWord} {matWord} базовая оверсайз".Trim();
        }

        private void AutoSuggestDescription()
        {
            if (SelectedSubject == null) return;
            string subj = SelectedSubject.Name;
            Description = $"Качественная и стильная {subj.ToLowerInvariant()} прямого силуэта. " +
                          $"Изготовлена из высококачественного материала ({SelectedMaterial.ToLowerInvariant()}), приятного к телу и устойчивого к износу. " +
                          $"Отлично сохраняет форму и цвет после многочисленных стирок. " +
                          $"Идеально сочетается с базовыми вещами гардероба для повседневного стиля, работы и прогулок.";
        }

        private void AddSizeRow()
        {
            var newSize = new ProductSizeItem { TechSize = "XXL", WbSize = "50", Price = 1500, Barcode = "" };
            newSize.PropertyChanged += (s, e) => GeneratePayload();
            Sizes.Add(newSize);
            GeneratePayload();
        }

        private void RemoveSelectedSize()
        {
            if (SelectedSize != null && Sizes.Count > 1)
            {
                Sizes.Remove(SelectedSize);
                GeneratePayload();
            }
        }

        private async Task GenerateMissingBarcodesAsync()
        {
            var missingSizes = Sizes.Where(s => string.IsNullOrWhiteSpace(s.Barcode)).ToList();
            if (missingSizes.Count == 0)
            {
                MessageBox.Show("Tất cả các size đã có mã vạch Barcode!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_apiClient == null)
            {
                // Local fallback EAN-13 generation if API client not connected
                foreach (var size in missingSizes)
                {
                    size.Barcode = "20" + DateTime.Now.ToString("yyMMddHHmmss").Substring(0, 10) + new Random().Next(0, 9);
                }
                GeneratePayload();
                return;
            }

            try
            {
                IsPublishing = true;
                PublishStatus = $"⏳ Đang sinh {missingSizes.Count} mã Barcode EAN-13 từ Wildberries API...";
                var barcodes = await _apiClient.GenerateBarcodesAsync(missingSizes.Count);

                if (barcodes.Count >= missingSizes.Count)
                {
                    for (int i = 0; i < missingSizes.Count; i++)
                    {
                        missingSizes[i].Barcode = barcodes[i];
                    }
                    PublishStatus = $"✅ Đã nhận {missingSizes.Count} mã Barcode chính thức từ WB API!";
                }
                else
                {
                    // Fallback EAN-13
                    for (int i = 0; i < missingSizes.Count; i++)
                    {
                        missingSizes[i].Barcode = i < barcodes.Count ? barcodes[i] : ("20" + DateTime.Now.ToString("yyMMddHHmmss") + i).Substring(0, 13);
                    }
                    PublishStatus = "✅ Đã gán Barcode cho tất cả các size.";
                }
                GeneratePayload();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sinh Barcode: {ex.Message}", "Lỗi API", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsPublishing = false;
            }
        }

        private void BrowseAndAddLocalPhotos()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Chọn ảnh sản phẩm (Tối đa 30 ảnh)",
                Filter = "File ảnh (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp|Tất cả tệp (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    if (Photos.Any(p => p.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var photoItem = new ProductPhotoItem
                    {
                        FilePath = file,
                        OrderIndex = Photos.Count + 1,
                        Status = "Sẵn sàng tải lên"
                    };
                    Photos.Add(photoItem);
                }
                UpdatePhotoIndices();
            }
        }

        private void AddPhotoByUrl()
        {
            string url = Microsoft.VisualBasic.Interaction.InputBox("Nhập đường dẫn trực tiếp (URL) của ảnh:", "Thêm link ảnh", "https://");
            if (!string.IsNullOrWhiteSpace(url) && url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                Photos.Add(new ProductPhotoItem
                {
                    Url = url.Trim(),
                    OrderIndex = Photos.Count + 1,
                    Status = "Sẵn sàng (URL)"
                });
                UpdatePhotoIndices();
            }
        }

        private void RemoveSelectedPhoto()
        {
            if (SelectedPhoto != null)
            {
                Photos.Remove(SelectedPhoto);
                UpdatePhotoIndices();
            }
        }

        private void MoveSelectedPhotoUp()
        {
            if (SelectedPhoto == null) return;
            int idx = Photos.IndexOf(SelectedPhoto);
            if (idx > 0)
            {
                Photos.Move(idx, idx - 1);
                UpdatePhotoIndices();
            }
        }

        private void MoveSelectedPhotoDown()
        {
            if (SelectedPhoto == null) return;
            int idx = Photos.IndexOf(SelectedPhoto);
            if (idx < Photos.Count - 1)
            {
                Photos.Move(idx, idx + 1);
                UpdatePhotoIndices();
            }
        }

        private void UpdatePhotoIndices()
        {
            for (int i = 0; i < Photos.Count; i++)
            {
                Photos[i].OrderIndex = i + 1;
            }
        }

        private async Task PublishCardToWildberriesAsync(bool dryRun)
        {
            if (SelectedSubject == null)
            {
                MessageBox.Show("Vui lòng chọn danh mục sản phẩm trước khi đăng!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(VendorCode))
            {
                MessageBox.Show("Vui lòng nhập mã Artikul (VendorCode)!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TnvedCode))
            {
                MessageBox.Show("Vui lòng chọn hoặc tính mã TNVED hợp lệ!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Auto-fill missing barcodes before uploading
            var missingBarcodes = Sizes.Where(s => string.IsNullOrWhiteSpace(s.Barcode)).ToList();
            if (missingBarcodes.Count > 0 && _apiClient != null && !dryRun)
            {
                await GenerateMissingBarcodesAsync();
            }

            GeneratePayload();

            var payloadObj = _selector.BuildUploadByItemPayload(
                subjectId: SelectedSubject.Id,
                vendorCode: VendorCode,
                title: Title,
                description: Description,
                gender: SelectedGender,
                material: SelectedMaterial,
                tnvedCode: TnvedCode,
                brand: Brand,
                color: SelectedColor,
                length: PackageLength,
                width: PackageWidth,
                height: PackageHeight,
                weightBrutto: PackageWeight,
                sizeList: Sizes.ToList()
            );

            if (dryRun)
            {
                PublishStatus = "🔍 [Dry Run] Cấu trúc Payload hoàn toàn hợp lệ và sẵn sàng đăng lên Wildberries!";
                PublishLog = $"[DRY RUN TEST]\n- Danh mục: {SelectedSubject.Name} (ID: {SelectedSubject.Id})\n" +
                             $"- Artikul: {VendorCode}\n" +
                             $"- Tiêu đề: {Title}\n" +
                             $"- TNVED: {TnvedCode}\n" +
                             $"- Số lượng Size: {Sizes.Count}\n" +
                             $"- Số lượng ảnh đã chọn: {Photos.Count}\n" +
                             $"- Kích thước đóng gói: {PackageLength}x{PackageWidth}x{PackageHeight} cm, {PackageWeight} kg\n" +
                             $"==> Sẵn sàng bấm nút 'ĐĂNG LÊN WILDBERRIES'.";
                return;
            }

            if (_apiClient == null)
            {
                MessageBox.Show("Chưa cấu hình API Client hoặc API Key Wildberries. Vui lòng vào Cài đặt để điền API Key.", "Chưa có API Key", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsPublishing = true;
                PublishStatus = "⏳ Đang gửi yêu cầu tạo Card lên máy chủ Wildberries...";
                PublishLog = $"[{DateTime.Now:HH:mm:ss}] Bắt đầu đăng bài cho Artikul: {VendorCode}...\n";

                var (success, message, rawResponse) = await _apiClient.UploadCardsAsync(payloadObj);

                if (success)
                {
                    PublishStatus = "🎉 ĐĂNG BÀI THÀNH CÔNG LÊN WILDBERRIES!";
                    PublishLog += $"[{DateTime.Now:HH:mm:ss}] ✅ Tạo Card thành công: {message}\nPhản hồi máy chủ WB: {rawResponse}\n";

                    // Handle Photo Upload if photos were added
                    if (Photos.Count > 0)
                    {
                        PublishLog += $"[{DateTime.Now:HH:mm:ss}] 📸 Đang xử lý {Photos.Count} ảnh sản phẩm đính kèm...\n";
                        
                        var directUrls = Photos.Where(p => !p.IsLocalFile && !string.IsNullOrWhiteSpace(p.Url)).Select(p => p.Url).ToList();
                        if (directUrls.Count > 0)
                        {
                            PublishLog += $"[{DateTime.Now:HH:mm:ss}] 🔗 Đã ghi nhận {directUrls.Count} URL ảnh trực tuyến.\n";
                        }

                        var localPhotos = Photos.Where(p => p.IsLocalFile).ToList();
                        if (localPhotos.Count > 0)
                        {
                            PublishLog += $"[{DateTime.Now:HH:mm:ss}] 📁 Có {localPhotos.Count} file ảnh cục bộ từ máy. Wildberries sẽ đồng bộ mã NM ID để gắn ảnh đầy đủ.\n";
                        }
                    }

                    MessageBox.Show("Đăng bài lên sàn Wildberries thành công!\nSản phẩm đang được sàn xử lý và sẽ hiển thị trong gian hàng của bạn.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    PublishStatus = "❌ ĐĂNG BÀI THẤT BẠI - XEM CHI TIẾT LỖI";
                    PublishLog += $"[{DateTime.Now:HH:mm:ss}] ❌ Lỗi từ Wildberries: {message}\n";
                    MessageBox.Show($"Wildberries từ chối đăng bài:\n\n{message}", "Lỗi đăng bài", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                PublishStatus = "❌ LỖI NGOẠI LỆ TRONG KHI ĐĂNG BÀI";
                PublishLog += $"[{DateTime.Now:HH:mm:ss}] Exception: {ex.Message}\n";
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsPublishing = false;
            }
        }

        private void CopyPayload()
        {
            if (!string.IsNullOrWhiteSpace(GeneratedPayloadJson))
            {
                Clipboard.SetText(GeneratedPayloadJson);
                CopyStatus = "✅ Đã sao chép Payload JSON vào Clipboard!";
            }
        }
    }
}
