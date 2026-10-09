/* eslint-disable @typescript-eslint/no-unsafe-type-assertion -- validated owner factories attach quantity and window meanings */
import { MAX_CYCLIC_DAYS, MAX_FASTING_HOURS, MAX_INTERMITTENT_FAST_HOURS, MIN_FASTING_HOURS } from '../../../shared/lib/fasting.constants';
import { HOURS_PER_DAY } from '../../../shared/lib/time.constants';
import type { FastingProtocol } from '../../../shared/models/fasting.data';
import type { SemanticQuantity, UnbrandedQuantity } from '../../../shared/models/semantics/quantity-meaning';

export type FastingCycleDays = SemanticQuantity<'fasting-cycle-days'>;
export type ExtendedFastingHours = SemanticQuantity<'extended-fasting-hours'>;
export type DailyFastingHours = SemanticQuantity<'daily-fasting-hours'>;
export type DailyEatingHours = SemanticQuantity<'daily-eating-hours'>;
export type IntermittentFastingProtocol = Extract<FastingProtocol, 'Fast16Eat8' | 'Fast18Eat6' | 'Fast20Eat4' | 'CustomIntermittent'>;
export type ExtendedFastingProtocol = Exclude<FastingProtocol, IntermittentFastingProtocol>;
export class FastingDailyWindow {
    private readonly fastingHours: DailyFastingHours;

    private constructor(value: UnbrandedQuantity | DailyFastingHours) {
        this.fastingHours = value as DailyFastingHours;
        Object.freeze(this);
    }

    public get fastHours(): DailyFastingHours {
        return this.fastingHours;
    }

    public get eatingWindowHours(): DailyEatingHours {
        return (HOURS_PER_DAY - this.fastingHours) as DailyEatingHours;
    }

    public static fromFastHours(value: UnbrandedQuantity | DailyFastingHours): FastingDailyWindow {
        validateWholeNumber(value, MAX_INTERMITTENT_FAST_HOURS);
        return new FastingDailyWindow(value);
    }
}

type StartNotes = { readonly notes?: string };
export type FastingStartIntent = StartNotes &
    (
        | {
              readonly planType: 'Intermittent';
              readonly protocol: IntermittentFastingProtocol;
              readonly window: FastingDailyWindow;
              readonly durationHours?: never;
              readonly fastDays?: never;
              readonly eatDays?: never;
              readonly eatDayWindow?: never;
          }
        | {
              readonly planType: 'Extended';
              readonly protocol: ExtendedFastingProtocol;
              readonly durationHours: ExtendedFastingHours;
              readonly window?: never;
              readonly fastDays?: never;
              readonly eatDays?: never;
              readonly eatDayWindow?: never;
          }
        | {
              readonly planType: 'Cyclic';
              readonly fastDays: FastingCycleDays;
              readonly eatDays: FastingCycleDays;
              readonly eatDayWindow: FastingDailyWindow;
              readonly protocol?: never;
              readonly window?: never;
              readonly durationHours?: never;
          }
    );

function validateWholeNumber(value: number, maximum: number): void {
    if (!Number.isInteger(value) || value < MIN_FASTING_HOURS || value > maximum) {
        throw new RangeError('Fasting quantities must be normalized whole numbers within their existing bounds.');
    }
}

export function fastingCycleDays(value: UnbrandedQuantity | FastingCycleDays): FastingCycleDays {
    validateWholeNumber(value, MAX_CYCLIC_DAYS);
    return value as FastingCycleDays;
}

export function extendedFastingHours(value: UnbrandedQuantity | ExtendedFastingHours): ExtendedFastingHours {
    validateWholeNumber(value, MAX_FASTING_HOURS);
    return value as ExtendedFastingHours;
}

export function fastingDailyWindow(value: UnbrandedQuantity | DailyFastingHours): FastingDailyWindow {
    return FastingDailyWindow.fromFastHours(value);
}

export function isIntermittentFastingProtocol(value: FastingProtocol): value is IntermittentFastingProtocol {
    return value === 'Fast16Eat8' || value === 'Fast18Eat6' || value === 'Fast20Eat4' || value === 'CustomIntermittent';
}

export function isExtendedFastingProtocol(value: FastingProtocol): value is ExtendedFastingProtocol {
    return value === 'Fast24' || value === 'Fast36' || value === 'Fast72' || value === 'Custom';
}

export function intermittentFastingStart(
    protocol: IntermittentFastingProtocol,
    dailyWindow: FastingDailyWindow,
    notes?: string,
): FastingStartIntent {
    return { planType: 'Intermittent', protocol, window: dailyWindow, ...(notes === undefined ? {} : { notes }) };
}

export function extendedFastingStart(
    protocol: ExtendedFastingProtocol,
    durationHours: ExtendedFastingHours,
    notes?: string,
): FastingStartIntent {
    return { planType: 'Extended', protocol, durationHours, ...(notes === undefined ? {} : { notes }) };
}

export function cyclicFastingStart(
    fastDays: FastingCycleDays,
    eatDays: FastingCycleDays,
    eatDayWindow: FastingDailyWindow,
    notes?: string,
): FastingStartIntent {
    return { planType: 'Cyclic', fastDays, eatDays, eatDayWindow, ...(notes === undefined ? {} : { notes }) };
}
