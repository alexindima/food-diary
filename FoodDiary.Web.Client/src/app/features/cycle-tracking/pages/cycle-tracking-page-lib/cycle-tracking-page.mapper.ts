import { formatDateValue } from '../../../../shared/lib/local-date.utils';
import {
    BLEEDING_TYPE_BLEEDING,
    type BleedingEntry,
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FACTOR_TYPE_LACTATION,
    CYCLE_FACTOR_TYPE_NO_PERIOD,
    CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION,
    CYCLE_FACTOR_TYPE_PERIMENOPAUSE,
    CYCLE_FACTOR_TYPE_POSTPARTUM,
    CYCLE_FACTOR_TYPE_PREGNANCY,
    CYCLE_FLOW_HEAVY,
    CYCLE_TRACKING_MODE_NO_PERIOD,
    CYCLE_TRACKING_MODE_PERIMENOPAUSE,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION,
    CYCLE_TRACKING_MODE_PREGNANCY,
    CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    type CycleFactor,
    type CycleFactorType,
    type CyclePredictions,
    type CycleResponse,
    type CycleSymptomEntry,
    type CycleTrackingMode,
    type FertilitySignal,
    OVULATION_TEST_RESULT_NEGATIVE,
    OVULATION_TEST_RESULT_POSITIVE,
} from '../../models/cycle.data';
import { DEFAULT_DAY_ACCENT_COLOR, PERIOD_DAY_ACCENT_COLOR } from './cycle-tracking-page.config';
import type {
    CycleActiveFactorViewModel,
    CycleDayCarePromptViewModel,
    CycleDaySignalItemViewModel,
    CycleDayViewModel,
    CycleFactorListItemViewModel,
    CyclePredictionViewModel,
    CycleSummaryItemViewModel,
    CycleViewModel,
} from './cycle-tracking-page.types';

const FULL_DATE_OPTIONS: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'short', year: 'numeric' };
const SHORT_DATE_OPTIONS: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'short' };
const UTC_TIME_ZONE: Intl.DateTimeFormatOptions['timeZone'] = 'UTC';
const ISO_DATE_KEY_LENGTH = 10;
const MS_PER_DAY = 86_400_000;
const PROLONGED_BLEEDING_DAYS = 8;
const SEVERE_PAIN_THRESHOLD = 8;
const SUMMARY_ACCENTS = [
    'var(--fd-color-purple-500)',
    'var(--fd-color-sky-500)',
    'var(--fd-color-teal-500)',
    'var(--fd-color-green-500)',
    'var(--fd-color-orange-500)',
    'var(--fd-color-primary-500)',
] as const;

export function buildCycleCurrentView(cycle: CycleResponse | null, locale: string): CycleViewModel | null {
    if (cycle === null) {
        return null;
    }

    return {
        cycle,
        trackingStartDateLabel: formatCycleDate(cycle.trackingStartDate, locale, FULL_DATE_OPTIONS),
        summaryItems: buildCycleSummaryItems(cycle, locale),
        activeFactorItems: buildActiveFactorItems(cycle.factors, locale),
    };
}

export function buildCyclePredictionView(prediction: CyclePredictions | null, locale: string): CyclePredictionViewModel | null {
    if (prediction === null) {
        return null;
    }

    const nextPeriodRangeLabel = formatRange(prediction.nextPeriodStartFrom, prediction.nextPeriodStartTo, locale);
    const ovulationRangeLabel = formatRange(prediction.ovulationFrom, prediction.ovulationTo, locale);
    const pmsRangeLabel = formatRange(prediction.pmsWindowStart, prediction.pmsWindowEnd, locale);
    const hasPredictionRanges = [nextPeriodRangeLabel, ovulationRangeLabel, pmsRangeLabel].some(label => label.length > 0);

    return {
        prediction,
        nextPeriodRangeLabel,
        ovulationRangeLabel,
        pmsRangeLabel,
        confidenceLabel: prediction.confidence,
        hasPredictionRanges,
        limitedReasonKey: hasPredictionRanges ? null : 'CYCLE_TRACKING.PREDICTIONS_LIMITED',
    };
}

