using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WbTnvedManager.Models;

namespace WbTnvedManager.ViewModels
{
    public class DocumentsViewModel : ViewModelBase
    {
        private ConformityDocument? _selectedDocument;
        private string _searchQuery = string.Empty;
        private string _statusFilter = "Tất cả";
        private string _typeFilter = "Tất cả";

        private ConformityDocType _formType = ConformityDocType.DS;
        private string _formNumber = string.Empty;
        private string _formApplicant = string.Empty;
        private string _formTradeName = string.Empty;
        private DateTime _formStartDate = DateTime.Today;
        private DateTime _formEndDate = DateTime.Today.AddYears(3);
        private bool _formIsEndless = false;
        private string _formCoveredTnved = string.Empty;
        private string _formCoveredBrands = string.Empty;
        private bool _isEditing = false;
        private string _validationStatusMessage = string.Empty;

        public ObservableCollection<ConformityDocument> Documents { get; } = new();
        public ObservableCollection<ConformityDocument> FilteredDocuments { get; } = new();

        public ObservableCollection<ConformityDocType> DocumentTypes { get; } = new()
        {
            ConformityDocType.DS,
            ConformityDocType.SS,
            ConformityDocType.SGR,
            ConformityDocType.EXEMPTION_LETTER
        };

        public ObservableCollection<string> StatusFilterOptions { get; } = new() { "Tất cả", "Còn hiệu lực", "Sắp hết hạn (≤ 30 ngày)", "Đã hết hạn", "Vô thời hạn" };
        public ObservableCollection<string> TypeFilterOptions { get; } = new() { "Tất cả", "DS", "SS", "SGR", "EXEMPTION_LETTER" };

        public ConformityDocument? SelectedDocument
        {
            get => _selectedDocument;
            set
            {
                if (SetProperty(ref _selectedDocument, value) && value != null)
                {
                    LoadDocumentToForm(value);
                }
            }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set { if (SetProperty(ref _searchQuery, value)) ApplyFilter(); }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { if (SetProperty(ref _statusFilter, value)) ApplyFilter(); }
        }

        public string TypeFilter
        {
            get => _typeFilter;
            set { if (SetProperty(ref _typeFilter, value)) ApplyFilter(); }
        }

        public ConformityDocType FormType { get => _formType; set => SetProperty(ref _formType, value); }
        public string FormNumber { get => _formNumber; set => SetProperty(ref _formNumber, value); }
        public string FormApplicant { get => _formApplicant; set => SetProperty(ref _formApplicant, value); }
        public string FormTradeName { get => _formTradeName; set => SetProperty(ref _formTradeName, value); }
        public DateTime FormStartDate { get => _formStartDate; set => SetProperty(ref _formStartDate, value); }
        public DateTime FormEndDate { get => _formEndDate; set => SetProperty(ref _formEndDate, value); }
        public bool FormIsEndless { get => _formIsEndless; set => SetProperty(ref _formIsEndless, value); }
        public string FormCoveredTnved { get => _formCoveredTnved; set => SetProperty(ref _formCoveredTnved, value); }
        public string FormCoveredBrands { get => _formCoveredBrands; set => SetProperty(ref _formCoveredBrands, value); }
        public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }
        public string ValidationStatusMessage { get => _validationStatusMessage; set => SetProperty(ref _validationStatusMessage, value); }

        public ICommand SaveDocumentCommand { get; }
        public ICommand NewDocumentCommand { get; }
        public ICommand DeleteDocumentCommand { get; }
        public ICommand ValidateDocumentCommand { get; }

        public DocumentsViewModel()
        {
            SaveDocumentCommand = new RelayCommand(SaveDocument);
            NewDocumentCommand = new RelayCommand(ResetForm);
            DeleteDocumentCommand = new RelayCommand(DeleteDocument, () => SelectedDocument != null);
            ValidateDocumentCommand = new RelayCommand(ValidateCurrentForm);

            SeedSampleDocuments();
            ApplyFilter();
        }

