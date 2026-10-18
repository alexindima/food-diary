import {
    CYCLE_TRACKING_MODE_PERIMENOPAUSE,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    type CycleResponse,
} from '../models/cycle.data';
import { MS_PER_DAY } from './time.constants';

export function resolveCycleStartDateKey(
    cycle: Pick<CycleResponse, 'mode' | 'menstrualEpisodes'>,
    referenceDateKey: string,
): string | null {
    if (
        cycle.mode !== CYCLE_TRACKING_MODE_PERIOD_TRACKING &&
        cycle.mode !== CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE &&
        cycle.mode !== CYCLE_TRACKING_MODE_PERIMENOPAUSE
    ) {
        return null;
    }

    // The API may encode these calendar labels as UTC-midnight strings.
    // Preserve the label instead of converting the episode to local time.
    return (
        [...(cycle.menstrualEpisodes ?? [])]
            .map(episode => /^(\d{4}-\d{2}-\d{2})/.exec(episode.startDate)?.[1] ?? '')
            .filter(dateKey => dateKey.length > 0 && dateKey <= referenceDateKey)
            .sort((left, right) => right.localeCompare(left))
            .at(0) ?? null
    );
}

export function calculateCycleDayNumber(startDateKey: string | null, dateKey: string, averageCycleLength: number): number | null {
    if (startDateKey === null) {
        return null;
    }
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
