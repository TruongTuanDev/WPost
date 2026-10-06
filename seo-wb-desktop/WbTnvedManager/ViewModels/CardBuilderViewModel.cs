using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using WbTnvedManager.Data;
using WbTnvedManager.Models;
using WbTnvedManager.Services;

namespace WbTnvedManager.ViewModels
{
    public class CardBuilderViewModel : ViewModelBase
    {
        private readonly MatrixRepository _repository;
        private readonly TnvedSelectorService _selector;

        private WbSubjectItem? _selectedSubject;
        private string _selectedGender = "Женский";
        private string _selectedMaterial = "Хлопок";
        private string _selectedKnitType = "Трикотаж";

        private string _vendorCode = "SKU-PROD-001";
        private string _title = "Áo Thun Nữ Cotton Cao Cấp Phong Cách Trẻ Trung";
        private string _brand = "WB Fashion";
        private string _description = "Sản phẩm may từ chất liệu cotton thoáng mát, thấm hút mồ hôi tốt, độ co giãn thoải mái.";
        private decimal _price = 1500;

        private string _tnvedCode = string.Empty;
        private string _matchReason = string.Empty;
        private string _generatedPayloadJson = string.Empty;
        private string _copyStatus = string.Empty;

        public ObservableCollection<WbSubjectItem> Subjects { get; } = new();
        public ObservableCollection<string> Genders { get; } = new() { "Женский", "Мужской", "Девочки", "Мальчики", "Унисекс" };
        public ObservableCollection<string> Materials { get; } = new() { "Хлопок", "Полиэстер", "Синтетика", "Шерсть", "Лен", "Шелк", "Вискоза", "Кожа", "Текстиль" };
        public ObservableCollection<string> KnitTypes { get; } = new() { "Трикотаж", "Ткань" };

        public WbSubjectItem? SelectedSubject
        {
            get => _selectedSubject;
            set { if (SetProperty(ref _selectedSubject, value)) RecalculateTnved(); }
        }

        public string SelectedGender
        {
            get => _selectedGender;
            set { if (SetProperty(ref _selectedGender, value)) RecalculateTnved(); }
        }

        public string SelectedMaterial
        {
            get => _selectedMaterial;
            set { if (SetProperty(ref _selectedMaterial, value)) RecalculateTnved(); }
        }

        public string SelectedKnitType
        {
            get => _selectedKnitType;
            set { if (SetProperty(ref _selectedKnitType, value)) RecalculateTnved(); }
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

        public decimal Price
        {
            get => _price;
            set { if (SetProperty(ref _price, value)) GeneratePayload(); }
        }

        public string TnvedCode
        {
            get => _tnvedCode;
            set => SetProperty(ref _tnvedCode, value);
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

        public ICommand CopyPayloadCommand { get; }
        public ICommand RefreshSubjectsCommand { get; }

        public CardBuilderViewModel(MatrixRepository repository, TnvedSelectorService selector)
        {
            _repository = repository;
            _selector = selector;

            CopyPayloadCommand = new RelayCommand(CopyPayload);
            RefreshSubjectsCommand = new RelayCommand(LoadSubjectsFromMatrix);

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

        private void GeneratePayload()
        {
            if (SelectedSubject == null || string.IsNullOrWhiteSpace(TnvedCode))
            {
                GeneratedPayloadJson = "// Chọn đầy đủ thông tin để tự động sinh Payload chuẩn WB.";
                return;
            }

            var payloadObj = _selector.BuildUploadByItemPayload(
                SelectedSubject.Id,
                VendorCode,
                Title,
                Description,
                SelectedGender,
                SelectedMaterial,
                TnvedCode,
                Brand,
                Price
            );

            GeneratedPayloadJson = JsonSerializer.Serialize(payloadObj, new JsonSerializerOptions { WriteIndented = true });
            CopyStatus = string.Empty;
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
