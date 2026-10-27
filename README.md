<div align="center">

  <img src="wwwroot/images/app-logo.png" alt="Supplier Selection DSS logo" width="112" />

  # Supplier Selection DSS

  **Hệ thống hỗ trợ ra quyết định lựa chọn nhà cung cấp bằng AHP & TOPSIS**

  <sub>Evidence-driven supplier evaluation · Transparent ranking · Accountable decisions</sub>

  <p>
    <a href="https://github.com/daongochan/Supplier_Selection_Decision_Support_System/actions/workflows/ci.yml"><img alt="Build and Test" src="https://github.com/daongochan/Supplier_Selection_Decision_Support_System/actions/workflows/ci.yml/badge.svg" /></a>
    <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white" />
    <img alt="EF Core 10" src="https://img.shields.io/badge/EF%20Core-10-2563EB" />
    <img alt="SQL Server 2019+" src="https://img.shields.io/badge/SQL%20Server-2019%2B-CC292B" />
    <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/License-MIT-15803D" /></a>
  </p>

  <p>
    <a href="#quick-start"><strong>Quick start</strong></a> ·
    <a href="#features"><strong>Features</strong></a> ·
    <a href="#demo"><strong>Demo</strong></a> ·
    <a href="#testing"><strong>Testing</strong></a> ·
    <a href="docs/README.md"><strong>Documentation</strong></a>
  </p>

</div>

---

<a id="overview"></a>
## Tổng quan · Overview

**Supplier Selection Decision Support System (DSS)** là ứng dụng Web nguyên mẫu phục vụ đánh giá và lựa chọn nhà cung cấp theo phương pháp **Multi-Criteria Decision-Making (MCDM)**. Hệ thống kết hợp:

- **AHP (Analytic Hierarchy Process):** Xác định trọng số tiêu chí từ ma trận so sánh cặp Saaty, tính bằng **Row Geometric Mean Method (RGMM)** và kiểm tra tính nhất quán với ngưỡng `CR ≤ 0.10`.
- **TOPSIS:** Đánh giá các nhà cung cấp trên cùng bộ tiêu chí `Benefit` / `Cost`, tính nghiệm lý tưởng, khoảng cách Euclid, hệ số gần lý tưởng và thứ hạng.
- **Decision workflow:** Quản lý đợt đánh giá, phân tích độ nhạy, phê duyệt có giải trình, lưu lịch sử và xuất báo cáo.

> [!NOTE]
> Đây là **hệ thống nguyên mẫu phục vụ nghiên cứu và trình diễn**, không phải sản phẩm đã được kiểm chứng về hiệu quả vận hành tại doanh nghiệp thực tế. Dữ liệu seed và tài khoản mẫu chỉ dùng cho môi trường phát triển/demo.

<a id="features"></a>
## Tính năng nổi bật · Features

| Phân hệ | Khả năng chính |
|---|---|
| **Executive Dashboard** | KPI, trạng thái đợt đánh giá, biểu đồ tổng quan và tác vụ theo vai trò |
| **Master Data** | Quản lý nhà cung cấp, nhóm hàng, tiêu chí; tìm kiếm, lọc, liên kết nhà cung cấp với nhóm hàng |
| **Evaluation Batches** | Thiết lập ít nhất 2 nhà cung cấp và 2 tiêu chí; quản lý các trạng thái `Draft`, `Evaluating`, `Completed`, `Cancelled` |
| **Evaluation Matrix** | Nhập điểm dạng bảng, kiểm tra giá trị theo đơn vị đo, kiểm soát dữ liệu thiếu và thay đổi |
| **AHP Workspace** | Thang Saaty 1/9–9; ma trận nghịch đảo; trọng số RGMM; các chỉ số `CI`, `RI`, `CR` |
| **TOPSIS Workspace** | Chuẩn hóa vector; ma trận có trọng số; PIS/NIS; khoảng cách Euclid; `CC` và thứ hạng |
| **Sensitivity Analysis** | Biến thiên trọng số từng tiêu chí ±10% / ±20%; theo dõi thay đổi nhà cung cấp dẫn đầu trong các kịch bản |
| **Approval & History** | Phê duyệt / từ chối / yêu cầu đánh giá lại; bắt buộc giải trình nếu chọn ngoài hạng 1; lưu snapshot theo vòng |
| **Reporting & Audit** | Xuất PDF / Excel; lưu lịch sử báo cáo và nhật ký thao tác; kiểm soát quyền tải xuống |

### Quy trình hỗ trợ ra quyết định

