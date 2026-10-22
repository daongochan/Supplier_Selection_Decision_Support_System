# Hướng dẫn Dọn dẹp Lịch sử Git (Git History Cleanup Guide)

Tài liệu này cung cấp quy trình chuẩn kỹ thuật để dọn dẹp các tệp tin rác, lịch sử nhạy cảm hoặc chuỗi dữ liệu cũ trong lịch sử Git nếu nhóm phát triển có nhu cầu thực hiện trong tương lai.

> [!CAUTION]
> **CẢNH BÁO NGUY HIỂM — LÀM THAY ĐỔI TOÀN BỘ COMMIT HASH (REWRITE HISTORY)**
> - Viết lại lịch sử Git sẽ tạo ra chuỗi commit hash hoàn toàn mới cho toàn bộ các commit bị ảnh hưởng kể từ thời điểm thay đổi.
> - Bắt buộc phải thực hiện lệnh `git push --force` lên remote repository, điều này sẽ làm gãy các nhánh đang làm việc, các Pull Request đang mở, và gây xung đột nghiêm trọng cho tất cả các thành viên đang clone repo.
> - **Nguyên tắc an toàn:** Hệ thống CI và công cụ tự động hóa **TUYỆT ĐỐI KHÔNG** tự ý chạy các lệnh rewrite history trên repository dùng chung. Mọi thao tác dưới đây chỉ được thực hiện có chủ đích trên một bản sao độc lập (mirror clone) sau khi đã sao lưu và có sự đồng thuận của toàn đội ngũ.

---

## 1. Khi nào cần dọn dẹp lịch sử Git?

1. **Khắc phục lộ thông tin nhạy cảm (Credentials / Secrets):** Nếu trong quá khứ có commit chứa secret key, token, private key hoặc mật khẩu thật (Lưu ý: các tài khoản demo nội bộ hiện tại đã được đổi sang hash BCrypt chuẩn và dữ liệu giả lập).
2. **Loại bỏ các tệp tin binary dung lượng lớn:** Các tệp `.zip`, `.dll`, file database `.mdf`, thư mục `bin/`, `obj/` vô tình bị commit vào git tree trước khi có `.gitignore`.
3. **Chuẩn bị bàn giao hoặc công khai mã nguồn (Open Source):** Khi muốn repo sạch gọn, nhẹ nhàng và chỉ giữ lại cấu trúc chuẩn.

---

## 2. Công cụ Khuyến nghị: `git-filter-repo`

Theo khuyến cáo chính thức từ Git SCM, **`git-filter-repo`** (viết bằng Python) là công cụ hiện đại, an toàn và nhanh hơn rất nhiều so với `git filter-branch` (vốn đã bị Git đánh dấu là deprecated do dễ gây lỗi dữ liệu).

### Bước 1: Chuẩn bị môi trường & Tạo bản sao lưu (Mirror Clone)

Tuyệt đối không chạy trực tiếp trên thư mục làm việc hiện tại (`working copy`). Hãy tạo một bản clone dạng bare mirror riêng biệt:

```bash
# 1. Tạo thư mục làm việc tạm thời
mkdir C:\git-cleanup-workspace
cd C:\git-cleanup-workspace

# 2. Clone repo dạng mirror (chứa toàn bộ branches, tags, refs)
git clone --mirror https://github.com/daongochan/Supplier_Selection_Decision_Support_System.git repo-mirror.git

# 3. Tạo bản sao lưu dự phòng (Back-up)
cp -r repo-mirror.git repo-mirror-backup.git

cd repo-mirror.git
```

### Bước 2: Cài đặt công cụ `git-filter-repo`

Yêu cầu máy đã cài Python 3:

```bash
pip install git-filter-repo
```

Kiểm tra cài đặt:
```bash
git filter-repo --version
```

### Bước 3: Thực hiện dọn dẹp theo nhu cầu

#### Trường hợp A: Xóa bỏ hoàn toàn một thư mục hoặc tệp tin khỏi mọi commit
Ví dụ: Xóa thư mục rác `build/`, `packages/`, hoặc file nhạy cảm `.env`:

```bash
# Xóa thư mục/tệp và giữ lại tất cả các phần còn lại (--invert-paths)
git filter-repo --path path/to/unwanted_folder/ --invert-paths
```

#### Trường hợp B: Thay thế một chuỗi nhạy cảm (text replacement)
Tạo file `replace.txt` chứa danh sách chuỗi cần thay thế:
```
old_secret_value==>REDACTED_VALUE
dummy_hash_sample==>REDACTED_HASH
```

Chạy lệnh thay thế:
```bash
git filter-repo --replace-text replace.txt
```

### Bước 4: Kiểm tra và nghiệm thu trên local

Sau khi `git-filter-repo` xử lý xong, remote origin mặc định sẽ bị xóa tự động để bảo vệ bạn khỏi việc vô tình push nhầm:

```bash
# Xem lại nhật ký commit
git log --oneline -n 20

# Kiểm tra dung lượng repo sau khi dọn rác
git count-objects -vH
```

Clone thử ra một thư mục bình thường để build và chạy test kiểm tra xem mã nguồn có còn nguyên vẹn không:

```bash
cd ..
git clone repo-mirror.git repo-test-build
cd repo-test-build
dotnet build SupplierSelectionDSS.slnx
dotnet test SupplierSelectionDSS.slnx
```

### Bước 5: Đẩy lên Remote (Yêu cầu phối hợp nhóm)

Chỉ thực hiện bước này khi bản kiểm tra ở Bước 4 đã vượt qua 100% test và tất cả thành viên đã sẵn sàng:

1. Thêm lại remote origin:
   ```bash
   cd C:\git-cleanup-workspace\repo-mirror.git
   git remote add origin https://github.com/daongochan/Supplier_Selection_Decision_Support_System.git
   ```

2. Force push toàn bộ nhánh và tags:
   ```bash
   git push origin --force --all
   git push origin --force --tags
   ```

3. **Hướng dẫn bắt buộc cho các cộng tác viên / thành viên:**
   - Tất cả thành viên trong nhóm **KHÔNG ĐƯỢC** dùng `git pull` trên bản clone cũ (sẽ gây merge xung đột lịch sử).
   - Thay vào đó, mỗi người phải stash hoặc backup code chưa commit của mình, xóa thư mục local cũ và thực hiện `git clone` mới lại từ remote.

---

## 3. Lý do Antigravity không tự động rewrite history trong PR này

Trong quá trình thực hiện đợt rà soát và củng cố repo:
1. **Bảo tồn tính toàn vẹn của lịch sử Git:** Giữ nguyên commit hash để phục vụ đánh giá, audit trail và không làm đứt gãy Pull Request hiện hành (#2).
2. **Khắc phục an toàn ở cấp độ ứng dụng:**
   - Các file rác đã được đưa vào `.gitignore` và xóa khỏi git tree hiện tại ở commit thông thường.
   - Chuỗi hash demo trong seed SQL đã được thay thế bằng hash BCrypt hợp lệ ở code mới nhất.
   - Thư mục báo cáo đã được chuyển ra ngoài webroot và được chặn truy cập tĩnh trực tiếp bằng Middleware.
