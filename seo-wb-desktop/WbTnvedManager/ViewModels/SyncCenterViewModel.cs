using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using WbTnvedManager.Models;

namespace WbTnvedManager.ViewModels
{
    public class SyncCenterViewModel : ViewModelBase
    {
        private string _statusFilter = "Tất cả";
        private string _selectedSystem = "Tất cả";
        private RemoteJobItem? _selectedJob;
        private string _exportNotice = string.Empty;

        public ObservableCollection<RemoteJobItem> Jobs { get; } = new();
        public ObservableCollection<RemoteJobItem> FilteredJobs { get; } = new();

        public ObservableCollection<string> StatusOptions { get; } = new() { "Tất cả", "QUEUED", "PROCESSING", "VERIFIED", "REJECTED", "UNKNOWN_OUTCOME" };
        public ObservableCollection<string> SystemOptions { get; } = new() { "Tất cả", "Wildberries", "Честный ЗНАК / NK", "GS1 Russia" };

        public string StatusFilter
        {
            get => _statusFilter;
            set { if (SetProperty(ref _statusFilter, value)) ApplyFilter(); }
        }

        public string SelectedSystem
        {
            get => _selectedSystem;
            set { if (SetProperty(ref _selectedSystem, value)) ApplyFilter(); }
        }

        public RemoteJobItem? SelectedJob
        {
            get => _selectedJob;
            set => SetProperty(ref _selectedJob, value);
        }

        public string ExportNotice
        {
            get => _exportNotice;
            set => SetProperty(ref _exportNotice, value);
        }

        public ICommand ExportNationalCatalogTemplateCommand { get; }
        public ICommand ExportGs1RegistrationPacketCommand { get; }
        public ICommand ExportWbSupportTicketPacketCommand { get; }
        public ICommand RetryJobCommand { get; }
        public ICommand ClearFinishedJobsCommand { get; }

        public SyncCenterViewModel()
        {
            ExportNationalCatalogTemplateCommand = new RelayCommand(ExportNationalCatalogTemplate);
            ExportGs1RegistrationPacketCommand = new RelayCommand(ExportGs1RegistrationPacket);
            ExportWbSupportTicketPacketCommand = new RelayCommand(ExportWbSupportTicketPacket);
            RetryJobCommand = new RelayCommand(RetryJob, () => SelectedJob != null);
            ClearFinishedJobsCommand = new RelayCommand(ClearFinishedJobs);

            SeedSampleJobs();
            ApplyFilter();
        }

        private void SeedSampleJobs()
        {
            Jobs.Add(new RemoteJobItem
            {
                JobId = "JOB-WB-8921",
                TargetSystem = "Wildberries",
                OperationType = "Cập nhật mã ТН ВЭД & Giới tính",
                EntityId = "nmID: 182947192",
                Status = "VERIFIED",
                CreatedAt = DateTime.Now.AddMinutes(-45),
                CompletedAt = DateTime.Now.AddMinutes(-40),
                Details = "Ghi đè write-schema bảo toàn 100% sizes, photos, documents. Đọc lại xác nhận thành công."
            });

            Jobs.Add(new RemoteJobItem
            {
                JobId = "JOB-NK-4412",
                TargetSystem = "Честный ЗНАК / NK",
                OperationType = "Bàn giao hồ sơ tạo thẻ NK",
                EntityId = "GTIN: 04601234567890",
                Status = "QUEUED",
                CreatedAt = DateTime.Now.AddMinutes(-15),
                Details = "Thuộc tính bắt buộc nhóm dệt may không cho phép sửa trực tiếp qua API. Cần xuất gói bàn giao portal."
            });

            Jobs.Add(new RemoteJobItem
            {
                JobId = "JOB-GS1-1109",
                TargetSystem = "GS1 Russia",
                OperationType = "Đăng ký dải mã GTIN mới",
                EntityId = "Batch 6 SKU (Áo len)",
                Status = "UNKNOWN_OUTCOME",
                CreatedAt = DateTime.Now.AddHours(-2),
                Details = "Chờ người bán đối soát tại portal GS1rus.ru và nhập lại GTIN chính thức."
            });
        }

        public void ApplyFilter()
        {
            FilteredJobs.Clear();
            var list = Jobs.AsEnumerable();

            if (StatusFilter != "Tất cả")
            {
                list = list.Where(j => j.Status == StatusFilter);
            }

            if (SelectedSystem != "Tất cả")
            {
                list = list.Where(j => j.TargetSystem == SelectedSystem);
            }

            foreach (var item in list)
            {
                FilteredJobs.Add(item);
            }
        }

