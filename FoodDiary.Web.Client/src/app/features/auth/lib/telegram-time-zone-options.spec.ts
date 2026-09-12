import { describe, expect, it } from 'vitest';

import { telegramTimeZoneOptions } from './telegram-time-zone-options';

describe('telegramTimeZoneOptions', () => {
    it('keeps the detected zone selectable and displays its UTC offset', () => {
        const options = telegramTimeZoneOptions('Asia/Tbilisi', new Date('2026-01-15T12:00:00Z'));
        expect(options.find(option => option.value === 'Asia/Tbilisi')?.label).toContain('UTC+04:00');
        expect(options.filter(option => option.value === 'Asia/Tbilisi')).toHaveLength(1);
        expect(options.some(option => option.value === 'UTC')).toBe(true);
    });

    it('uses the offset for the supplied date, including daylight saving time', () => {
        const winter = telegramTimeZoneOptions('Europe/Berlin', new Date('2026-01-15T12:00:00Z'));
        const summer = telegramTimeZoneOptions('Europe/Berlin', new Date('2026-07-15T12:00:00Z'));
        expect(winter.find(option => option.value === 'Europe/Berlin')?.label).toContain('UTC+01:00');
        expect(summer.find(option => option.value === 'Europe/Berlin')?.label).toContain('UTC+02:00');
    });

    it('preserves fractional offsets and excludes invalid zone identifiers', () => {
        const options = telegramTimeZoneOptions('Asia/Kathmandu', new Date('2026-01-15T12:00:00Z'));
        expect(options.find(option => option.value === 'Asia/Kathmandu')?.label).toContain('UTC+05:45');
        expect(telegramTimeZoneOptions('Invalid/Zone', new Date()).some(option => option.value === 'Invalid/Zone')).toBe(false);
    });
});
