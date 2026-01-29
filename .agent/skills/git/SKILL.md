---
name: Git Workflow
description: Hướng dẫn sử dụng Git cho dự án DevComunity - quản lý branch, commit, merge và giải quyết conflict
---

# Git Workflow cho DevComunity

## Triết lý Git của nhóm

> **Git không chỉ là công cụ lưu trữ code - mà là công cụ ghi lại QUY TRÌNH TƯ DUY của nhóm.**

Mỗi branch, mỗi commit, mỗi merge đều phản ánh:
- 🧠 **Tư duy thiết kế**: Tại sao chọn cách này?
- 🔄 **Quá trình phát triển**: Làm từng bước như thế nào?
- 📝 **Lịch sử quyết định**: Đã thử những gì, thất bại ở đâu, thành công ra sao?

---

## Quy trình tư duy qua Branch

### 1. Trước khi tạo Branch - Phân tích vấn đề

```
📋 CHECKLIST TRƯỚC KHI CODE:
□ Vấn đề/yêu cầu cụ thể là gì?
□ Ảnh hưởng đến những module nào? (backend/frontend/database)
□ Có cần thay đổi API không?
□ Có breaking changes không?
□ Cần bao lâu để hoàn thành?
```

### 2. Đặt tên Branch - Mô tả mục đích rõ ràng

| Loại | Format | Mục đích |
|------|--------|----------|
| **Main** | `main` | Production code, luôn stable |
| **Develop** | `develop` | Integration branch, merge features vào đây trước |
| Feature | `feature/ten-tinh-nang` | Phát triển tính năng mới |
| Bugfix | `bugfix/mo-ta-loi` | Sửa lỗi đã biết |
| Hotfix | `hotfix/mo-ta` | Sửa lỗi khẩn cấp production |
| Experiment | `experiment/thu-nghiem` | Thử nghiệm ý tưởng mới |

**Git Flow:**
```
feature/* ─→ develop ─→ main (production)
bugfix/*  ─→ develop ─→ main
hotfix/*  ─────────────→ main (urgent fixes)
```

**Ví dụ thực tế:**
```
feature/realtime-chat          → Xây dựng tính năng chat realtime
bugfix/message-not-delivered   → Fix lỗi tin nhắn không gửi được
experiment/redis-caching       → Thử nghiệm Redis cho caching
```

---

## Quy trình tư duy qua Commit

### Commit Message = Ghi chép tư duy

Mỗi commit message phải trả lời được:
1. **WHAT** - Làm gì?
2. **WHY** - Tại sao làm?
3. **HOW** - Cách tiếp cận? (nếu phức tạp)

### Format Commit Message

```
<type>(<scope>): <mô tả ngắn>

[Giải thích chi tiết - TẠI SAO làm điều này]
[Những lựa chọn đã cân nhắc]
[Những vấn đề gặp phải và cách giải quyết]
```

**Types:**
- `feat`: Tính năng mới
- `fix`: Sửa lỗi
- `refactor`: Tái cấu trúc (không thay đổi behavior)
- `docs`: Documentation
- `test`: Thêm/sửa tests
- `perf`: Cải thiện performance
- `chore`: Maintenance tasks

**Scopes cho DevComunity:**
- `backend`, `frontend`, `api`, `domain`, `infra`, `signalr`, `auth`, `chat`

### Ví dụ Commit có tư duy

```
feat(signalr): implement real-time message delivery via ChatHub

Vấn đề: Tin nhắn không được gửi realtime giữa 2 users.

Nguyên nhân đã phân tích:
- SignalR connection được tạo nhưng không join đúng room
- UserId mapping với ConnectionId bị mất khi reconnect

Giải pháp:
- Thêm dictionary lưu mapping UserId -> List<ConnectionId>
- Gọi JoinRoom khi OnConnectedAsync
- Broadcast message đến tất cả connections của recipient

Tested với 2 browser windows - messages delivered trong <100ms
```

```
fix(backend): resolve null reference in VoteCommandHandler

Vấn đề: NullReferenceException khi user vote lần đầu.

Root cause: 
- GetExistingVote trả về null cho user chưa vote
- Code không check null trước khi access properties

Fix: Thêm null check và tạo vote mới nếu chưa tồn tại

Đã thử:
❌ Optional pattern - code verbose quá
✅ Simple null check - clear và đủ dùng
```

---

## Quy trình tư duy khi Merge

### Trước khi Merge - Review Checklist

