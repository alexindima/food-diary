import { PERCENT_MULTIPLIER as PERCENT_MAX } from '../../../shared/lib/nutrition.constants';
import type { NutrientBar } from './dashboard-summary-card.types';

const COLOR_FALLBACK = '#5aa9fa';
const WEEK_DAYS = 7;
const PROGRESS_CLAMP_MAX = 120;
const BLEND_HALF_WIDTH = 5;
const COLOR_SHORT_HEX_LENGTH = 3;
const COLOR_HEX_RADIX = 16;
const COLOR_RED_SHIFT = 16;
const COLOR_GREEN_SHIFT = 8;
const COLOR_BYTE_MASK = 0xff;
const RGB_CHANNEL_COUNT = 3;
const WHITE_CHANNEL = 255;
const DEFAULT_PROTEIN_CURRENT = 110;
const DEFAULT_PROTEIN_TARGET = 140;
const DEFAULT_CARBS_CURRENT = 180;
const DEFAULT_CARBS_TARGET = 250;
const DEFAULT_FATS_CURRENT = 45;
const DEFAULT_FATS_TARGET = 70;
const DEFAULT_FIBER_CURRENT = 18;
const DEFAULT_FIBER_TARGET = 30;
const CSS_VARIABLE_FUNCTION_PREFIX = 'var(';

const CSS_COLOR_VALUES: Partial<Record<string, string>> = {
    '--fd-color-sky-500': '#0ea5e9',
    '--fd-color-blue-500': '#3b82f6',
    '--fd-color-emerald-500': '#10b981',
    '--fd-color-green-500': '#22c55e',
    '--fd-color-emerald-700': '#047857',
    '--fd-color-amber-500': '#f59e0b',
    '--fd-color-orange-500': '#f97316',
    '--fd-color-danger': '#ef4444',
};

const COLOR_STOPS = [
    { percent: 0, color: 'var(--fd-color-sky-500)' },
    { percent: 50, color: 'var(--fd-color-blue-500)' },
    { percent: 70, color: 'var(--fd-color-blue-500)' },
    { percent: 80, color: 'var(--fd-color-emerald-500)' },
    { percent: 90, color: 'var(--fd-color-green-500)' },
    { percent: 100, color: 'var(--fd-color-emerald-700)' },
    { percent: 110, color: 'var(--fd-color-amber-500)' },
    { percent: 120, color: 'var(--fd-color-orange-500)' },
    { percent: 130, color: 'var(--fd-color-danger)' },
] as const;

export function normalizeDailyGoal(dailyGoal: number): number {
    return Math.max(dailyGoal, 0);
}

export function normalizeWeeklyGoal(weeklyGoal: number | null, normalizedDailyGoalValue: number): number {
    if (weeklyGoal !== null && weeklyGoal > 0) {
        return weeklyGoal;
    }

    return normalizedDailyGoalValue > 0 ? normalizedDailyGoalValue * WEEK_DAYS : 0;
}

export function calculateDashboardPercent(value: number, goal: number): number {
    if (goal <= 0) {
        return 0;
    }

    const normalized = Math.max(value, 0);
    return Math.round((normalized / goal) * PERCENT_MAX);
}

export function buildRingDasharray(percent: number, radius: number): string {
    const circumference = 2 * Math.PI * radius;
    const clamped = Math.min(Math.max(percent, 0), PERCENT_MAX);
    const filled = (circumference * clamped) / PERCENT_MAX;
    return `${filled} ${circumference}`;
}

export function clampDashboardPercent(value: number): number {
    if (Number.isNaN(value)) {
        return 0;
    }

    return Math.min(Math.max(value, 0), PROGRESS_CLAMP_MAX);
}

export function getDashboardColorForPercent(percent: number, colorCache = new Map<string, [number, number, number]>()): string {
    const clamped = Math.max(percent, 0);

    if (clamped <= COLOR_STOPS[0].percent) {
        return COLOR_STOPS[0].color;
    }
    if (clamped >= COLOR_STOPS[COLOR_STOPS.length - 1].percent) {
        return COLOR_STOPS[COLOR_STOPS.length - 1].color;
    }

    for (let i = 1; i < COLOR_STOPS.length; i += 1) {
        const previous = COLOR_STOPS[i - 1];
        const current = COLOR_STOPS[i];
        const gap = current.percent - previous.percent;
        const half = Math.min(BLEND_HALF_WIDTH, gap / 2);
        const blendStart = current.percent - half;
        const blendEnd = current.percent + half;

        if (clamped < blendStart) {
            return previous.color;
        }

        if (clamped <= blendEnd) {
            const ratio = half > 0 ? (clamped - blendStart) / (2 * half) : 1;
            return lerpColor(previous.color, current.color, ratio, colorCache);
        }
    }

    return COLOR_STOPS[COLOR_STOPS.length - 1].color;
}

export function mixDashboardColorWithWhite(color: string, ratio: number, colorCache = new Map<string, [number, number, number]>()): string {
    const [red, green, blue] = parseColor(color, colorCache);
    const mix = (channel: number): number => Math.round(channel + (WHITE_CHANNEL - channel) * ratio);
    return `#${toHex(mix(red))}${toHex(mix(green))}${toHex(mix(blue))}`;
}

export function getDashboardNutrientBarColor(bar: NutrientBar, colorCache = new Map<string, [number, number, number]>()): string {
    if (bar.target <= 0) {
        return 'var(--fd-color-gray-500-static)';
    }

    return getDashboardColorForPercent((bar.current / bar.target) * PERCENT_MAX, colorCache);
}

