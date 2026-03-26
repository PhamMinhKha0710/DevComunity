import http from 'k6/http';
import { check, sleep } from 'k6';
import { SharedArray } from 'k6/data';

const users = new SharedArray('users', function () {
  try {
    return JSON.parse(open('./credentials.json'));
  } catch {
    return [];
  }
});

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5122';
const API_BASE = `${BASE_URL}/api`;
const loginPauseSec = Number(__ENV.LOGIN_SLEEP_SEC || 0.3);

export const options = {
  scenarios: {
    tc_p_001_login: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 500),
      duration: __ENV.DURATION || '60s',
    },
  },
  thresholds: {
    'http_req_duration{tc:login}': ['p(50)<100', 'p(95)<300', 'p(99)<500'],
    http_req_failed: ['rate<0.001'],
  },
};

function getCreds() {
  if (users.length > 0) {
    const u = users[(__VU - 1) % users.length];
    return { email: u.email, password: u.password };
  }
  return {
    email: __ENV.LOGIN_EMAIL || `user${__VU}@loadtest.local`,
    password: __ENV.LOGIN_PASSWORD || 'TestPassword123!',
  };
}

export default function () {
  const c = getCreds();
  const payload = JSON.stringify({ email: c.email, password: c.password });
  const res = http.post(`${API_BASE}/auth/login`, payload, {
    headers: { 'Content-Type': 'application/json' },
    tags: { tc: 'login' },
  });
  check(res, { 'login 200': (r) => r.status === 200 });
  sleep(loginPauseSec);
}

export function handleSummary(data) {
  const m = data.metrics;
  const reqDur = m.http_req_duration;
  const failed = m.http_req_failed;
  const reqs = m.http_reqs;
  const out = {
    tc: 'TC-P-001',
    p50_ms: reqDur?.values?.['p(50)'],
    p95_ms: reqDur?.values?.['p(95)'],
    p99_ms: reqDur?.values?.['p(99)'],
    http_req_failed_rate: failed?.values?.rate,
    total_requests: reqs?.values?.count,
    duration_sec: data.state?.testRunDurationMs ? data.state.testRunDurationMs / 1000 : null,
  };
  if (out.duration_sec && out.total_requests) {
    out.approx_rps = out.total_requests / out.duration_sec;
  }
  return {
    stdout: textSummary(out),
    'results/tc-p-001-login.json': JSON.stringify(out, null, 2),
  };
}

function textSummary(out) {
  return `\n=== TC-P-001 Login load ===\n${JSON.stringify(out, null, 2)}\n`;
}
