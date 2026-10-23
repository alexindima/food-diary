import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FdUiButtonComponent } from 'fd-ui-kit';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminMealPlansFacade } from '../lib/admin-meal-plans.facade';
import type { CatalogPlan, CatalogRecipe } from '../models/admin-meal-plan.data';
import { AdminMealPlanEditorComponent } from './admin-meal-plan-editor';

const plan: CatalogPlan = {
    id: adminId<'meal-plan'>('plan-id'),
    name: 'Weekly plan',
    description: null,
    dietType: 'Balanced',
    durationDays: 1,
    targetCaloriesPerDay: null,
    isCurated: true,
    days: [{ dayNumber: 1, meals: [{ mealType: 'Lunch', recipeId: adminId<'recipe'>('recipe-id'), recipeName: 'Rice', servings: 2 }] }],
};
const api = { recipes: vi.fn(), save: vi.fn() };
beforeEach(async () => {
    vi.clearAllMocks();
    api.recipes.mockReturnValue(of([]));
    api.save.mockReturnValue(of(plan));
    await TestBed.configureTestingModule({
        imports: [AdminMealPlanEditorComponent],
        providers: [...provideTranslateTesting(), { provide: AdminMealPlansFacade, useValue: api }],
    }).compileComponents();
});

describe('AdminMealPlanEditorComponent', () => {
    it('preserves an edited plan after a failed save and allows retry', () => {
        const fixture = TestBed.createComponent(AdminMealPlanEditorComponent);
        fixture.componentRef.setInput('plan', plan);
        fixture.detectChanges();
        api.save.mockReturnValueOnce(throwError(() => new Error('offline')));
        const saved = vi.fn();
        fixture.componentInstance.saved.subscribe(saved);
        const saveButton = fixture.debugElement
            .queryAll(By.directive(FdUiButtonComponent))
            .find(button => (button.nativeElement as HTMLElement).textContent.includes('ADMIN_MEAL_PLANS.SAVE'));
        saveButton?.triggerEventHandler('click');
        fixture.detectChanges();
        expect(saved).not.toHaveBeenCalled();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_MEAL_PLANS.SAVE_ERROR');
        expect((fixture.nativeElement as HTMLElement).querySelector('input')?.value).toBe('Weekly plan');
        saveButton?.triggerEventHandler('click');
        expect(saved).toHaveBeenCalledOnce();
        expect(api.save).toHaveBeenLastCalledWith('plan-id', expect.objectContaining({ isPublished: true }));
        const request = api.save.mock.lastCall?.[1] as { days: unknown };
        expect(JSON.parse(JSON.stringify(request.days))).toEqual(plan.days);
    });

    it('blocks publication of an empty day before sending a request', () => {
        const fixture = TestBed.createComponent(AdminMealPlanEditorComponent);
        fixture.componentRef.setInput('plan', { ...plan, days: [] });
        fixture.detectChanges();
        const saveButton = fixture.debugElement
            .queryAll(By.directive(FdUiButtonComponent))
            .find(button => (button.nativeElement as HTMLElement).textContent.includes('ADMIN_MEAL_PLANS.SAVE'));
        saveButton?.triggerEventHandler('click');
        fixture.detectChanges();
        expect(api.save).not.toHaveBeenCalled();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_MEAL_PLANS.INCOMPLETE');
    });

    it('keeps the latest recipe search and selected recipes when earlier requests finish late', () => {
        const fixture = TestBed.createComponent(AdminMealPlanEditorComponent);
        fixture.componentRef.setInput('plan', plan);
        fixture.detectChanges();
        const earlier = new Subject<CatalogRecipe[]>();
        const latest = new Subject<CatalogRecipe[]>();
        api.recipes.mockReturnValueOnce(earlier).mockReturnValueOnce(latest);
        const component = fixture.componentInstance;
        component['recipeSearch'].set('old search');
        component['searchRecipes']();
        component['recipeSearch'].set('new search');
        component['searchRecipes']();
        latest.next([{ id: adminId<'recipe'>('latest-id'), name: 'Latest recipe', servings: 1 }]);
        earlier.next([{ id: adminId<'recipe'>('stale-id'), name: 'Stale recipe', servings: 1 }]);
        earlier.error(new Error('stale failure'));

        expect(component['recipeOptions']()).toEqual([
            { value: 'recipe-id', label: 'Rice' },
            { value: 'latest-id', label: 'Latest recipe' },
        ]);
        expect(component['error']()).toBeNull();
        expect(api.recipes).toHaveBeenLastCalledWith('new search');
    });
});

describe('AdminMealPlanEditorComponent pending saves', () => {
    it('locks the editor during a pending save and restores the draft controls after a failure', () => {
        const fixture = TestBed.createComponent(AdminMealPlanEditorComponent);
        fixture.componentRef.setInput('plan', plan);
        fixture.detectChanges();
        const pending = new Subject<CatalogPlan>();
        api.save.mockReturnValueOnce(pending);
        const component = fixture.componentInstance;
        const cancelled = vi.fn();
        component.cancelled.subscribe(cancelled);
        const element = fixture.nativeElement as HTMLElement;
        const originalDays = JSON.stringify(component['model']().days);

        component['save']();
        fixture.detectChanges();

        for (const control of element.querySelectorAll<HTMLInputElement | HTMLTextAreaElement>('input, textarea')) {
            expect(control.disabled).toBe(true);
        }
        for (const button of element.querySelectorAll<HTMLButtonElement>('button')) {
            expect(button.disabled).toBe(true);
        }
        component['addMeal'](0);
        component['removeMeal'](0, 0);
        component['cancel']();
        component['searchRecipes']();
        expect(JSON.stringify(component['model']().days)).toBe(originalDays);
        expect(cancelled).not.toHaveBeenCalled();
        expect(api.recipes).toHaveBeenCalledOnce();

        pending.error(new Error('offline'));
        fixture.detectChanges();

        expect(element.querySelector<HTMLInputElement>('input')?.disabled).toBe(false);
        expect(element.querySelector<HTMLInputElement>('input')?.value).toBe(plan.name);
        expect(element.textContent).toContain('ADMIN_MEAL_PLANS.SAVE_ERROR');
        component['save']();
        expect(api.save).toHaveBeenCalledTimes(2);
        expect(api.save).toHaveBeenLastCalledWith(plan.id, expect.objectContaining({ name: plan.name }));
        const request = api.save.mock.lastCall?.[1] as { days: unknown };
        expect(JSON.parse(JSON.stringify(request.days))).toEqual(plan.days);
    });
});
