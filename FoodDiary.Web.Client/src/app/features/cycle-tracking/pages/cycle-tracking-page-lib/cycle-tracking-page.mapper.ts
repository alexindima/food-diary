import { formatDateValue } from '../../../../shared/lib/local-date.utils';
import { CYCLE_SYMPTOM_FIELDS } from '../../lib/cycle-tracking.config';
import {
    BLEEDING_TYPE_BLEEDING,
    BLEEDING_TYPE_SPOTTING,
    type BleedingEntry,
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FACTOR_TYPE_LACTATION,
    CYCLE_FACTOR_TYPE_NO_PERIOD,
    CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION,
    CYCLE_FACTOR_TYPE_PERIMENOPAUSE,
    CYCLE_FACTOR_TYPE_POSTPARTUM,
    CYCLE_FACTOR_TYPE_PREGNANCY,
    CYCLE_FLOW_HEAVY,
    CYCLE_FLOW_LIGHT,
    CYCLE_FLOW_MEDIUM,
    CYCLE_FLOW_NONE,
    CYCLE_TRACKING_MODE_NO_PERIOD,
    CYCLE_TRACKING_MODE_PERIMENOPAUSE,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION,
    CYCLE_TRACKING_MODE_PREGNANCY,
    CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    type CycleFactor,
    type CycleFactorType,
    type CycleNutritionSummary,
    type CyclePredictions,
    type CycleResponse,
    type CycleSymptomEntry,
    type CycleTrackingMode,
    type FertilitySignal,
    MENSTRUAL_EPISODE_STATUS_CONFIRMED,
    type MenstrualEpisode,
    OVULATION_TEST_RESULT_NEGATIVE,
    OVULATION_TEST_RESULT_POSITIVE,
} from '../../models/cycle.data';
import { DEFAULT_DAY_ACCENT_COLOR, PERIOD_DAY_ACCENT_COLOR } from './cycle-tracking-page.config';
import type {
    CycleActiveFactorViewModel,
    CycleDayBleedingSummaryViewModel,
    CycleDayCarePromptViewModel,
    CycleDaySignalItemViewModel,
    CycleDaySymptomSummaryViewModel,
    CycleDayViewModel,
    CycleFactorListItemViewModel,
    CycleNutritionSummaryViewModel,
    CycleObservationsViewModel,
    CycleOverviewDayViewModel,
    CycleOverviewViewModel,
    CyclePredictionViewModel,
    CycleSummaryItemViewModel,
    CycleViewModel,
} from './cycle-tracking-page.types';

const FULL_DATE_OPTIONS: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'short', year: 'numeric' };
const SHORT_DATE_OPTIONS: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'short' };
const UTC_TIME_ZONE: Intl.DateTimeFormatOptions['timeZone'] = 'UTC';
const ISO_DATE_KEY_LENGTH = 10;
const MS_PER_DAY = 86_400_000;
const OVERVIEW_DAY_RADIUS = 5;
const PROLONGED_BLEEDING_DAYS = 8;
const SEVERE_PAIN_THRESHOLD = 8;
const OBSERVATION_MINIMUM_TRACKED_DAYS = 3;
const HISTORY_SYMPTOM_LIMIT = 4;
const MILD_MAX_INTENSITY = 3;
const MODERATE_MAX_INTENSITY = 6;
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

