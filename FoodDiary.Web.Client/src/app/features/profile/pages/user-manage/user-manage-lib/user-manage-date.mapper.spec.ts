import { describe, expect, it } from 'vitest';

import { formatUserManageDate, formatUserManageDateTime } from './user-manage-date.mapper';

describe('user manage date mapper', () => {
    it('should format date values', () => {
        expect(formatUserManageDate('2026-01-02T12:00:00Z', 'en-US')).toContain('2026');
    });

    it('should format date time values', () => {
        expect(formatUserManageDateTime('2026-01-02T12:00:00Z', 'en-US')).toContain('2026');
    });

    it('should return null for empty or invalid values', () => {
        expect(formatUserManageDate('', 'en-US')).toBeNull();
        expect(formatUserManageDate(null, 'en-US')).toBeNull();
        expect(formatUserManageDate(undefined, 'en-US')).toBeNull();
        expect(formatUserManageDate('not-a-date', 'en-US')).toBeNull();
    });
});
