using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WbTnvedManager.Data;
using WbTnvedManager.Models;
using WbTnvedManager.Services;

namespace WbTnvedManager.ViewModels
{
    public class MarkirovkaViewModel : ViewModelBase
    {
        private readonly IWbApiClient _apiClient;
        private readonly INationalCatalogConnector _nkConnector;
        private readonly MarkirovkaReconciliationEngine _reconciliationEngine;
        private readonly MarkirovkaUpdateWorker _updateWorker;
        private readonly MatrixRepository _matrixRepo;
        private readonly SellerAccount _sellerAccount;

        private ObservableCollection<MarkirovkaFinding> _findings = new();
        private ObservableCollection<MarkirovkaProposal> _proposals = new();
        private MarkirovkaFinding? _selectedFinding;
        private MarkirovkaProposal? _selectedProposal;

        private string _statusMessage = "Sẵn sàng đối chiếu Честный ЗНАК và Wildberries.";
        private bool _isBusy = false;
        private string _selectedTab = "Proposals"; // "Proposals" or "Findings"
        private CancellationTokenSource? _cts;

        public ObservableCollection<MarkirovkaFinding> Findings => _findings;
        public ObservableCollection<MarkirovkaProposal> Proposals => _proposals;

        public MarkirovkaFinding? SelectedFinding
        {
            get => _selectedFinding;
            set => SetProperty(ref _selectedFinding, value);
        }