export function buildCycleOverviewView(cycle: CycleResponse | null, locale: string, today = new Date()): CycleOverviewViewModel | null {
    if (cycle === null) {
        return null;
    }

    const normalizedToday = new Date(today.getFullYear(), today.getMonth(), today.getDate());
    const todayDateKey = toLocalDateKey(normalizedToday);
    const cycleStartDateKey = resolveCycleStartDateKey(cycle, todayDateKey);
    const trackedDateKeys = new Set([
        ...cycle.bleedingEntries.map(entry => toDateKey(entry.date)),
        ...cycle.symptoms.map(entry => toDateKey(entry.date)),
        ...cycle.fertilitySignals.map(entry => toDateKey(entry.date)),
    ]);
    const bleedingDateKeys = new Set(cycle.bleedingEntries.map(entry => toDateKey(entry.date)));
    const days: CycleOverviewDayViewModel[] = [];

    for (let offset = -OVERVIEW_DAY_RADIUS; offset <= OVERVIEW_DAY_RADIUS; offset += 1) {
        const date = new Date(normalizedToday);
        date.setDate(date.getDate() + offset);
        const dateKey = toLocalDateKey(date);
        days.push({
            dateKey,
            weekdayLabel: new Intl.DateTimeFormat(locale, { weekday: 'short' }).format(date),
            dayLabel: new Intl.DateTimeFormat(locale, { day: 'numeric' }).format(date),
            cycleDayNumber: calculateCycleDayNumber(cycleStartDateKey, dateKey, cycle.averageCycleLength),
            isToday: offset === 0,
            isFuture: offset > 0,
            isBleeding: bleedingDateKeys.has(dateKey),
            isPredictedPeriod: isDateInRange(dateKey, cycle.predictions?.nextPeriodStartFrom, cycle.predictions?.nextPeriodStartTo),
            isTracked: trackedDateKeys.has(dateKey),
        });
    }

    return {
        todayDateKey,
        todayDateLabel: new Intl.DateTimeFormat(locale, { weekday: 'long', day: 'numeric', month: 'long' }).format(normalizedToday),
        monthLabel: new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric' }).format(normalizedToday),
        cycleDayNumber: calculateCycleDayNumber(cycleStartDateKey, todayDateKey, cycle.averageCycleLength),
        hasTodayEntry: trackedDateKeys.has(todayDateKey),
        days,
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
        confidenceLabel: prediction.dataSufficiency ?? prediction.confidence,
        dataSufficiencyKey: getDataSufficiencyKey(prediction.dataSufficiency),
        completedCycleCount: prediction.completedCycleCount ?? 0,
        usedEpisodeCount: prediction.usedEpisodeCount ?? 0,
        excludedEpisodeCount: prediction.excludedEpisodeCount ?? 0,
        explanationKey: 'CYCLE_TRACKING.PREDICTION_EXPLANATION',
        hasPredictionRanges,
        limitedReasonKey: hasPredictionRanges
            ? null
            : prediction.reasonCodes?.some(code => code === 'insufficient_completed_cycles' || code === 'ambiguous_episode_history') ===
                true
              ? 'CYCLE_TRACKING.PREDICTIONS_LEARNING'
              : 'CYCLE_TRACKING.PREDICTIONS_LIMITED',
    };
}

function getDataSufficiencyKey(value: string | undefined): string {
    switch (value) {
        case 'Established': {
            return 'CYCLE_TRACKING.SUFFICIENCY_ESTABLISHED';
        }
        case 'Limited': {
            return 'CYCLE_TRACKING.SUFFICIENCY_LIMITED';
        }
        case 'Insufficient': {
            return 'CYCLE_TRACKING.SUFFICIENCY_INSUFFICIENT';
        }
        case undefined: {
            return 'CYCLE_TRACKING.SUFFICIENCY_UNAVAILABLE';
        }
        default: {
            return 'CYCLE_TRACKING.SUFFICIENCY_UNAVAILABLE';
        }
    }
}

export function buildCycleNutritionSummaryView(
    summary: CycleNutritionSummary | null,
    locale: string,
): CycleNutritionSummaryViewModel | null {
    if (summary === null) {
        return null;
    }

    const numberFormatter = new Intl.NumberFormat(locale, { maximumFractionDigits: 1 });
    const limitedLabel = '\u2014';
    const formatComparison = (value: number): string => (summary.hasEnoughNutritionData ? numberFormatter.format(value) : limitedLabel);

    return {
        summary,
        hasEnoughData: summary.hasEnoughNutritionData,
        bleedingCaloriesLabel: formatComparison(summary.averageCaloriesOnBleedingDays),
        nonBleedingCaloriesLabel: formatComparison(summary.averageCaloriesOnNonBleedingCycleDays),
        bleedingFiberLabel: formatComparison(summary.averageFiberOnBleedingDays),
        nonBleedingFiberLabel: formatComparison(summary.averageFiberOnNonBleedingCycleDays),
        painImpactLabel: formatComparison(summary.averagePainImpactOnDaysWithMeals),
    };
}

