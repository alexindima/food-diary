import type { BodyTargetKey, MacroKey } from '../../lib/goals.facade';
import type { DayCalorieKey } from '../../models/goals.data';

export type BodyTarget = {
    key: BodyTargetKey;
    titleKey: string;
    value: number;
    unit: string;
    current?: string | null;
    delta?: string | null;
};

export type CyclingDayControl = {
    key: DayCalorieKey;
    labelKey: string;
    inputId: string;
};

export type MacroSliderView = {
    key?: MacroKey;
    labelKey: string;
    unit: string;
    max: number;
    value: number;
    accent: string;
    gradient: string;
    progressOffset: string;
    progressRatio: number;
};

export type MacroInputChange = {
    key: MacroKey;
    event: Event;
};

export type BodyTargetInputChange = {
    key: BodyTargetKey;
    event: Event;
};

export type DayCaloriesInputChange = {
    key: DayCalorieKey;
    event: Event;
};

export type PointerCoordinates = {
    clientX: number;
    clientY: number;
};

export type RingRect = Pick<DOMRect, 'left' | 'top' | 'width' | 'height'>;
