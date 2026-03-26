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
const HOT_ID = Number(__ENV.HOT_QUESTION_ID || '0');

export const options = {
  scenarios: {
    tc_p_003_hot_question: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 500),
      duration: __ENV.DURATION || '60s',
    },
  },
  thresholds: {
    'http_req_duration{tc:hot_detail}': ['p(95)<400', 'p(99)<800'],
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
  if (HOT_ID <= 0) {
    throw new Error('Set HOT_QUESTION_ID (integer) for TC-P-003');
  }
  if (!loginDone) {
    tokenCache = getToken();
    loginDone = true;
  }
  const url = `${API_BASE}/questions/${HOT_ID}`;
  const res = http.get(url, {
    headers: { Authorization: `Bearer ${tokenCache}` },
    tags: { tc: 'hot_detail' },
  });
  check(res, { 'detail 200 or 404': (r) => r.status === 200 || r.status === 404 });
  sleep(0.1);
}

export function handleSummary(data) {
  const reqDur = data.metrics.http_req_duration;
  const out = {
    tc: 'TC-P-003',
    hot_question_id: HOT_ID,
    p95_ms: reqDur?.values?.['p(95)'],
    p99_ms: reqDur?.values?.['p(99)'],
  };
  return {
    stdout: `\n=== TC-P-003 Hot question GET ===\n${JSON.stringify(out, null, 2)}\n`,
    'results/tc-p-003-question-hot.json': JSON.stringify(out, null, 2),
  };
}
