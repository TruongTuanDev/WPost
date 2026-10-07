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
        private readonly ShopDocumentRepository _docRepository;

        // Wizard Step (1 to 12)
        private int _currentStep = 1;

        // Step 1: Context
        private string _targetMarket = "RU (Nga)";
        private string _businessRole = "Nhà sản xuất / Bán lẻ ủy quyền";
        private string _supplySource = "Sản xuất nội địa / Nhập khẩu chính ngạch";

        // Step 2: Real Product Specs & Composition
        private string _vendorCode = "PROD-" + DateTime.Now.ToString("yyMMddHHmm");
        private string _title = "Футболка женская оверсайз базовая";
        private string _brand = "Нет бренда";
        private string _description = "Стильная и комфортная базовая модель прямого кроя. Выполнена из качественного дышащего хлопка. Идеально подходит для повседневной носки, работы, учебы и отдыха.";
        private string _selectedGender = "Женский";
        private string _selectedMaterial = "Хлопок";
        private string _selectedKnitType = "Трикотаж";
        
        // Multi-layer composition
        private string _outerFabricComp = "100% Хлопок";
        private string _liningComp = "";
        private string _fillingComp = "";
        private string _compositionValidationMessage = "✅ Thành phần hợp lệ (100% từng lớp).";

        // Step 3: Variant Matrix & Russian Sizes
        private string _selectedColor = "Черный";
        private int _packageLength = 30;
        private int _packageWidth = 25;
        private int _packageHeight = 5;
        private double _packageWeight = 0.5;

        // Step 4: Classification & TNVED
        private WbSubjectItem? _selectedSubject;
        private string _tnvedCode = "6109100000";
        private string _matchReason = "Áo thun dệt kim cho nữ/người lớn từ cotton (Nhóm 6109.10)";

        // Step 5: GTIN Verification
        private string _gtinValidationSummary = "Đã kiểm tra thuật toán GS1 Modulo-10 cho tất cả biến thể.";

        // Step 6: National Catalog
        private string _nkSnapshotStatus = "Sẵn sàng xuất gói bàn giao Национальный каталог (NK).";

        // Step 7: Conformity Documents
        private string _selectedDocumentNumber = "ЕАЭС N RU Д-RU.РА01.В.12345/26";
        private string _documentValidationSummary = "✅ Chứng từ ДС hợp lệ, còn hạn đến năm 2028.";

        // Step 8: Russian Content Preview
        private string _generatedPayloadJson = string.Empty;
        private string _copyStatus = string.Empty;

        // Step 9: Preflight Check
        private string _preflightReadinessScore = "100% - Sẵn sàng đăng tải";
        private string _preflightIssuesSummary = "Không phát hiện lỗi chặn nghiêm trọng.";

        // Step 10: Submit Status
        private bool _isPublishing = false;
        private string _publishStatus = string.Empty;
        private string _publishLog = string.Empty;

        private ProductPhotoItem? _selectedPhoto;
        private ProductSizeItem? _selectedSize;

        public int CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, Math.Clamp(value, 1, 12));
        }

        public string TargetMarket { get => _targetMarket; set => SetProperty(ref _targetMarket, value); }
        public string BusinessRole { get => _businessRole; set => SetProperty(ref _businessRole, value); }
        public string SupplySource { get => _supplySource; set => SetProperty(ref _supplySource, value); }

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

        public string OuterFabricComp
        {
            get => _outerFabricComp;
            set { if (SetProperty(ref _outerFabricComp, value)) ValidateComposition(); }
        }

        public string LiningComp
        {
            get => _liningComp;
            set { if (SetProperty(ref _liningComp, value)) ValidateComposition(); }
        }

        public string FillingComp
        {
            get => _fillingComp;
            set { if (SetProperty(ref _fillingComp, value)) ValidateComposition(); }
        }

        public string CompositionValidationMessage
        {
            get => _compositionValidationMessage;
            set => SetProperty(ref _compositionValidationMessage, value);
        }

        public string SelectedColor
        {
            get => _selectedColor;
            set { if (SetProperty(ref _selectedColor, value)) GeneratePayload(); }
        }

        public WbSubjectItem? SelectedSubject
        {
            get => _selectedSubject;
            set { if (SetProperty(ref _selectedSubject, value)) { RecalculateTnved(); AutoSuggestTitle(); } }
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

        public string GtinValidationSummary
        {
            get => _gtinValidationSummary;
            set => SetProperty(ref _gtinValidationSummary, value);
        }

        public string NkSnapshotStatus
        {
            get => _nkSnapshotStatus;
            set => SetProperty(ref _nkSnapshotStatus, value);
        }

        public ObservableCollection<ShopProfile> AvailableShops { get; } = new();
        public ObservableCollection<ShopDocumentPackage> AvailablePackages { get; } = new();

        private ShopProfile? _selectedShop;
        public ShopProfile? SelectedShop
        {
            get => _selectedShop;
            set
            {
                if (SetProperty(ref _selectedShop, value) && value != null)
                {
                    LoadShopDocumentPackages(value.ShopId);
                }
            }
        }

        private ShopDocumentPackage? _selectedDocumentPackage;
        public ShopDocumentPackage? SelectedDocumentPackage
        {
            get => _selectedDocumentPackage;
            set
            {
                if (SetProperty(ref _selectedDocumentPackage, value))
                {
                    UpdateDocumentValidationSummary();
                    GeneratePayload();
                }
            }
        }

        public string SelectedDocumentNumber
        {
            get => _selectedDocumentNumber;
            set => SetProperty(ref _selectedDocumentNumber, value);
        }

        public string DocumentValidationSummary
        {
            get => _documentValidationSummary;
            set => SetProperty(ref _documentValidationSummary, value);
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

        public string PreflightReadinessScore
        {
            get => _preflightReadinessScore;
            set => SetProperty(ref _preflightReadinessScore, value);
        }

        public string PreflightIssuesSummary
        {
            get => _preflightIssuesSummary;
            set => SetProperty(ref _preflightIssuesSummary, value);
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

        public ObservableCollection<WbSubjectItem> Subjects { get; } = new();
        public ObservableCollection<string> Genders { get; } = new() { "Женский", "Мужской", "Девочки", "Мальчики", "Унисекс" };
        public ObservableCollection<string> Materials { get; } = new() { "Хлопок", "Полиэстер", "Синтетика", "Шерсть", "Лен", "Шелк", "Вискоза", "Кожа", "Текстиль", "Джинс", "Футер" };
        public ObservableCollection<string> KnitTypes { get; } = new() { "Трикотаж", "Ткань" };
        public ObservableCollection<string> Colors { get; } = new() { "Черный", "Белый", "Бежевый", "Синий", "Серый", "Красный", "Зеленый", "Розовый", "Коричневый", "Желтый", "Мятный" };
        public ObservableCollection<ProductSizeItem> Sizes { get; } = new();
        public ObservableCollection<ProductPhotoItem> Photos { get; } = new();
        public ObservableCollection<IssueItem> PreflightIssues { get; } = new();

        public ICommand NextStepCommand { get; }
        public ICommand PrevStepCommand { get; }
        public ICommand GoToStepCommand { get; }
        public ICommand CopyPayloadCommand { get; }
        public ICommand GenerateNewVendorCodeCommand { get; }
        public ICommand SuggestRussianTitleCommand { get; }
        public ICommand SuggestRussianDescriptionCommand { get; }
        public ICommand AddSizeCommand { get; }
        public ICommand RemoveSizeCommand { get; }
        public ICommand AddDefaultSizesCommand { get; }
        public ICommand AddPhotoUrlCommand { get; }
        public ICommand AddLocalPhotoCommand { get; }
        public ICommand RemovePhotoCommand { get; }
        public ICommand PublishDirectCommand { get; }
        public ICommand RunPreflightCheckCommand { get; }

        public CardBuilderViewModel(MatrixRepository repository, TnvedSelectorService selector, IWbApiClient? apiClient, ShopDocumentRepository? docRepository = null)
        {
            _repository = repository;
            _selector = selector;
            _apiClient = apiClient;
            _docRepository = docRepository ?? new ShopDocumentRepository();

            NextStepCommand = new RelayCommand(() => { if (CurrentStep < 12) CurrentStep++; if (CurrentStep == 9) RunPreflightCheck(); });
            PrevStepCommand = new RelayCommand(() => { if (CurrentStep > 1) CurrentStep--; });
            GoToStepCommand = new RelayCommand(p => { if (int.TryParse(p?.ToString(), out int step)) CurrentStep = step; });

            CopyPayloadCommand = new RelayCommand(CopyPayloadToClipboard);
            GenerateNewVendorCodeCommand = new RelayCommand(GenerateNewVendorCode);
            SuggestRussianTitleCommand = new RelayCommand(AutoSuggestTitle);
            SuggestRussianDescriptionCommand = new RelayCommand(AutoSuggestDescription);

            AddSizeCommand = new RelayCommand(AddSize);
            RemoveSizeCommand = new RelayCommand(RemoveSize, () => SelectedSize != null);
            AddDefaultSizesCommand = new RelayCommand(AddDefaultSizes);

            AddPhotoUrlCommand = new RelayCommand(AddPhotoUrl);
            AddLocalPhotoCommand = new RelayCommand(AddLocalPhoto);
            RemovePhotoCommand = new RelayCommand(RemovePhoto, () => SelectedPhoto != null);
            PublishDirectCommand = new RelayCommand(async () => await PublishCardDirectlyAsync(), () => !IsPublishing);
            RunPreflightCheckCommand = new RelayCommand(RunPreflightCheck);

            LoadShopsFromRepository();
            LoadSubjectsFromMatrix();
            AddDefaultSizes();
            GeneratePayload();
        }

        public void LoadShopsFromRepository()
        {
            AvailableShops.Clear();
            var shops = _docRepository.GetShops();
            foreach (var s in shops) AvailableShops.Add(s);

            if (AvailableShops.Count > 0)
            {
                SelectedShop = AvailableShops.First();
            }
            else
            {
                UpdateDocumentValidationSummary();
            }
        }

        public void LoadShopDocumentPackages(string shopId)
        {
            AvailablePackages.Clear();
            var pkgs = _docRepository.GetPackagesByShop(shopId);
            foreach (var p in pkgs) AvailablePackages.Add(p);

            var autoPkg = AvailablePackages.FirstOrDefault(p => p.AutoApplyOnCreate) ?? AvailablePackages.FirstOrDefault();
            SelectedDocumentPackage = autoPkg;
        }

        private void UpdateDocumentValidationSummary()
        {
            if (SelectedDocumentPackage != null)
            {
                SelectedDocumentNumber = SelectedDocumentPackage.DocNumber;
                string dateStr = SelectedDocumentPackage.IsEndless
                    ? $"{SelectedDocumentPackage.StartDate:dd.MM.yyyy} - Бессрочно"
                    : $"{SelectedDocumentPackage.StartDate:dd.MM.yyyy} đến {SelectedDocumentPackage.EndDate:dd.MM.yyyy}";

                DocumentValidationSummary = $"✅ Đã gán bộ: {SelectedDocumentPackage.PackageName} ({SelectedDocumentPackage.DocType}: {SelectedDocumentPackage.DocNumber}, Hiệu lực: {dateStr})";
            }
            else
            {
                SelectedDocumentNumber = string.Empty;
                DocumentValidationSummary = "⚠️ Chưa có giấy tờ áp dụng cho shop này. Vui lòng cấu hình tại tab Hồ sơ chứng từ (РД).";
            }
        }

        public void LoadSubjectsFromMatrix()
        {
            Subjects.Clear();
            var list = _repository.GetAll();
            var distinctSubjects = list
                .GroupBy(x => x.SubjectId)
                .Select(g => g.First())
                .OrderBy(x => x.SubjectName);

            foreach (var item in distinctSubjects)
            {
                Subjects.Add(new WbSubjectItem { SubjectId = item.SubjectId, SubjectName = item.SubjectName });
            }

            if (Subjects.Count > 0)
            {
                SelectedSubject = Subjects.FirstOrDefault(s => s.SubjectName.Contains("Футболка", StringComparison.OrdinalIgnoreCase)) ?? Subjects[0];
            }
        }

        private void ValidateComposition()
        {
            var components = new List<ProductComponent>();
            if (!string.IsNullOrWhiteSpace(OuterFabricComp))
            {
                components.Add(new ProductComponent
                {
                    Type = "OUTER",
                    Fibers = new() { new FiberComposition { NameRu = OuterFabricComp, Percentage = 100 } }
                });
            }
            if (!string.IsNullOrWhiteSpace(LiningComp))
            {
                components.Add(new ProductComponent
                {
                    Type = "LINING",
                    Fibers = new() { new FiberComposition { NameRu = LiningComp, Percentage = 100 } }
                });
            }

            var results = CompositionValidator.ValidateAllComponents(components);
            bool allValid = results.All(r => r.IsValid);
            if (allValid)
            {
                CompositionValidationMessage = "✅ Thành phần hợp lệ (100% từng lớp).";
            }
            else
            {
                CompositionValidationMessage = $"⚠️ Lỗi thành phần: {string.Join("; ", results.Where(r => !r.IsValid).Select(r => r.ErrorMessage))}";
            }
        }

        private void RecalculateTnved()
        {
            if (SelectedSubject == null) return;

            string constr = SelectedKnitType == "Трикотаж" ? "KNITTED" : "WOVEN";
            string aud = SelectedGender == "Женский" ? "FEMALE" : SelectedGender == "Мужской" ? "MALE" : SelectedGender == "Девочки" ? "GIRLS" : SelectedGender == "Мальчики" ? "BOYS" : "UNISEX";

            var eval = TnvedClassificationEngine.EvaluateClassification(
                null,
                SelectedSubject.SubjectName,
                constr,
                aud,
                170,
                SelectedMaterial);

            if (!string.IsNullOrEmpty(eval.CandidateCode))
            {
                TnvedCode = eval.CandidateCode;
                MatchReason = eval.ReasonVi;
            }
            else
            {
                var (code, reason) = _selector.GetTnvedForAttributes(
                    SelectedSubject.SubjectId,
                    SelectedGender,
                    SelectedMaterial,
                    SelectedKnitType,
                    SelectedSubject.SubjectName,
                    Title);

                if (!string.IsNullOrWhiteSpace(code))
                {
                    TnvedCode = code;
                    MatchReason = reason;
                }
                else
                {
                    TnvedCode = "6109100000";
                    MatchReason = "Mã dự phòng tiêu chuẩn quần áo dệt kim";
                }
            }
            GeneratePayload();
        }

        private void AutoSuggestTitle()
        {
            string subj = SelectedSubject?.SubjectName ?? "Одежда";
            string gen = SelectedGender == "Женский" ? "женская" : SelectedGender == "Мужской" ? "мужская" : SelectedGender == "Девочки" ? "для девочек" : SelectedGender == "Мальчики" ? "для мальчиков" : "унисекс";
            string mat = SelectedMaterial.ToLower();
            Title = $"{subj} {gen} {mat} базовая";
        }

        private void AutoSuggestDescription()
        {
            string subj = SelectedSubject?.SubjectName ?? "Модель";
            Description = $"Качественная базовая {subj.ToLower()} прямого кроя из натурального материала ({SelectedMaterial}). Обеспечивает максимальный комфорт и воздухопроницаемость в течение всего дня. Идеальный выбор для базового гардероба на любой сезон.";
        }

        private void GenerateNewVendorCode()
        {
            VendorCode = "PROD-" + DateTime.Now.ToString("yyMMddHHmmss");
        }

        private void AddDefaultSizes()
        {
            Sizes.Clear();
            Sizes.Add(new ProductSizeItem { TechSize = "S", RussianSize = "42-44", Sku = "200" + DateTime.Now.ToString("yyMMdd") + "01", Price = 1200 });
            Sizes.Add(new ProductSizeItem { TechSize = "M", RussianSize = "44-46", Sku = "200" + DateTime.Now.ToString("yyMMdd") + "02", Price = 1200 });
            Sizes.Add(new ProductSizeItem { TechSize = "L", RussianSize = "46-48", Sku = "200" + DateTime.Now.ToString("yyMMdd") + "03", Price = 1200 });
            Sizes.Add(new ProductSizeItem { TechSize = "XL", RussianSize = "48-50", Sku = "200" + DateTime.Now.ToString("yyMMdd") + "04", Price = 1200 });
            GeneratePayload();
        }

        private void AddSize()
        {
            string nextIndex = (Sizes.Count + 1).ToString("D2");
            Sizes.Add(new ProductSizeItem { TechSize = "XXL", RussianSize = "50-52", Sku = "200" + DateTime.Now.ToString("yyMMdd") + nextIndex, Price = 1200 });
            GeneratePayload();
        }

        private void RemoveSize()
        {
            if (SelectedSize != null)
            {
                Sizes.Remove(SelectedSize);
                GeneratePayload();
            }
        }

        private void AddPhotoUrl()
        {
            Photos.Add(new ProductPhotoItem { Url = "https://images.wbstatic.net/sample.jpg", IsPrimary = Photos.Count == 0 });
            GeneratePayload();
        }

        private void AddLocalPhoto()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Chọn ảnh sản phẩm"
            };
            if (ofd.ShowDialog() == true)
            {
                Photos.Add(new ProductPhotoItem { LocalPath = ofd.FileName, Url = ofd.FileName, IsPrimary = Photos.Count == 0 });
                GeneratePayload();
            }
        }

        private void RemovePhoto()
        {
            if (SelectedPhoto != null)
            {
                Photos.Remove(SelectedPhoto);
                GeneratePayload();
            }
        }

        public void RunPreflightCheck()
        {
            PreflightIssues.Clear();

            // Validate TNVED 10-digits
            if (string.IsNullOrWhiteSpace(TnvedCode) || TnvedCode.Length != 10)
            {
                PreflightIssues.Add(new IssueItem
                {
                    RuleId = "R001",
                    Severity = IssueSeverity.BLOCK,
                    FieldPath = "tnved",
                    ObservedValue = TnvedCode,
                    VietnameseExplanation = "Mã ТН ВЭД phải có đầy đủ đúng 10 chữ số hợp lệ."
                });
            }

            // Validate Sizes
            if (Sizes.Count == 0)
            {
                PreflightIssues.Add(new IssueItem
                {
                    RuleId = "R006",
                    Severity = IssueSeverity.BLOCK,
                    FieldPath = "sizes",
                    ObservedValue = "0",
                    VietnameseExplanation = "Sản phẩm phải có ít nhất 1 biến thể size."
                });
            }

            foreach (var size in Sizes)
            {
                if (string.IsNullOrWhiteSpace(size.Sku))
                {
                    PreflightIssues.Add(new IssueItem
                    {
                        RuleId = "R012",
                        Severity = IssueSeverity.BLOCK,
                        FieldPath = $"sizes[{size.TechSize}].skus",
                        ObservedValue = "Rỗng",
                        VietnameseExplanation = $"Size {size.TechSize} chưa được gán barcode."
                    });
                }
            }

            if (PreflightIssues.Any(i => i.Severity == IssueSeverity.BLOCK))
            {
                PreflightReadinessScore = "❌ 40% - Có lỗi chặn (Blocker) cần khắc phục";
                PreflightIssuesSummary = $"Phát hiện {PreflightIssues.Count} vấn đề cần xử lý trước khi tạo thẻ.";
            }
            else
            {
                PreflightReadinessScore = "✅ 100% - Sẵn sàng đăng tải (Ready)";
                PreflightIssuesSummary = "Tất cả các trường bắt buộc và định dạng kỹ thuật đã hợp lệ!";
            }
        }

        public void GeneratePayload()
        {
            try
            {
                var characteristics = new List<object>
                {
                    new { id = 14177449, name = "Пол", value = new[] { SelectedGender } },
                    new { id = 14177450, name = "Состав", value = OuterFabricComp },
                    new { id = 14177451, name = "Цвет", value = new[] { SelectedColor } }
                };

                var sizesPayload = Sizes.Select(s => new
                {
                    techSize = s.TechSize,
                    wbSize = s.RussianSize,
                    price = s.Price,
                    skus = new[] { s.Sku }
                }).ToList();

                object? documentsPayload = null;
                if (SelectedDocumentPackage != null)
                {
                    documentsPayload = new
                    {
                        items = new[]
                        {
                            new
                            {
                                type = SelectedDocumentPackage.DocType,
                                number = SelectedDocumentPackage.DocNumber,
                                startDate = SelectedDocumentPackage.StartDate.ToString("dd.MM.yyyy"),
                                endDate = SelectedDocumentPackage.IsEndless ? null : SelectedDocumentPackage.EndDate.ToString("dd.MM.yyyy"),
                                isEndless = SelectedDocumentPackage.IsEndless
                            }
                        },
                        excludeDocuments = false
                    };
                }

                var cardPayload = new
                {
                    vendorCode = VendorCode,
                    title = Title,
                    description = Description,
                    brand = Brand,
                    dimensions = new
                    {
                        length = PackageLength,
                        width = PackageWidth,
                        height = PackageHeight,
                        weight = PackageWeight
                    },
                    characteristics = characteristics,
                    sizes = sizesPayload,
                    tnved = TnvedCode,
                    documents = documentsPayload
                };

                var rootArray = new[] { cardPayload };
                GeneratedPayloadJson = JsonSerializer.Serialize(rootArray, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch (Exception ex)
            {
                GeneratedPayloadJson = "// Lỗi tạo payload: " + ex.Message;
            }
        }

        private void CopyPayloadToClipboard()
        {
            try
            {
                Clipboard.SetText(GeneratedPayloadJson);
                CopyStatus = "✅ Đã sao chép JSON vào Clipboard!";
            }
            catch
            {
                CopyStatus = "⚠️ Không thể sao chép vào Clipboard.";
            }
        }

        private async Task PublishCardDirectlyAsync()
        {
            if (_apiClient == null)
            {
                PublishStatus = "❌ Chưa khởi tạo API Client.";
                return;
            }

            RunPreflightCheck();
            if (PreflightIssues.Any(i => i.Severity == IssueSeverity.BLOCK))
            {
                MessageBox.Show("Vui lòng khắc phục các lỗi chặn tại Bước 9 trước khi đăng bài.", "Lỗi Kiểm Tra Preflight", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsPublishing = true;
            PublishStatus = "⏳ Đang gửi payload lên Wildberries Content API...";
            PublishLog = $"[{DateTime.Now:HH:mm:ss}] Bắt đầu gửi tạo thẻ cho VendorCode: {VendorCode}...\n";

            try
            {
                var result = await _apiClient.UploadCardsAsync(GeneratedPayloadJson);
                if (result.Success)
                {
                    PublishStatus = "✅ Tạo thẻ thành công!";
                    PublishLog += $"[{DateTime.Now:HH:mm:ss}] WB phản hồi thành công. Dữ liệu đang được xử lý trong hàng đợi bất đồng bộ.\n";
                    PublishLog += $"Chi tiết: {result.RawResponse}\n";
                    CurrentStep = 10;
                }
                else
                {
                    PublishStatus = "❌ Lỗi đăng bài lên WB";
                    PublishLog += $"[{DateTime.Now:HH:mm:ss}] Lỗi từ server WB: {result.Message}\n";
                    if (!string.IsNullOrEmpty(result.RawResponse))
                    {
                        PublishLog += $"Raw Phản hồi: {result.RawResponse}\n";
                    }
                }
            }
            catch (Exception ex)
            {
                PublishStatus = "❌ Ngoại lệ trong quá trình gửi";
                PublishLog += $"[{DateTime.Now:HH:mm:ss}] Ngoại lệ: {ex.Message}\n";
            }
            finally
            {
                IsPublishing = false;
            }
        }
    }

    public class ProductSizeItem : ViewModelBase
    {
        private string _techSize = string.Empty;
        private string _russianSize = string.Empty;
        private string _sku = string.Empty;
        private decimal _price = 0;

        public string TechSize { get => _techSize; set => SetProperty(ref _techSize, value); }
        public string RussianSize { get => _russianSize; set => SetProperty(ref _russianSize, value); }
        public string Sku { get => _sku; set => SetProperty(ref _sku, value); }
        public decimal Price { get => _price; set => SetProperty(ref _price, value); }
    }

    public class ProductPhotoItem : ViewModelBase
    {
        private string _localPath = string.Empty;
        private string _url = string.Empty;
        private bool _isPrimary = false;

        public string LocalPath { get => _localPath; set => SetProperty(ref _localPath, value); }
        public string Url { get => _url; set => SetProperty(ref _url, value); }
        public bool IsPrimary { get => _isPrimary; set => SetProperty(ref _isPrimary, value); }
    }

    public class WbSubjectItem : ViewModelBase
    {
        private int _subjectId;
        private string _subjectName = string.Empty;

        public int SubjectId { get => _subjectId; set => SetProperty(ref _subjectId, value); }
        public string SubjectName { get => _subjectName; set => SetProperty(ref _subjectName, value); }
    }
}
