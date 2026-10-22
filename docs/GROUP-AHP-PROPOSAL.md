# ĐỀ XUẤT KIẾN TRÚC GROUP AHP (HỘI ĐỒNG ĐA CHUYÊN GIA)
## Hệ thống Hỗ trợ Ra Quyết định Lựa chọn Nhà Cung Cấp (SupplierSelectionDSS)

---

## 1. ĐẶT VẤN ĐỀ VÀ BỐI CẢNH

### 1.1. Hiện trạng hệ thống (Single Decision Maker)
Trong cấu trúc CSDL hiện tại (`database/01_schema.sql`), bảng so sánh cặp tiêu chí AHP được định nghĩa như sau:

```sql
CREATE TABLE AHPPairwiseComparisons (
    ComparisonID    INT IDENTITY(1,1) NOT NULL,
    BatchID         INT            NOT NULL,
    CriteriaRowID   INT            NOT NULL,
    CriteriaColID   INT            NOT NULL,
    ComparisonValue DECIMAL(10, 4) NOT NULL,
    ...
    CONSTRAINT UQ_AHPComparison UNIQUE (BatchID, CriteriaRowID, CriteriaColID)
);
```

- **Ràng buộc:** Ràng buộc `UQ_AHPComparison (BatchID, CriteriaRowID, CriteriaColID)` chỉ cho phép **duy nhất một giá trị so sánh** cho mỗi cặp tiêu chí trong một đợt đánh giá (`BatchID`).
- **Hạn chế học thuật & thực tiễn:** Trong các doanh nghiệp sản xuất và thương mại thực tế, việc xác định tầm quan trọng của các tiêu chí mua hàng (chất lượng, đơn giá, thời gian giao hàng, uy tín, tài chính, chứng chỉ ESG) thường do một **Hội đồng đánh giá (Decision Making Council / Purchasing Committee)** gồm đại diện từ nhiều phòng ban (Mua hàng, Kỹ thuật/Sản xuất, Tài chính, Đảm bảo chất lượng - QA) cùng thực hiện. Việc hệ thống chỉ lưu một ma trận duy nhất do một người nhập dễ dẫn đến ý kiến chủ quan và là điểm yếu lớn khi phản biện/bảo vệ đồ án trước Hội đồng chấm.

---

## 2. CƠ SỞ KHOA HỌC: PHƯƠNG PHÁP TỔNG HỢP GROUP AHP

Theo công trình kinh điển của GS. Thomas L. Saaty và Ernest H. Forman (1993, 2001), có hai trường phái chính để tổng hợp ý kiến nhóm trong AHP:

### 2.1. Phương pháp 1: Tổng hợp phán đoán cá nhân (AIJ - Aggregation of Individual Judgments)
- **Nguyên lý:** Nhóm chuyên gia được xem như một thực thể đồng thuận (synergistic group). Các phần tử của ma trận so sánh cặp $a_{ij}^{(k)}$ từ $m$ chuyên gia ($k = 1, \dots, m$) được tổng hợp thành một ma trận so sánh nhóm duy nhất $A^G = [a_{ij}^G]$ trước khi giải vector riêng tìm trọng số.
- **Công thức:** Saaty và Aczél (1983) đã chứng minh toán học rằng **Trung bình nhân (Geometric Mean)** là hàm toán học duy nhất bảo toàn tính chất đối xứng nghịch đảo của thang đo AHP ($a_{ji} = 1 / a_{ij}$):

$$a_{ij}^G = \prod_{k=1}^{m} \left(a_{ij}^{(k)}\right)^{w_k}$$

Trong đó $w_k$ là trọng số quyền biểu quyết của chuyên gia thứ $k$ ($\sum_{k=1}^m w_k = 1$). Nếu tất cả chuyên gia có quyền hạn ngang nhau:

$$a_{ij}^G = \left(\prod_{k=1}^{m} a_{ij}^{(k)}\right)^{\frac{1}{m}} = \sqrt[m]{a_{ij}^{(1)} \times a_{ij}^{(2)} \times \dots \times a_{ij}^{(m)}}$$

### 2.2. Phương pháp 2: Tổng hợp vector trọng số cá nhân (AIP - Aggregation of Individual Priorities)
- **Nguyên lý:** Mỗi chuyên gia $k$ hoàn thành ma trận độc lập, hệ thống tính toán vector trọng số riêng $W^{(k)} = [w_1^{(k)}, w_2^{(k)}, \dots, w_n^{(k)}]^T$ cho từng người. Sau đó, vector trọng số cuối cùng của nhóm $W^G$ là trung bình cộng hoặc trung bình nhân có trọng số của các vector $W^{(k)}$.