export function buildCycleDayItems(
    bleedingEntries: BleedingEntry[],
    symptoms: CycleSymptomEntry[],
    fertilitySignals: FertilitySignal[],
    locale: string,
): CycleDayViewModel[] {
    const dates = new Set([
        ...bleedingEntries.map(entry => entry.date),
        ...symptoms.map(symptom => symptom.date),
        ...fertilitySignals.map(signal => signal.date),
    ]);
    const bleedingStreakByDate = buildBleedingStreakByDate(bleedingEntries);
    return [...dates]
        .sort((a, b) => b.localeCompare(a))
        .map(date => {
            const dayBleeding = bleedingEntries.filter(entry => entry.date === date);
            const dateKey = toDateKey(date);
            const fertilitySignal = fertilitySignals.find(signal => signal.date === date) ?? null;
            const hasBleeding = dayBleeding.some(entry => entry.type === BLEEDING_TYPE_BLEEDING);
            return {
                date,
                dateLabel: formatCycleDate(date, locale, FULL_DATE_OPTIONS),
                bleedingEntries: dayBleeding,
                symptoms: symptoms.filter(symptom => symptom.date === date),
                fertilitySignal,
                fertilitySignalItems: buildFertilitySignalItems(fertilitySignal),
                carePromptItems: buildCarePromptItems(dayBleeding, bleedingStreakByDate.get(dateKey) ?? 0),
                notes:
                    dayBleeding.find(entry => entry.notes !== null && entry.notes !== undefined)?.notes ?? fertilitySignal?.notes ?? null,
                accentColor: hasBleeding ? PERIOD_DAY_ACCENT_COLOR : DEFAULT_DAY_ACCENT_COLOR,
                badgeLabelKey: hasBleeding ? 'CYCLE_TRACKING.BADGE_PERIOD' : 'CYCLE_TRACKING.BADGE_TRACKED',
            };
        });
}

function buildCarePromptItems(bleedingEntries: BleedingEntry[], bleedingStreakDays: number): CycleDayCarePromptViewModel[] {
    const prompts: CycleDayCarePromptViewModel[] = [];
    if (
        bleedingEntries.some(
            entry => entry.painImpact !== null && entry.painImpact !== undefined && entry.painImpact >= SEVERE_PAIN_THRESHOLD,
        )
    ) {
        prompts.push({ id: 'severe-pain', textKey: 'CYCLE_TRACKING.CARE_SEVERE_PAIN' });
    }

    if (bleedingEntries.some(entry => entry.flow === CYCLE_FLOW_HEAVY)) {
        prompts.push({ id: 'heavy-flow', textKey: 'CYCLE_TRACKING.CARE_HEAVY_FLOW' });
    }

    if (bleedingStreakDays >= PROLONGED_BLEEDING_DAYS) {
        prompts.push({ id: 'prolonged-bleeding', textKey: 'CYCLE_TRACKING.CARE_PROLONGED_BLEEDING' });
    }

    return prompts;
}

function buildBleedingStreakByDate(bleedingEntries: BleedingEntry[]): Map<string, number> {
    const bleedingDateKeys = [
        ...new Set(bleedingEntries.filter(entry => entry.type === BLEEDING_TYPE_BLEEDING).map(entry => toDateKey(entry.date))),
    ]
        .filter(dateKey => dateKey.length > 0)
        .sort();
    const streakByDate = new Map<string, number>();
    let previousTime: number | null = null;
    let streak = 0;

    for (const dateKey of bleedingDateKeys) {
        const time = Date.parse(`${dateKey}T00:00:00.000Z`);
        if (Number.isNaN(time)) {
            streakByDate.set(dateKey, 1);
            previousTime = null;
            streak = 0;
            continue;
        }

        streak = previousTime !== null && time - previousTime === MS_PER_DAY ? streak + 1 : 1;
        streakByDate.set(dateKey, streak);
        previousTime = time;
    }

    return streakByDate;
}

