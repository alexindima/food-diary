import { PERCENT_MULTIPLIER as PERCENT_MAX } from '../../../shared/lib/nutrition.constants';

const HALF_PERCENT = 50;
const DOUBLE_PERCENT = 200;
const WARNING_PROGRESS_MAX = 125;
const WARNING_PROGRESS_RANGE = 25;
const DANGER_PROGRESS_RANGE = 50;
const TEXT_DARK_THRESHOLD_FACTOR = 0.5;
const COLOR_CHANNEL_MIN = 0;
const COLOR_CHANNEL_MAX = 255;
const COLOR_HEX_RADIX = 16;
const COLOR_HEX_PAD_LENGTH = 2;
const GREEN_BASE_CHANNEL = 80;
const GREEN_START_CHANNEL = 150;
const ORANGE_START_CHANNEL = 200;
const RED_START_CHANNEL = 100;

export function calculateProgressPercent(current: number, max: number): number {
    if (max <= 0) {
        return 0;
    }

    return Math.round((current / max) * PERCENT_MAX);
}

export function calculateProgressBarWidth(progress: number): string {
    return `${Math.min(progress, PERCENT_MAX)}%`;
}

export function calculateMaxPosition(current: number, max: number): number {
    if (max <= 0 || current <= 0) {
        return PERCENT_MAX;
    }

    return current > max ? (max / current) * PERCENT_MAX : PERCENT_MAX;
}

export function calculateTextPosition(progress: number, current: number, max: number, maxPosition: number): string {
    let position: number;
    if (max <= 0) {
        position = HALF_PERCENT;
    } else if (progress < HALF_PERCENT) {
        position = PERCENT_MAX - (PERCENT_MAX - progress) / 2;
    } else if (progress > DOUBLE_PERCENT) {
        position = PERCENT_MAX - (PERCENT_MAX - (max / current) * PERCENT_MAX) / 2;
    } else if (progress > PERCENT_MAX) {
        position = maxPosition / 2;
    } else {
        position = Math.min(progress / 2, HALF_PERCENT);
    }

    return `${position}%`;
}

export function calculateProgressColor(progress: number): string {
    if (progress <= PERCENT_MAX) {
        const greenIntensity = Math.round((progress / PERCENT_MAX) * PERCENT_MAX);
        return toHex([GREEN_BASE_CHANNEL, GREEN_START_CHANNEL + greenIntensity, GREEN_BASE_CHANNEL]);
    }

    if (progress <= WARNING_PROGRESS_MAX) {
        const orangeIntensity = Math.round(((progress - PERCENT_MAX) / WARNING_PROGRESS_RANGE) * PERCENT_MAX);
        return toHex([COLOR_CHANNEL_MAX, ORANGE_START_CHANNEL - orangeIntensity, GREEN_BASE_CHANNEL]);
    }

    const redIntensity = Math.min(
        COLOR_CHANNEL_MAX,
        Math.round(((progress - WARNING_PROGRESS_MAX) / DANGER_PROGRESS_RANGE) * COLOR_CHANNEL_MAX),
    );
    return toHex([COLOR_CHANNEL_MAX, RED_START_CHANNEL - redIntensity, GREEN_BASE_CHANNEL - redIntensity / 2]);
}

export function resolveProgressTextColorClass(progress: number): 'text-black' | 'text-white' {
    return progress < PERCENT_MAX * TEXT_DARK_THRESHOLD_FACTOR ? 'text-black' : 'text-white';
}

function toHex(channels: number[]): string {
    return `#${channels
        .map(channel =>
            Math.max(COLOR_CHANNEL_MIN, Math.min(COLOR_CHANNEL_MAX, Math.round(channel)))
                .toString(COLOR_HEX_RADIX)
                .padStart(COLOR_HEX_PAD_LENGTH, '0'),
        )
        .join('')}`;
}
