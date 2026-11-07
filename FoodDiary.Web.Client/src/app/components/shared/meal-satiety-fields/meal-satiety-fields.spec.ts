import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { MealSatietyFieldsComponent } from './meal-satiety-fields';

const LEGACY_SATIETY_LEVEL = 8;
const NORMALIZED_LEGACY_SATIETY_LEVEL = 4;
const BELOW_MIN_POSITIVE_SATIETY_LEVEL = 0.5;
const MIN_SATIETY_LEVEL = 1;
const POST_MEAL_SATIETY_LEVEL = 4;

describe('MealSatietyFieldsComponent', () => {
    it.each(['before', 'after'] as const)('clears only the optional %s rating through its button', async kind => {
        const { component, fixture } = await setupComponentAsync();
        fixture.componentRef.setInput('preMealSatietyLevel', 2);
        fixture.componentRef.setInput('postMealSatietyLevel', POST_MEAL_SATIETY_LEVEL);
        const changed = vi.fn();
        const change = kind === 'before' ? component.preMealSatietyLevel : component.postMealSatietyLevel;
        change.subscribe(changed);
        fixture.detectChanges();
        const key = kind === 'before' ? 'MEAL_DETAILS.CLEAR_HUNGER' : 'MEAL_DETAILS.CLEAR_SATIETY';
        const button = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).find(
            value => value.textContent.trim() === key,
        );

        expect(button, 'Optional rating must have an explicit clear button').toBeDefined();
        expect(button?.type).toBe('button');
        button?.click();
        fixture.detectChanges();

        expect(component.preMealSatietyLevel()).toBe(kind === 'before' ? null : 2);
        expect(component.postMealSatietyLevel()).toBe(kind === 'after' ? null : POST_MEAL_SATIETY_LEVEL);
        expect(changed).toHaveBeenCalledExactlyOnceWith(null);
        expect(button?.disabled).toBe(true);
        const label = kind === 'before' ? component['preMealSatietyAriaLabel']() : component['postMealSatietyAriaLabel']();
        expect(label).toContain('MEAL_DETAILS.SATIETY_UNANSWERED');
    });

    it('should normalize and emit pre meal satiety changes', async () => {
        const { component } = await setupComponentAsync();

        component['onSatietyLevelChange']('before', LEGACY_SATIETY_LEVEL);

        expect(component['preMealSatietyLevel']()).toBe(NORMALIZED_LEGACY_SATIETY_LEVEL);
    });

    it('should normalize and emit post meal satiety changes', async () => {
        const { component } = await setupComponentAsync();

        component['onSatietyLevelChange']('after', BELOW_MIN_POSITIVE_SATIETY_LEVEL);

        expect(component['postMealSatietyLevel']()).toBe(MIN_SATIETY_LEVEL);
    });

    it('should build translated aria labels from configured label keys', async () => {
        const { component, fixture } = await setupComponentAsync();
        fixture.componentRef.setInput('labelBeforeKey', 'MEAL_MANAGE.HUNGER_BEFORE_LABEL');
        fixture.detectChanges();

        expect(component['preMealSatietyAriaLabel']()).toContain('MEAL_MANAGE.HUNGER_BEFORE_LABEL');
    });
});

type MealSatietyFieldsSetup = {
    component: MealSatietyFieldsComponent;
    fixture: ComponentFixture<MealSatietyFieldsComponent>;
};

async function setupComponentAsync(): Promise<MealSatietyFieldsSetup> {
    await TestBed.configureTestingModule({
        imports: [MealSatietyFieldsComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealSatietyFieldsComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}
