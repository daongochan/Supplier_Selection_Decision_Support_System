# Tài liệu Thuật toán MCDM: AHP, TOPSIS & Phân tích Độ nhạy

Tài liệu này trình bày chi tiết cơ sở toán học, quy trình tính toán, các giả định thiết kế và ví dụ số đối chiếu độc lập cho các thuật toán Hỗ trợ ra Quyết định Đa tiêu chí (MCDM - Multi-Criteria Decision Making) được triển khai trong hệ thống **Supplier Selection DSS**.

---

## 1. Phương pháp Phân tích Thứ bậc (AHP - Analytic Hierarchy Process)

Phương pháp AHP do Thomas L. Saaty đề xuất được sử dụng để xác định trọng số ưu tiên của các tiêu chí đánh giá nhà cung cấp thông qua ma trận so sánh cặp.

### 1.1. Thang đo Saaty 1–9

| Giá trị $a_{ij}$ | Ý nghĩa định tính |
| :---: | :--- |
| **1** | Hai tiêu chí có tầm quan trọng ngang nhau (Equal importance) |
| **3** | Tiêu chí $i$ quan trọng hơn nhẹ so với tiêu chí $j$ (Moderate importance) |
| **5** | Tiêu chí $i$ quan trọng hơn mạnh so với tiêu chí $j$ (Strong importance) |
| **7** | Tiêu chí $i$ quan trọng hơn rất mạnh so với tiêu chí $j$ (Very strong) |
| **9** | Tiêu chí $i$ quan trọng hơn tuyệt đối so với tiêu chí $j$ (Extreme importance) |
| **2, 4, 6, 8** | Các mức độ thỏa hiệp trung gian |
| **$1/a_{ji}$** | Giá trị nghịch đảo khi tiêu chí $j$ so sánh ngược lại với $i$ ($a_{ji} = 1 / a_{ij}$) |

### 1.2. Tính toán Trọng số: Phương pháp Trung bình Nhân theo hàng (RGMM)

Hệ thống triển khai phương pháp **Row Geometric Mean Method (RGMM)** (còn gọi là Logarithmic Least Squares Method). Đây là phương pháp chuẩn học thuật, bảo toàn tính nghịch đảo và xấp xỉ chính xác eigenvector chính của ma trận:

1. **Tính trung bình nhân theo từng hàng:**
   $$r_i = \left( \prod_{j=1}^n a_{ij} \right)^{1/n} \quad \forall i = 1, \dots, n$$

2. **Chuẩn hóa vector trọng số (Tổng bằng 1.0):**
   $$w_i = \frac{r_i}{\sum_{k=1}^n r_k} \quad \forall i = 1, \dots, n$$

### 1.3. Kiểm tra Tính nhất quán (Consistency Check)

Để đảm bảo các phán đoán so sánh cặp của chuyên gia không bị mâu thuẫn logic:

1. **Tính tích $A \times w$:**
   $$(Aw)_i = \sum_{j=1}^n a_{ij} w_j$$

2. **Tính giá trị riêng cực đại $\lambda_{\max}$:**
   $$\lambda_{\max} = \frac{1}{n} \sum_{i=1}^n \frac{(Aw)_i}{w_i}$$

3. **Chỉ số nhất quán (Consistency Index - CI):**
   $$CI = \frac{\lambda_{\max} - n}{n - 1}$$
   *(Khi $n \le 2$, $CI = 0$ do luôn luôn nhất quán theo định nghĩa toán học).*

4. **Tỷ số nhất quán (Consistency Ratio - CR):**
   $$CR = \frac{CI}{RI}$$
   Trong đó, $RI$ (Random Index) là chỉ số ngẫu nhiên tra từ bảng Saaty:

| $n$ | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **RI** | 0.00 | 0.00 | 0.58 | 0.90 | 1.12 | 1.24 | 1.32 | 1.41 | 1.45 | 1.49 |

5. **Quy tắc chấp nhận:**
   - Nếu $CR \le 0.10$ ($10\%$): Ma trận đạt tính nhất quán. Trọng số $w$ đủ độ tin cậy để đưa vào TOPSIS.
   - Nếu $CR > 0.10$: Ma trận không nhất quán, hệ thống khóa bước tính TOPSIS và yêu cầu chuyên gia điều chỉnh lại phán đoán.

---

## 2. Phương pháp TOPSIS (Technique for Order Preference by Similarity to Ideal Solution)

Phương pháp TOPSIS của Hwang & Yoon xếp hạng các nhà cung cấp dựa trên nguyên lý: phương án tối ưu nhất phải có khoảng cách hình học ngắn nhất tới Giải pháp lý tưởng dương ($PIS$) và khoảng cách xa nhất tới Giải pháp lý tưởng âm ($NIS$).

