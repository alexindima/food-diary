export type FdTourPlacement = 'auto' | 'top' | 'right' | 'bottom' | 'left';

export type FdTourStep = {
    id: string;
    target: string;
    title: string;
    description?: string;
    placement?: FdTourPlacement;
    allowInteraction?: boolean;
};

export type FdTourLabels = {
    previous: string;
    next: string;
    finish: string;
    skip: string;
    close: string;
};

export type FdTourDefinition = {
    id: string;
    version: number;
    steps: readonly FdTourStep[];
    labels?: Partial<FdTourLabels>;
};

export type FdTourStartOptions = {
    force?: boolean;
};

export type FdTourSnapshot = {
    tour: FdTourDefinition;
    step: FdTourStep;
    stepIndex: number;
    stepCount: number;
    isFirstStep: boolean;
    isLastStep: boolean;
};