export function buildCycleFactorItems(factors: CycleFactor[], locale: string): CycleFactorListItemViewModel[] {
    return [...factors]
        .sort((a, b) => b.startDate.localeCompare(a.startDate))
        .map(factor => {
            const isActive = factor.endDate === null || factor.endDate === undefined;
            return {
                id: factor.id,
                labelKey: getFactorLabelKey(factor.type),
                dateRangeLabel: formatFactorDateRange(factor, locale),
                statusLabelKey: isActive ? 'CYCLE_TRACKING.FACTOR_ACTIVE' : 'CYCLE_TRACKING.FACTOR_ENDED',
                isActive,
            };
        });
}

function buildFertilitySignalItems(signal: FertilitySignal | null): CycleDaySignalItemViewModel[] {
    if (signal === null) {
        return [];
    }

    const items: CycleDaySignalItemViewModel[] = [];
    if (signal.basalBodyTemperatureCelsius !== null && signal.basalBodyTemperatureCelsius !== undefined) {
        items.push({
            textKey: 'CYCLE_TRACKING.BBT_SUMMARY',
            params: { value: signal.basalBodyTemperatureCelsius.toFixed(2) },
        });
    }

    if (signal.ovulationTestResult === OVULATION_TEST_RESULT_POSITIVE) {
        items.push({
            textKey: 'CYCLE_TRACKING.OVULATION_TEST_POSITIVE_SUMMARY',
        });
    }

    if (signal.ovulationTestResult === OVULATION_TEST_RESULT_NEGATIVE) {
        items.push({
            textKey: 'CYCLE_TRACKING.OVULATION_TEST_NEGATIVE_SUMMARY',
        });
    }

    if (signal.cervicalFluid !== null && signal.cervicalFluid !== undefined && signal.cervicalFluid.trim().length > 0) {
        items.push({
            textKey: 'CYCLE_TRACKING.CERVICAL_FLUID_SUMMARY',
            params: { value: signal.cervicalFluid },
        });
    }

    if (signal.hadSex === true) {
        items.push({
            textKey: 'CYCLE_TRACKING.HAD_SEX',
        });
    }

    return items;
}

function buildCycleSummaryItems(cycle: CycleResponse, locale: string): CycleSummaryItemViewModel[] {
    return [
        {
            labelKey: 'CYCLE_TRACKING.STARTED',
            valueKey: 'CYCLE_TRACKING.STARTED_SUMMARY',
            params: { value: formatCycleDate(cycle.trackingStartDate, locale, FULL_DATE_OPTIONS) },
            accentColor: SUMMARY_ACCENTS[0],
        },
        {
            labelKey: 'CYCLE_TRACKING.MODE',
            valueKey: getModeLabelKey(cycle.mode),
            accentColor: SUMMARY_ACCENTS[1],
        },
        {
            labelKey: 'CYCLE_TRACKING.AVG_LENGTH',
            valueKey: 'CYCLE_TRACKING.LENGTH_DAYS_SUMMARY',
            params: { value: cycle.averageCycleLength },
            accentColor: SUMMARY_ACCENTS[2],
        },
        {
            labelKey: 'CYCLE_TRACKING.LUTEAL_LENGTH',
            valueKey: 'CYCLE_TRACKING.LENGTH_DAYS_SUMMARY',
            params: { value: cycle.lutealLength },
            accentColor: SUMMARY_ACCENTS[3],
        },
        {
            labelKey: 'CYCLE_TRACKING.REGULARITY',
            valueKey: cycle.isRegular ? 'CYCLE_TRACKING.REGULARITY_REGULAR' : 'CYCLE_TRACKING.REGULARITY_IRREGULAR',
            accentColor: SUMMARY_ACCENTS[4],
        },
        {
            labelKey: 'CYCLE_TRACKING.FERTILITY_ESTIMATES',
            valueKey: cycle.showFertilityEstimates ? 'CYCLE_TRACKING.ENABLED' : 'CYCLE_TRACKING.DISABLED',
            accentColor: SUMMARY_ACCENTS[5],
        },
    ];
}

