using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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
    public class MatrixManagerViewModel : ViewModelBase
    {
        private readonly MatrixRepository _repository;
        private readonly IWbApiClient _apiClient;

        private ObservableCollection<TnvedMatrixEntry> _matrixItems = new();
        private TnvedMatrixEntry? _selectedItem;
        private string _searchKeyword = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy = false;

        // Edit/Add Form Properties
        private int _formSubjectId = 105;
        private string _formSubjectName = "Футболка";
        private string _formGender = "Женский";
        private string _formMaterial = "Хлопок";
        private string _formKnitType = "Трикотаж";
        private string _formTnvedCode = "6109100000";
        private string _formDescription = string.Empty;
        private bool _isEditing = false;

        public ObservableCollection<TnvedMatrixEntry> MatrixItems => _matrixItems;

        public TnvedMatrixEntry? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value) && value != null)
                {
                    FormSubjectId = value.SubjectId;
                    FormSubjectName = value.SubjectName;
                    FormGender = value.Gender;
                    FormMaterial = value.Material;
                    FormKnitType = value.KnitType;
                    FormTnvedCode = value.TnvedCode;
                    FormDescription = value.Description;
                    IsEditing = true;
                }
            }
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set { if (SetProperty(ref _searchKeyword, value)) RefreshList(); }
        }

        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        public int FormSubjectId { get => _formSubjectId; set => SetProperty(ref _formSubjectId, value); }
        public string FormSubjectName { get => _formSubjectName; set => SetProperty(ref _formSubjectName, value); }
        public string FormGender { get => _formGender; set => SetProperty(ref _formGender, value); }
        public string FormMaterial { get => _formMaterial; set => SetProperty(ref _formMaterial, value); }
        public string FormKnitType { get => _formKnitType; set => SetProperty(ref _formKnitType, value); }
        public string FormTnvedCode { get => _formTnvedCode; set => SetProperty(ref _formTnvedCode, value); }
        public string FormDescription { get => _formDescription; set => SetProperty(ref _formDescription, value); }
        public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }

        public ICommand SaveEntryCommand { get; }
        public ICommand NewEntryCommand { get; }
        public ICommand DeleteEntryCommand { get; }
        public ICommand ResetDefaultsCommand { get; }
        public ICommand SyncFromWbCommand { get; }
        public ICommand ExportJsonCommand { get; }
        public ICommand ImportJsonCommand { get; }

        public MatrixManagerViewModel(MatrixRepository repository, IWbApiClient apiClient)
        {
            _repository = repository;
            _apiClient = apiClient;

            SaveEntryCommand = new RelayCommand(SaveEntry);
            NewEntryCommand = new RelayCommand(NewEntry);
            DeleteEntryCommand = new RelayCommand(DeleteEntry, () => SelectedItem != null);
            ResetDefaultsCommand = new RelayCommand(ResetToDefaults);
            SyncFromWbCommand = new RelayCommand(async () => await SyncFromWbAsync());
            ExportJsonCommand = new RelayCommand(ExportJson);
            ImportJsonCommand = new RelayCommand(ImportJson);

            RefreshList();
        }

        public void RefreshList()
        {
            var items = _repository.Search(SearchKeyword);
            _matrixItems.Clear();
            foreach (var item in items)
            {
                _matrixItems.Add(item);
            }
            StatusMessage = $"Tổng cộng: {_matrixItems.Count} quy tắc trong cơ sở dữ liệu.";
        }

        private void NewEntry()
        {
            SelectedItem = null;
            IsEditing = false;
            FormSubjectId = 0;
            FormSubjectName = string.Empty;
            FormGender = "Женский";
            FormMaterial = "Хлопок";
            FormKnitType = "Трикотаж";
            FormTnvedCode = string.Empty;
            FormDescription = string.Empty;
        }

        private void SaveEntry()
        {
            if (FormSubjectId <= 0 || string.IsNullOrWhiteSpace(FormSubjectName) || string.IsNullOrWhiteSpace(FormTnvedCode))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Subject ID, Tên danh mục và Mã TNVED.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var entry = new TnvedMatrixEntry
            {
                Id = IsEditing && SelectedItem != null ? SelectedItem.Id : 0,
                SubjectId = FormSubjectId,
                SubjectName = FormSubjectName.Trim(),
                Gender = FormGender?.Trim() ?? string.Empty,
                Material = FormMaterial?.Trim() ?? string.Empty,
                KnitType = FormKnitType?.Trim() ?? string.Empty,
                TnvedCode = FormTnvedCode.Trim(),
                Description = FormDescription?.Trim() ?? string.Empty
            };

            if (IsEditing && SelectedItem != null)
            {
                _repository.Update(entry);
                StatusMessage = $"Đã cập nhật quy tắc ID {entry.Id}.";
            }
            else
            {
                _repository.Insert(entry);
                StatusMessage = "Đã thêm mới quy tắc thành công.";
            }

            RefreshList();
            NewEntry();
        }

        private void DeleteEntry()
        {
            if (SelectedItem == null) return;
            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa quy tắc '{SelectedItem.SubjectName} - {SelectedItem.Gender} - {SelectedItem.Material}'?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                _repository.Delete(SelectedItem.Id);
                RefreshList();
                NewEntry();
            }
        }

        private void ResetToDefaults()
        {
            var confirm = MessageBox.Show(
                "Bạn có muốn nạp lại danh sách Ma trận mặc định (hơn 50 quy tắc thời trang chuẩn)?\nCác quy tắc hiện tại sẽ được cập nhật.",
                "Khôi phục mặc định",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                var seeds = DatabaseInitializer.GetDefaultSeeds();
                _repository.BulkInsertOrUpdate(seeds);
                RefreshList();
                MessageBox.Show("Đã nạp lại Ma trận mặc định thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task SyncFromWbAsync()
        {
            IsBusy = true;
            StatusMessage = "Đang đồng bộ danh mục TNVED từ API Wildberries...";

            try
            {
                var wbTnvedList = await _apiClient.GetAllTnvedDirectoryAsync();
                if (wbTnvedList == null || wbTnvedList.Count == 0)
                {
                    // Fallback to subjects
                    var subjects = await _apiClient.GetSubjectsAsync();
                    StatusMessage = $"Đã lấy {subjects.Count} danh mục từ WB.";
                    MessageBox.Show($"Đã đồng bộ {subjects.Count} danh mục từ WB.", "Đồng bộ WB", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var newEntries = new List<TnvedMatrixEntry>();
                    foreach (var item in wbTnvedList)
                    {
                        var code = item.GetTnvedCode();
                        if (!string.IsNullOrEmpty(code))
                        {
                            newEntries.Add(new TnvedMatrixEntry
                            {
                                SubjectId = item.SubjectId,
                                SubjectName = item.SubjectName ?? item.Name ?? "Danh mục WB",
                                Gender = "Унисекс",
                                Material = "Хлопок",
                                KnitType = "Трикотаж",
                                TnvedCode = code,
                                Description = item.Description ?? item.Name ?? ""
                            });
                        }
                    }

                    _repository.BulkInsertOrUpdate(newEntries);
                    RefreshList();
                    MessageBox.Show($"Đồng bộ thành công {newEntries.Count} mã TNVED từ sàn Wildberries!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đồng bộ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExportJson()
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json|CSV Files (*.csv)|*.csv",
                    FileName = "TnvedMatrix_Backup.json"
                };

                if (sfd.ShowDialog() == true)
                {
                    var items = _repository.GetAll();
                    if (sfd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("SubjectId,SubjectName,Gender,Material,KnitType,TnvedCode,Description");
                        foreach (var item in items)
                        {
                            sb.AppendLine($"{item.SubjectId},\"{item.SubjectName}\",\"{item.Gender}\",\"{item.Material}\",\"{item.KnitType}\",\"{item.TnvedCode}\",\"{item.Description}\"");
                        }
                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    }
                    else
                    {
                        var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(sfd.FileName, json, Encoding.UTF8);
                    }
                    MessageBox.Show("Xuất dữ liệu Ma trận thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportJson()
        {
            try
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "JSON or CSV Files (*.json;*.csv)|*.json;*.csv|JSON Files (*.json)|*.json|CSV Files (*.csv)|*.csv"
                };

                if (ofd.ShowDialog() == true)
                {
                    var entries = new List<TnvedMatrixEntry>();
                    if (ofd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        var lines = File.ReadAllLines(ofd.FileName);
                        for (int i = 1; i < lines.Length; i++)
                        {
                            var parts = lines[i].Split(',');
                            if (parts.Length >= 6 && int.TryParse(parts[0].Trim(), out int sid))
                            {
                                entries.Add(new TnvedMatrixEntry
                                {
                                    SubjectId = sid,
                                    SubjectName = parts[1].Trim('\"', ' '),
                                    Gender = parts[2].Trim('\"', ' '),
                                    Material = parts[3].Trim('\"', ' '),
                                    KnitType = parts[4].Trim('\"', ' '),
                                    TnvedCode = parts[5].Trim('\"', ' '),
                                    Description = parts.Length > 6 ? parts[6].Trim('\"', ' ') : ""
                                });
                            }
                        }
                    }
                    else
                    {
                        var json = File.ReadAllText(ofd.FileName);
                        var parsed = JsonSerializer.Deserialize<List<TnvedMatrixEntry>>(json);
                        if (parsed != null) entries = parsed;
                    }

                    if (entries.Count > 0)
                    {
                        _repository.BulkInsertOrUpdate(entries);
                        RefreshList();
                        MessageBox.Show($"Nhập thành công {entries.Count} quy tắc vào cơ sở dữ liệu!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi nhập dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
