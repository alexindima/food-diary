import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminRecipeModerationRuleCategoriesComponent } from './admin-recipe-moderation-rule-categories';

type CategoriesFixture = ComponentFixture<AdminRecipeModerationRuleCategoriesComponent>;
type CategoryChange = { category: string; selected: boolean };

it('labels the category group and reports the selected and removed category from native checkboxes', () => {
    const { fixture, changed } = mountCategories();
    const violence = checkbox(fixture, 'Violence');
    const graphic = checkbox(fixture, 'Graphic violence');
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('fieldset legend')?.textContent).toBe('Image review');
    expect(violence.checked).toBe(true);
    expect(graphic.checked).toBe(false);

    graphic.click();
    violence.click();
    expect(changed.mock.calls).toEqual([[{ category: 'violence/graphic', selected: true }], [{ category: 'violence', selected: false }]]);
});

it('blocks a disabled category and applies a later selection supplied by its parent', () => {
    const { fixture, changed } = mountCategories();
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    const checkboxControl = checkbox(fixture, 'Graphic violence');
    expect(checkboxControl.disabled).toBe(true);
    checkboxControl.click();
    expect(checkboxControl.checked).toBe(false);
    expect(changed).not.toHaveBeenCalled();

    fixture.componentRef.setInput('selected', ['violence/graphic']);
    fixture.componentRef.setInput('disabled', false);
    fixture.detectChanges();
    expect(checkboxControl.disabled).toBe(false);
    expect(checkboxControl.checked).toBe(true);
    expect(checkbox(fixture, 'Violence').checked).toBe(false);
    expect(changed).not.toHaveBeenCalled();
});

function mountCategories(): { fixture: CategoriesFixture; changed: Mock<(event: CategoryChange) => void> } {
    TestBed.configureTestingModule({ imports: [AdminRecipeModerationRuleCategoriesComponent], providers: provideTranslateTesting() });
    const fixture = TestBed.createComponent(AdminRecipeModerationRuleCategoriesComponent);
    const changed = vi.fn<(event: CategoryChange) => void>();
    fixture.componentInstance.categoryChange.subscribe(changed);
    fixture.componentRef.setInput('legend', 'Image review');
    fixture.componentRef.setInput('options', [
        { value: 'violence', label: 'Violence' },
        { value: 'violence/graphic', label: 'Graphic violence' },
    ]);
    fixture.componentRef.setInput('selected', ['violence']);
    fixture.detectChanges();
    return { fixture, changed };
}

function checkbox(fixture: CategoriesFixture, label: string): HTMLInputElement {
    const root = fixture.nativeElement as HTMLElement;
    const control = Array.from(root.querySelectorAll('label')).find(element => element.textContent.trim() === label)?.control;
    if (!(control instanceof HTMLInputElement)) {
        throw new Error(`Expected a native checkbox for ${label}.`);
    }
    return control;
}
