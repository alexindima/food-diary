import type { StartFastingPayload } from '../../../shared/models/fasting.data';
import type { FastingStartIntent } from '../models/fasting-start-intent';

export function fastingStartToSdk(intent: FastingStartIntent): StartFastingPayload {
    const notes = intent.notes === undefined ? {} : { notes: intent.notes };
    switch (intent.planType) {
        case 'Intermittent': {
            return { planType: intent.planType, protocol: intent.protocol, plannedDurationHours: intent.window.fastHours, ...notes };
        }
        case 'Extended': {
            return { planType: intent.planType, protocol: intent.protocol, plannedDurationHours: intent.durationHours, ...notes };
        }
        case 'Cyclic': {
            return {
                planType: intent.planType,
                cyclicFastDays: intent.fastDays,
                cyclicEatDays: intent.eatDays,
                cyclicEatDayFastHours: intent.eatDayWindow.fastHours,
                cyclicEatDayEatingWindowHours: intent.eatDayWindow.eatingWindowHours,
                ...notes,
            };
        }
    }
}
