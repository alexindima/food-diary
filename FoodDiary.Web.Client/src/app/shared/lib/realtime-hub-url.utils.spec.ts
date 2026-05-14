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

    it('should keep auth API suffix when version segment is invalid', () => {
        expect(buildRealtimeHubUrl('http://localhost:5300/api/v.1/auth', '/hubs/notifications')).toBe(
            'http://localhost:5300/api/v.1/auth/hubs/notifications',
        );
    });
});