~~~mermaid
flowchart LR
    A["Master data<br/>Nhóm hàng · NCC · Tiêu chí"] --> B["Create batch<br/>Draft"]
    B --> C["Input scores<br/>Evaluating"]
    C --> D["AHP<br/>RGMM + CR"]
    D --> E["TOPSIS<br/>CC + Ranking"]
    E --> F["Sensitivity<br/>±10% / ±20%"]
    F --> G["Manager decision"]
    G --> H["Snapshot + PDF / Excel"]
~~~

### Vòng đời đợt đánh giá

~~~mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> Evaluating: Start evaluation
    Draft --> Cancelled: Cancel
    Evaluating --> Evaluating: Update / Re-evaluate
    Evaluating --> Completed: Final approval
    Evaluating --> Cancelled: Cancel
    Completed --> [*]
    Cancelled --> [*]
~~~

> **Phân biệt quan trọng:** TOPSIS đưa ra **thứ hạng tham khảo**; quyết định lựa chọn cuối cùng thuộc về người quản lý có thẩm quyền. Việc yêu cầu đánh giá lại không đồng nghĩa với phê duyệt cuối cùng.

<a id="architecture"></a>
## Kiến trúc & công nghệ · Architecture

~~~mermaid
flowchart TB
    U["Web browser<br/>4 business roles"] --> P["ASP.NET Core MVC<br/>Controllers + Razor Views"]
    P --> S["Business services<br/>Validation · Workflow · RBAC"]
    S --> A["AHP Calculation<br/>RGMM · CR"]
    S --> T["TOPSIS Calculation<br/>PIS/NIS · Ranking"]
    S --> V["Sensitivity Analysis"]
    S --> EF["Entity Framework Core<br/>ApplicationDbContext"]
    EF --> DB[("SQL Server")]
    S --> R["Report exporters<br/>QuestPDF · ClosedXML"]
    R --> FS["App_Data/reports"]
~~~

| Thành phần | Công nghệ |
|---|---|
| Web application | ASP.NET Core MVC · `net10.0` · C# · Razor Views |
| Data access | Entity Framework Core `10.0.11` |
| Database | Microsoft SQL Server 2019+ · `Vietnamese_CI_AS` |
| Authentication | Cookie Authentication · BCrypt.Net-Next |
| Reporting | QuestPDF `2026.7.3` · ClosedXML `0.105.1` |
| Automated tests | xUnit · FluentAssertions · Moq · Coverlet |
| CI / tooling | GitHub Actions · Python verification tools |

Cơ sở dữ liệu chuẩn của nhánh hiện tại có **18 bảng, 7 triggers, 2 views và 21 chỉ mục**. Ngoài các ràng buộc SQL, hệ thống sử dụng `RowVersion` để phát hiện xung đột cập nhật và `BatchRoundSnapshots` để lưu dữ liệu tại những thời điểm nghiệp vụ quan trọng.

Xem chi tiết tại [Architecture](docs/ARCHITECTURE.md) và [Workflow & Permissions](docs/WORKFLOW-AND-PERMISSIONS.md).

<a id="quick-start"></a>
## Bắt đầu nhanh · Quick start

**Yêu cầu:** .NET 10 SDK, SQL Server 2019+ (hoặc LocalDB), `sqlcmd` và Git. Các lệnh dưới đây minh họa **Windows + SQL Server dùng Windows Authentication**. Nếu dùng SQL Authentication, thay tham số kết nối tương ứng (`-U`/`-P` hoặc biến môi trường).

**1. Lấy mã nguồn và khôi phục dependencies**

~~~powershell
git clone https://github.com/daongochan/Supplier_Selection_Decision_Support_System.git
cd Supplier_Selection_Decision_Support_System
dotnet restore SupplierSelectionDSS.slnx
~~~

**2. Tạo cơ sở dữ liệu demo riêng biệt**

~~~powershell
cd database/scripts
sqlcmd -S . -E -C -b -v DatabaseName=SupplierSelectionDSS_Demo -i Run_Production.sql
cd ../..
~~~

**3. Nạp dữ liệu demo và chạy xác minh**

~~~powershell
dotnet run --project tools/demo/DemoSeeder/DemoSeeder.csproj -- --seed
~~~

**4. Cấu hình kết nối cho ứng dụng Web**

~~~powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=SupplierSelectionDSS_Demo;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project SupplierSelectionDSS.csproj
~~~

**5. Chạy ứng dụng**

~~~powershell
dotnet run --project SupplierSelectionDSS.csproj --launch-profile https
~~~