        private void SeedSampleDocuments()
        {
            Documents.Add(new ConformityDocument
            {
                Id = "DOC-001",
                Type = ConformityDocType.DS,
                ExactNumber = "ЕАЭС N RU Д-RU.РА01.В.12345/26",
                Applicant = "ООО ВЕСТ ЛАЙН",
                TradeName = "Одежда швейная и трикотажная 2-го слоя",
                StartDate = DateTime.Today.AddMonths(-6),
                EndDate = DateTime.Today.AddYears(2).AddMonths(6),
                IsEndless = false,
                CoveredTnvedPrefixes = new() { "6109100000", "6109902000", "6104620000", "6103420000" },
                CoveredModels = new() { "WPOST", "BASIC LOOK" },
                RegistryStatus = "Действует (Đang hiệu lực)"
            });

            Documents.Add(new ConformityDocument
            {
                Id = "DOC-002",
                Type = ConformityDocType.SS,
                ExactNumber = "ЕАЭС RU C-RU.АБ02.В.54321/25",
                Applicant = "ИП Иванов И.И.",
                TradeName = "Изделия трикотажные 1-го слоя для взрослых",
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddMonths(1),
                IsEndless = false,
                CoveredTnvedPrefixes = new() { "6109100000", "6107110000", "6108210000" },
                CoveredModels = new() { "COMFORT WEAR" },
                RegistryStatus = "Действует (Sắp hết hạn)"
            });

            Documents.Add(new ConformityDocument
            {
                Id = "DOC-003",
                Type = ConformityDocType.SGR,
                ExactNumber = "RU.77.99.11.002.E.001234.05.24",
                Applicant = "ООО ДЕТСТВО ПЛЮС",
                TradeName = "Одежда для детей до 1 года (1-й слой)",
                StartDate = DateTime.Today.AddYears(-2),
                EndDate = DateTime.Today.AddYears(10),
                IsEndless = true,
                CoveredTnvedPrefixes = new() { "6111209000", "6111309000", "6209200000" },
                CoveredModels = new() { "BABY CARE" },
                RegistryStatus = "Бессрочно (Vô thời hạn)"
            });
        }

        private void LoadDocumentToForm(ConformityDocument doc)
        {
            IsEditing = true;
            FormType = doc.Type;
            FormNumber = doc.ExactNumber;
            FormApplicant = doc.Applicant ?? string.Empty;
            FormTradeName = doc.TradeName ?? string.Empty;
            FormStartDate = doc.StartDate ?? DateTime.Today;
            FormEndDate = doc.EndDate ?? DateTime.Today.AddYears(3);
            FormIsEndless = doc.IsEndless;
            FormCoveredTnved = string.Join(", ", doc.CoveredTnvedPrefixes);
            FormCoveredBrands = string.Join(", ", doc.CoveredModels);
            ValidateCurrentForm();
        }

        private void ResetForm()
        {
            SelectedDocument = null;
            IsEditing = false;
            FormType = ConformityDocType.DS;
            FormNumber = string.Empty;
            FormApplicant = string.Empty;
            FormTradeName = string.Empty;
            FormStartDate = DateTime.Today;
            FormEndDate = DateTime.Today.AddYears(3);
            FormIsEndless = false;
            FormCoveredTnved = string.Empty;
            FormCoveredBrands = string.Empty;
            ValidationStatusMessage = string.Empty;
        }

        private void ValidateCurrentForm()
        {
            if (string.IsNullOrWhiteSpace(FormNumber))
            {
                ValidationStatusMessage = "⚠️ Số chứng từ không được để trống.";
                return;
            }

            if (!FormIsEndless && FormEndDate < DateTime.Today)
            {
                ValidationStatusMessage = "❌ Chứng từ đã hết hạn hiệu lực theo ngày kết thúc.";
                return;
            }

            if (!FormIsEndless && FormEndDate <= DateTime.Today.AddDays(30))
            {
                ValidationStatusMessage = "⚠️ Chứng từ sắp hết hạn trong vòng 30 ngày tới. Cần chuẩn bị gia hạn.";
                return;
            }

            ValidationStatusMessage = "✅ Định dạng hợp lệ. Đang trong thời hạn hiệu lực.";
        }

