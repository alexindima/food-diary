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
    await settlePlansAsync();
});

describe('AdminMealPlansComponent editor selection', () => {
    it('preserves a new editor draft and create target when an earlier Edit detail arrives', () => {
        const pending = new Subject<CatalogPlan>();
        api.get.mockReturnValue(pending);
        const component = fixture.componentInstance;
        component['edit'](plan);
        fixture.detectChanges();
        expect(pending.observed).toBe(true);
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

    it('cancels a superseded Edit and retains the newer plan selection', async () => {
        const first = new Subject<CatalogPlan>();
        const second = new Subject<CatalogPlan>();
        const newer = { ...plan, id: adminId<'meal-plan'>('newer-plan'), name: 'Newer selection' };
        api.get.mockReturnValueOnce(first).mockReturnValueOnce(second);
        fixture.componentInstance['edit'](plan);
        fixture.detectChanges();
        expect(first.observed).toBe(true);
        fixture.componentInstance['edit'](newer);
        fixture.detectChanges();

        expect(first.observed).toBe(false);
        second.next(newer);
        second.complete();
        first.next(plan);
        first.complete();
        await settlePlansAsync();

        expect(fixture.componentInstance['selected']()).toEqual(newer);
        expect(fixture.componentInstance['loading']()).toBe(false);
    });

    it.each(['create', 'cancel', 'onSaved'] as const)('invalidates the pending selection on %s', action => {
        const pending = new Subject<CatalogPlan>();
        api.get.mockReturnValue(pending);
        fixture.componentInstance['edit'](plan);
        fixture.detectChanges();
        expect(pending.observed).toBe(true);
        fixture.componentInstance[action]();
        fixture.detectChanges();
        expect(pending.observed).toBe(false);
        pending.error(new Error('late error'));
        expect(fixture.componentInstance['error']()).toBeNull();
        expect(fixture.componentInstance['selected']()).toBeNull();
    });

    it('cancels the detail request when the page is destroyed', () => {
        const pending = new Subject<CatalogPlan>();
        api.get.mockReturnValue(pending);
        fixture.componentInstance['edit'](plan);
        fixture.detectChanges();
        expect(pending.observed).toBe(true);
        fixture.destroy();
        expect(pending.observed).toBe(false);
    });
});

describe('Meal plan list and editor lifecycle', () => {
    it('offers retry after a failed list request and distinguishes hidden and published plans', async () => {
        fixture.destroy();
        const pending = new Subject<CatalogPlan[]>();
        const published = { ...plan, id: adminId<'meal-plan'>('published'), name: 'Published menu', isCurated: true };
        api.getAll.mockReturnValueOnce(pending).mockReturnValueOnce(of([plan, published]));
        fixture = TestBed.createComponent(AdminMealPlansComponent);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="status"]:not([hidden])')?.textContent).toContain('ADMIN_MEAL_PLANS.LOADING');
        expect(host.querySelector('article')).toBeNull();

        pending.error(new Error('list unavailable'));
        await settlePlansAsync();
        const retry = host.querySelector<HTMLButtonElement>('fd-admin-load-error button');
        expect(retry).not.toBeNull();
        retry?.click();
        await settlePlansAsync();

        const rows = Array.from(host.querySelectorAll('article'), article => article.textContent);
        expect(rows).toHaveLength(2);
        expect(rows[0]).toContain('Old server plan');
        expect(rows[0]).toContain('ADMIN_MEAL_PLANS.HIDDEN');
        expect(rows[1]).toContain('Published menu');
        expect(rows[1]).toContain('ADMIN_MEAL_PLANS.PUBLISHED');
        expect(host.querySelector('fd-admin-load-error')).toBeNull();
    });

    it('recovers after an edit detail error, closes on cancel, and reloads the list after saving', async () => {
        const pending = new Subject<CatalogPlan>();
        api.get.mockReturnValueOnce(pending).mockReturnValue(of(plan));
        const host = fixture.nativeElement as HTMLElement;
        const editSelector = 'article button';
        host.querySelector<HTMLButtonElement>(editSelector)?.click();
        fixture.detectChanges();
        pending.error(new Error('detail unavailable'));
        await settlePlansAsync();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_MEAL_PLANS.LOAD_ERROR');
        expect(host.querySelector('fd-admin-meal-plan-editor')).toBeNull();

        host.querySelector<HTMLButtonElement>(editSelector)?.click();
        await settlePlansAsync();
        clickMealPlanAction(host, 'CANCEL');
        fixture.detectChanges();
        expect(host.querySelector('fd-admin-meal-plan-editor')).toBeNull();

        host.querySelector<HTMLButtonElement>(editSelector)?.click();
        await settlePlansAsync();
        api.getAll.mockReturnValueOnce(of([{ ...plan, name: 'Saved server plan' }]));
        clickMealPlanAction(host, 'SAVE');
        await settlePlansAsync();
        expect(api.save).toHaveBeenCalledExactlyOnceWith(plan.id, expect.objectContaining({ name: plan.name }));
        expect(host.querySelector('fd-admin-meal-plan-editor')).toBeNull();
        expect(host.querySelector('article h2')?.textContent).toBe('Saved server plan');
        expect(host.querySelector('[role="status"]:not([hidden])')?.textContent).toContain('ADMIN_MEAL_PLANS.SAVED');
    });
});

async function settlePlansAsync(): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
}

function clickMealPlanAction(host: HTMLElement, action: string): void {
    const button = Array.from(host.querySelectorAll('button')).find(control => control.textContent.trim() === `ADMIN_MEAL_PLANS.${action}`);
    if (button === undefined) {
        throw new Error(`Missing meal plan action ${action}`);
    }
    button.click();
}
