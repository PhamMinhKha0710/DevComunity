import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5122';
const API_BASE = `${BASE_URL}/api`;

export const options = {
  scenarios: {
    tc_p_007_search: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 300),
      duration: __ENV.DURATION || '90s',
    },
  },
  thresholds: {
    'http_req_duration{tc:search}': ['p(95)<1000', 'p(99)<2000'],
    http_req_failed: ['rate<0.01'],
  },
};

const QUERIES = ['javascript', 'react', 'python', 'docker', 'api', 'sql', 'redis'];

export default function () {
  const q = QUERIES[__VU % QUERIES.length];
  const url = `${API_BASE}/search?q=${encodeURIComponent(q)}&maxResults=5`;
  const res = http.get(url, {
    headers: { 'Content-Type': 'application/json' },
    tags: { tc: 'search' },
  });
  check(res, { 'search 200': (r) => r.status === 200 });
  sleep(0.3);
}

export function handleSummary(data) {
  const out = {
    tc: 'TC-P-007',
    p95_ms: data.metrics.http_req_duration?.values?.['p(95)'],
    p99_ms: data.metrics.http_req_duration?.values?.['p(99)'],
  };
  return {
    stdout: `\n=== TC-P-007 Search ===\n${JSON.stringify(out, null, 2)}\n`,
    'results/tc-p-007-search.json': JSON.stringify(out, null, 2),
  };
}
