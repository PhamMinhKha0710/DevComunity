# Checklist trạng thái cần kiểm tra khi Review Pull Request

Dùng checklist này khi tạo hoặc review PR từ nhánh feature/fix vào `dev` (hoặc `main`).

---

## 1. Trạng thái CI / Checks (tự động)

- [ ] **Backend Build** — workflow `Backend Build` chạy thành công (dotnet build)
- [ ] **Frontend Build** — workflow `Frontend Build` chạy thành công (npm run build)
- [ ] **Frontend Lint** — không có lỗi ESLint

*Các check trên chạy trong GitHub Actions khi có PR vào `dev` hoặc `main`.*

---

## 2. Nội dung PR

- [ ] **Base branch** — merge vào đúng branch (thường là `dev`)
- [ ] **Compare branch** — nhánh nguồn đúng (vd: `optimize/sla-and-diagnostics`)
- [ ] **Mô tả rõ ràng** — title và description giải thích thay đổi
- [ ] **Không có file thừa** — không commit file build, cache, secret, .env

---

## 3. Code & Chất lượng

- [ ] **Logic đúng** — không phá chức năng hiện có
- [ ] **Không có debug/console thừa** — đã xóa log tạm, debug code
- [ ] **Đặt tên rõ ràng** — biến, hàm, file dễ hiểu
- [ ] **Không duplicate** — tái sử dụng code có sẵn khi có thể

---

## 4. Bảo mật & Cấu hình

- [ ] **Không hardcode secret** — API key, password chỉ dùng biến môi trường
- [ ] **Cấu hình nhạy cảm** — không commit file chứa credential thật

---

## 5. Sau khi merge

- [ ] **Đã test cơ bản** — build/run local hoặc trên môi trường tương ứng
- [ ] **Đã merge vào base** — PR đã được merge, không còn conflict

---

## Tạo Pull Request (từ nhánh → dev)

1. Đảm bảo code đã push lên remote:
   ```bash
   git push -u origin optimize/sla-and-diagnostics
   ```
2. Mở link tạo PR (thay `optimize/sla-and-diagnostics` nếu dùng nhánh khác):
   - **GitHub:** https://github.com/PhamMinhKha0710/DevComunity/compare/dev...optimize/sla-and-diagnostics
3. Chọn **base: dev**, **compare: optimize/sla-and-diagnostics**, điền title/description rồi tạo PR.

Sau khi tạo PR, các trạng thái cần check sẽ xuất hiện trong tab "Checks" của PR.
