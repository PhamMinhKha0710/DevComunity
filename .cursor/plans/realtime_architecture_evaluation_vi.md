---
name: Đánh giá kiến trúc Realtime (bản dịch)
overview: Đánh giá kiến trúc realtime hiện tại (SignalR, MongoDB, Redis), chỉ ra điểm nghẽn, đề xuất cải thiện giao nhận tin/offline/scaling và kiến trúc production kiểu Messenger/Slack.
---

# Đánh giá và Kế hoạch cải thiện Kiến trúc Realtime

## 1. Tổng quan kiến trúc hiện tại

(Sơ đồ mermaid giữ nguyên như bản gốc)

### 1.1 Các SignalR Hub (6 hub)

| Hub | Endpoint | Mục đích | Ghi chú |
|-----|----------|----------|---------|
| ChatHub | `/hubs/chat` | Nhắn tin, đang gõ, reaction, đã đọc | Dùng MediatR, giới hạn tốc độ, group `conversation_{id}` |
| NotificationHub | `/hubs/notifications` | Đẩy thông báo tới user | Group `user_{id}` |
| QuestionHub | `/hubs/question` | Cập nhật vote, answer, comment | Group `question_{id}` |
| PresenceHub | `/hubs/presence` | Trạng thái online/offline | Dùng Redis, broadcast `Clients.Others` |
| ActivityHub | `/hubs/activity` | Bảng tin, bài viết nhóm | Groups `activity_feed`, `group_{id}` |
| CallHub | `/hubs/call` | WebRTC signaling | Hub cơ bản |

### 1.2 Lưu trữ Chat (MongoDB)

- **Collections**: `conversations`, `messages`, `counters`
- **Index**: `(ConversationId, SentDate)`, `(ConversationId, IsRead, SenderId)`
- **Tính năng**: Đồng bộ delta qua `GetMessagesSinceAsync(conversationId, sinceMessageId, limit=200)`

### 1.3 Cách dùng Redis

- **SignalR backplane** (khi bật `Redis:Enabled`) – scale nhiều instance
- **RedisChatCacheService**: sequence, cache hội thoại (TTL 10 phút), thông tin user (TTL 5 phút), số tin chưa đọc
- **RedisPresenceService**: `presence:user:{userId}` (Set connectionId), set theo dõi `presence:online`
- **RedisChatRateLimiter**: Cửa sổ trượt (30 tin/phút/user)
- **CacheService L1+L2**: Cache phân tán cho query

---

## 2. Phân tích điểm nghẽn

### 2.1 PresenceHub – Broadcast tràn

**File**: [PresenceHub.cs](backend/src/SocialTechsy.SocialNetwork.Api/Hubs/PresenceHub.cs)

- Gửi tới **tất cả** client đang kết nối, không chỉ bạn bè/user liên quan.
- Ở quy mô lớn (vd. 100k+ user đồng thời), mỗi lần connect/disconnect sẽ broadcast cho mọi người.
- **Messenger/Slack**: Presence chỉ gửi cho bạn bè / thành viên workspace.

### 2.2 ActivityHub – Một group toàn cục

**File**: [ActivityHub.cs](backend/src/SocialTechsy.SocialNetwork.Api/Hubs/ActivityHub.cs)

- Mọi user kết nối đều vào `activity_feed`.
- `NewPost` / `NewGroupPost` gửi vào `activity_feed` = broadcast toàn bộ user.
- Frontend không có `useHub('activity')` trong [connectionManager.ts](frontend/src/lib/signalr/connectionManager.ts) – `HubName` thiếu `'activity'`.

### 2.3 Xử lý tin nhắn offline – Một phần

| Thành phần | Hành vi |
|------------|---------|
| ChatHub.BroadcastMessageAsync | Chỉ gửi `NewMessageNotification` tới `user_{uid}` khi user đang kết nối |
| ChatMessageConsumerService | Tạo Notification trong SQL cho tin chat qua RabbitMQ |
| SyncMessages | Client gọi khi reconnect với `lastMessageId` → delta từ MongoDB |

- Push realtime chat **chỉ khi online**; user offline chỉ nhận notification lưu DB.
- Không có hàng đợi "pending push" phía server khi user quay lại.
- SyncMessages đủ cho lịch sử tin; reconnect không kích hoạt event SignalR "bạn có N tin nhắn offline".

### 2.4 Luồng ghi Chat – Broadcast đồng bộ

- `SendMessageCommand` → ghi MongoDB → ChatHub `BroadcastMessageAsync` trong cùng request.
- Nếu gửi SignalR lỗi (vd. người nhận ở instance khác, trễ backplane), tin vẫn lưu nhưng giao realtime có thể mất.
- Không có retry hay đảm bảo at-least-once cho giao tin SignalR.

### 2.5 Connection Manager – Thiếu Activity Hub

**File**: [connectionManager.ts](frontend/src/lib/signalr/connectionManager.ts)

- Thiếu `'activity'`; ActivityHub không được dùng bởi connection manager dùng chung.

### 2.6 Presence – GetOnlineUsers O(N)

**File**: [RedisPresenceService.cs](backend/src/SocialTechsy.SocialNetwork.Infrastructure/Redis/RedisPresenceService.cs)

- `GetOnlineUserIdsAsync()` trả về toàn bộ user ID đang online.
- Trang chat gọi cho mọi user trong hội thoại; ở quy mô lớn sẽ nặng.
- Nên giới hạn theo "bạn bè" hoặc "thành viên hội thoại" và cache theo user.