export function buildDefaultDashboardNutrientBars(): NutrientBar[] {
    return [
        {
            id: 'protein',
            label: 'Protein',
            labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
            current: DEFAULT_PROTEIN_CURRENT,
            target: DEFAULT_PROTEIN_TARGET,
            unit: 'g',
            unitKey: 'GENERAL.UNITS.G',
            colorStart: 'var(--fd-gradient-brand-start)',
            colorEnd: 'var(--fd-color-primary-600)',
        },
        {
            id: 'carbs',
            label: 'Carbs',
            labelKey: 'GENERAL.NUTRIENTS.CARB',
            current: DEFAULT_CARBS_CURRENT,
            target: DEFAULT_CARBS_TARGET,
            unit: 'g',
            unitKey: 'GENERAL.UNITS.G',
            colorStart: 'var(--fd-color-teal-500)',
            colorEnd: 'var(--fd-color-sky-500)',
        },
        {
            id: 'fats',
            label: 'Fats',
            labelKey: 'GENERAL.NUTRIENTS.FAT',
            current: DEFAULT_FATS_CURRENT,
            target: DEFAULT_FATS_TARGET,
            unit: 'g',
            unitKey: 'GENERAL.UNITS.G',
            colorStart: 'var(--fd-color-yellow-300)',
            colorEnd: 'var(--fd-color-orange-500)',
        },
        {
            id: 'fiber',
            label: 'Fiber',
            labelKey: 'SHARED.NUTRIENTS_SUMMARY.FIBER',
            current: DEFAULT_FIBER_CURRENT,
            target: DEFAULT_FIBER_TARGET,
            unit: 'g',
            unitKey: 'GENERAL.UNITS.G',
            colorStart: 'var(--fd-color-rose-500)',
            colorEnd: 'var(--fd-color-rose-500)',
        },
    ];
}

function lerpColor(colorA: string, colorB: string, ratio: number, colorCache: Map<string, [number, number, number]>): string {
    const [redA, greenA, blueA] = parseColor(colorA, colorCache);
    const [redB, greenB, blueB] = parseColor(colorB, colorCache);
    const lerp = (start: number, end: number): number => Math.round(start + (end - start) * ratio);

    return `#${toHex(lerp(redA, redB))}${toHex(lerp(greenA, greenB))}${toHex(lerp(blueA, blueB))}`;
}

function hexToChannels(hex: string): [number, number, number] {
    const normalized = hex.replace('#', '');
    const value =
        normalized.length === COLOR_SHORT_HEX_LENGTH
            ? normalized
                  .split('')
                  .map(character => character + character)
                  .join('')
            : normalized;
    const numeric = parseInt(value, COLOR_HEX_RADIX);
    const red = (numeric >> COLOR_RED_SHIFT) & COLOR_BYTE_MASK;
    const green = (numeric >> COLOR_GREEN_SHIFT) & COLOR_BYTE_MASK;
    const blue = numeric & COLOR_BYTE_MASK;
    return [red, green, blue];
}

function toHex(value: number): string {
    return value.toString(COLOR_HEX_RADIX).padStart(2, '0');
}

function parseColor(value: string, colorCache: Map<string, [number, number, number]>): [number, number, number] {
    const cached = colorCache.get(value);
    if (cached !== undefined) {
        return cached;
    }

    let channels: [number, number, number] | null = null;

    if (value.startsWith('#')) {
        channels = hexToChannels(value);
    } else {
        const cssVariable = parseCssVariableSyntax(value.trim());
        if (cssVariable !== null) {
            channels = parseCssVariable(cssVariable.variableName, cssVariable.fallback, colorCache);
        } else {
            channels = parseRgbChannels(value);
        }
    }

    const resolved = channels ?? hexToChannels(COLOR_FALLBACK);
    colorCache.set(value, resolved);
    return resolved;
}

function parseCssVariableSyntax(value: string): { variableName: string; fallback?: string } | null {
    if (!value.startsWith(CSS_VARIABLE_FUNCTION_PREFIX) || !value.endsWith(')')) {
        return null;
    }

    const content = value.slice(CSS_VARIABLE_FUNCTION_PREFIX.length, -1).trim();
    const commaIndex = content.indexOf(',');
    const rawVariableName = commaIndex === -1 ? content : content.slice(0, commaIndex);
    const variableName = rawVariableName.trim();
    if (!isValidCssVariableName(variableName)) {
        return null;
    }

    const fallback = commaIndex === -1 ? undefined : content.slice(commaIndex + 1).trim();
    return fallback === undefined || fallback.length === 0 ? { variableName } : { variableName, fallback };
}

function isValidCssVariableName(value: string): boolean {
    if (!value.startsWith('--') || value.length <= 2) {
        return false;
    }

    for (const character of value) {
        if (character.trim().length === 0 || character === ')') {
            return false;
        }
    }

    return true;
}

function parseCssVariable(
    variableName: string,
    fallback: string | undefined,
    colorCache: Map<string, [number, number, number]>,
): [number, number, number] | null {
    const colorValue = CSS_COLOR_VALUES[variableName];
    if (colorValue !== undefined) {
        return parseColor(colorValue, colorCache);
    }

    return fallback !== undefined && fallback.length > 0 ? parseColor(fallback.trim(), colorCache) : null;
}

function parseRgbChannels(value: string): [number, number, number] | null {
    const channels = value.match(/\d+/g)?.slice(0, RGB_CHANNEL_COUNT).map(Number);
    if (channels?.length === RGB_CHANNEL_COUNT) {
        return [channels[0], channels[1], channels[2]];
    }

    return null;
}
