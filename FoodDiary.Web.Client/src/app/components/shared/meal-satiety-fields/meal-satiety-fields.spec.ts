import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
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

    it('keeps the keyboard-selected rating focused while translating its label and option', async () => {
        const { component, fixture } = await setupComponentAsync();
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', {
            MEAL_MANAGE: { HUNGER_BEFORE_LABEL: 'Before the meal' },
            MEAL_DETAILS: { SATIETY_UNANSWERED: 'Unanswered' },
            HUNGER_BEFORE_SCALE: { LEVEL_2: { TITLE: 'Hungry', DESCRIPTION: 'Ready to eat' } },
        });
        translate.setTranslation('ru', {
            MEAL_MANAGE: { HUNGER_BEFORE_LABEL: 'До еды' },
            HUNGER_BEFORE_SCALE: { LEVEL_2: { TITLE: 'Голоден', DESCRIPTION: 'Хочется поесть' } },
        });
        await firstValueFrom(translate.use('en'));
        const changed = vi.fn();
        component.preMealSatietyLevel.subscribe(changed);
        fixture.componentRef.setInput('labelBeforeKey', 'MEAL_MANAGE.HUNGER_BEFORE_LABEL');
        fixture.detectChanges();

        const group = (fixture.nativeElement as HTMLElement).querySelector('[role="radiogroup"]');
        const first = group?.querySelector('button[role="radio"][tabindex="0"]');
        if (!(first instanceof HTMLButtonElement)) {
            throw new Error('Expected the first keyboard-accessible hunger rating');
        }
        expect(group?.getAttribute('aria-label')).toBe('Before the meal. Unanswered.');
        first.focus();
        first.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true, cancelable: true }));
        fixture.detectChanges();

        const selected = document.activeElement;
        expect(selected?.getAttribute('aria-checked')).toBe('true');
        expect(selected?.getAttribute('aria-label')).toBe('Hungry. Ready to eat');
        expect(group?.getAttribute('aria-label')).toBe('Before the meal. Hungry. Ready to eat');
        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();

        expect(document.activeElement).toBe(selected);
        expect(selected?.getAttribute('aria-label')).toBe('Голоден. Хочется поесть');
        expect(group?.getAttribute('aria-label')).toBe('До еды. Голоден. Хочется поесть');
        expect(changed).toHaveBeenCalledExactlyOnceWith(2);
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