        private void RetryJob()
        {
            if (SelectedJob != null)
            {
                SelectedJob.Status = "PROCESSING";
                SelectedJob.Details = $"Tác vụ được xếp hàng thực hiện lại lúc {DateTime.Now:HH:mm:ss}...";
                ApplyFilter();
            }
        }

        private void ClearFinishedJobs()
        {
            var finished = Jobs.Where(j => j.Status == "VERIFIED").ToList();
            foreach (var job in finished)
            {
                Jobs.Remove(job);
            }
            ApplyFilter();
        }

        private void ExportNationalCatalogTemplate()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "CSV UTF-8 (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = $"NK_Handover_Template_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (sfd.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine("GTIN;Наименование товара;Код ТН ВЭД;Товарный знак / Бренд;Вид изделия;Пол;Состав сырья;Номер РД (ДС/СС/СГР);Страна производства;Целевой рынок");
                sb.AppendLine("04601234567890;Футболка женская базовая;6109100000;WPOST;Футболка;Женский;100% Хлопок;ЕАЭС N RU Д-RU.РА01.В.12345/26;Россия;РФ");
                sb.AppendLine("04601234567891;Брюки женские классические;6204620000;WPOST;Брюки;Женский;95% Хлопок 5% Эластан;ЕАЭС N RU Д-RU.РА01.В.12345/26;Россия;РФ");

                File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
                ExportNotice = $"✅ Đã xuất mẫu bàn giao Национальный каталог (NK) tại: {Path.GetFileName(sfd.FileName)}";
            }
        }

        private void ExportGs1RegistrationPacket()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "CSV UTF-8 (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = $"GS1_Registration_Packet_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (sfd.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine("VendorCode;ProductType;Color;Size;RussianSize;Material;TargetGender;Country");
                sb.AppendLine("TSHIRT-BLK-S;Футболка;Черный;S;42-44;100% Cotton;Женский;RU");
                sb.AppendLine("TSHIRT-BLK-M;Футболка;Черный;M;44-46;100% Cotton;Женский;RU");
                sb.AppendLine("TSHIRT-BLK-L;Футболка;Черный;L;46-48;100% Cotton;Женский;RU");

                File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
                ExportNotice = $"✅ Đã xuất gói đăng ký GS1 Russia tại: {Path.GetFileName(sfd.FileName)}";
            }
        }

        private void ExportWbSupportTicketPacket()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "Text File (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = $"WB_Support_Ticket_{DateTime.Now:yyyyMMdd_HHmm}.txt"
            };

            if (sfd.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== HỒ SƠ YÊU CẦU HỖ TRỢ WILDBERRIES (THAY ĐỔI THUỘC TÍNH BỊ KHÓA) ===");
                sb.AppendLine($"Thời gian lập: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                sb.AppendLine("Lý do: Thuộc tính subjectID/danh mục hoặc mã ТН ВЭД bị khóa không thể sửa qua Content API v2.");
                sb.AppendLine();
                sb.AppendLine("nmID cần sửa: 182947192");
                sb.AppendLine("vendorCode: TSHIRT-BASIC-01");
                sb.AppendLine("Giá trị hiện tại trên WB: ТН ВЭД = 6109 (Chưa đủ 10 số)");
                sb.AppendLine("Giá trị chính thức theo hồ sơ: ТН ВЭД = 6109100000 (Áo thun dệt kim 100% cotton)");
                sb.AppendLine("Chứng từ đính kèm: Декларация о соответствии ЕАЭС N RU Д-RU.РА01.В.12345/26");

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                ExportNotice = $"✅ Đã xuất hồ sơ Support Ticket Wildberries tại: {Path.GetFileName(sfd.FileName)}";
            }
        }
    }

    public class RemoteJobItem : ViewModelBase
    {
        private string _jobId = string.Empty;
        private string _targetSystem = string.Empty;
        private string _operationType = string.Empty;
        private string _entityId = string.Empty;
        private string _status = "QUEUED";
        private DateTime _createdAt = DateTime.Now;
        private DateTime? _completedAt;
        private string _details = string.Empty;

        public string JobId { get => _jobId; set => SetProperty(ref _jobId, value); }
        public string TargetSystem { get => _targetSystem; set => SetProperty(ref _targetSystem, value); }
        public string OperationType { get => _operationType; set => SetProperty(ref _operationType, value); }
        public string EntityId { get => _entityId; set => SetProperty(ref _entityId, value); }
        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }
        public DateTime? CompletedAt { get => _completedAt; set => SetProperty(ref _completedAt, value); }
        public string Details { get => _details; set => SetProperty(ref _details, value); }
    }
}
