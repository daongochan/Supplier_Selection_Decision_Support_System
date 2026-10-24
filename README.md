# Supplier Selection Decision Support System

[![Build and Test](https://github.com/daongochan/Supplier_Selection_Decision_Support_System/actions/workflows/ci.yml/badge.svg)](https://github.com/daongochan/Supplier_Selection_Decision_Support_System/actions/workflows/ci.yml)

Ứng dụng web hỗ trợ ra quyết định lựa chọn nhà cung cấp kết hợp hai phương pháp đa tiêu chí (MCDM):
- **AHP (Analytic Hierarchy Process):** Xác định vector trọng số các tiêu chí qua ma trận so sánh cặp, tính trung bình nhân theo hàng và kiểm tra tính nhất quán ($CR \le 0.10$).
- **TOPSIS (Technique for Order Preference by Similarity to Ideal Solution):** Chuẩn hóa vector điểm số, tính khoảng cách Euclid tới giải pháp lý tưởng tốt nhất ($S^+$) và xấu nhất ($S^-$), xếp hạng nhà cung cấp theo hệ số gần gũi ($C_i$) và phân tích độ nhạy ($\pm 10\%$, $\pm 20\%$).

Hệ thống được phát triển trên nền tảng **ASP.NET Core MVC** (.NET 10) và **SQL Server**, quản lý toàn bộ quy trình: chuẩn bị danh mục, tạo đợt đánh giá, chấm điểm ma trận, tính toán MCDM, phân tích độ nhạy, trình phê duyệt quyết định và xuất báo cáo PDF/Excel.

---

## Chức năng Chính

1. **Quản lý danh mục (Master Data):** Quản lý người dùng, nhóm hàng, danh sách nhà cung cấp (kèm liên kết nhóm hàng) và tiêu chí đánh giá (phân loại Benefit/Cost, đơn vị tính Điểm/VND/Ngày).
2. **Quy trình đợt đánh giá (Evaluation Batch Lifecycle):**
   - Trạng thái `Draft`: Chuẩn bị phạm vi (chọn nhóm hàng, tối thiểu 2 nhà cung cấp và 2 tiêu chí).
   - Trạng thái `Evaluating`: Chấm điểm ma trận quyết định và hoàn thiện so sánh cặp AHP.
   - Trạng thái `Completed` / `Cancelled`: Đóng băng dữ liệu, cấm sửa đổi qua kiểm tra lớp service và trigger SQL Server.
3. **Không gian làm việc AHP (AHP Workspace):**
   - Nhập phán đoán theo thang đo Saaty 1–9 hoặc phân số nghịch đảo (`1/3`, `1/5`, `1/7`, `1/9`).
   - Tự động điền giá trị đối xứng nghịch đảo; đường chéo cố định bằng 1.0.
   - Kiểm tra tính nhất quán ($CR \le 0.10$); hiển thị cảnh báo trực quan khi ma trận mâu thuẫn.
   - *Lưu ý về độ chính xác lưu trữ:* Cột dữ liệu trong database sử dụng `DECIMAL(10,4)`, do đó các phân số vô hạn tuần hoàn (như $1/3 \approx 0.3333$) được lưu trữ ở độ chính xác 4 chữ số thập phân.
   - *Phạm vi hiện tại:* Mỗi đợt đánh giá sử dụng **một ma trận AHP chung**. Đề xuất mở rộng Hội đồng đa chuyên gia xem tại [Đề xuất Group AHP](docs/proposals/GROUP-AHP-PROPOSAL.md) (chưa triển khai trong mã nguồn hiện hành).
4. **Không gian làm việc TOPSIS & Độ nhạy (TOPSIS Workspace):**
   - Tính toán ma trận quyết định chuẩn hóa vector và chuẩn hóa nhân trọng số.
   - Xác định nghiệm lý tưởng PIS ($A^+$) và NIS ($A^-$) phân biệt đúng cho tiêu chí Benefit (càng lớn càng tốt) và Cost (càng nhỏ càng tốt).
   - Phân tích độ nhạy với các biến thiên trọng số $\pm 10\%$ và $\pm 20\%$, theo dõi sự thay đổi thứ hạng (rank reversal).
5. **Phê duyệt & Quyết định (Decision & Approvals):**
   - Trình duyệt và ghi nhận phê duyệt (`Approved`), từ chối (`Rejected`), hoặc yêu cầu đánh giá lại (`Reevaluation`).
   - Lưu trữ lịch sử snapshot từng vòng đánh giá trong bảng `BatchRoundSnapshots`.
   - Kiểm soát tương tranh lạc quan (Optimistic Concurrency Control) thông qua cột `RowVersion` trên đợt đánh giá.
6. **Báo cáo & Kiểm toán (Reporting & Auditing):**
   - Xuất hồ sơ quyết định dạng **PDF** (chuẩn QuestPDF) và **Excel** (chuẩn ClosedXML).
   - Báo cáo sinh trong runtime được lưu tại `App_Data/reports` với kiểm tra phân quyền, đường dẫn `/reports` tĩnh bị chặn truy cập công khai.
   - Nhật ký hoạt động (Audit Log) ghi nhận toàn bộ thao tác nhạy cảm kèm mã theo dõi TraceIdentifier.

---

## Công nghệ & Thư viện

| Thành phần | Công nghệ / Gói NuGet | Trách nhiệm |
|---|---|---|
| **Web Framework** | ASP.NET Core MVC (.NET 10) | Tầng trình diễn Razor, Controller endpoints, ViewModels |
| **Data Access** | EF Core 10.0.11 (`Microsoft.EntityFrameworkCore.SqlServer`) | ORM truy vấn SQL Server, DbContext, Entity mapping |
| **Cơ sở dữ liệu** | Microsoft SQL Server (2019 trở lên) | Lưu trữ quan hệ, 18 bảng, 7 triggers nghiệp vụ, 21 indexes, 2 views |
| **Xác thực** | Cookie Authentication + BCrypt.Net-Next (4.2.0) | Đăng nhập an toàn, quản lý phiên cookie, băm mật khẩu cost 11 |
| **Báo cáo PDF** | QuestPDF (2026.7.3) | Sinh báo cáo hồ sơ phê duyệt phân trang và bảng biểu |
| **Báo cáo Excel** | ClosedXML (0.105.1) | Xuất bảng dữ liệu đánh giá và kết quả xếp hạng định dạng `.xlsx` |
| **Giao diện & Asset** | CSS3 Design Tokens, Source Serif 4, Bootstrap Icons | Giao diện quản trị, pipeline bundle determinism không phụ thuộc Node |
| **Kiểm thử** | xUnit 2.9.3, FluentAssertions 7.2.0, Moq 4.20.72 | Unit tests, Integration tests, SQL trigger tests, Parity tests |

---

## Hướng Dẫn Môi Trường Phát Triển

### 1. Yêu cầu cài đặt sẵn (Prerequisites)

- **.NET SDK 10.0** (hỗ trợ `net10.0`).
- **Microsoft SQL Server** (bản Developer, Standard, Express hoặc LocalDB) kèm tiện ích dòng lệnh `sqlcmd`.
- **Python 3** (dùng cho các công cụ kiểm tra tài nguyên và schema).
- **Git**.

### 2. Clone repository & Restore dependencies

**PowerShell (Windows):**
```powershell
git clone https://github.com/daongochan/Supplier_Selection_Decision_Support_System.git
cd Supplier_Selection_Decision_Support_System
dotnet restore SupplierSelectionDSS.slnx
```

**Bash (Linux / macOS):**
```bash
git clone https://github.com/daongochan/Supplier_Selection_Decision_Support_System.git
cd Supplier_Selection_Decision_Support_System
dotnet restore SupplierSelectionDSS.slnx
```

---

### 3. Khởi tạo Cơ sở dữ liệu Phát triển (Fresh Database Setup)

Chỉ thực hiện trên SQL Server phát triển **chưa có database `SupplierSelectionDSS`**. Chạy tuần tự và kiểm tra lỗi bằng cờ `-b`:

**Windows PowerShell (SQL Server cục bộ `.` dùng Windows Authentication):**
```powershell
# Bước 1: Tạo Schema (18 bảng, 7 triggers, 2 views, 21 indexes)
sqlcmd -S . -E -C -b -v ConfirmRecreate=NO -i database/01_schema.sql

# Bước 2: Chỉ nạp dữ liệu seed khi bước 1 thành công
sqlcmd -S . -E -C -b -i database/02_seed.sql
```

**Linux / macOS / Docker (SQL Server dùng tài khoản `sa`):**
```bash
# Bước 1: Tạo Schema
sqlcmd -S localhost,1433 -U sa -P 'YourPassword' -C -b -v ConfirmRecreate=NO -i database/01_schema.sql

# Bước 2: Nạp dữ liệu seed
sqlcmd -S localhost,1433 -U sa -P 'YourPassword' -C -b -i database/02_seed.sql
```

> [!NOTE]
> - `ConfirmRecreate=NO`: Đảm bảo biến SQLCMD được định nghĩa an toàn và ngăn ngừa xóa nhầm database đã có.
> - `database/02_seed.sql` là nguồn dữ liệu khởi tạo phát triển duy nhất. Không chạy thêm `database/scripts/Run_All.sql` vì file đó chỉ dành cho schema.
> - **Nâng cấp database đang chạy:** Nếu database đã có dữ liệu nghiệp vụ, không chạy lệnh tạo mới mà áp dụng tuần tự các bản vá trong [database/patches](database/patches) theo hướng dẫn tại [database/README.md](database/README.md).

---

### 4. Cấu hình Chuỗi kết nối (Secrets Configuration)

Project đã được khai báo sẵn `UserSecretsId` (`dss-supplier-selection-system-2026`) trong [SupplierSelectionDSS.csproj](SupplierSelectionDSS.csproj), cho phép thiết lập User Secrets ngay lập tức mà không cần chạy `init`:

**Windows PowerShell:**
```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=SupplierSelectionDSS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project SupplierSelectionDSS.csproj
```

**Hoặc dùng biến môi trường (Linux / Docker / CI):**
```bash
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=SupplierSelectionDSS;User Id=sa;Password=YourPassword;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

---

### 5. Build và Chạy ứng dụng

**Build cấu hình Release:**
```bash
dotnet build SupplierSelectionDSS.slnx --configuration Release
```

**Cài đặt chứng chỉ HTTPS cục bộ (chỉ cần chạy một lần trên máy dev):**
```bash
dotnet dev-certs https --trust
```

**Chạy ứng dụng với HTTPS profile:**
```bash
dotnet run --project SupplierSelectionDSS.csproj --launch-profile https
```

- Mở trình duyệt tại: **[https://localhost:7146](https://localhost:7146)** (Profile `https` dùng cổng `7146`).
- Profile `http` chạy tại cổng `5218` ([http://localhost:5218](http://localhost:5218)).

---

### 6. Tài khoản Mẫu Môi trường Phát triển (Dev Only)

Khai báo sẵn trong `database/02_seed.sql`. **Tuyệt đối không dùng các tài khoản này trên Production:**

| Tài khoản | Mật khẩu demo | Vai trò (Role) | Chức năng chính |
|---|---|---|---|
| `admin` | `Admin@123456` | Administrator | Quản trị người dùng, nhật ký audit, xem toàn bộ đợt |
| `staff01` | `Staff@123456` | PurchasingStaff | Quản lý NCC, nhóm hàng, tiêu chí, tạo đợt đánh giá, nhập điểm |
| `expert01` | `Expert@123456` | Expert | Nhập điểm đánh giá, thực hiện so sánh cặp AHP & tính toán TOPSIS |
| `mgr01` | `Manager@123456` | Manager | Xem kết quả, ra quyết định phê duyệt/từ chối/đánh giá lại, xuất báo cáo |

---

## Phân Quyền & Quy Trình Nghiệp Vụ

### Ma trận Phân quyền Vai trò

| Chức năng | Endpoint / Controller | Administrator | PurchasingStaff | Expert | Manager |
|---|---|:---:|:---:|:---:|:---:|
| Quản trị người dùng | `/User` | Có | Không | Không | Không |
| Nhật ký kiểm toán | `/AuditLog` | Có | Không | Không | Không |
| Quản lý nhóm hàng & NCC | `/ProductGroup`, `/Supplier` | Có | Có | Không | Không |
| Quản lý tiêu chí đánh giá | `/Criteria` | Có | Có | Không | Không |
| Tạo / Cấu hình đợt đánh giá | `/EvaluationBatch/Create` | Có | Có | Không | Không |
| Xem danh sách đợt & chi tiết | `/EvaluationBatch` | Có | Có | Có | Có |
| Nhập / Lưu ma trận điểm | `/Evaluation?batchId=...` | Có | Có | Có | Không |
| Workspace AHP (so sánh cặp) | `/AHP?batchId=...` | Có | Không | Có | Không |
| Workspace TOPSIS & Độ nhạy | `/TOPSIS?batchId=...` | Có | Không | Có | Không |
| Xem kết quả quyết định | `/DecisionResult?batchId=...` | Có | Có | Có | Có |
| Ra quyết định (Phê duyệt/Hủy) | `/DecisionResult/Approve`, `/Reject` | Có | Không | Không | Có |
| Xuất báo cáo PDF / Excel | `/Report/ExportPdf`, `/ExportExcel` | Có | Không | Không | Có |

---

## Cấu Trúc Repository

```
SupplierSelectionDSS/
├── Controllers/                 # HTTP endpoints, routing, role-based authorization
├── Views/                       # Razor views, layouts, confirmation modal, error pages
├── Models/                      # ViewModels, DTOs, paged result models
├── Services/                    # Core business orchestration, AHP/TOPSIS calculations
├── Domain/                      # Batch state rules, lifecycle enum mappings
├── Constants/                   # Saaty scale codes, persisted string constants, roles
├── Exceptions/                  # BusinessRuleException and domain errors
├── Data/                        # ApplicationDbContext, EF Core entity models
├── Infrastructure/Exports/      # QuestPDF (PDF) and ClosedXML (Excel) report exporters
├── Web/Extensions/              # ClaimsPrincipal and HTTP context utilities
├── wwwroot/                     # Client assets (CSS, JS, fonts, images, vendor lib)
├── App_Data/reports/            # Vị trí mặc định lưu trữ báo cáo sinh trong runtime
├── database/                    # 01_schema.sql (canonical), 02_seed.sql, patches/
├── SupplierSelectionDSS.Tests/  # Unit, integration, SQL trigger, and calculation parity tests
├── tools/
│   ├── assets/                  # Deterministic bundle_css.py and localize_fonts.py
│   ├── verify/                  # verify_schema_sync.py and MCDM parity checks
│   └── perf/                    # Benchmark runner scripts and perf DB provisioner
└── docs/                        # Architecture, algorithms, workflow, QA matrix, archive
```

---

## Kiểm Thử & Kiểm Chứng Hệ Thống

### 1. Kiểm tra Tính đồng bộ CSDL
```bash
python tools/verify/verify_schema_sync.py
```
Kiểm tra cấu trúc 18 bảng, 7 triggers, 2 views, 21 indexes và đối chiếu danh sách cột giữa aggregate schema và các module scripts.

### 2. Kiểm tra Tính Tái lập của CSS Bundle
```bash
python tools/assets/bundle_css.py --check
```
Kiểm tra đảm bảo file `wwwroot/css/site.css` phục vụ trình duyệt khớp hoàn toàn với các source components (LF line endings, UTF-8, SHA-256).

### 3. Chạy Toàn Bộ Test Suite
```bash
dotnet test SupplierSelectionDSS.slnx --configuration Release
```

### 4. Kiểm thử Tích hợp SQL Server Thực
Lớp kiểm thử `SqlServerConcurrencyAndTriggerTests` xác minh kiểm soát tương tranh `RowVersion` và trigger bảo vệ trạng thái trên SQL Server thực tế:
- Mặc định kết nối tới `DSS_TEST_SQL_SERVER` (hoặc `.`) và tự động tạo database tạm riêng biệt có tên ngẫu nhiên `SupplierSelectionDSS_Test_<id>`.
- Tự động dọn dẹp sạch sẽ database tạm khi hoàn tất.
- Để bắt buộc kiểm thử SQL Server phải chạy (và fail nếu không kết nối được), đặt biến môi trường:
  ```powershell
  $env:DSS_REQUIRE_SQL = "1"
  dotnet test SupplierSelectionDSS.slnx --filter "Category=SqlServer"
  ```

---

## Cấu Hình Vận Hành & Giới Hạn Thực Tế

- **Đường dẫn Báo cáo:** Cấu hình qua khóa `Reports:Directory` (hoặc biến môi trường `Reports__Directory`), mặc định là thư mục `App_Data/reports`. Cần cấp quyền ghi cho tiến trình ứng dụng. Mọi truy vấn trực tiếp vào `/reports/*` đều bị middleware chặn.
- **Giới hạn Đăng nhập (Rate Limiting):** Triển khai bằng bộ nhớ trong tiến trình (In-Memory). Khi triển khai cụm nhiều máy chủ (load balancing / multi-instance), cần tích hợp distributed cache (Redis) để chia sẻ trạng thái khóa tài khoản.
- **Độ chính xác Số học:** Các phép tính AHP và TOPSIS được tính toán trên kiểu `double` và `decimal` có độ chính xác cao. Dữ liệu lưu trữ trong CSDL sử dụng `DECIMAL(10,4)` theo thiết kế schema hiện hữu.
- **Hội đồng Đánh giá:** Phiên bản hiện tại sử dụng một ma trận AHP chung cho mỗi đợt đánh giá; chưa hỗ trợ thu thập phiếu chấm riêng rẽ từ nhiều chuyên gia.

---

## Tài Liệu Tham Khảo

- [Kiến trúc Hệ thống](docs/ARCHITECTURE.md)
- [Thuật toán AHP & TOPSIS](docs/ALGORITHMS.md)
- [Quy trình Nghiệp vụ & Phân quyền](docs/WORKFLOW-AND-PERMISSIONS.md)
- [Hệ thống Thiết kế & Giao diện](docs/DESIGN-SYSTEM.md)
- [Hướng dẫn Tối ưu & Hiệu năng](docs/PERFORMANCE.md)
- [Ma trận Kiểm thử QA](docs/qa/TEST_MATRIX.md)
- [Hướng dẫn Cơ sở Dữ liệu](database/README.md)
- [Nhật ký Thay đổi (CHANGELOG)](CHANGELOG.md)
- [Quy chuẩn Phát triển (AGENTS.md)](AGENTS.md)

---

## Giấy Phép (License)

Dự án được phân phối dưới giấy phép [MIT License](LICENSE). Các thư viện bên thứ ba trong `wwwroot/lib/` tuân thủ giấy phép nguồn mở tương ứng của chúng.