export function buildCycleDayItems(
    bleedingEntries: BleedingEntry[],
    symptoms: CycleSymptomEntry[],
    fertilitySignals: FertilitySignal[],
    localeOrOptions: string | { locale: string; menstrualEpisodes: MenstrualEpisode[] },
): CycleDayViewModel[] {
    const locale = typeof localeOrOptions === 'string' ? localeOrOptions : localeOrOptions.locale;
    const menstrualEpisodes = typeof localeOrOptions === 'string' ? [] : localeOrOptions.menstrualEpisodes;
    const activeSymptoms = symptoms.filter(symptom => symptom.intensity > 0);
    const dates = new Set([
        ...bleedingEntries.map(entry => entry.date),
        ...activeSymptoms.map(symptom => symptom.date),
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
            const daySymptoms = activeSymptoms.filter(symptom => symptom.date === date);
            const symptomSummaryItems = buildSymptomSummaryItems(daySymptoms);
            const startEpisode = menstrualEpisodes.find(episode => toDateKey(episode.startDate) === dateKey);
            return {
                date,
                dateLabel: formatCycleDate(date, locale, FULL_DATE_OPTIONS),
                bleedingEntries: dayBleeding,
                symptoms: daySymptoms,
                bleedingSummaryItems: buildBleedingSummaryItems(dayBleeding),
                symptomSummaryItems: symptomSummaryItems.slice(0, HISTORY_SYMPTOM_LIMIT),
                additionalSymptomCount: Math.max(0, symptomSummaryItems.length - HISTORY_SYMPTOM_LIMIT),
                fertilitySignal,
                fertilitySignalItems: buildFertilitySignalItems(fertilitySignal),
                carePromptItems: buildCarePromptItems(dayBleeding, bleedingStreakByDate.get(dateKey) ?? 0),
                notes:
                    dayBleeding.find(entry => entry.notes !== null && entry.notes !== undefined)?.notes ?? fertilitySignal?.notes ?? null,
                accentColor: hasBleeding ? PERIOD_DAY_ACCENT_COLOR : DEFAULT_DAY_ACCENT_COLOR,
                badgeLabelKey: getDayBadgeLabelKey(dayBleeding),
                isPeriodStart: startEpisode !== undefined,
                isPeriodStartConfirmed: startEpisode?.status === MENSTRUAL_EPISODE_STATUS_CONFIRMED,
            };
        });
}

export function buildCycleObservationsView(dayItems: CycleDayViewModel[]): CycleObservationsViewModel {
    const activeSymptoms = dayItems.flatMap(day => day.symptoms).filter(symptom => symptom.intensity > 0);
    const groupedSymptoms = new Map<number, { labelKey: string; loggedDayCount: number; totalIntensity: number }>();

    for (const symptom of activeSymptoms) {
        const labelKey = getSymptomLabelKey(symptom.category);
        const current = groupedSymptoms.get(symptom.category) ?? { labelKey, loggedDayCount: 0, totalIntensity: 0 };
        groupedSymptoms.set(symptom.category, {
            labelKey,
            loggedDayCount: current.loggedDayCount + 1,
            totalIntensity: current.totalIntensity + symptom.intensity,
        });
    }

    const topSymptomEntry = [...groupedSymptoms.entries()]
        .sort(([leftCategory, left], [rightCategory, right]) => {
            const countDifference = right.loggedDayCount - left.loggedDayCount;
            if (countDifference !== 0) {
                return countDifference;
            }

            const leftAverage = left.totalIntensity / left.loggedDayCount;
            const rightAverage = right.totalIntensity / right.loggedDayCount;
            const averageDifference = rightAverage - leftAverage;
            return averageDifference !== 0 ? averageDifference : leftCategory - rightCategory;
        })
        .at(0)?.[1];

    return {
        hasEnoughData: dayItems.length >= OBSERVATION_MINIMUM_TRACKED_DAYS,
        trackedDayCount: dayItems.length,
        bleedingDayCount: dayItems.filter(day => day.bleedingEntries.length > 0).length,
        activeSymptomRecordCount: activeSymptoms.length,
        topSymptom:
            topSymptomEntry === undefined
                ? null
                : {
                      labelKey: topSymptomEntry.labelKey,
                      loggedDayCount: topSymptomEntry.loggedDayCount,
                      severityKey: getIntensitySeverityKey(topSymptomEntry.totalIntensity / topSymptomEntry.loggedDayCount),
                  },
    };
}

function buildBleedingSummaryItems(entries: BleedingEntry[]): CycleDayBleedingSummaryViewModel[] {
    return entries.map(entry => ({
        id: entry.id,
        typeLabelKey:
            entry.type === BLEEDING_TYPE_SPOTTING ? 'CYCLE_TRACKING.BLEEDING_TYPE_SPOTTING' : 'CYCLE_TRACKING.BLEEDING_TYPE_BLEEDING',
        flowLabelKey: getFlowLabelKey(entry.flow),
        painSeverityKey:
            entry.painImpact !== null && entry.painImpact !== undefined && entry.painImpact > 0
                ? getIntensitySeverityKey(entry.painImpact)
                : null,
    }));
}

