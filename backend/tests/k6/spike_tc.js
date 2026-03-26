import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5122';
const API_BASE = `${BASE_URL}/api`;

export const options = {
  scenarios: {
    tc_p_009_spike: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '10s', target: 1000 },
        { duration: '60s', target: 1000 },
        { duration: '10s', target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
  },
};

export default function () {
  const res = http.get(`${API_BASE}/questions?page=1&pageSize=20&sort=newest`, {
    tags: { tc: 'spike' },
  });
  check(res, { 'spike GET ok': (r) => r.status === 200 || r.status === 401 });
  sleep(0.05);
}

export function handleSummary(data) {
  const out = {
    tc: 'TC-P-009',
    http_req_failed_rate: data.metrics.http_req_failed?.values?.rate,
    note: 'Anonymous GET /questions; use auth if endpoint requires it on staging',
  };
  return {
    stdout: `\n=== TC-P-009 Spike ===\n${JSON.stringify(out, null, 2)}\n`,
    'results/tc-p-009-spike.json': JSON.stringify(out, null, 2),
  };
}