### 2.1. Quy trình 6 bước chuẩn hóa

Gọi ma trận đánh giá ban đầu là $X = [x_{ij}]_{m \times n}$ với $m$ nhà cung cấp ($i = 1, \dots, m$) và $n$ tiêu chí ($j = 1, \dots, n$).

#### Bước 1: Chuẩn hóa vector (Vector Normalization)
Để đưa các tiêu chí có đơn vị đo khác nhau về cùng thang đo không thứ nguyên $[0, 1]$:
$$r_{ij} = \frac{x_{ij}}{\sqrt{\sum_{k=1}^m x_{kj}^2}}$$

> **Quy tắc xử lý biên:** Nếu một cột tiêu chí có toàn bộ giá trị $x_{kj} = 0$ hoặc bằng nhau (phương sai bằng 0), mẫu số bằng 0. Hệ thống gán $r_{ij} = 0$ cho toàn bộ cột và ghi cảnh báo rằng tiêu chí này không có khả năng phân biệt giữa các nhà cung cấp, thay vì làm sập ứng dụng.

#### Bước 2: Tạo ma trận chuẩn hóa có trọng số (Weighted Normalized Matrix)
Nhân giá trị chuẩn hóa với trọng số $w_j$ nhận được từ bước AHP:
$$v_{ij} = w_j \cdot r_{ij} \quad \text{với } \sum_{j=1}^n w_j = 1$$

#### Bước 3: Xác định Giải pháp lý tưởng dương ($PIS - A^+$) và âm ($NIS - A^-$)
Tùy thuộc vào bản chất của từng tiêu chí là **Hưởng lợi (Benefit - càng cao càng tốt)** hay **Chi phí (Cost - càng thấp càng tốt)**:

$$v_j^+ = \begin{cases} 
\max_{i} v_{ij} & \text{nếu } j \in \text{Benefit} \\
\min_{i} v_{ij} & \text{nếu } j \in \text{Cost}
\end{cases}$$

$$v_j^- = \begin{cases} 
\min_{i} v_{ij} & \text{nếu } j \in \text{Benefit} \\
\max_{i} v_{ij} & \text{nếu } j \in \text{Cost}
\end{cases}$$

#### Bước 4: Tính khoảng cách hình học Euclid
Khoảng cách từ nhà cung cấp $i$ tới giải pháp lý tưởng dương và âm:
$$D_i^+ = \sqrt{\sum_{j=1}^n (v_{ij} - v_j^+)^2}$$
$$D_i^- = \sqrt{\sum_{j=1}^n (v_{ij} - v_j^-)^2}$$

#### Bước 5: Tính hệ số tương đồng (Relative Closeness Coefficient - $CC_i$)
$$CC_i = \frac{D_i^-}{D_i^+ + D_i^-} \quad (0 \le CC_i \le 1)$$
- $CC_i \to 1$: Nhà cung cấp càng tiệm cận giải pháp hoàn hảo.
- $CC_i \to 0$: Nhà cung cấp càng gần mức tồi tệ nhất.

> **Quy tắc xử lý biên ($D_i^+ + D_i^- = 0$):** Xảy ra khi tất cả các nhà cung cấp có cùng số điểm trên mọi tiêu chí. Hệ thống kiểm tra tối thiểu 2 nhà cung cấp và đánh dấu trạng thái `IsIndistinguishable = true` kèm thông báo phân tích rõ ràng.

#### Bước 6: Xếp hạng Nhà cung cấp (Standard Competition Ranking)
Nhà cung cấp có $CC_i$ cao nhất được xếp hạng 1.
Khi có hai hay nhiều nhà cung cấp có cùng $CC_i$ (hòa điểm), hệ thống áp dụng quy chuẩn **Standard Competition Ranking ("1224" ranking)**:
- Ví dụ: Điểm số của 4 nhà cung cấp là 0.85, 0.72, 0.72, 0.60 thì thứ hạng tương ứng là **1, 2, 2, 4** (thay vì 1, 2, 3, 4 làm sai lệch bản chất bình đẳng).

---

## 3. Phân tích Độ nhạy (Sensitivity Analysis)

Để tăng cường độ tin cậy khi bảo vệ đồ án và hỗ trợ hội đồng đánh giá rủi ro ra quyết định, hệ thống triển khai thuật toán phân tích độ nhạy dựa trên biến thiên trọng số:

1. **Kịch bản biến thiên:** Với mỗi tiêu chí $k$, trọng số $w_k$ lần lượt thay đổi theo 4 mức độ:
   $$\delta \in \{-20\%, -10\%, +10\%, +20\%\}$$
   $$w_k' = w_k \times (1 + \delta)$$
   *(Được giới hạn trong khoảng $[0.001, 0.999]$ để tránh triệt tiêu).*

