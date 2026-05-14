import { describe, expect, it } from 'vitest';

import { buildStatisticsExportRequest } from './statistics-export.mapper';

const TIME_ZONE_OFFSET_MINUTES = 240;

describe('statistics-export.mapper', () => {
    it('builds export request with normalized range and current locale', () => {
        const request = buildStatisticsExportRequest({
            range: {
                start: new Date('2026-05-01T12:00:00'),
                end: new Date('2026-05-07T12:00:00'),
            },
            format: 'pdf',
            currentLang: 'ru',
            fallbackLang: 'en',
            timeZoneOffsetMinutes: TIME_ZONE_OFFSET_MINUTES,
        });

        expect(request.dateFrom).toContain('T');
        expect(request.dateTo).toContain('T');
        expect(request.format).toBe('pdf');
        expect(request.locale).toBe('ru');
        expect(request.timeZoneOffsetMinutes).toBe(TIME_ZONE_OFFSET_MINUTES);
    });

    it('uses fallback locale when current language is empty', () => {
        const request = buildStatisticsExportRequest({
            range: {
                start: new Date('2026-05-01T12:00:00'),
                end: new Date('2026-05-07T12:00:00'),
            },
            format: 'csv',
            currentLang: '',
            fallbackLang: 'en',
            timeZoneOffsetMinutes: TIME_ZONE_OFFSET_MINUTES,
        });

        expect(request.locale).toBe('en');
    });
});