        private void SaveDocument()
        {
            ValidateCurrentForm();
            if (string.IsNullOrWhiteSpace(FormNumber)) return;

            var tnvedList = FormCoveredTnved
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length >= 4)
                .ToList();

            var brandList = FormCoveredBrands
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(b => b.Trim())
                .Where(b => !string.IsNullOrEmpty(b))
                .ToList();

            string regStatus = FormIsEndless ? "Бессрочно (Vô thời hạn)" :
                               FormEndDate < DateTime.Today ? "Истек (Đã hết hạn)" :
                               FormEndDate <= DateTime.Today.AddDays(30) ? "Истекает (Sắp hết hạn)" : "Действует (Đang hiệu lực)";

            if (IsEditing && SelectedDocument != null)
            {
                SelectedDocument.Type = FormType;
                SelectedDocument.ExactNumber = FormNumber;
                SelectedDocument.Applicant = FormApplicant;
                SelectedDocument.TradeName = FormTradeName;
                SelectedDocument.StartDate = FormStartDate;
                SelectedDocument.EndDate = FormIsEndless ? null : FormEndDate;
                SelectedDocument.IsEndless = FormIsEndless;
                SelectedDocument.CoveredTnvedPrefixes = tnvedList;
                SelectedDocument.CoveredModels = brandList;
                SelectedDocument.RegistryStatus = regStatus;
            }
            else
            {
                var newDoc = new ConformityDocument
                {
                    Id = "DOC-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper(),
                    Type = FormType,
                    ExactNumber = FormNumber,
                    Applicant = FormApplicant,
                    TradeName = FormTradeName,
                    StartDate = FormStartDate,
                    EndDate = FormIsEndless ? null : FormEndDate,
                    IsEndless = FormIsEndless,
                    CoveredTnvedPrefixes = tnvedList,
                    CoveredModels = brandList,
                    RegistryStatus = regStatus
                };
                Documents.Add(newDoc);
                SelectedDocument = newDoc;
            }

            ApplyFilter();
        }

        private void DeleteDocument()
        {
            if (SelectedDocument != null)
            {
                Documents.Remove(SelectedDocument);
                ResetForm();
                ApplyFilter();
            }
        }

        public void ApplyFilter()
        {
            FilteredDocuments.Clear();
            var list = Documents.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                string q = SearchQuery.ToLower();
                list = list.Where(d =>
                    (d.ExactNumber?.ToLower().Contains(q) ?? false) ||
                    (d.Applicant?.ToLower().Contains(q) ?? false) ||
                    (d.TradeName?.ToLower().Contains(q) ?? false) ||
                    d.CoveredTnvedPrefixes.Any(t => t.Contains(q)) ||
                    d.CoveredModels.Any(b => b.ToLower().Contains(q)));
            }

            if (TypeFilter != "Tất cả")
            {
                list = list.Where(d => d.Type.ToString().Equals(TypeFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (StatusFilter == "Còn hiệu lực")
            {
                list = list.Where(d => d.IsEndless || (d.EndDate.HasValue && d.EndDate.Value >= DateTime.Today));
            }
            else if (StatusFilter == "Sắp hết hạn (≤ 30 ngày)")
            {
                list = list.Where(d => !d.IsEndless && d.EndDate.HasValue && d.EndDate.Value >= DateTime.Today && d.EndDate.Value <= DateTime.Today.AddDays(30));
            }
            else if (StatusFilter == "Đã hết hạn")
            {
                list = list.Where(d => !d.IsEndless && d.EndDate.HasValue && d.EndDate.Value < DateTime.Today);
            }
            else if (StatusFilter == "Vô thời hạn")
            {
                list = list.Where(d => d.IsEndless);
            }

            foreach (var item in list)
            {
                FilteredDocuments.Add(item);
            }
        }
    }
}
