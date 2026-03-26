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

export const options = {
  scenarios: {
    tc_p_002_questions: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 1000),
      duration: __ENV.DURATION || '120s',
    },
  },
  thresholds: {
    'http_req_duration{tc:questions_list}': ['p(95)<500', 'p(99)<1000'],
    http_req_failed: ['rate<0.005'],
  },
};

function getToken() {
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
    tokenCache = getToken();
    loginDone = true;
  }
  const url = `${API_BASE}/questions?page=1&pageSize=20&sort=newest`;
  const res = http.get(url, {
    headers: { Authorization: `Bearer ${tokenCache}` },
    tags: { tc: 'questions_list' },
  });
  check(res, { 'questions 200': (r) => r.status === 200 });
  sleep(0.2);
}

export function handleSummary(data) {
  const reqDur = data.metrics.http_req_duration;
  const failed = data.metrics.http_req_failed;
  const reqs = data.metrics.http_reqs;
  const out = {
    tc: 'TC-P-002',
    p95_ms: reqDur?.values?.['p(95)'],
    p99_ms: reqDur?.values?.['p(99)'],
    http_req_failed_rate: failed?.values?.rate,
    total_requests: reqs?.values?.count,
  };
  return {
    stdout: `\n=== TC-P-002 Questions list ===\n${JSON.stringify(out, null, 2)}\n`,
    'results/tc-p-002-questions.json': JSON.stringify(out, null, 2),
  };
}
