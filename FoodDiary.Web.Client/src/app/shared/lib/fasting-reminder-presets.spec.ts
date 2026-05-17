import { describe, expect, it } from 'vitest';

import { resolveFastingReminderPresetId } from './fasting-reminder-presets';

const STEADY_FIRST_REMINDER_HOURS = 16;
const STEADY_FOLLOW_UP_REMINDER_HOURS = 24;
const CUSTOM_FIRST_REMINDER_HOURS = 13;
const CUSTOM_FOLLOW_UP_REMINDER_HOURS = 21;

describe('fasting reminder presets', () => {
    it('resolves known preset id by reminder hours', () => {
        expect(resolveFastingReminderPresetId(STEADY_FIRST_REMINDER_HOURS, STEADY_FOLLOW_UP_REMINDER_HOURS)).toBe('steady');
    });

    it('returns custom for non-preset reminder hours', () => {
        expect(resolveFastingReminderPresetId(CUSTOM_FIRST_REMINDER_HOURS, CUSTOM_FOLLOW_UP_REMINDER_HOURS)).toBe('custom');
    });
});
