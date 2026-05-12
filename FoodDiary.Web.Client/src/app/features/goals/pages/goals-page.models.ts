import type { BodyTargetKey, MacroKey } from '../lib/goals.facade';

export type BodyTarget = {
    key: BodyTargetKey;
    titleKey: string;
    value: number;
    unit: string;
    current?: string | null;
    delta?: string | null;
};

export type CyclingDayControl = {
    key: string;
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
