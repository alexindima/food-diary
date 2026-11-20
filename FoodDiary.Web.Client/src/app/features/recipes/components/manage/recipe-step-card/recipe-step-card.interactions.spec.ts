import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { RecipeStepCardComponent, type RecipeStepCardState, type RecipeStepIngredientState } from './recipe-step-card';

function stepFixture(): ComponentFixture<RecipeStepCardComponent> {
    TestBed.configureTestingModule({
        imports: [RecipeStepCardComponent],
        providers: [provideRouter([]), provideTranslateTesting({ fallbackLang: 'en' })],
    });
    const fixture = TestBed.createComponent(RecipeStepCardComponent);
    const initial: RecipeStepCardState = {
        title: { value: 'Prepare rice', error: null },
        imageUrl: { value: null, error: null },
        description: { value: '', error: null },
        ingredients: [ingredient('Rice')],
    };
    fixture.componentRef.setInput('step', initial);
    fixture.componentRef.setInput('stepIndex', 0);
    fixture.componentRef.setInput('isExpanded', true);
    fixture.componentRef.setInput('dragDisabled', false);
    fixture.detectChanges();
    return fixture;
}

function ingredient(name: string): RecipeStepIngredientState {
    return {
        textName: name,
        amountText: 'to taste',
        amount: { value: null, error: null },
        food: null,
        foodName: { value: '', error: null },
        nestedRecipeId: null,
    };
}

function element<T extends HTMLElement>(root: ParentNode, selector: string, type: abstract new () => T): T {
    const found = root.querySelector(selector);
    if (!(found instanceof type)) {
        throw new Error(`Expected recipe step element ${selector}`);
    }
    return found;
}

function typeText(input: HTMLInputElement | HTMLTextAreaElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
}

function openTitleEditor(fixture: ComponentFixture<RecipeStepCardComponent>, root: HTMLElement): HTMLInputElement {
    element(root, 'button[aria-label="RECIPE_MANAGE.STEP_ACTIONS"]', HTMLButtonElement).click();
    fixture.detectChanges();
    const rename = [...document.querySelectorAll<HTMLButtonElement>('button[role="menuitem"]')].find(
        button => button.textContent.trim() === 'RECIPE_MANAGE.EDIT_STEP_TITLE',
    );
    if (rename === undefined) {
        throw new Error('Expected the rendered step rename action');
    }
    rename.click();
    fixture.detectChanges();
    return element(root, '.recipe-step-card__title-input input', HTMLInputElement);
}

it('edits a title from the menu, trims it on blur, and restores the numbered fallback after clearing', async () => {
    const fixture = stepFixture();
    const root = fixture.nativeElement as HTMLElement;
    const step = fixture.componentInstance;
    const titles = vi.fn<(value: string | null) => void>();
    step.stepTitleChange.subscribe(value => {
        titles(value);
        fixture.componentRef.setInput('step', { ...step.step(), title: { value, error: null } });
    });

    const editor = openTitleEditor(fixture, root);
    await fixture.whenStable();
    expect(document.activeElement).toBe(editor);
    typeText(editor, '  Simmer the rice  ');
    expect(titles).toHaveBeenLastCalledWith('  Simmer the rice  ');
    editor.blur();
    fixture.detectChanges();
    expect(titles).toHaveBeenLastCalledWith('Simmer the rice');
    expect(element(root, '.recipe-step-card__title', HTMLElement).textContent.trim()).toBe('Simmer the rice');

    const clearing = openTitleEditor(fixture, root);
    await fixture.whenStable();
    typeText(clearing, '');
    clearing.blur();
    fixture.detectChanges();
    expect(titles).toHaveBeenLastCalledWith(null);
    expect(element(root, '.recipe-step-card__title', HTMLElement).textContent.trim()).toBe('RECIPE_MANAGE.STEP_TITLE');
});

it('keeps the native description and ingredient edits when the user collapses and reopens the step', () => {
    const fixture = stepFixture();
    const root = fixture.nativeElement as HTMLElement;
    const step = fixture.componentInstance;
    step.stepDescriptionChange.subscribe(value => {
        fixture.componentRef.setInput('step', { ...step.step(), description: { value, error: null } });
    });
    step.addIngredient.subscribe(() => {
        fixture.componentRef.setInput('step', { ...step.step(), ingredients: [...step.step().ingredients, ingredient('Salt')] });
    });
    step.removeIngredient.subscribe(index => {
        fixture.componentRef.setInput('step', {
            ...step.step(),
            ingredients: step.step().ingredients.filter((_, row) => row !== index),
        });
    });
    step.toggleExpanded.subscribe(() => {
        fixture.componentRef.setInput('isExpanded', !step.isExpanded());
    });

    typeText(element(root, '.recipe-step-card__description textarea', HTMLTextAreaElement), '  Stir gently.  ');
    element(root, '.recipe-step-card__add-ingredient button', HTMLButtonElement).click();
    fixture.detectChanges();
    expect([...root.querySelectorAll<HTMLInputElement>('.recipe-ingredient__name input')].map(input => input.value)).toEqual([
        'Rice',
        'Salt',
    ]);
    element(root, '.recipe-ingredient:last-of-type button[aria-label="COMMON.DELETE"]', HTMLButtonElement).click();
    fixture.detectChanges();
    expect([...root.querySelectorAll<HTMLInputElement>('.recipe-ingredient__name input')].map(input => input.value)).toEqual(['Rice']);

    const toggle = element(root, 'button[aria-label="RECIPE_MANAGE.TOGGLE_STEP"]', HTMLButtonElement);
    toggle.click();
    fixture.detectChanges();
    expect(element(root, '.recipe-step-card__summary-description', HTMLElement).textContent.trim()).toBe('Stir gently.');
    toggle.click();
    fixture.detectChanges();
    expect(element(root, '.recipe-step-card__description textarea', HTMLTextAreaElement).value).toBe('  Stir gently.  ');
    expect(element(root, '.recipe-ingredient__name input', HTMLInputElement).value).toBe('Rice');
});

it('updates the empty description of the same collapsed step when the language changes', async () => {
    const fixture = stepFixture();
    const root = fixture.nativeElement as HTMLElement;
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { RECIPE_MANAGE: { STEP_NO_DESCRIPTION: 'No instructions yet' } });
    translate.setTranslation('ru', { RECIPE_MANAGE: { STEP_NO_DESCRIPTION: 'Описание пока отсутствует' } });
    await firstValueFrom(translate.use('en'));
    fixture.componentRef.setInput('isExpanded', false);
    fixture.detectChanges();
    const summary = element(root, '.recipe-step-card__summary-description', HTMLElement);
    expect(summary.textContent.trim()).toBe('No instructions yet');

    await firstValueFrom(translate.use('ru'));
    fixture.detectChanges();
    expect(summary.textContent.trim()).toBe('Описание пока отсутствует');
});
