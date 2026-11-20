import { describe, expect, it } from 'vitest';

import { redactTelemetryDetails, redactTelemetryText } from './telemetry-redaction';

describe('Telemetry redaction', () => {
    it('keeps the path of a valid absolute URL and masks malformed URLs', () => {
        const text = 'Catalog https://diagnostics.example.test/catalog?search=sample#section and fallback https://[example';

        expect(redactTelemetryText(text)).toBe('Catalog /catalog and fallback [redacted]');
    });

    it('preserves diagnostic scalar values while masking private keys and bounding nested structures', () => {
        const latencyMs = 3;
        const details = {
            retried: true,
            attempts: 2,
            status: null,
            cancelled: undefined,
            account: { email: 'synthetic@example.test', optional: undefined },
            events: [{ url: 'https://diagnostics.example.test/progress?note=sample', state: { ready: true } }, false, latencyMs, null],
        };

        const redacted = redactTelemetryDetails(details);

        expect(redacted).toEqual({
            retried: true,
            attempts: 2,
            status: null,
            account: { email: '[redacted]' },
            events: [{ url: '/progress', state: { ready: '[truncated]' } }, false, latencyMs, null],
        });
        expect(JSON.stringify(redacted)).not.toMatch(/cancelled|optional|synthetic@example\.test|note=sample/u);
    });

    it('shares the field budget across records and arrays', () => {
        const perArrayLimit = 16;
        const remainingFieldBudget = 5;
        const sequence = Array.from({ length: perArrayLimit }, (_, index) => index);

        expect(redactTelemetryDetails({ first: sequence, second: sequence, third: sequence, later: 'not retained' })).toEqual({
            first: sequence,
            second: sequence,
            third: sequence.slice(0, remainingFieldBudget),
        });
    });

    it('drops details whose escaped JSON exceeds the transport limit', () => {
        const detailCharacterLimit = 512;
        const escaped = '\n'.repeat(detailCharacterLimit);

        expect(redactTelemetryDetails({ blocks: [escaped, escaped, escaped, escaped] })).toEqual({ truncated: true });
    });
});