Truy cập **https://localhost:7146** (hoặc **http://localhost:5218**). Để tin cậy chứng chỉ phát triển HTTPS, có thể chạy `dotnet dev-certs https --trust` trên máy phát triển.

> [!CAUTION]
> Script `Run_Production.sql` **chỉ dùng để khởi tạo database mới**, không dùng để nâng cấp database đang chứa dữ liệu. Các bước sửa đổi cơ sở dữ liệu hiện hữu nằm trong [database/README.md](database/README.md) và thư mục [database/patches](database/patches).

<details>
<summary><strong>Các cách khởi tạo khác: Development và Production</strong></summary>

**Development với dữ liệu seed cơ bản** (database `SupplierSelectionDSS` chưa tồn tại):

~~~powershell
sqlcmd -S . -E -C -b -v ConfirmRecreate=NO -i database/01_schema.sql
sqlcmd -S . -E -C -b -i database/02_seed.sql
~~~

**Fresh Production** (không nạp tài khoản mẫu):

~~~powershell
cd database/scripts
sqlcmd -S . -E -C -b -v DatabaseName=SupplierSelectionDSS -i Run_Production.sql
cd ../..
~~~

Cấu hình kết nối production qua **secret store / environment variable**, không commit mật khẩu vào repository. Chỉ triển khai database mới khi đã kiểm tra tên đích và quyền truy cập.

Xem [Production provisioning guide](database/scripts/PRODUCTION_README.md).

</details>

<a id="demo"></a>
## Dữ liệu trình diễn · Reproducible demo

`DemoSeeder` tạo dữ liệu trong **`SupplierSelectionDSS_Demo`** với chốt an toàn để không chạy nhầm vào database phát triển, benchmark hoặc database hệ thống.

| Dữ liệu | Quy mô |
|---|---:|
| Tài khoản demo | 5 (4 vai trò hoạt động, 1 tài khoản Inactive) |
| Nhóm hàng | 4 |
| Nhà cung cấp | 12 |
| Tiêu chí | 7 |
| Đợt đánh giá | 8 |

### Các kịch bản đã chuẩn bị

| Đợt | Trạng thái | Mục đích minh họa |
|---|---|---|
| `DEMO-B01` | `Completed` | Đã phê duyệt; có snapshot và báo cáo PDF / Excel |
| `DEMO-B02` | `Completed` | Đã phê duyệt; có snapshot và báo cáo PDF / Excel |
| `DEMO-B03` | `Evaluating` | Hoàn thành TOPSIS, sẵn sàng xem xét phê duyệt |
| `DEMO-B04` | `Evaluating` | AHP đã đạt yêu cầu, chuẩn bị tính TOPSIS |
| `DEMO-B05` | `Evaluating` | Ma trận AHP không nhất quán, `CR > 0.10` |
| `DEMO-B06` | `Evaluating` | Đang nhập điểm; có 7/12 giá trị |
| `DEMO-B07` | `Draft` | Đợt mới tạo, chưa nhập điểm |
| `DEMO-B08` | `Cancelled` | Đợt đã hủy, lưu trạng thái lịch sử |

**Tài khoản cho database demo** (được tạo bởi `DemoSeeder`):

| Username | Role |
|---|---|
| `demo.admin` | Administrator |
| `demo.staff` | PurchasingStaff |
| `demo.expert` | Expert |
| `demo.manager` | Manager |
| `demo.inactive` | Expert — Inactive |

Mật khẩu mặc định của nhóm tài khoản demo là **`Demo@123456`**; có thể thay đổi bằng biến môi trường `DEMO_PASSWORD` hoặc tùy chọn `--password` khi chạy seeder. Tài khoản Inactive được dùng để kiểm thử điều kiện từ chối đăng nhập.

<details>
<summary><strong>Tài khoản development seed (khác tài khoản demo)</strong></summary>

Dữ liệu từ `database/02_seed.sql` sử dụng:

| Username | Mật khẩu khởi tạo |
|---|---|
| `admin` | `Admin@123456` |
| `staff01` | `Staff@123456` |
| `expert01` | `Expert@123456` |
| `mgr01` | `Manager@123456` |

Các tài khoản này **chỉ dành cho môi trường development**, không dùng với production.

</details>

<details>
<summary><strong>Lệnh DemoSeeder mở rộng</strong></summary>

~~~powershell
# Kiểm tra khả năng chạy, không ghi dữ liệu
dotnet run --project tools/demo/DemoSeeder/DemoSeeder.csproj -- --seed --dry-run

