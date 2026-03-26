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
    tc_p_020_journey: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 100),
      duration: __ENV.DURATION || '5m',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<3000'],
  },
};

function getCreds() {
  if (users.length > 0) {
    const u = users[(__VU - 1) % users.length];
    return { email: u.email, password: u.password };
  }
  return {
    email: __ENV.LOGIN_EMAIL || 'john.doe@example.com',
    password: __ENV.LOGIN_PASSWORD || 'User@123',
  };
}

let journeyToken = '';

export default function () {
  if (HOT_POST_ID <= 0) {
    throw new Error('Set HOT_POST_ID for TC-P-020');
  }
  if (__ITER === 0) {
    const c = getCreds();
    const loginRes = http.post(
      `${API_BASE}/auth/login`,
      JSON.stringify({ email: c.email, password: c.password }),
      { headers: { 'Content-Type': 'application/json' }, tags: { journey: 'login' } }
    );
    const okLogin = check(loginRes, { 'login 200': (r) => r.status === 200 });
    journeyToken = okLogin ? loginRes.json('token') : '';
  }
  if (!journeyToken) {
    sleep(1);
    return;
  }
  const auth = { Authorization: `Bearer ${journeyToken}` };

  const feedRes = http.get(`${API_BASE}/newsfeed?page=1&pageSize=20`, {
    headers: { ...auth },
    tags: { journey: 'feed' },
  });
  check(feedRes, { 'feed 200': (r) => r.status === 200 });

  const likeRes = http.post(`${API_BASE}/newsfeed/posts/${HOT_POST_ID}/like`, null, {
    headers: { ...auth, 'Content-Type': 'application/json' },
    tags: { journey: 'like' },
  });
  check(likeRes, { 'like 200 or 400': (r) => r.status === 200 || r.status === 400 });

  const commentRes = http.post(
    `${API_BASE}/comments/post/${HOT_POST_ID}`,
    JSON.stringify({ body: `journey ${Date.now()} vu${__VU}` }),
    {
      headers: { ...auth, 'Content-Type': 'application/json' },
      tags: { journey: 'comment' },
    }
  );
  check(commentRes, { 'comment 201 or 400/404': (r) =>
    r.status === 201 || r.status === 400 || r.status === 404
  });

  if (__ENV.SKIP_LOGOUT !== 'true') {
    const logoutRes = http.post(`${API_BASE}/auth/logout`, '{}', {
      headers: { ...auth, 'Content-Type': 'application/json' },
      tags: { journey: 'logout' },
    });
    check(logoutRes, { 'logout 200 or 401': (r) => r.status === 200 || r.status === 401 });
  }

  sleep(1);
}

export function handleSummary(data) {
  const out = {
    tc: 'TC-P-020',
    hot_post_id: HOT_POST_ID,
    p95_ms: data.metrics.http_req_duration?.values?.['p(95)'],
    http_req_failed_rate: data.metrics.http_req_failed?.values?.rate,
  };
  return {
    stdout: `\n=== TC-P-020 Full journey ===\n${JSON.stringify(out, null, 2)}\n`,
    'results/tc-p-020-full-journey.json': JSON.stringify(out, null, 2),
  };
}
