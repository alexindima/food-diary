import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminDailyAdvicesFacade } from '../lib/admin-daily-advices.facade';
import type { AdminDailyAdvice } from '../models/admin-daily-advice.models';
import { DailyAdviceGroupCardComponent } from './daily-advice-group-card';

const advice: AdminDailyAdvice = {
    id: '11111111-1111-1111-1111-111111111111',
    ru: 'Russian text',
    en: 'English text',
    weight: 1,
    tag: null,
};

describe('daily advice group editor', () => {
    const update = vi.fn();
    const deleteGroup = vi.fn();
    beforeEach(async () => {
        update.mockReset().mockReturnValue(of(advice));
        deleteGroup.mockReset().mockReturnValue(of(undefined));
        await TestBed.configureTestingModule({
            imports: [DailyAdviceGroupCardComponent],
            providers: [...provideTranslateTesting(), { provide: AdminDailyAdvicesFacade, useValue: { update, delete: deleteGroup } }],
        }).compileComponents();
    });

    function createEditor(): ComponentFixture<DailyAdviceGroupCardComponent> {
        const fixture = TestBed.createComponent(DailyAdviceGroupCardComponent);
        fixture.componentRef.setInput('advice', advice);
        fixture.detectChanges();
        fixture.componentInstance['edit']();
        return fixture;
    }

    it('rejects an empty translation without sending either text', async () => {
        const fixture = createEditor();
        fixture.componentInstance['formModel'].update(value => ({ ...value, en: '   ' }));
        await fixture.componentInstance['saveAsync']();
        expect(update).not.toHaveBeenCalled();
    });

    it('submits both translations together, prevents double save and retains edits after failure', async () => {
        const fixture = createEditor();
        const component = fixture.componentInstance;
        const pending = new Subject<AdminDailyAdvice>();
        update.mockReturnValueOnce(pending);
        component['formModel'].set({ ru: ' Edited Russian ', en: ' Edited English ', weight: 2, tag: ' ' });
        const saving = component['saveAsync']();
        await component['saveAsync']();
        expect(update).toHaveBeenCalledTimes(1);
        expect(update).toHaveBeenCalledWith(advice.id, { ru: 'Edited Russian', en: 'Edited English', weight: 2, tag: null });
        pending.error(new Error('offline'));
        await saving;
        expect(component['editing']()).toBe(true);
        expect(component['error']()).toBe(true);
        expect(component['busy']()).toBe(false);
        await component['saveAsync']();
        expect(component['editing']()).toBe(false);
    });

    it('requires delete confirmation and allows retry without removing the card on failure', () => {
        const fixture = createEditor();
        const component = fixture.componentInstance;
        const changed = vi.fn();
        component.changed.subscribe(changed);
        component['delete']();
        expect(deleteGroup).not.toHaveBeenCalled();
        component['confirmingDelete'].set(true);
        deleteGroup.mockReturnValueOnce(throwError(() => new Error('offline')));
        component['delete']();
        expect(changed).not.toHaveBeenCalled();
        expect(component['error']()).toBe(true);
        component['delete']();
        expect(deleteGroup).toHaveBeenCalledWith(advice.id);
        expect(changed).toHaveBeenCalledTimes(1);
    });
});