# Thẩm định lại database demo hiện hữu
dotnet run --project tools/demo/DemoSeeder/DemoSeeder.csproj -- --verify

# Neo dữ liệu theo ngày cố định
dotnet run --project tools/demo/DemoSeeder/DemoSeeder.csproj -- --seed --as-of-date 2026-10-26

# Kiểm tra bổ sung bằng SQL
sqlcmd -S . -E -C -d SupplierSelectionDSS_Demo -i tools/demo/verify_demo.sql
~~~

</details>

Xem [Demo guide](docs/DEMO_GUIDE.md) để theo dõi các tình huống và kịch bản trình diễn chi tiết.

<a id="methods"></a>
## Mô hình quyết định · AHP & TOPSIS

### AHP — Trọng số và tính nhất quán

Với ma trận so sánh cặp `A = [aᵢⱼ]` kích thước `n × n`, hệ thống sử dụng RGMM:

$$
g_i = \left(\prod_{j=1}^{n} a_{ij}\right)^{1/n},
\qquad
w_i = \frac{g_i}{\sum_{k=1}^{n}g_k}
$$

$$
CI = \frac{\lambda_{\max}-n}{n-1},
\qquad
CR = \frac{CI}{RI}
$$

Ma trận từ 3 tiêu chí trở lên cần thỏa **`CR ≤ 0.10`** để dùng trọng số trong bước TOPSIS. Với `n ≤ 2`, hệ thống xử lý riêng trường hợp nhất quán. `lambda_max` được ước lượng dựa trên vector trọng số RGMM trong hiện thực hiện tại.

> Giá trị nghịch đảo Saaty được biểu diễn bằng số thập phân ở tầng lưu trữ (ví dụ `1/3 ≈ 0.3333` với `DECIMAL(10,4)`); không nên xem đây là biểu diễn phân số chính xác tuyệt đối.

### TOPSIS — Xếp hạng nhà cung cấp

$$
r_{ij} = \frac{x_{ij}}{\sqrt{\sum_{k=1}^{m}x_{kj}^2}},
\qquad
v_{ij} = w_j r_{ij}
$$

$$
D_i^+ = \sqrt{\sum_j(v_{ij}-v_j^+)^2},
\qquad
D_i^- = \sqrt{\sum_j(v_{ij}-v_j^-)^2}
$$

$$
CC_i = \frac{D_i^-}{D_i^+ + D_i^-}
$$

`CC` lớn hơn thể hiện phương án gần nghiệm lý tưởng dương hơn trong tập phương án và tiêu chí được đánh giá. Hệ thống hỗ trợ xử lý tiêu chí `Benefit` / `Cost`, trường hợp dữ liệu biên và **Standard Competition Ranking** (ví dụ `1, 2, 2, 4`).

Phân tích độ nhạy thay đổi từng trọng số ở các mức **±10% và ±20%**, điều chỉnh các trọng số còn lại để tổng bằng 1 và chạy lại TOPSIS. Kết quả thể hiện mức độ ổn định **trong những kịch bản đã kiểm tra**, không phải chứng minh độ vững bền tuyệt đối.

Chi tiết công thức, điều kiện biên và benchmark: [docs/ALGORITHMS.md](docs/ALGORITHMS.md).

<a id="roles"></a>
## Vai trò & phân quyền · RBAC

| Vai trò | Phạm vi chính |
|---|---|
| **Administrator** | Tài khoản, audit log, danh mục, các nghiệp vụ quản trị được cấp quyền |
| **PurchasingStaff** | Nhóm hàng, nhà cung cấp, tiêu chí, tạo đợt đánh giá, nhập điểm |
| **Expert** | Nhập điểm, so sánh cặp AHP, tính TOPSIS |
| **Manager** | Theo dõi kết quả, phân tích độ nhạy, phê duyệt/từ chối/yêu cầu đánh giá lại, báo cáo |

Quyền thao tác được kiểm tra tại Controller và lớp nghiệp vụ, không chỉ ẩn/hiện nút ở giao diện. Xem [ma trận phân quyền và quy tắc chuyển trạng thái](docs/WORKFLOW-AND-PERMISSIONS.md).

<a id="testing"></a>
## Kiểm thử & chất lượng · Testing

Pipeline [Build and Test](.github/workflows/ci.yml) thực hiện kiểm tra schema, tài nguyên giao diện, biên dịch và chạy bộ kiểm thử .NET. Repo cũng có bài kiểm thử tích hợp chạy với SQL Server trong CI.