2. **Tái cân bằng trọng số:** Để tổng trọng số luôn bảo toàn đúng bằng 1.0, phần trọng số còn lại $(1 - w_k')$ được phân bổ tỷ lệ thuận cho các tiêu chí còn lại ($j \neq k$):
   $$w_j' = w_j \times \frac{1 - w_k'}{1 - w_k} \quad (\forall j \neq k)$$

3. **Đánh giá tính ổn định (Robustness):**
   - Với mỗi kịch bản, chạy lại quy trình TOPSIS để xác định nhà cung cấp dẫn đầu (#1 mới).
   - Nếu qua toàn bộ các kịch bản, nhà cung cấp #1 không đổi $\rightarrow$ Quyết định đạt độ vững bền $100\%$ (`IsOverallRobust = true`).
   - Nếu có kịch bản làm đổi ngôi vị trí #1 $\rightarrow$ Hệ thống cảnh báo các tiêu chí nhạy cảm và tính toán tỷ lệ vững bền `%`.

---

## 4. Ví dụ Số Thực tế & Đối chiếu Độc lập (Python vs C#)

Tập dữ liệu kiểm thử độc lập được tính bằng thư viện NumPy (`tools/verify/verify_mcdm.py`), khớp tuyệt đối với test tự động trong `SupplierSelectionDSS.Tests`.

### 4.1. Dữ liệu đầu vào

- **3 Nhà cung cấp:** S1, S2, S3
- **4 Tiêu chí:**
  - $C_1$: Chất lượng kỹ thuật (Benefit, $w_1 = 0.40$)
  - $C_2$: Giá cả (Cost, $w_2 = 0.30$)
  - $C_3$: Thời gian giao hàng (Cost, $w_3 = 0.20$)
  - $C_4$: Dịch vụ hỗ trợ (Benefit, $w_4 = 0.10$)

Ma trận quyết định $X$:
$$X = \begin{bmatrix}
80.0 & 50.0 & 10.0 & 90.0 \\
70.0 & 40.0 & 15.0 & 80.0 \\
90.0 & 60.0 & 20.0 & 70.0
\end{bmatrix}$$

### 4.2. Kết quả tính toán chi tiết

1. **Độ dài vector chuẩn hóa từng cột:**
   - Cột 1: $\sqrt{80^2 + 70^2 + 90^2} = \sqrt{19400} \approx 139.283883$
   - Cột 2: $\sqrt{50^2 + 40^2 + 60^2} = \sqrt{7700} \approx 87.749644$
   - Cột 3: $\sqrt{10^2 + 15^2 + 20^2} = \sqrt{725} \approx 26.925824$
   - Cột 4: $\sqrt{90^2 + 80^2 + 70^2} = \sqrt{19400} \approx 139.283883$

2. **Ma trận chuẩn hóa vector $R$:**
   $$R \approx \begin{bmatrix}
   0.574367 & 0.569803 & 0.371391 & 0.646162 \\
   0.502571 & 0.455842 & 0.557086 & 0.574367 \\
   0.646162 & 0.683763 & 0.742781 & 0.502571
   \end{bmatrix}$$

3. **Ma trận có trọng số $V$ ($w = [0.4, 0.3, 0.2, 0.1]$):**
   $$V \approx \begin{bmatrix}
   0.229747 & 0.170941 & 0.074278 & 0.064616 \\
   0.201028 & 0.136753 & 0.111417 & 0.057437 \\
   0.258465 & 0.205129 & 0.148556 & 0.050257
   \end{bmatrix}$$

4. **Giải pháp lý tưởng $PIS$ ($A^+$) và $NIS$ ($A^-$):**
   - $PIS = [\max(C_1), \min(C_2), \min(C_3), \max(C_4)] = [0.258465, 0.136753, 0.074278, 0.064616]$
   - $NIS = [\min(C_1), \max(C_2), \max(C_3), \min(C_4)] = [0.201028, 0.205129, 0.148556, 0.050257]$

5. **Khoảng cách và hệ số tương đồng ($CC_i$):**
   - **S1:** $D^+ \approx 0.044634$, $D^- \approx 0.089454$, $CC_1 \approx 0.667132 \rightarrow$ **Hạng 1**
   - **S2:** $D^+ \approx 0.068809$, $D^- \approx 0.078278$, $CC_2 \approx 0.532191 \rightarrow$ **Hạng 2**
   - **S3:** $D^+ \approx 0.101740$, $D^- \approx 0.057437$, $CC_3 \approx 0.360838 \rightarrow$ **Hạng 3**

*Sai số giữa C# và NumPy: $< 10^{-6}$ (đạt độ chính xác tuyệt đối).*
