import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminMealPlansFacade } from '../lib/admin-meal-plans.facade';
import type { CatalogPlan } from '../models/admin-meal-plan.data';
import { AdminMealPlanEditorComponent } from './admin-meal-plan-editor';
import { AdminMealPlansComponent } from './admin-meal-plans';

const plan: CatalogPlan = {
    id: adminId<'meal-plan'>('existing-plan'),
    name: 'Old server plan',
    description: null,
    dietType: 'Balanced',
    durationDays: 1,
    targetCaloriesPerDay: null,
    isCurated: false,
    days: [{ dayNumber: 1, meals: [] }],
};
const api = { getAll: vi.fn(), get: vi.fn(), recipes: vi.fn(), save: vi.fn() };
let fixture: ComponentFixture<AdminMealPlansComponent>;

beforeEach(async () => {
    vi.clearAllMocks();
    api.getAll.mockReturnValue(of([plan]));
    api.recipes.mockReturnValue(of([]));
    api.save.mockReturnValue(of(plan));
    await TestBed.configureTestingModule({
        imports: [AdminMealPlansComponent],
        providers: provideTranslateTesting(),
    })
        .overrideComponent(AdminMealPlansComponent, {
            set: { providers: [{ provide: AdminMealPlansFacade, useValue: api }] },
        })
        .compileComponents();
    fixture = TestBed.createComponent(AdminMealPlansComponent);
    fixture.detectChanges();
});

describe('AdminMealPlansComponent editor selection', () => {
    it('preserves a new editor draft and create target when an earlier Edit detail arrives', () => {
        const pending = new Subject<CatalogPlan>();
        api.get.mockReturnValue(pending);
        const component = fixture.componentInstance;
        component['edit'](plan);
        component['create']();
        fixture.detectChanges();
        const input = (fixture.nativeElement as HTMLElement).querySelector('input');
        if (input === null) {
            throw new Error('Expected a new plan name field');
        }
        input.value = 'Typed brand new plan';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();

        pending.next(plan);
        pending.complete();
        fixture.detectChanges();

        expect(component['selected']()).toBeNull();
        expect(input.value).toBe('Typed brand new plan');
        const editor = fixture.debugElement.query(By.directive(AdminMealPlanEditorComponent))
            .componentInstance as AdminMealPlanEditorComponent;
        editor['save']();
        expect(api.save).toHaveBeenCalledWith(null, expect.objectContaining({ name: 'Typed brand new plan' }));
    });

    it('cancels a superseded Edit and retains the newer plan selection', () => {
        const first = new Subject<CatalogPlan>();
        const second = new Subject<CatalogPlan>();
        const newer = { ...plan, id: adminId<'meal-plan'>('newer-plan'), name: 'Newer selection' };
        api.get.mockReturnValueOnce(first).mockReturnValueOnce(second);
        fixture.componentInstance['edit'](plan);
        fixture.componentInstance['edit'](newer);

        expect(first.observed).toBe(false);
        second.next(newer);
        second.complete();
        first.next(plan);
        first.complete();

        expect(fixture.componentInstance['selected']()).toEqual(newer);
        expect(fixture.componentInstance['loading']()).toBe(false);
    });

    it.each(['create', 'cancel', 'onSaved'] as const)('invalidates the pending selection on %s', action => {
        const pending = new Subject<CatalogPlan>();
        api.get.mockReturnValue(pending);
        fixture.componentInstance['edit'](plan);
        fixture.componentInstance[action]();
        expect(pending.observed).toBe(false);
        pending.error(new Error('late error'));
        expect(fixture.componentInstance['error']()).toBeNull();
        expect(fixture.componentInstance['selected']()).toBeNull();
    });

    it('cancels the detail request when the page is destroyed', () => {
        const pending = new Subject<CatalogPlan>();
        api.get.mockReturnValue(pending);
        fixture.componentInstance['edit'](plan);
        fixture.destroy();
        expect(pending.observed).toBe(false);
    });
});
