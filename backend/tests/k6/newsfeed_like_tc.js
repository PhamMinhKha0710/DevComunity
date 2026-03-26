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
const HOT_POST_ID = Number(__ENV.HOT_POST_ID || '0');

export const options = {
  scenarios: {
    tc_p_004_like: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 500),
      duration: __ENV.DURATION || '30s',
    },
  },
  thresholds: {
    'http_req_duration{tc:like}': ['p(95)<200', 'p(99)<500'],
    http_req_failed: ['rate<0.001'],
  },
};

function login(email, password) {
  const res = http.post(
    `${API_BASE}/auth/login`,
    JSON.stringify({ email, password }),
    { headers: { 'Content-Type': 'application/json' } }
  );
  if (res.status !== 200) return '';
  return res.json('token') || '';
}

export default function () {
  if (HOT_POST_ID <= 0) {
    throw new Error('Set HOT_POST_ID for TC-P-004');
  }
  if (users.length === 0) {
    throw new Error('TC-P-004 needs credentials.json with one row per VU (distinct users)');
  }
  const c = users[(__VU - 1) % users.length];
  const token = login(c.email, c.password);
  if (!token) {
    return;
  }
  const url = `${API_BASE}/newsfeed/posts/${HOT_POST_ID}/like`;
  const res = http.post(url, null, {
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    tags: { tc: 'like' },
  });
  check(res, { 'like 200': (r) => r.status === 200 });
  sleep(0.2);
}

export function handleSummary(data) {
  const out = {
    tc: 'TC-P-004',
    hot_post_id: HOT_POST_ID,
    p95_ms: data.metrics.http_req_duration?.values?.['p(95)'],
    http_req_failed_rate: data.metrics.http_req_failed?.values?.rate,
  };
  return {
    stdout: `\n=== TC-P-004 Newsfeed like ===\n${JSON.stringify(out, null, 2)}\n`,
    'results/tc-p-004-newsfeed-like.json': JSON.stringify(out, null, 2),
  };
}
