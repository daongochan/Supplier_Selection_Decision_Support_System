# AGENTS.md — Quy Chuẩn Dành Cho AI Agent & Nhà Phát Triển

Tài liệu này quy định các nguyên tắc, quy ước phát triển và phạm vi kỹ thuật bắt buộc khi làm việc trên dự án **Supplier Selection Decision Support System (DSS)**.

---

## 1. Lệnh Thực Thi Cơ Bản

- **Build ứng dụng:**
  ```bash
  dotnet build SupplierSelectionDSS.slnx
  ```
- **Chạy kiểm thử tự động:**
  ```bash
  dotnet test SupplierSelectionDSS.slnx
  ```
- **Chạy kiểm thử với báo cáo chi tiết:**
  ```bash
  dotnet test --logger "console;verbosity=normal"
  ```
- **Chạy ứng dụng Web:**
  ```bash
  dotnet run --project SupplierSelectionDSS.csproj
  ```

---

## 2. Cấu Trúc Dự Án & Tham Chiếu

- `SupplierSelectionDSS.csproj`: Dự án ASP.NET Core MVC chính (`net10.0`).
- `SupplierSelectionDSS.Tests/`: Dự án kiểm thử xUnit (`net10.0`). Đặt nội bộ trong thư mục repo.
- `database/`: Chứa các script SQL (`01_schema.sql`, `02_seed.sql`) là nguồn sự thật (Source of Truth) cho cấu trúc CSDL.
- `docs/`: Chứa tài liệu thiết kế, audit, giải thuật và kiến trúc.

---

## 3. Quy Ước Bắt Buộc

1. **Language & Terminology:**
   - Primary UI language (`<html lang="en">`, navigation, workflow steppers, page headers, table headers, confirmation dialogs): **English**.
   - Code identifiers (variables, methods, classes, DB schema, commit messages): **English**.
   - Database enum/status values (`Active`, `Inactive`, `Draft`, `Evaluating`, `Completed`, `Cancelled`, `Benefit`, `Cost`, `Approved`, `Rejected`, `PendingReevaluation`, `FinalApproval`, `Reevaluation`) must never be translated in persisted storage.
2. **Quy tắc Commit:**
   - Tuân thủ [Conventional Commits](https://www.conventionalcommits.org/): `feat:`, `fix:`, `chore:`, `test:`, `docs:`, `refactor:`.
   - Làm việc trên nhánh tính năng/bảo trì, không commit trực tiếp trên `main`, tuyệt đối không `git push --force`.
3. **Tính toán Thuật toán (AHP & TOPSIS):**
   - Không tự ý thay đổi công thức toán trong `AHPCalculationService` và `TOPSISCalculationService` khi chưa có kiểm thử đối chiếu độc lập trong `tools/verify/` hoặc bộ test xUnit tương ứng.
   - Mọi thay đổi hành vi thuật toán phải được ghi nhận trong `CHANGELOG.md`.

---

## 4. Những Thứ Tuyệt Đối Không Được Phép Đụng Vào

- **KHÔNG** chỉnh sửa hoặc tự ý cập nhật các thư viện bên thứ ba trong `wwwroot/lib/` (Bootstrap, FontAwesome, Chart.js...).
- **KHÔNG** chạy các câu lệnh SQL phá hủy dữ liệu (`DROP TABLE`, `TRUNCATE`, `DELETE`) trên môi trường thực của người dùng khi chưa có chỉ định rõ ràng.
- **KHÔNG** commit các file tạm, file cấu hình IDE (`.vs/`, `bin/`, `obj/`, `*.user`, `*.suo`).
- **KHÔNG** đưa mật khẩu, khóa bí mật hoặc connection string production vào mã nguồn; sử dụng User Secrets hoặc Biến Môi Trường.
