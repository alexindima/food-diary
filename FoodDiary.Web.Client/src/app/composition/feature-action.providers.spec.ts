import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DIETOLOGIST_RELATIONSHIP_ACTIONS } from '../features/dietologist/contracts/relationship-actions';
import { DietologistFacade } from '../features/dietologist/lib/dietologist.facade';
import { HydrationService } from '../features/hydration/api/hydration.service';
import { HYDRATION_ACTIONS } from '../features/hydration/contracts/hydration-actions';
import { FEATURE_ACTION_PROVIDERS } from './feature-action.providers';

const HYDRATION_AMOUNT = 250;
const hydrationOwner = { addEntry: vi.fn(() => of({ id: 'entry', timestampUtc: '2026-10-07T00:00:00Z', amountMl: HYDRATION_AMOUNT })) };
const relationshipOwner = { getRelationship: vi.fn(() => of(null)), revokeRelationship: vi.fn(() => of(undefined)) };

describe('Lazy capability composition', () => {
    beforeEach(() => {
        hydrationOwner.addEntry.mockClear();
        relationshipOwner.getRelationship.mockClear();
        relationshipOwner.revokeRelationship.mockClear();
        TestBed.configureTestingModule({
            providers: [
                ...FEATURE_ACTION_PROVIDERS,
                {
                    provide: HydrationService,
                    useValue: hydrationOwner,
                },
                {
                    provide: DietologistFacade,
                    useValue: relationshipOwner,
                },
            ],
        });
    });

    it('resolves an overridden owner and keeps the selected hydration instant', async () => {
        const timestamp = new Date('2026-10-07T00:15:00+05:45');
        const request = TestBed.inject(HYDRATION_ACTIONS).addEntry(HYDRATION_AMOUNT, timestamp);
        expect(hydrationOwner.addEntry).not.toHaveBeenCalled();
        await firstValueFrom(request);
        expect(hydrationOwner.addEntry).toHaveBeenCalledExactlyOnceWith(HYDRATION_AMOUNT, timestamp);
    });

    it('delegates relationship actions to the same owner registered in DI', async () => {
        const actions = TestBed.inject(DIETOLOGIST_RELATIONSHIP_ACTIONS);
        expect(await firstValueFrom(actions.getRelationship())).toBeNull();
        await firstValueFrom(actions.revokeRelationship());
        expect(relationshipOwner.getRelationship).toHaveBeenCalledOnce();
        expect(relationshipOwner.revokeRelationship).toHaveBeenCalledOnce();
    });
});
