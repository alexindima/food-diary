import type { ParamMap } from '@angular/router';

import { ADMIN_DATE_TEXT_LENGTH } from './admin-query';

const DAY_MS = 86_400_000;
const MAX_RANGE_DAYS = 3660;
const PRESET_DAYS: Readonly<Partial<Record<string, number>>> = { '7d': 7, '30d': 30 };
export type AdminPeriod = { from?: string; to?: string };

export function adminExclusiveDatePeriod(range: AdminPeriod, now = new Date()): { from: string; to: string } {
    const lastDay = range.to ?? now.toISOString().slice(0, ADMIN_DATE_TEXT_LENGTH);
    return {
        from: range.from ?? '1970-01-01',
        to: new Date(Date.parse(`${lastDay}T00:00:00Z`) + DAY_MS).toISOString().slice(0, ADMIN_DATE_TEXT_LENGTH),
    };
}

export function adminUtcPeriod(range: AdminPeriod): Record<string, string> {
    return range.from !== undefined && range.to !== undefined
        ? {
              fromUtc: `${range.from}T00:00:00Z`,
              toUtc: new Date(Date.parse(`${range.to}T00:00:00Z`) + DAY_MS).toISOString(),
          }
        : {};
}

export function validAdminDate(value: string | null): value is string {
    return (
        value !== null &&
        /^\d{4}-\d{2}-\d{2}$/.test(value) &&
        Number.isFinite(Date.parse(value)) &&
        new Date(value).toISOString().slice(0, ADMIN_DATE_TEXT_LENGTH) === value
    );
}

function customPeriod(params: ParamMap, today: string): AdminPeriod | null {
    const from = params.get('from');
    const to = params.get('to');
    if (!validAdminDate(from) || !validAdminDate(to)) {
        return null;
    }
    return from >= '1970-01-01' && from <= to && to <= today && Date.parse(to) - Date.parse(from) <= MAX_RANGE_DAYS * DAY_MS
        ? { from, to }
        : null;
}

export function adminPeriod(params: ParamMap, defaultPeriod = 'all', now = new Date()): AdminPeriod | null {
    const period = params.get('period') ?? defaultPeriod;
    if (period === 'all') {
        return {};
    }
    const today = now.toISOString().slice(0, ADMIN_DATE_TEXT_LENGTH);
    if (period === 'custom') {
        return customPeriod(params, today);
    }
    const start = new Date(`${today}T00:00:00Z`);
    if (period === 'month') {
        start.setUTCDate(1);
    } else if (PRESET_DAYS[period] !== undefined) {
        start.setUTCDate(start.getUTCDate() - PRESET_DAYS[period] + 1);
    } else if (period !== 'today') {
        return null;
    }
    return { from: start.toISOString().slice(0, ADMIN_DATE_TEXT_LENGTH), to: today };
}