function buildSymptomSummaryItems(symptoms: CycleSymptomEntry[]): CycleDaySymptomSummaryViewModel[] {
    return [...symptoms]
        .sort((left, right) => {
            const intensityDifference = right.intensity - left.intensity;
            return intensityDifference !== 0 ? intensityDifference : left.category - right.category;
        })
        .map(symptom => ({
            id: symptom.id,
            labelKey: getSymptomLabelKey(symptom.category),
            severityKey: getIntensitySeverityKey(symptom.intensity),
        }));
}

function getDayBadgeLabelKey(entries: BleedingEntry[]): string {
    if (entries.some(entry => entry.type === BLEEDING_TYPE_BLEEDING)) {
        return 'CYCLE_TRACKING.BADGE_PERIOD';
    }

    if (entries.some(entry => entry.type === BLEEDING_TYPE_SPOTTING)) {
        return 'CYCLE_TRACKING.BADGE_SPOTTING';
    }

    return 'CYCLE_TRACKING.BADGE_TRACKED';
}

function getSymptomLabelKey(category: CycleSymptomEntry['category']): string {
    return CYCLE_SYMPTOM_FIELDS.find(field => field.category === category)?.labelKey ?? 'CYCLE_TRACKING.SYMPTOM_OTHER';
}

function getFlowLabelKey(flow: BleedingEntry['flow']): string | null {
    switch (flow) {
        case CYCLE_FLOW_LIGHT: {
            return 'CYCLE_TRACKING.FLOW_LIGHT';
        }
        case CYCLE_FLOW_MEDIUM: {
            return 'CYCLE_TRACKING.FLOW_MEDIUM';
        }
        case CYCLE_FLOW_HEAVY: {
            return 'CYCLE_TRACKING.FLOW_HEAVY';
        }
        case CYCLE_FLOW_NONE: {
            return null;
        }
    }
}

function getIntensitySeverityKey(value: number): string {
    if (value <= MILD_MAX_INTENSITY) {
        return 'CYCLE_TRACKING.SEVERITY_MILD';
    }

    if (value <= MODERATE_MAX_INTENSITY) {
        return 'CYCLE_TRACKING.SEVERITY_MODERATE';
    }

    return 'CYCLE_TRACKING.SEVERITY_STRONG';
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

function toLocalDateKey(value: Date): string {
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');
    return `${value.getFullYear()}-${month}-${day}`;
}

function resolveCycleStartDateKey(cycle: CycleResponse, todayDateKey: string): string {
    const latestEpisode = [...(cycle.menstrualEpisodes ?? [])]
        .map(episode => toDateKey(episode.startDate))
        .filter(dateKey => dateKey.length > 0 && dateKey <= todayDateKey)
        .sort((left, right) => right.localeCompare(left))
        .at(0);

    return latestEpisode ?? toDateKey(cycle.trackingStartDate);
}

function calculateCycleDayNumber(startDateKey: string, dateKey: string, averageCycleLength: number): number | null {
    const startTime = Date.parse(`${startDateKey}T00:00:00.000Z`);
    const dateTime = Date.parse(`${dateKey}T00:00:00.000Z`);
    if (Number.isNaN(startTime) || Number.isNaN(dateTime)) {
        return null;
    }

    const elapsedDays = Math.floor((dateTime - startTime) / MS_PER_DAY);
    const normalizedLength = Math.max(1, averageCycleLength);
    const normalizedOffset = ((elapsedDays % normalizedLength) + normalizedLength) % normalizedLength;
    return normalizedOffset + 1;
}

function isDateInRange(dateKey: string, from: string | null | undefined, to: string | null | undefined): boolean {
    if (from === null || from === undefined) {
        return false;
    }

    const fromKey = toDateKey(from);
    const toKey = to === null || to === undefined ? fromKey : toDateKey(to);
    return dateKey >= fromKey && dateKey <= toKey;
}

function formatRange(from: string | null | undefined, to: string | null | undefined, locale: string): string {
    const fromLabel = formatCycleDate(from, locale, SHORT_DATE_OPTIONS, UTC_TIME_ZONE);
    const toLabel = formatCycleDate(to, locale, SHORT_DATE_OPTIONS, UTC_TIME_ZONE);
    if (fromLabel.length === 0) {
        return toLabel;
    }

    if (fromLabel === toLabel || toLabel.length === 0) {
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