### 2.3. Lựa chọn khuyến nghị cho SupplierSelectionDSS: **AIJ (Geometric Mean)**
1. **Bảo toàn quy trình nghiệp vụ hiện tại:** Sau khi tổng hợp ra ma trận $A^G$, toàn bộ thuật toán tính vector trọng số $\lambda_{\max}$, tính $CI$, $CR$ và kiểm tra độ nhất quán hiện có trong `AHPService.cs` được **tái sử dụng nguyên vẹn 100%**, không cần sửa đổi bất kỳ công thức TOPSIS nào phía sau.
2. **Tính logic của tiêu chí mua hàng:** Một nhóm tiêu chí chỉ nên có một bảng trọng số đồng thuận duy nhất phản ánh chiến lược của đợt mua sắm.

---

## 3. THIẾT KẾ CƠ SỞ DỮ LIỆU ĐỀ XUẤT

Để nâng cấp lên Group AHP mà không phá vỡ dữ liệu hiện tại, thiết kế gồm 2 bảng mới và giữ nguyên bảng `AHPPairwiseComparisons` làm ma trận tổng hợp:

```
+-----------------------------------------------------------+
|                     EvaluationBatches                     |
+-----------------------------------------------------------+
       | 1                                          | 1
       |                                            |
       v N                                          v N
+----------------------------------+     +-----------------------------------------+
|      AHPEvaluatorAssignments     |     |       AHPEvaluatorComparisons           |
+----------------------------------+     +-----------------------------------------+
| AssignmentID (PK)                |     | EvaluatorComparisonID (PK)              |
| BatchID (FK)                     |     | BatchID (FK)                            |
| UserID (FK - Expert)             |     | UserID (FK)                             |
| Weight (DECIMAL, default 1.0)    |     | CriteriaRowID (FK)                      |
| Status (Assigned/Submitted)      |     | CriteriaColID (FK)                      |
| SubmittedAt (DATETIME2)          |     | ComparisonValue (DECIMAL(10,4))         |
| IndividualCR (DECIMAL(10,4))     |     +-----------------------------------------+
+----------------------------------+                        |
                                                            | Geometric Mean (Tổng hợp)
                                                            v
                                         +-----------------------------------------+
                                         |         AHPPairwiseComparisons          |
                                         |         (Consensus Matrix - Hiện có)    |
                                         +-----------------------------------------+
```

### 3.1. Kịch bản DDL SQL:

```sql
-- 1. Phân công hội đồng chuyên gia tham gia đợt đánh giá
CREATE TABLE dbo.AHPEvaluatorAssignments (
    AssignmentID    INT IDENTITY(1,1) NOT NULL,
    BatchID         INT            NOT NULL,
    UserID          INT            NOT NULL,
    ExpertWeight    DECIMAL(5, 4)  NOT NULL CONSTRAINT DF_AHP_ExpertWeight DEFAULT (1.0000),
    Status          NVARCHAR(20)   NOT NULL CONSTRAINT DF_AHP_AssignStatus DEFAULT (N'Assigned'), -- Assigned, Draft, Submitted
    SubmittedAt     DATETIME2      NULL,
    IndividualCR    DECIMAL(10, 4) NULL,
    IsConsistent    BIT            NULL,
    CONSTRAINT PK_AHPEvaluatorAssignments PRIMARY KEY (AssignmentID),
    CONSTRAINT FK_AHPEvaluator_Batch FOREIGN KEY (BatchID) REFERENCES dbo.EvaluationBatches(BatchID),
    CONSTRAINT FK_AHPEvaluator_User  FOREIGN KEY (UserID)  REFERENCES dbo.Users(UserID),
    CONSTRAINT UQ_AHPEvaluator UNIQUE (BatchID, UserID),
    CONSTRAINT CK_AHPEvaluator_Status CHECK (Status IN (N'Assigned', N'Draft', N'Submitted'))
);
GO

-- 2. Ma trận so sánh cặp riêng rẽ của từng chuyên gia
CREATE TABLE dbo.AHPEvaluatorComparisons (
    EvaluatorComparisonID INT IDENTITY(1,1) NOT NULL,
    BatchID               INT            NOT NULL,
    UserID                INT            NOT NULL,
    CriteriaRowID         INT            NOT NULL,
    CriteriaColID         INT            NOT NULL,
    ComparisonValue       DECIMAL(10, 4) NOT NULL,
    CreatedAt             DATETIME2      NOT NULL CONSTRAINT DF_AHPEvalComp_CreatedAt DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_AHPEvaluatorComparisons PRIMARY KEY (EvaluatorComparisonID),
    CONSTRAINT FK_AHPEvalComp_Batch FOREIGN KEY (BatchID) REFERENCES dbo.EvaluationBatches(BatchID),
    CONSTRAINT FK_AHPEvalComp_User  FOREIGN KEY (UserID)  REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_AHPEvalComp_Row   FOREIGN KEY (CriteriaRowID) REFERENCES dbo.Criteria(CriteriaID),
    CONSTRAINT FK_AHPEvalComp_Col   FOREIGN KEY (CriteriaColID) REFERENCES dbo.Criteria(CriteriaID),
    CONSTRAINT UQ_AHPEvalComp UNIQUE (BatchID, UserID, CriteriaRowID, CriteriaColID)
);
GO

-- 3. Chỉ mục tối ưu truy vấn hội đồng
CREATE INDEX IX_AHPEvalComp_Batch_User ON dbo.AHPEvaluatorComparisons(BatchID, UserID);
CREATE INDEX IX_AHPEvaluator_Batch     ON dbo.AHPEvaluatorAssignments(BatchID, Status);
GO
```

