import { centimetersToInches, kilogramsToPounds, type MeasurementSystem } from '../../../shared/measurements/measurement-system.service';
import type { WeekTrend } from '../models/weekly-check-in.data';
import type {
    WeeklyCheckInSuggestionViewModel,
    WeeklyCheckInTrendCardConfig,
    WeeklyCheckInTrendCardViewModel,
} from './weekly-check-in.types';

const TREND_ICON_UP = 'trending_up';
const TREND_ICON_DOWN = 'trending_down';
const TREND_ICON_FLAT = 'trending_flat';
const TREND_COLOR_NEUTRAL = 'var(--fd-color-slate-500)';
const TREND_COLOR_POSITIVE = 'var(--fd-color-green-500)';
const TREND_COLOR_NEGATIVE = 'var(--fd-color-danger)';
const BODY_TREND_INSERT_INDEX = 2;

export function buildWeeklyCheckInSuggestionRows(suggestions: string[]): WeeklyCheckInSuggestionViewModel[] {
    return suggestions.map(suggestion => ({
        key: suggestion,
        labelKey: `WEEKLY_CHECK_IN.${suggestion}`,
    }));
}

export function buildWeeklyCheckInTrendCards(
    trends: WeekTrend | null | undefined,
    measurementSystem: MeasurementSystem = 'metric',
): WeeklyCheckInTrendCardViewModel[] {
    if (trends === null || trends === undefined) {
        return [];
    }

    const cards: WeeklyCheckInTrendCardViewModel[] = [
        buildWeeklyCheckInTrendCard({
            key: 'calories',
            labelKey: 'WEEKLY_CHECK_IN.CALORIES',
            value: trends.calorieChange,
            unitKey: 'GENERAL.UNITS.KCAL',
            numberFormat: '1.0-0',
        }),
        buildWeeklyCheckInTrendCard({
            key: 'protein',
            labelKey: 'WEEKLY_CHECK_IN.PROTEIN',
            value: trends.proteinChange,
            unitKey: 'GENERAL.UNITS.G',
            numberFormat: '1.1-1',
        }),
        buildWeeklyCheckInTrendCard({
            key: 'hydration',
            labelKey: 'WEEKLY_CHECK_IN.HYDRATION',
            value: trends.hydrationChange,
            unitKey: 'GENERAL.UNITS.ML',
            numberFormat: '1.0-0',
        }),
    ];

    if (trends.weightChange !== null) {
        cards.splice(
            BODY_TREND_INSERT_INDEX,
            0,
            buildWeeklyCheckInTrendCard({
                key: 'weight',
                labelKey: 'WEEKLY_CHECK_IN.WEIGHT',
                value: displayWeight(trends.weightChange, measurementSystem),
                unitKey: weightUnitKey(measurementSystem),
                numberFormat: '1.1-1',
                invertPositive: true,
            }),
        );
    }

    if (trends.waistChange !== null) {
        cards.splice(
            trends.weightChange !== null ? BODY_TREND_INSERT_INDEX + 1 : BODY_TREND_INSERT_INDEX,
            0,
            buildWeeklyCheckInTrendCard({
                key: 'waist',
                labelKey: 'WEEKLY_CHECK_IN.WAIST',
                value: displayLength(trends.waistChange, measurementSystem),
                unitKey: lengthUnitKey(measurementSystem),
                numberFormat: '1.1-1',
                invertPositive: true,
            }),
        );
    }

    return cards;
}

export function buildWeeklyCheckInTrendCard(config: WeeklyCheckInTrendCardConfig): WeeklyCheckInTrendCardViewModel {
    const { key, labelKey, value, unitKey, numberFormat, invertPositive = false, unitSeparator = ' ' } = config;
    return {
        key,
        labelKey,
        value,
        unitKey,
        unitSeparator,
        numberFormat,
        valuePrefix: value > 0 ? '+' : '',
        color: getWeeklyCheckInTrendColor(value, invertPositive),
        icon: getWeeklyCheckInTrendIcon(value),
    };
}

export function getWeeklyCheckInTrendIcon(value: number): string {
    if (value > 0) {
        return TREND_ICON_UP;
    }

    if (value < 0) {
        return TREND_ICON_DOWN;
    }

    return TREND_ICON_FLAT;
}

export function getWeeklyCheckInTrendColor(value: number, invertPositive = false): string {
    if (value === 0) {
        return TREND_COLOR_NEUTRAL;
    }

    const isPositive = invertPositive ? value < 0 : value > 0;
    return isPositive ? TREND_COLOR_POSITIVE : TREND_COLOR_NEGATIVE;
}

function displayWeight(weightKg: number, system: MeasurementSystem): number {
    return system === 'imperial' ? kilogramsToPounds(weightKg) : weightKg;
}

function displayLength(lengthCm: number, system: MeasurementSystem): number {
    return system === 'imperial' ? centimetersToInches(lengthCm) : lengthCm;
}

function weightUnitKey(system: MeasurementSystem): string {
    return system === 'imperial' ? 'GENERAL.UNITS.LB' : 'GENERAL.UNITS.KG';
}

function lengthUnitKey(system: MeasurementSystem): string {
    return system === 'imperial' ? 'GENERAL.UNITS.IN' : 'GENERAL.UNITS.CM';
}
