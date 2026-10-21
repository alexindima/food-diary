import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { adminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminDailyAdvicesService } from '../api/admin-daily-advices.service';
import type { AdminDailyAdvice } from '../models/admin-daily-advice.models';
import { AdminDailyAdvicesFacade } from './admin-daily-advices.facade';

describe('AdminDailyAdvicesFacade refresh ordering', () => {
    const getAll = vi.fn();

    beforeEach(() => {
        getAll.mockReset();
        TestBed.configureTestingModule({
            providers: [AdminDailyAdvicesFacade, { provide: AdminDailyAdvicesService, useValue: { getAll } }],
        });
    });

    it('retains the refreshed advice list when an older request completes or fails', () => {
        const previous = new Subject<AdminDailyAdvice[]>();
        const refresh = new Subject<AdminDailyAdvice[]>();
        getAll.mockReturnValueOnce(previous).mockReturnValueOnce(refresh);
        const facade = TestBed.inject(AdminDailyAdvicesFacade);
        const advice: AdminDailyAdvice = {
            id: adminId<'daily-advice'>('imported-advice'),
            ru: 'Новый совет',
            en: 'New advice',
            weight: 1,
            tag: null,
        };

        facade.loadAdvices();
        facade.loadAdvices();
        refresh.next([advice]);
        previous.next([]);
        previous.error(new Error('obsolete request failed'));

        expect(facade.advices()).toEqual([advice]);
        expect(facade.loading()).toBe(false);
        expect(facade.loadFailed()).toBe(false);
    });
});