---

## 3. Đề xuất cải thiện

### 3.1 Giao tin nhắn

| Cải thiện | Mô tả |
|-----------|--------|
| Giao tin at-least-once | Sau khi ghi MongoDB, publish tin lên RabbitMQ; consumer đẩy qua SignalR. Lỗi thì retry/DLQ. |
| Receiver idempotent | Client loại trùng theo `messageId` (logic React đã hỗ trợ). |
| Delivery receipts | Đã có `AcknowledgeDelivery`; đảm bảo frontend gửi `delivered`/`read` khi nhận. |
| Fan-out song song | Dùng `Task.WhenAll` cho gửi nhiều người nhận (đã có trong `BroadcastMessageAsync`). |

### 3.2 Xử lý tin offline

| Cải thiện | Mô tả |
|-----------|--------|
| Kích hoạt sync khi reconnect | Trong `onreconnected`, client gọi `SyncMessages` cho từng hội thoại đang mở với `lastMessageId`. |
| Đẩy thông báo offline | `ChatMessageConsumerService` nên gọi SignalR khi user reconnect – cần hàng đợi "pending notifications" theo user trong Redis. |
| Tùy chọn: Hàng đợi Redis theo user | Lưu `user:{userId}:pending_push` (danh sách messageId) khi user offline; khi reconnect, consumer xử lý và đẩy. |

### 3.3 Scale SignalR

| Hiện tại | Đề xuất |
|----------|---------|
| Redis backplane (in-memory) | Giữ cho multi-instance; cân nhắc **Azure SignalR Service** khi 10k+ concurrent. |
| Hub tách riêng | Đã tách (Chat, Notification, Question...); phù hợp scale ngang. |

---

## 4. Kiến trúc Production (kiểu Messenger/Slack)

(Sơ đồ mermaid giữ nguyên)

### 4.1 Thay đổi kiến trúc

| Tầng | Hiện tại | Mục tiêu |
|------|----------|----------|
| Realtime | SignalR + Redis backplane | Giữ Redis backplane (tới ~10k). Hoặc Azure SignalR Service (10k+). |
| Presence | Broadcast toàn bộ | Thu hẹp: danh sách bạn / thành viên workspace. `Clients.Group("friends_of_{userId}")`. |
| Activity | `activity_feed` toàn cục | Chia nhỏ: `activity_feed:{userId}` hoặc `activity_feed:friends_{userId}`. |
| Chat push | Inline trong ChatHub | Chuyển sang consumer RabbitMQ (giống LikeNotificationConsumer) để retry, DLQ, scale. |
| Offline | Notification DB + SyncMessages | Thêm Redis `pending_push:{userId}`; xử lý khi reconnect. |

### 4.2 Presence theo phạm vi (theo bạn bè)

- User A kết nối → Join groups: `user_A`, `friends_A`.
- User B (bạn) lên online → Chỉ notify `Clients.Group("friends_B")` → Chỉ bạn của B nhận UserOnline(B).
- Cần group `friends_of_{userId}` dựa trên đồ thị bạn bè.
- Thay `GetOnlineUsers` bằng `GetOnlineFriends` hoặc `GetOnlineInConversation(conversationId)`.

### 4.3 Activity feed chia nhỏ

- Mỗi user join `activity_feed:user_{userId}` hoặc `activity_feed:friends_{userId}`.
- Server chỉ gửi tới các group liên quan khi có bài viết/câu hỏi mới.
- Giảm kích thước broadcast từ N user xuống cỡ số bạn bè.

### 4.4 Chat push qua RabbitMQ

```
SendMessageCommand → MongoDB → Publish ChatPushEvent lên RabbitMQ
→ ChatPushConsumer: với mỗi người nhận, nếu online → push SignalR; không thì → Redis pending_push
→ Khi reconnect: đọc pending_push, xử lý và push
```

---

## 5. Thứ tự ưu tiên triển khai

| Ưu tiên | Task | Công sức | Tác động |
|---------|------|----------|----------|
| P1 | Thêm `activity` vào HubName và kết nối ActivityHub trên newsfeed | Thấp | Sửa thiếu realtime activity |
| P1 | Thu hẹp PresenceHub theo bạn bè (bỏ `Clients.Others`) | Trung bình | Loại broadcast storm |
| P2 | Chuyển chat push sang consumer RabbitMQ (giống LikeNotificationConsumer) | Trung bình | Retry, DLQ, scale |
| P2 | Reconnect sync: gọi SyncMessages trong `onreconnected` cho hội thoại đang mở | Thấp | Phục hồi offline tốt hơn |
| P3 | Redis pending_push cho user offline; xử lý khi reconnect | Trung bình | Offline kiểu Messenger |
| P3 | Thay GetOnlineUsers bằng GetOnlineInConversation / GetOnlineFriends | Thấp | Giảm tải |

---

## 6. File tham chiếu

| Mảng | File |
|------|------|
| Cấu hình SignalR | Program.cs L190-206 |
| Lưu chat | MongoChatRepository.cs |
| Redis chat cache | RedisChatCacheService.cs |
| Presence | RedisPresenceService.cs, PresenceHub.cs |
| Luồng chat | ChatHub.cs, ChatCommandHandlers.cs |
| Frontend SignalR | connectionManager.ts, useHub.ts |
