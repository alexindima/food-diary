import { describe, expect, it } from 'vitest';

import { parseInstallPromotionState, recordInstallPromotionVisit } from './pwa-install-promotion-state';

const START = 1_700_000_000_000;
const HALF_HOUR = 1_800_000;
const THREE_VISITS = 3;

describe('PWA install promotion persisted state', () => {
    it.each([null, undefined, 'invalid', [], { visits: -1, never: 'true', lastSeenAt: NaN, snoozedUntil: Infinity }])(
        'recovers malformed state without opting a user out: %j',
        value => {
            expect(parseInstallPromotionState(value)).toEqual({ visits: 0, lastSeenAt: null, snoozedUntil: null, never: false });
        },
    );

    it('bounds visit counts and preserves deliberate opt-out', () => {
        expect(parseInstallPromotionState({ visits: 999, never: true, lastSeenAt: START, snoozedUntil: null })).toEqual({
            visits: 3,
            never: true,
            lastSeenAt: START,
            snoozedUntil: null,
        });
    });
});

describe('PWA install visit boundaries', () => {
    it('counts the first visit and waits a full half hour between separate visits', () => {
        const first = recordInstallPromotionVisit(parseInstallPromotionState(null), START);
        expect(first.visits).toBe(1);
        const refreshed = recordInstallPromotionVisit(first, START + HALF_HOUR - 1);
        expect(refreshed.visits).toBe(1);
        const second = recordInstallPromotionVisit(refreshed, START + HALF_HOUR + HALF_HOUR - 1);
        expect(second.visits).toBe(2);
        expect(recordInstallPromotionVisit(second, START + HALF_HOUR + HALF_HOUR + HALF_HOUR).visits).toBe(THREE_VISITS);
    });

    it('recovers after a backwards clock change and never exceeds three visits', () => {
        const rolledBack = recordInstallPromotionVisit(
            { visits: 1, lastSeenAt: START + HALF_HOUR, snoozedUntil: null, never: false },
            START,
        );
        expect(rolledBack).toMatchObject({ visits: 1, lastSeenAt: START });
        expect(recordInstallPromotionVisit({ ...rolledBack, visits: 3 }, START + HALF_HOUR).visits).toBe(THREE_VISITS);
    });
});