```
📋 MERGE CHECKLIST:
□ Code đã được test locally?
□ Không có conflict với main?
□ Commit messages có ý nghĩa?
□ Documentation đã cập nhật?
□ Có breaking changes không? → Thông báo team
```

### Merge Message - Tổng kết feature/fix

```powershell
git merge --no-ff feature/realtime-chat -m "
Merge feature/realtime-chat: Hoàn thành tính năng chat realtime

Tổng kết:
- ChatHub xử lý kết nối và gửi tin nhắn
- MessageService lưu trữ và truy vấn messages
- Frontend ChatPage với real-time updates

Thay đổi chính:
- Backend: 5 files mới, 3 files modified
- Frontend: 8 components mới

Testing:
- Unit tests cho MessageService
- Manual test với 2 users đồng thời

Known issues:
- Typing indicator chưa implement (để phase 2)
"
```

---

## Workflow hoàn chỉnh với tư duy

### Phase 1: Phân tích (Analysis)
```powershell
# 1. Cập nhật code mới nhất
git checkout main
git pull origin main

# 2. Tạo branch với tên có ý nghĩa
git checkout -b feature/user-reputation-system
```

📝 **Ghi chép vào commit đầu tiên:**
```powershell
git commit --allow-empty -m "
feat(backend): start user reputation system

Mục tiêu:
- Users nhận/mất điểm khi được vote
- Hiển thị reputation trên profile

Kế hoạch:
1. Thêm UpdateReputation vào UserRepository
2. Modify VoteCommandHandler để gọi UpdateReputation  
3. Thêm endpoint GET /users/{id}/reputation
4. Frontend hiển thị badge theo reputation level
"
```

### Phase 2: Implementation
```powershell
# Commit theo từng bước logic
git add backend/src/DevComunity.Infrastructure/
git commit -m "feat(infra): add UpdateReputationAsync to UserRepository

Implement IUserRepository.UpdateReputationAsync:
- Tăng/giảm points atomic để tránh race condition
- Sử dụng ExecuteUpdateAsync của EF Core 7
"

git add backend/src/DevComunity.Application/
git commit -m "feat(backend): integrate reputation in VoteCommandHandler

Khi user vote:
- Upvote question/answer: author +10 points
- Downvote: author -2 points, voter -1 point

Rationale: Tham khảo Stack Overflow scoring system
"
```

### Phase 3: Testing & Documentation
```powershell
git add tests/
git commit -m "test(backend): add unit tests for reputation system

Coverage:
- VoteCommandHandler với các scenarios
- Edge cases: self-vote, duplicate vote
"

git add README.md docs/
git commit -m "docs: update API documentation for reputation

- Thêm endpoint /users/{id}/reputation
- Cập nhật vote response với new reputation
"
```

### Phase 4: Merge với tổng kết
```powershell
git checkout main
git merge --no-ff feature/user-reputation-system
git push origin main
```

---

## Giải quyết Conflict = Giải quyết Xung đột Tư duy

Conflict không chỉ là code conflict - mà là **xung đột về cách tiếp cận**:

```powershell
# Khi gặp conflict
git pull origin main

# Xem các file conflict
git status
```

📝 **Phân tích xung đột:**
```
File: UserRepository.cs

<<<<<<< HEAD (Code của bạn)
// Approach: Sử dụng transaction cho atomic update
await _context.Database.BeginTransactionAsync();
=======  
// Approach: Sử dụng EF Core ExecuteUpdateAsync
await _context.Users.ExecuteUpdateAsync(...);
>>>>>>> origin/main (Code của teammate)
```

**Quyết định:**
- Discuss với teammate về 2 approaches
- Chọn approach phù hợp hơn
- Commit với giải thích rõ ràng

```powershell
git add UserRepository.cs
git commit -m "fix: resolve conflict in UserRepository

Conflict: 2 approaches cho atomic reputation update
- Transaction-based (branch này)
- ExecuteUpdateAsync (main)

Decision: Sử dụng ExecuteUpdateAsync
Reason: Performance tốt hơn, code cleaner, EF Core 7 recommend
"
```

---

## Commands Reference

```powershell
# Xem lịch sử commit với context
git log --oneline -20
git log --graph --oneline --all

# Xem chi tiết một commit (đọc lại tư duy)
git show <commit-hash>

# So sánh thay đổi
git diff main..feature/branch-name

# Stash với mô tả
git stash save "WIP: đang thử approach X cho feature Y"

# Hoàn tác commit gần nhất (giữ changes)
git reset --soft HEAD~1
```
