import { describe, expect, it } from 'vitest';

import { buildRealtimeHubUrl } from './realtime-hub-url.utils';

describe('buildRealtimeHubUrl', () => {
    it('should build a hub URL from a versioned auth API base URL', () => {
        expect(buildRealtimeHubUrl('https://fooddiary.club/api/v1/auth', '/hubs/email-verification')).toBe(
            'https://fooddiary.club/hubs/email-verification',
        );
    });

    it('should build a hub URL from a dotted version auth API base URL', () => {
        expect(buildRealtimeHubUrl('https://fooddiary.club/api/v1.2/auth', 'hubs/notifications')).toBe(
            'https://fooddiary.club/hubs/notifications',
        );
    });

    it('should build a hub URL from a non-versioned auth API base URL', () => {
        expect(buildRealtimeHubUrl('http://localhost:5300/api/auth', '/hubs/notifications')).toBe(
            'http://localhost:5300/hubs/notifications',
        );
    });

    it('should ignore trailing slashes in auth API base URL', () => {
        expect(buildRealtimeHubUrl('http://localhost:5300/api/auth/', '/hubs/notifications')).toBe(
            'http://localhost:5300/hubs/notifications',
        );
    });

    it('should append the hub path when auth API suffix is absent', () => {
        expect(buildRealtimeHubUrl('http://localhost:5300', '/hubs/notifications')).toBe('http://localhost:5300/hubs/notifications');
    });

    it('preserves the deployment prefix when stripping a versioned auth API suffix', () => {
        expect(buildRealtimeHubUrl('http://localhost:5300/preview/api/v1.2/auth', '/hubs/notifications')).toBe(
            'http://localhost:5300/preview/hubs/notifications',
        );
    });

    it.each(['/api/v.1/auth', '/preview/auth', '/api/views/auth'])('preserves an auth path without a numeric API version: %s', path => {
        const base = `http://localhost:5300${path}`;

        expect(buildRealtimeHubUrl(base, '/hubs/notifications')).toBe(`${base}/hubs/notifications`);
    });
});
