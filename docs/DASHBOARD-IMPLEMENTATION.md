# KẾ HOẠCH TRIỂN KHAI KỸ THUẬT DASHBOARD (IMPLEMENTATION SPECIFICATION)

**Hệ thống:** SupplierSelectionDSS  
**Mục tiêu:** Xây dựng Dashboard hỗ trợ ra quyết định doanh nghiệp hoàn chỉnh, 100% dữ liệu thật, tối ưu hiệu năng và visual aesthetics đỉnh cao.

---

## 1. Cấu Trúc File & Thành Phần

```text
SupplierSelectionDSS/
├── Models/
│   └── DashboardViewModel.cs         <-- Refactor toàn diện: Context, KPIs, Charts DTOs, Workflow, Approval
├── Services/
│   └── DashboardService.cs           <-- Truy vấn dữ liệu thực tế theo Batch, tổng hợp DTOs tối ưu AsNoTracking()
├── Controllers/
│   └── DashboardController.cs        <-- Hỗ trợ tham số int? batchId = null, điều phối luồng mượt mà
├── Views/
│   └── Dashboard/
│       └── Index.cshtml              <-- Giao diện Razor chuẩn Enterprise, liên kết Chart.js cục bộ
├── wwwroot/
│   ├── lib/chartjs/
│   │   └── chart.umd.min.js          <-- Thư viện Chart.js 4.4.7 offline (đã tải sẵn)
│   ├── css/pages/
│   │   └── dashboard.css             <-- Stylesheet chuyên biệt cho Dashboard, kế thừa tokens.css
│   └── js/pages/
│       └── dashboard.js              <-- Khởi tạo biểu đồ Donut, Line, Bar, Radar và đồng hồ thời gian thực
└── docs/
    ├── DASHBOARD-DESIGN.md           <-- Tài liệu thiết kế giao diện
    ├── DASHBOARD-DATA-MAPPING.md     <-- Bảng ánh xạ nguồn dữ liệu 100%
    └── DASHBOARD-IMPLEMENTATION.md   <-- Tài liệu kế hoạch kỹ thuật này
```

---

## 2. Thiết Kế Lớp Dữ Liệu (ViewModel Hierarchy)

```csharp
namespace SupplierSelectionDSS.Models;

public class DashboardViewModel
{
    // 1. Context đợt đánh giá
    public int? SelectedBatchId { get; set; }
    public List<BatchSelectionOptionViewModel> AvailableBatches { get; set; } = new();
    public DashboardCurrentBatchViewModel? CurrentBatch { get; set; }

    // 2. Thống kê KPI hệ thống
    public int TotalSuppliers { get; set; }
    public int ActiveSuppliers { get; set; }
    public int TotalProductGroups { get; set; }
    public int TotalBatches { get; set; }
    public int CompletedBatches { get; set; }
    public int TotalCriteria { get; set; }

    // 3. KPI AHP & TOPSIS của đợt hiện tại
    public decimal? AhpCrValue { get; set; }
    public bool? IsAhpConsistent { get; set; }
    public string? Top1SupplierName { get; set; }
    public decimal? Top1ClosenessCoefficient { get; set; }

    // 4. DTOs Biểu đồ Hàng 1
    public DashboardDonutChartDto BatchStatusDistribution { get; set; } = new();
    public DashboardTrendChartDto EvaluationTrend { get; set; } = new();
    public DashboardDonutChartDto SupplierStatusDistribution { get; set; } = new();

    // 5. Bảng & Biểu đồ Hàng 2: TOPSIS & Workflow
    public List<DashboardTopsisRankingItemDto> TopTopsisRankings { get; set; } = new();
    public DashboardWorkflowProgressDto WorkflowProgress { get; set; } = new();

    // 6. DTOs Hàng 3: Trọng số AHP, Radar so sánh, Hệ thống, Phê duyệt
    public List<DashboardAhpWeightItemDto> AhpCriteriaWeights { get; set; } = new();
    public DashboardRadarComparisonDto RadarComparison { get; set; } = new();
    public DashboardSystemInfoDto SystemInfo { get; set; } = new();
    public DashboardApprovalDto? ApprovalInfo { get; set; }

    // 7. Hoạt động gần đây
    public List<DashboardActivityItemDto> RecentActivities { get; set; } = new();
}
```

---

## 3. Kiến Trúc Truy Vấn Tại `DashboardService`

- Tất cả các truy vấn đều dùng `.AsNoTracking()` để đạt tốc độ phản hồi dưới 50ms.
- **Xác định Batch mặc định:**
  1. Ưu tiên Batch được người dùng chọn qua URL query `?batchId=...`.
  2. Nếu không có query: Lấy đợt gần nhất có trạng thái `Evaluating` hoặc `Completed`.
  3. Nếu không có: Lấy đợt mới nhất được tạo trong `EvaluationBatches`.
- **Tính toán Tiến độ đánh giá thực tế:**
  - So sánh số lượng nhà cung cấp trong `BatchSuppliers` với số lượng nhà cung cấp đã có ít nhất một bản ghi điểm trong `EvaluationScores` của đợt đó.
- **Tính toán Workflow 5 bước:**
  - Bước 1 (Nhập dữ liệu): Đạt nếu có `EvaluationScores`.
  - Bước 2 (AHP): Đạt nếu có bản ghi trong `AHPResults`.
  - Bước 3 (TOPSIS): Đạt nếu có bản ghi trong `TopsisResults`.
  - Bước 4 (So sánh): Đạt nếu đã có kết quả TOPSIS.
  - Bước 5 (Phê duyệt): Đạt nếu có bản ghi `Approvals` với trạng thái `Approved`.
- **Trực quan hóa Radar Chart Top 3 NCC:**
  - Lấy Top 3 NCC có hạng cao nhất từ `TopsisResults`.
  - Truy vấn điểm trung bình các tiêu chí của 3 NCC này trong `EvaluationScores`, ánh xạ tương ứng vào từng trục tiêu chí của biểu đồ Radar.

---

## 4. Chuyển Đổi Ngữ Cảnh Batch (Interactive Batch Switching)

- Dropdown chọn Batch trên Header hoạt động qua URL query `window.location.href = '/Dashboard?batchId=' + selectedId;`.
- Giữ vững tính mượt mà của trang, không làm gián đoạn Sidebar hay Topbar.
- Lưu lại lựa chọn trong sessionStorage nếu cần tiện ích truy cập nhanh.

---

## 5. Quy Trình Kiểm Thử & Nghiệm Thu

1. Kiểm thử đợt có đầy đủ kết quả AHP + TOPSIS + Approval (VD: `BAT-002`, `EB001`).
2. Kiểm thử đợt đang đánh giá (Evaluating) chưa phê duyệt (VD: `EB005`, `EB007`).
3. Kiểm thử đợt bản nháp hoặc hủy (Draft/Cancelled).
4. Kiểm thử trường hợp database chưa có kết quả để xác nhận hiển thị Empty State chuyên nghiệp.
5. Kiểm thử toàn bộ 67 test hiện có để bảo đảm 0 lỗi regression.
