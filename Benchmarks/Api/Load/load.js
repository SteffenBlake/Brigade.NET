import http from 'k6/http';
import { check } from 'k6';
import exec from 'k6/execution';

const baseUrl = __ENV.BASE_URL;
const database = __ENV.DATABASE;
const operation = __ENV.OPERATION;
const runId = __ENV.RUN_ID || 'run';
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 400));

if (!baseUrl || !database || !['invalid', 'create', 'search'].includes(operation)) {
    throw new Error('Set BASE_URL, DATABASE, and a known OPERATION.');
}

const warmup = __ENV.WARMUP === '1';
if (Number(__ENV.RATE) <= 0 || !__ENV.DURATION) {
    throw new Error('Invalid fixed-rate settings.');
}

const threshold = expression => ({
    threshold: expression,
    abortOnFail: false,
});
const thresholds = warmup ? {} : {
    http_req_failed: [threshold('rate<0.001')],
    checks: [threshold('rate>0.999')],
    dropped_iterations: [threshold('count==0')],
    http_req_duration: [
        threshold(`p(95)<${__ENV.P95_MS || 250}`),
        threshold(`p(99)<${__ENV.P99_MS || 500}`),
    ],
};

export const options = {
    scenarios: {
        requests: {
            executor: 'constant-arrival-rate',
            rate: Number(__ENV.RATE),
            timeUnit: '1s',
            duration: __ENV.DURATION,
            preAllocatedVUs: Number(__ENV.VUS || 100),
            maxVUs: Number(__ENV.MAX_VUS || 1000),
        },
    },
    thresholds,
};

export default function () {
    const number = exec.scenario.iterationInTest;
    const url = `${baseUrl}/api/${database}/items`;
    if (operation === 'search') {
        const categoryId = number % 9 + 1;
        const minScore = [0, 25, 50, 75, 95][number % 5];
        const response = http.get(`${url}?categoryId=${categoryId}&minScore=${minScore}`);
        check(response, {
            'search returns items': reply => reply.status === 200 && Array.isArray(reply.json()),
        });
        return;
    }

    const body = operation === 'invalid'
        ? { title: 'x', categoryId: 0, score: -1 }
        : {
            title: `Item ${runId} ${number}`,
            categoryId: number % 9 + 1,
            score: 50 + number % 51,
        };
    const response = http.post(url, JSON.stringify(body), {
        headers: { 'Content-Type': 'application/json' },
    });
    check(response, {
        'expected status and body': reply => operation === 'invalid'
            ? reply.status === 400
            : reply.status === 200 && Number(reply.json('id')) > 0,
    });
}

export function handleSummary(data) {
    return { [__ENV.SUMMARY_FILE]: JSON.stringify(data) };
}
