import http from 'k6/http';
import { check, fail, sleep } from 'k6';

const baseUrl = (__ENV.BASE_URL || 'http://127.0.0.1:8080').replace(/\/$/, '');

export const options = {
    scenarios: {
        authenticated_reads: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: __ENV.RAMP_DURATION || '30s', target: Number(__ENV.VUS || 10) },
                { duration: __ENV.HOLD_DURATION || '2m', target: Number(__ENV.VUS || 10) },
                { duration: '15s', target: 0 },
            ],
            gracefulRampDown: '15s',
        },
    },
    thresholds: {
        checks: ['rate>0.99'],
        http_req_failed: ['rate<0.01'],
        http_req_duration: ['p(95)<750', 'p(99)<1500'],
        'http_req_duration{endpoint:dashboard}': ['p(95)<750'],
        'http_req_duration{endpoint:products}': ['p(95)<500'],
        'http_req_duration{endpoint:recipes}': ['p(95)<500'],
        'http_req_duration{endpoint:consumptions}': ['p(95)<650'],
    },
};

export function setup() {
    if (__ENV.AUTH_TOKEN) {
        return { accessToken: __ENV.AUTH_TOKEN };
    }

    if (!__ENV.TEST_EMAIL || !__ENV.TEST_PASSWORD) {
        fail('Set AUTH_TOKEN or both TEST_EMAIL and TEST_PASSWORD. Use a disposable load-test account.');
    }

    const response = http.post(
        `${baseUrl}/api/v1/auth/login`,
        JSON.stringify({ email: __ENV.TEST_EMAIL, password: __ENV.TEST_PASSWORD, rememberMe: false }),
        { headers: { 'Content-Type': 'application/json' }, tags: { endpoint: 'login-setup' } },
    );

    const authenticated = check(response, { 'load-test login succeeded': value => value.status === 200 });
    if (!authenticated) {
        fail(`Login failed with HTTP ${response.status}.`);
    }

    return { accessToken: response.json('accessToken') };
}

export default function (data) {
    const params = {
        headers: { Authorization: `Bearer ${data.accessToken}` },
    };

    request('/api/v1/dashboard', 'dashboard', params);
    request('/api/v1/products?page=1&limit=25&includePublic=false', 'products', params);
    request('/api/v1/recipes?page=1&limit=25&includePublic=false', 'recipes', params);
    request('/api/v1/consumptions?page=1&limit=25', 'consumptions', params);
    sleep(Number(__ENV.ITERATION_PAUSE_SECONDS || 1));
}

function request(path, endpoint, params) {
    const response = http.get(`${baseUrl}${path}`, {
        ...params,
        tags: { endpoint },
    });

    check(response, {
        [`${endpoint} returned 2xx`]: value => value.status >= 200 && value.status < 300,
    });
}