function buildActiveFactorItems(factors: CycleFactor[], locale: string): CycleActiveFactorViewModel[] {
    return factors
        .filter(factor => factor.endDate === null || factor.endDate === undefined)
        .sort((a, b) => b.startDate.localeCompare(a.startDate))
        .map(factor => ({
            id: factor.id,
            labelKey: getFactorLabelKey(factor.type),
            startDateLabel: formatCycleDate(factor.startDate, locale, SHORT_DATE_OPTIONS, UTC_TIME_ZONE),
        }));
}

function formatFactorDateRange(factor: CycleFactor, locale: string): string {
    return formatRange(factor.startDate, factor.endDate, locale);
}

function getModeLabelKey(mode: CycleTrackingMode): string {
    switch (mode) {
        case CYCLE_TRACKING_MODE_PERIOD_TRACKING: {
            return 'CYCLE_TRACKING.MODE_PERIOD_TRACKING';
        }
        case CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE: {
            return 'CYCLE_TRACKING.MODE_TRYING_TO_CONCEIVE';
        }
        case CYCLE_TRACKING_MODE_PREGNANCY: {
            return 'CYCLE_TRACKING.MODE_PREGNANCY';
        }
        case CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION: {
            return 'CYCLE_TRACKING.MODE_POSTPARTUM_LACTATION';
        }
        case CYCLE_TRACKING_MODE_PERIMENOPAUSE: {
            return 'CYCLE_TRACKING.MODE_PERIMENOPAUSE';
        }
        case CYCLE_TRACKING_MODE_NO_PERIOD: {
            return 'CYCLE_TRACKING.MODE_NO_PERIOD';
        }
    }
}

function getFactorLabelKey(type: CycleFactorType): string {
    switch (type) {
        case CYCLE_FACTOR_TYPE_PREGNANCY: {
            return 'CYCLE_TRACKING.FACTOR_PREGNANCY';
        }
        case CYCLE_FACTOR_TYPE_LACTATION: {
            return 'CYCLE_TRACKING.FACTOR_LACTATION';
        }
        case CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION: {
            return 'CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION';
        }
        case CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION: {
            return 'CYCLE_TRACKING.FACTOR_NON_HORMONAL_CONTRACEPTION';
        }
        case CYCLE_FACTOR_TYPE_POSTPARTUM: {
            return 'CYCLE_TRACKING.FACTOR_POSTPARTUM';
        }
        case CYCLE_FACTOR_TYPE_PERIMENOPAUSE: {
            return 'CYCLE_TRACKING.FACTOR_PERIMENOPAUSE';
        }
        case CYCLE_FACTOR_TYPE_NO_PERIOD: {
            return 'CYCLE_TRACKING.FACTOR_NO_PERIOD';
        }
    }
}

function toDateKey(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return value;
    }

    return date.toISOString().slice(0, ISO_DATE_KEY_LENGTH);
}

function formatRange(from: string | null | undefined, to: string | null | undefined, locale: string): string {
    const fromLabel = formatCycleDate(from, locale, SHORT_DATE_OPTIONS, UTC_TIME_ZONE);
    const toLabel = formatCycleDate(to, locale, SHORT_DATE_OPTIONS, UTC_TIME_ZONE);
    if (fromLabel.length === 0) {
        return toLabel;
    }

    if (toLabel.length === 0 || fromLabel === toLabel) {
        return fromLabel;
    }

    return `${fromLabel} - ${toLabel}`;
}

function formatCycleDate(
    value: string | null | undefined,
    locale: string,
    options: Intl.DateTimeFormatOptions,
    timeZone?: Intl.DateTimeFormatOptions['timeZone'],
): string {
    return (
        formatDateValue(value, locale, {
            ...options,
            timeZone,
        }) ??
        value ??
        ''
    );
}
