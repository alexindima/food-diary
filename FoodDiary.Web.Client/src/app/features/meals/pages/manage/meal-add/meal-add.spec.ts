import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { describe, expect, it } from 'vitest';

import type { MealTypeOption } from '../../../../../shared/lib/meal-type.util';
import { MealManageFormComponent } from '../../../components/manage/meal-manage-form';
import { MealAddComponent } from './meal-add';

const ADD_URL = '/meals/add';

@Component({ selector: 'fd-meal-manage-form', template: '', changeDetection: ChangeDetectionStrategy.OnPush })
class MealManageFormStubComponent {
    public readonly openManualItem = input(false);
    public readonly initialMealType = input<MealTypeOption | null>(null);
}

describe('MealAddComponent', () => {
    it('creates the add page wrapper', async () => {
        const harness = await setupHarnessAsync();

        expect(await harness.navigateByUrl(ADD_URL, MealAddComponent)).toBeTruthy();
    });

    it('binds the first query values and clears inputs when query parameters are removed', async () => {
        const harness = await setupHarnessAsync();
        const page = await harness.navigateByUrl(`${ADD_URL}?manual=1&manual=0&mealType=%20dinner%20&mealType=BREAKFAST`, MealAddComponent);
        const editor = renderedEditor(harness);
        expect(editor.openManualItem()).toBe(true);
        expect(editor.initialMealType()).toBe('DINNER');

        expect(await harness.navigateByUrl(`${ADD_URL}?manual=0&manual=1&mealType=BREAKFAST`, MealAddComponent)).toBe(page);
        expect(editor.openManualItem()).toBe(false);
        expect(editor.initialMealType()).toBe('BREAKFAST');

        expect(await harness.navigateByUrl(ADD_URL, MealAddComponent)).toBe(page);
        expect(editor.openManualItem()).toBe(false);
        expect(editor.initialMealType()).toBeNull();
    });

    it.each([
        { stateMealType: ' lunch ', expectedMealType: 'LUNCH' },
        { stateMealType: 'unknown', expectedMealType: null },
        { stateMealType: 123, expectedMealType: 'DINNER' },
    ])('resolves navigation state meal type $stateMealType over the query when it is a string', async testCase => {
        const harness = await setupHarnessAsync();

        await TestBed.inject(Router).navigateByUrl(`${ADD_URL}?mealType=DINNER`, { state: { mealType: testCase.stateMealType } });
        harness.detectChanges();

        expect(renderedEditor(harness).initialMealType()).toBe(testCase.expectedMealType);
    });
});

async function setupHarnessAsync(): Promise<RouterTestingHarness> {
    TestBed.configureTestingModule({
        providers: [provideRouter([{ path: 'meals/add', component: MealAddComponent }], withComponentInputBinding())],
    });
    TestBed.overrideComponent(MealAddComponent, {
        remove: { imports: [MealManageFormComponent] },
        add: { imports: [MealManageFormStubComponent] },
    });

    return RouterTestingHarness.create();
}

function renderedEditor(harness: RouterTestingHarness): MealManageFormStubComponent {
    const editor = harness.routeDebugElement?.queryAll(By.directive(MealManageFormStubComponent)).at(0);
    if (editor === undefined) {
        throw new Error('Expected the meal form in the add page');
    }
    return editor.injector.get(MealManageFormStubComponent);
}
