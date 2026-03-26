import http from 'k6/http';
import { sleep } from 'k6';
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

export const options = {
  scenarios: {
    tc_p_010_soak: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 300),
      duration: __ENV.SOAK_DURATION || '2h',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.02'],
  },
};

function loginOnce() {
  const c =
    users.length > 0
      ? users[(__VU - 1) % users.length]
      : {
          email: __ENV.LOGIN_EMAIL || 'john.doe@example.com',
          password: __ENV.LOGIN_PASSWORD || 'User@123',
        };
  const res = http.post(
    `${API_BASE}/auth/login`,
    JSON.stringify({ email: c.email, password: c.password }),
    { headers: { 'Content-Type': 'application/json' } }
  );
  if (res.status !== 200) return '';
  return res.json('token') || '';
}

let tokenCache = '';
let loginDone = false;

export default function () {
  if (!loginDone) {
    tokenCache = loginOnce();
    loginDone = true;
  }
  const r = Math.random();
  const headers = tokenCache ? { Authorization: `Bearer ${tokenCache}` } : {};
  if (r < 0.35) {
    http.get(`${API_BASE}/questions?page=1&pageSize=20&sort=newest`, { tags: { soak: 'questions' } });
  } else if (r < 0.65) {
    http.get(`${API_BASE}/search?q=javascript&maxResults=5`, { tags: { soak: 'search' } });
  } else {
    http.get(`${API_BASE}/questions/1`, { headers, tags: { soak: 'detail' } });
  }
  sleep(1);
}

export function handleSummary(data) {
  const out = {
    tc: 'TC-P-010',
    http_req_failed_rate: data.metrics.http_req_failed?.values?.rate,
    p95_ms: data.metrics.http_req_duration?.values?.['p(95)'],
  };
  return {
    stdout: `\n=== TC-P-010 Soak ===\n${JSON.stringify(out, null, 2)}\n`,
    'results/tc-p-010-soak.json': JSON.stringify(out, null, 2),
  };
}