---

## 4. THUẬT TOÁN TỔNG HỢP VÀ ĐIỀU HÀNH ĐỒNG THUẬN

### 4.1. Quy trình xử lý tại Service (`GroupAHPService.cs`):

1. **Bước 1: Chuyên gia nhập liệu độc lập**
   - Mỗi chuyên gia chỉ truy cập và chỉnh sửa ma trận của riêng mình (`UserID` lấy từ Claims Authentication).
   - Kiểm tra tính nhất quán riêng: Tính $CR_k$. Nếu $CR_k > 0.1$, giao diện cảnh báo để chuyên gia hiệu chỉnh lại nhận định của mình trước khi nhấn "Gửi kết quả" (Submit).

2. **Bước 2: Quản trị viên/Chủ tịch hội đồng kích hoạt "Tổng hợp ý kiến nhóm" (Aggregate)**
   - Hệ thống kiểm tra điều kiện: Ít nhất $M \ge 2$ chuyên gia đã ở trạng thái `Submitted`.
   - Chuẩn hóa trọng số chuyên gia:
     $$\bar{w}_k = \frac{\text{ExpertWeight}_k}{\sum_{j \in \text{Submitted}} \text{ExpertWeight}_j}$$
   - Tính phần tử ma trận nhóm cho từng cặp $(i, j)$ với $i < j$:
     $$a_{ij}^G = \prod_{k \in \text{Submitted}} \left(a_{ij}^{(k)}\right)^{\bar{w}_k} = \exp\left( \sum_{k \in \text{Submitted}} \bar{w}_k \ln a_{ij}^{(k)} \right)$$
   - Ghi đè ma trận nhóm $a_{ij}^G$ vào bảng `AHPPairwiseComparisons` hiện tại.

3. **Bước 3: Tự động chạy thuật toán AHP hiện tại**
   - Gọi lại `AHPService.CalculateWeightsAsync(batchId)` để tính $\lambda_{\max}$, $CI$, $CR$ của nhóm và lưu vào `AHPCriteriaWeights`.

4. **Bước 4: Đo lường độ đồng thuận (Group Consensus Index)**
   - Sử dụng chỉ số tương đồng nhận định (Compatibility Index / Geometric Standard Deviation):
     $$\sigma_{ij} = \exp\left( \sqrt{\sum_{k=1}^m \bar{w}_k \left(\ln a_{ij}^{(k)} - \ln a_{ij}^G\right)^2} \right)$$
   - Nếu $\sigma_{ij} > 1.5$, cặp tiêu chí $(i, j)$ có độ bất đồng cao giữa các phòng ban -> Giao diện highlight màu cam để Hội đồng tổ chức họp giải trình.

---

## 5. THIẾT KẾ GIAO DIỆN (UI/UX)

1. **Màn hình phân công hội đồng (Dành cho Quản trị viên / Trưởng ban mua hàng):**
   - Chọn các tài khoản có vai trò `Expert`.
   - Thiết lập trọng số quyền hạn (ví dụ: Trưởng phòng Kỹ thuật 1.5, Chuyên viên 1.0).
   - Theo dõi tiến độ: Badge trạng thái (Đã gửi 3/4 chuyên gia).
2. **Màn hình đánh giá của Chuyên gia:**
   - Chỉ thấy ma trận của chính mình; có hướng dẫn chi tiết thang đo Saaty.
   - Thước đo $CR$ cá nhân tự động tính toán tức thì bằng JavaScript.
3. **Màn hình đối chiếu và đồng thuận của Hội đồng:**
   - Bảng so sánh trực quan đa cột: Hiển thị giá trị nhận định của từng chuyên gia bên cạnh giá trị trung bình nhân nhóm.
   - Nút hành động "Áp dụng kết quả đồng thuận vào đợt đánh giá".

---

## 6. LỘ TRÌNH TRIỂN KHAI KHUYẾN NGHỊ

- **Giai đoạn 1 (Đã hoàn thành ở Vòng 3):** Hoàn thiện và tối ưu triệt để Single AHP, đảm bảo hiệu năng < 25 ms, split query, A11y, sticky matrix, và lưu vết `BatchRoundSnapshots`.
- **Giai đoạn 2 (Dự kiến Vòng tiếp theo):** Tạo 2 bảng CSDL theo script mục 3, cài đặt `GroupAHPService.cs` với thuật toán Geometric Mean, giữ nguyên toàn bộ tầng TOPSIS và Report.
