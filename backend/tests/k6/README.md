# k6 Load Tests

This directory contains k6 load tests for the SocialTechsy API.

## Prerequisites

- [k6](https://k6.io/docs/getting-started/installation/) installed
- API server running (default: `http://localhost:5000`)

## Running Tests

### All Tests

```bash
k6 run auth.yaml questions.yaml
```

### Individual Tests

```bash
# Auth endpoints (login, register, refresh-token)
k6 run auth.yaml

# Questions endpoints (list, detail, create)
k6 run questions.yaml
```

### With Custom Base URL

```bash
BASE_URL=https://api.socialtechsy.com k6 run auth.yaml
```

## Test Scenarios

### Auth Tests (`auth.yaml`)

| Scenario | VUs | Duration | SLA Threshold |
|----------|-----|----------|---------------|
| Login | 50 | 5m | p95 < 800ms |
| Register | 30 | 3m | p95 < 800ms |
| Refresh Token | 200 (ramp) | 5m | p95 < 100ms |

### Questions Tests (`questions.yaml`)

| Scenario | VUs | Duration | SLA Threshold |
|----------|-----|----------|---------------|
| GET Questions List | 100 | 5m | p95 < 200ms (cache miss), <50ms (hit) |
| GET Question Detail | 50 | 5m | p95 < 50ms |
| POST Create Question | 20 | 3m | p95 < 300ms |

## Output

Results are saved to:
- `results/auth-summary.json`
- `results/questions-summary.json`

## SLA Targets

Based on the Performance SLA document:

| Endpoint Type | Cache Hit | Cache Miss | Write + Events |
|---------------|-----------|------------|----------------|
| Questions | ≤50ms | ≤200ms | ≤300ms |
| Auth | - | ≤800ms | ≤800ms |
| Search | - | ≤300ms | - |

## Integration with CI/CD

Example GitHub Actions:

```yaml
- name: Run k6 tests
  run: |
    k6 run auth.yaml --out json=results/auth.json
    k6 run questions.yaml --out json=results/questions.json
  working-directory: backend/tests/k6
```
