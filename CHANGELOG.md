# CHANGELOG — Nhật Ký Thay Đổi Dự Án

Toàn bộ các thay đổi quan trọng về kiến trúc, thuật toán, bảo mật và tính năng của hệ thống **Supplier Selection DSS** được ghi nhận tại đây theo chuẩn [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased] - 2026-10-22

### Thay Đổi Thuật Toán & Kiểm Thử (Phase 3)
- **TOPSIS - Xử lý cột toàn 0 (Zero-variance criteria):**
  - *Hành vi trước:* Ném `InvalidOperationException` làm dừng toàn bộ tiến trình đánh giá nếu có một tiêu chí mà tất cả NCC đều có giá trị bằng 0 (ví dụ: tỷ lệ lỗi = 0% ở mọi NCC).
  - *Hành vi sau:* Không ném lỗi. Chuẩn hóa cột này về 0, ghi nhận chỉ số tiêu chí vào `ZeroVarianceCriteriaIndices` trong `TOPSISCalculationResult` để cảnh báo rằng tiêu chí này không có khả năng phân biệt giữa các nhà cung cấp.
- **TOPSIS - Hạng khi Closeness Coefficient bằng nhau:**
  - *Hành vi trước:* Gán thứ hạng tuần tự (1, 2, 3, 4) ngay cả khi hai nhà cung cấp có cùng CC và khoảng cách $D^+$.
  - *Hành vi sau:* Áp dụng xếp hạng chuẩn thi đấu (Standard Competition Ranking - 1, 2, 2, 4). Cung cấp cờ `HasTies = true` khi có đồng hạng.
- **TOPSIS - Trường hợp $D^+ + D^- = 0$ (Các nhà cung cấp giống hệt nhau):**
  - *Hành vi trước:* Gán $CC = 0.0$ nhưng vẫn gán hạng tuần tự 1, 2, 3.
  - *Hành vi sau:* Bắt buộc tối thiểu 2 nhà cung cấp. Khi tất cả NCC có điểm giống hệt nhau trên mọi tiêu chí, toàn bộ nhận $CC = 0.0$ và đồng hạng 1 (`IsIndistinguishable = true`).
- **TOPSIS - Ràng buộc số lượng nhà cung cấp:**
  - Yêu cầu tối thiểu 2 nhà cung cấp để thực hiện so sánh đa tiêu chí TOPSIS.
- **Bộ Kiểm Thử Độc Lập:**
  - Bổ sung công cụ kiểm chứng độc lập bằng Python & NumPy (`tools/verify/verify_mcdm.py`).
  - Đối chiếu trọng số AHP giữa Geometric Mean và Principal Eigenvector (sai số $< 0.0015$).
  - Đối chiếu 100% số liệu trung gian của TOPSIS (PIS, NIS, $D^+$, $D^-$, CC, Rank) với bộ benchmark độc lập.
  - Kiểm thử các tính chất bất biến toán học: tính bất biến theo tỷ lệ nhân dương (Scale Invariance), tính bất biến theo hoán vị hàng (Permutation Invariance).