        public MarkirovkaProposal? SelectedProposal
        {
            get => _selectedProposal;
            set => SetProperty(ref _selectedProposal, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    (RunReconciliationCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (ApproveSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (SyncToWbCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public int OpenFindingsCount => _findings.Count(f => f.Status == FindingStatus.OPEN);
        public int ReadyProposalsCount => _proposals.Count(p => p.State == ProposalState.READY);
        public int ApprovedProposalsCount => _proposals.Count(p => p.State == ProposalState.APPROVED);

        public ICommand RunReconciliationCommand { get; }
        public ICommand ApproveSelectedCommand { get; }
        public ICommand RejectSelectedCommand { get; }
        public ICommand SyncToWbCommand { get; }
        public ICommand CreateSourceTaskCommand { get; }
        public ICommand LoadSampleDataCommand { get; }

        public MarkirovkaViewModel(
            IWbApiClient apiClient,
            INationalCatalogConnector nkConnector,
            SafeWbUpdatePipeline safePipeline,
            MatrixRepository matrixRepo,
            SellerAccount? sellerAccount = null)
        {
            _apiClient = apiClient;
            _nkConnector = nkConnector;
            _matrixRepo = matrixRepo;
            _sellerAccount = sellerAccount ?? new SellerAccount { INN = "7707083893" };
            _reconciliationEngine = new MarkirovkaReconciliationEngine();
            _updateWorker = new MarkirovkaUpdateWorker(_apiClient, safePipeline);

            RunReconciliationCommand = new RelayCommand(async () => await RunReconciliationAsync(), () => !IsBusy);
            ApproveSelectedCommand = new RelayCommand(ApproveSelected, () => !IsBusy && _proposals.Any(p => p.IsSelected && p.CanApprove));
            RejectSelectedCommand = new RelayCommand(RejectSelected, () => !IsBusy && _proposals.Any(p => p.IsSelected));
            SyncToWbCommand = new RelayCommand(async () => await ExecuteSyncToWbAsync(), () => !IsBusy && _proposals.Any(p => p.State == ProposalState.APPROVED));
            CreateSourceTaskCommand = new RelayCommand(CreateSourceTask, () => SelectedFinding != null);
            LoadSampleDataCommand = new RelayCommand(LoadSampleMarkirovkaData);

            // Load sample tri-source fixture on start
            LoadSampleMarkirovkaData();
        }

        public void LoadSampleMarkirovkaData()
        {
            _findings.Clear();
            _proposals.Clear();

            // Sample Findings based on T01-T12
            _findings.Add(new MarkirovkaFinding
            {
                RuleId = "R03",
                RuleTitle = "GTIN sai cấu trúc / Checksum",
                SourceKind = FindingSourceKind.LOCAL_VALIDATION_FINDING,
                Severity = IssueSeverity.BLOCK,
                NmId = 195827103,
                VendorCode = "DRESS-SILK-SUMMER",
                SizeTech = "44",
                RawMessageRu = "Неверная контрольная цифра в GTIN 0000000000000",
                TranslatedExplanationVi = "Mã GTIN không đúng định dạng hoặc sai check digit GS1. Cấm đoán mò số để lách kiểm duyệt.",
                EvidenceSummary = "GS1 Specification"
            });

            _findings.Add(new MarkirovkaFinding
            {
                RuleId = "R06",
                RuleTitle = "КИЗ lô hàng thực tế không khớp thẻ WB",
                SourceKind = FindingSourceKind.DATA_CONFLICT,
                Severity = IssueSeverity.BLOCK,
                NmId = 209148201,
                VendorCode = "JEANS-MALE-BLACK",
                SizeTech = "32/34",
                RawMessageRu = "Несоответствие КИЗ фактической партии (04601234567890) и карточки (04609876543210)",
                TranslatedExplanationVi = "Nhãn quét từ kho dán mã 04601234567890 nhưng thẻ WB lại khai mã khác. Sàn WB sẽ từ chối nhận hàng tại kho.",
                EvidenceSummary = "Lô hàng: LOT-2026-JEANS-01"
            });

            _findings.Add(new MarkirovkaFinding
            {
                RuleId = "R08",
                RuleTitle = "Thẻ National Catalog chưa công bố (Bản nháp)",
                SourceKind = FindingSourceKind.OFFICIAL_SOURCE_ERROR,
                Severity = IssueSeverity.BLOCK,
                NmId = 239105829,
                VendorCode = "SHIRT-MEN-WHITE",
                SizeTech = "42",
                RawMessageRu = "Карточка в Нац. каталоге в статусе ''Черновик (Ошибки модерации)''",
                TranslatedExplanationVi = "Thẻ trên Национальный каталог chưa công bố xong. Không được lấy bản nháp đang lỗi làm dữ liệu chuẩn sửa WB.",
                EvidenceSummary = "Нац. каталог Feed: 994821"
            });

            // Sample Proposals (3-way comparison)
            _proposals.Add(new MarkirovkaProposal
            {
                NmId = 184920192,
                VendorCode = "TSHIRT-FEMALE-01",
                TechSize = "M",
                Title = "Футболка женская базовая оверсайз хлопок",
                PhysicalGtin = "04607001234567",
                PhysicalSize = "M (44-46)",
                NkGtin = "04607001234567",
                NkStatus = "Опубликована",
                NkTnved = "6109100000",
                WbCurrentGtin = "4607001234567",
                WbCurrentTnved = "6403999600", // Sai: Mã giày dép
                WbCurrentBarcode = "4607001234567",
                WbNeedKiz = true,
                WbKizMarked = false,
                DesiredGtin = "04607001234567",
                DesiredTnved = "6109100000",
                DesiredGender = "Женский",
                DesiredKizMarked = true,
                Justification = "Đồng bộ mã ТН ВЭД chuẩn may mặc 6109100000 và xác nhận kizMarked=true theo đúng thẻ NK đã công bố.",
                State = ProposalState.READY
            });

            _proposals.Add(new MarkirovkaProposal
            {
                NmId = 284019284,
                VendorCode = "JACKET-LEATHER-MEN",
                TechSize = "50",
                Title = "Куртка мужская кожаная демисезонная",
                PhysicalGtin = "04607009876543",
                PhysicalSize = "50",
                NkGtin = "04607009876543",
                NkStatus = "Опубликована",
                NkTnved = "4203100001",
                WbCurrentGtin = "4607009876543",
                WbCurrentTnved = "6201400000", // Sai: Mã vải thay vì da
                WbCurrentBarcode = "4607009876543",
                WbNeedKiz = true,
                WbKizMarked = false,
                DesiredGtin = "04607009876543",
                DesiredTnved = "4203100001",
                DesiredGender = "Мужской",
                DesiredKizMarked = true,
                Justification = "Sửa mã ТН ВЭД thành 4203100001 (áo da thật) theo đúng chứng nhận hợp quy và thẻ NK.",
                State = ProposalState.READY
            });

            UpdateStats();
            StatusMessage = $"Đã nạp fixture kiểm thử маркировка ({_findings.Count} lỗi nguồn, {_proposals.Count} đề xuất đối chiếu).";
        }

        public async Task RunReconciliationAsync()
        {
            IsBusy = true;
            _cts = new CancellationTokenSource();
            StatusMessage = "🔄 Đang tải thẻ WB và đối chiếu với Честный ЗНАК / National Catalog...";

            try
            {
                var progress = new Progress<string>(m => StatusMessage = m);
                var wbCards = await _apiClient.GetAllCardsAsync(progress, _cts.Token);
                var nkProducts = await _nkConnector.GetProductsAsync(_sellerAccount.INN, cancellationToken: _cts.Token);
                var matrixSeeds = _matrixRepo.GetAll();

                var stockEvidences = new List<StockEvidence>();
                var recResult = _reconciliationEngine.Reconcile(_sellerAccount, wbCards, nkProducts, stockEvidences, matrixSeeds);

                _findings.Clear();
                foreach (var f in recResult.Findings) _findings.Add(f);

                _proposals.Clear();
                foreach (var p in recResult.Proposals) _proposals.Add(p);

                UpdateStats();
                StatusMessage = $"Đối chiếu hoàn tất: Phát hiện {_findings.Count} lỗi nguồn, lập {_proposals.Count} đề xuất sửa WB.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi đối chiếu: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApproveSelected()
        {
            var selected = _proposals.Where(p => p.IsSelected && p.CanApprove).ToList();
            if (selected.Count == 0) return;

            foreach (var p in selected)
            {
                p.State = ProposalState.APPROVED;
                p.ApprovedBy = "Chủ shop";
                p.ApprovedAt = DateTime.UtcNow;
            }
            UpdateStats();
            StatusMessage = $"Đã duyệt {selected.Count} đề xuất. Sẵn sàng gửi đồng bộ lên Wildberries.";
            (SyncToWbCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void RejectSelected()
        {
            var selected = _proposals.Where(p => p.IsSelected).ToList();
            foreach (var p in selected)
            {
                p.State = ProposalState.REJECTED;
            }
            UpdateStats();
            StatusMessage = $"Đã từ chối {selected.Count} đề xuất.";
        }

        private async Task ExecuteSyncToWbAsync()
        {
            var approved = _proposals.Where(p => p.State == ProposalState.APPROVED).ToList();
            if (approved.Count == 0)
            {
                MessageBox.Show("Vui lòng duyệt ít nhất 1 đề xuất trước khi gửi lên Wildberries.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn gửi {approved.Count} đề xuất đã duyệt lên Wildberries?\n\n" +
                "- Hệ thống sẽ kiểm tra bảo toàn 100% dữ liệu gốc (ảnh, barcode, sizes, documents).\n" +
                "- Trạng thái sẽ được theo dõi qua 4 trục: Delivery, Readback, Marketplace Check (180 ngày).",
                "Xác nhận gửi an toàn",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var progress = new Progress<string>(m => StatusMessage = m);
                var res = await _updateWorker.ExecuteApprovedProposalsAsync(approved, _sellerAccount, progress);

                UpdateStats();
                MessageBox.Show(res.SummaryMessage, "Kết quả gửi Wildberries", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi gửi WB: {ex.Message}";
                MessageBox.Show($"Lỗi gửi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void CreateSourceTask()
        {
            if (SelectedFinding == null) return;
            MessageBox.Show(
                $"Đã tạo tác vụ xử lý dữ liệu nguồn:\n\n" +
                $"Mã lỗi: {SelectedFinding.RuleId}\n" +
                $"Nguyên nhân tiếng Nga: {SelectedFinding.RawMessageRu}\n" +
                $"Hướng dẫn tiếng Việt: {SelectedFinding.TranslatedExplanationVi}\n\n" +
                $"Vui lòng truy cập Cổng Quốc gia Национальный Каталог (Честный ЗНАК) hoặc liên hệ phòng kho để xử lý nhãn vật lý trước khi sửa thẻ WB.",
                "Tạo việc sửa dữ liệu nguồn",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void UpdateStats()
        {
            OnPropertyChanged(nameof(OpenFindingsCount));
            OnPropertyChanged(nameof(ReadyProposalsCount));
            OnPropertyChanged(nameof(ApprovedProposalsCount));
        }
    }
}