~~~powershell
# Build và chạy tests
dotnet build SupplierSelectionDSS.slnx --configuration Release
dotnet test SupplierSelectionDSS.slnx --configuration Release

# Đối chiếu công thức AHP/TOPSIS bằng Python
python tools/verify/verify_mcdm.py

# Xác minh đồng bộ schema và các script SQL
python tools/verify/verify_schema_sync.py

# Kiểm tra an toàn / đồng bộ tài nguyên giao diện
python tools/assets/bundle_css.py --check
python tools/assets/localize_fonts.py --check
~~~

**Các nhóm kiểm thử:** tính toán AHP/TOPSIS, phân tích độ nhạy, quy trình nghiệp vụ, phân quyền, chống cập nhật đồng thời, tích hợp SQL Server, dữ liệu demo và smoke test các màn hình.

> [!IMPORTANT]
> Kết quả `dotnet test` chạy trên máy không có SQL Server có thể **skip** những ca tích hợp phụ thuộc instance thực. Khi đánh giá nghiệm thu, phải đọc số lượng **passed / failed / skipped** và chạy bộ kiểm thử SQL Server trong môi trường có kết nối phù hợp. Không xem trạng thái build xanh là bằng chứng đã kiểm thử mọi điều kiện vận hành.

Xem [QA test matrix](docs/qa/TEST_MATRIX.md) và [Performance](docs/PERFORMANCE.md).

<a id="repository"></a>
## Cấu trúc repository

~~~text
.
├── Controllers/                 # HTTP endpoints, route & role checks
├── Views/                       # Razor UI
├── Models/                      # ViewModels, DTOs
├── Services/                    # Business logic + AHP/TOPSIS engines
├── Domain/                      # Batch state rules
├── Data/                        # EF Core entities & ApplicationDbContext
├── Infrastructure/Exports/      # PDF / Excel exporters
├── database/                    # Schema, seed, patches, production runner
├── tools/
│   ├── demo/                    # DemoSeeder & SQL demo verification
│   ├── verify/                  # Mathematical / SQL verification scripts
│   ├── perf/                    # Performance tools
│   └── assets/                  # Offline CSS / font checks
├── SupplierSelectionDSS.Tests/  # Automated tests
├── docs/                        # Architecture, algorithms, workflow, QA
└── wwwroot/                     # CSS, JS, images, fonts
~~~

<a id="documentation"></a>
## Tài liệu kỹ thuật · Documentation

| Tài liệu | Nội dung |
|---|---|
| [Documentation index](docs/README.md) | Danh mục tài liệu đang được duy trì |
| [Architecture](docs/ARCHITECTURE.md) | Kiến trúc và mô hình dữ liệu |
| [Algorithms](docs/ALGORITHMS.md) | Công thức AHP, TOPSIS, phân tích độ nhạy |
| [Workflow & Permissions](docs/WORKFLOW-AND-PERMISSIONS.md) | Vai trò, vòng đời đợt đánh giá, concurrency |
| [Design System](docs/DESIGN-SYSTEM.md) | Token giao diện, typography và thành phần UI |
| [Demo Guide](docs/DEMO_GUIDE.md) | Dữ liệu và kịch bản demo |
| [Performance](docs/PERFORMANCE.md) | Phương pháp đánh giá hiệu năng |
| [Database guide](database/README.md) | Khởi tạo, seed, migration patches |
| [Changelog](CHANGELOG.md) | Lịch sử thay đổi |

<details>
<summary><strong>Giới hạn hiện tại và định hướng mở rộng</strong></summary>

- Mỗi đợt đánh giá đang sử dụng **một ma trận AHP chung**. Hướng nghiên cứu Group AHP đa chuyên gia được ghi tại [GROUP-AHP-PROPOSAL](docs/proposals/GROUP-AHP-PROPOSAL.md).
- Cơ chế giới hạn đăng nhập dùng bộ nhớ ứng dụng; môi trường nhiều instance cần xem xét giải pháp lưu trạng thái phân tán.
- Việc đánh giá thứ hạng chịu ảnh hưởng của bộ tiêu chí, chất lượng dữ liệu đầu vào và các phán đoán chuyên gia.
- Cần thử nghiệm về hiệu năng, bảo mật và nghiệp vụ trong điều kiện triển khai thực tế trước khi sử dụng ở quy mô sản xuất.

</details>

---

<div align="center">

  **Supplier Selection DSS** · Built with ASP.NET Core · Powered by AHP + TOPSIS

  Licensed under the [MIT License](LICENSE).

</div>
