# k6 Load Tests

Thư mục chứa script k6 cho API SocialTechsy. Script **cổ điển** (`auth.yaml`, `questions.yaml`, …) dùng SLA nội bộ; script **TC-P-*** khớp bảng kiểm thử hiệu năng (xem [PERFORMANCE_BACKGROUND_TEST_REPORT.md](../PERFORMANCE_BACKGROUND_TEST_REPORT.md)).

## Yêu cầu

- [k6](https://k6.io/docs/getting-started/installation/) đã cài
- API đang chạy (mặc định `http://localhost:5122`) hoặc **Staging** qua `BASE_URL`

## Bí mật & credentials

- Sao chép `credentials.json.example` → `credentials.json` (không commit — đã có trong `.gitignore`).
- File là mảng JSON: `[{ "email": "...", "password": "..." }, ...]`.
- Một số TC (TC-P-001, TC-P-004, …) cần **đủ user** trên server khớp file.

## Biến môi trường thường dùng

| Biến | Mô tả | Ví dụ |
|------|--------|--------|
| `BASE_URL` | Gốc HTTP API | `https://staging.example.com` |
| `VUS` | Số virtual users | `500` |
| `DURATION` | Thời lượng scenario | `60s`, `5m`, `2h` |
| `SOAK_DURATION` | TC-P-010 (mặc định 2h) | `2m` (smoke local) |
| `HOT_QUESTION_ID` | TC-P-003 | `42` |
| `HOT_POST_ID` | TC-P-004, TC-P-020 | `1` |
| `LOGIN_EMAIL` / `LOGIN_PASSWORD` | Khi không dùng `credentials.json` | |
| `LOGIN_SLEEP_SEC` | TC-P-001: pause giữa các login (tránh **429** khi policy `auth` = 10 req/phút trên Development) | `7` |
| `SKIP_LOGOUT` | TC-P-020: `true` = bỏ bước logout để tái dùng JWT giữa các iteration (không tắt khi benchmark Staging đầy đủ journey) | `true` |

## Script theo TC (khuyến nghị Staging)

| File | TC | Ghi chú |
|------|-----|--------|
| `login_load_tc.js` | TC-P-001 | 500 VU, 60s; SLA p50/p95/p99 trong thresholds |
| `questions_load_tc.js` | TC-P-002 | 1000 VU, 120s; cần JWT (credentials) |
| `question_hot_tc.js` | TC-P-003 | Bắt buộc `HOT_QUESTION_ID` |
| `newsfeed_like_tc.js` | TC-P-004 | Bắt buộc `HOT_POST_ID` + nhiều user trong `credentials.json` |
| `search_load_tc.js` | TC-P-007 | 300 VU, 90s |
| `spike_tc.js` | TC-P-009 | Ramp 0→1000 trong 10s |
| `soak_tc.js` | TC-P-010 | 300 VU; `SOAK_DURATION` (vd. `2h`) |
| `full_journey_tc.js` | TC-P-020 | 100 VU, 5m; cần `HOT_POST_ID` |

## Chạy nhanh

```bash
cd backend/tests/k6

# TC-P-001 (cần credentials.json nếu muốn 500 user thật)
BASE_URL=http://localhost:5122 k6 run login_load_tc.js

# TC-P-003
HOT_QUESTION_ID=1 BASE_URL=http://localhost:5122 k6 run question_hot_tc.js

# TC-P-010 smoke ngắn
SOAK_DURATION=2m VUS=5 k6 run soak_tc.js
```

## Script cổ điển (SLA README cũ)

```bash
k6 run auth.yaml
k6 run questions.yaml
k6 run search.yaml
k6 run chat.yaml
```

## Kết quả

- Summary JSON: `results/tc-p-*.json` (gitignored, trừ `.gitkeep`).
- Có thể thêm `--out json=results/run.json` nếu cần export đầy đủ metrics k6.

## SignalR / WebSocket (TC-P-005, TC-P-006)

HTTP k6 không thay thế load test SignalR đầy đủ. Xem [artillery/README.md](../artillery/README.md).
