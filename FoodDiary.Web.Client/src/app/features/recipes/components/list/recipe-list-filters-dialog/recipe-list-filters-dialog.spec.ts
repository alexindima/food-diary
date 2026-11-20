import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { RecipeListFiltersDialogComponent } from './recipe-list-filters-dialog';
import type { RecipeListFiltersDialogData } from './recipe-list-filters-dialog.types';

const DEFAULT_DIALOG_DATA: RecipeListFiltersDialogData = {
    onlyMine: false,
    category: null,
    maxTotalTime: null,
    caloriesFrom: null,
    caloriesTo: null,
    hasImage: null,
};

const MAX_RECIPE_TIME = 2_147_483_647;
const FRACTIONAL_RECIPE_TIME = 1.5;
const CALORIES_FROM = 400;
const CALORIES_TO = 750;
const REVERSED_CALORIES_FROM = 900;
let component: RecipeListFiltersDialogComponent;
let fixture: ComponentFixture<RecipeListFiltersDialogComponent>;
let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

function createComponent(data: RecipeListFiltersDialogData = DEFAULT_DIALOG_DATA): void {
    dialogRefSpy = { close: vi.fn() };

    TestBed.configureTestingModule({
        imports: [RecipeListFiltersDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: FdUiDialogRef, useValue: dialogRefSpy },
            { provide: FD_UI_DIALOG_DATA, useValue: data },
        ],
    });

    fixture = TestBed.createComponent(RecipeListFiltersDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}

describe('RecipeListFiltersDialogComponent', () => {
    it.each([null, '', 1, MAX_RECIPE_TIME])('allows empty or supported integer time %s', value => {
        createComponent();
        component['maxTotalTimeValue'] = value;
        component['onApply']();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(expect.objectContaining({ maxTotalTime: value === '' ? null : value }));
        expect(component['maxTotalTimeError']()).toBe(false);
    });

    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should initialize visibility from data when onlyMine is false', () => {
        createComponent({ ...DEFAULT_DIALOG_DATA, onlyMine: false });
        expect(component['visibilityValue']).toBe('all');
    });

    it('should initialize visibility from data when onlyMine is true', () => {
        createComponent({ ...DEFAULT_DIALOG_DATA, onlyMine: true });
        expect(component['visibilityValue']).toBe('mine');
    });

    it('should change visibility', () => {
        createComponent();
        component['onVisibilityChange']('mine');
        expect(component['visibilityValue']).toBe('mine');

        component['onVisibilityChange']('all');
        expect(component['visibilityValue']).toBe('all');
    });

    it('should close with result on apply', () => {
        createComponent({ ...DEFAULT_DIALOG_DATA, onlyMine: false });
        component['onVisibilityChange']('mine');
        component['onApply']();
        expect(dialogRefSpy.close).toHaveBeenCalledWith({
            onlyMine: true,
            category: null,
            maxTotalTime: null,
            caloriesFrom: null,
            caloriesTo: null,
            hasImage: null,
        });
    });

    it('should close with null on cancel', () => {
        createComponent();
        component['onCancel']();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(null);
    });
});

describe('RecipeListFiltersDialogComponent rendered filters', () => {
    it.each([-1, 0, FRACTIONAL_RECIPE_TIME, MAX_RECIPE_TIME + 1])(
        'rejects and then applies a corrected maximum time after entering %s',
        value => {
            createComponent();
            const root = fixture.nativeElement as HTMLElement;
            const timeField = 'RECIPE_LIST.FILTER_MAX_TOTAL_TIME';
            const validTime = 30;
            const apply = filterButton(root, 'RECIPE_LIST.FILTER_APPLY_BUTTON');
            changeNumberField(root, timeField, value);
            apply.click();
            fixture.detectChanges();
            expect(root.textContent).toContain('FORM_ERRORS.INVALID_NUMBER');
            expect(dialogRefSpy.close).not.toHaveBeenCalled();

            changeNumberField(root, timeField, validTime);
            apply.click();
            fixture.detectChanges();
            expect(root.textContent).not.toContain('FORM_ERRORS.INVALID_NUMBER');
            expect(dialogRefSpy.close).toHaveBeenCalledExactlyOnceWith({ ...DEFAULT_DIALOG_DATA, maxTotalTime: validTime });
        },
    );

    it.each([
        [null, 'RECIPE_LIST.FILTER_IMAGE_WITH', true],
        [true, 'RECIPE_LIST.FILTER_IMAGE_WITHOUT', false],
        [false, 'RECIPE_LIST.FILTER_IMAGE_ANY', null],
    ] as const)('applies the visible media control with initial value %s', (initial, label, expected) => {
        createComponent({ ...DEFAULT_DIALOG_DATA, hasImage: initial });
        const root = fixture.nativeElement as HTMLElement;
        filterButton(root, label).click();
        fixture.detectChanges();
        expect(filterButton(root, label).getAttribute('aria-checked')).toBe('true');

        filterButton(root, 'RECIPE_LIST.FILTER_APPLY_BUTTON').click();
        expect(dialogRefSpy.close).toHaveBeenCalledExactlyOnceWith({ ...DEFAULT_DIALOG_DATA, hasImage: expected });
    });

    it('keeps a reversed calorie range open, then submits the corrected range and chosen category', () => {
        createComponent();
        const root = fixture.nativeElement as HTMLElement;
        changeNumberField(root, 'RECIPE_LIST.FILTER_CALORIES_FROM', REVERSED_CALORIES_FROM);
        changeNumberField(root, 'RECIPE_LIST.FILTER_CALORIES_TO', CALORIES_TO);
        const apply = filterButton(root, 'RECIPE_LIST.FILTER_APPLY_BUTTON');
        apply.click();
        fixture.detectChanges();
        expect(root.textContent).toContain('RECIPE_LIST.FILTER_CALORIES_ORDER_ERROR');
        expect(dialogRefSpy.close).not.toHaveBeenCalled();

        changeNumberField(root, 'RECIPE_LIST.FILTER_CALORIES_FROM', CALORIES_FROM);
        root.querySelector<HTMLButtonElement>('button[aria-haspopup="listbox"]')?.click();
        fixture.detectChanges();
        filterButton(document.body, 'RECIPE_CATEGORIES.desserts').click();
        fixture.detectChanges();
        filterButton(root, 'RECIPE_LIST.FILTER_MY_RECIPES').click();
        apply.click();
        fixture.detectChanges();

        expect(root.textContent).not.toContain('RECIPE_LIST.FILTER_CALORIES_ORDER_ERROR');
        expect(dialogRefSpy.close).toHaveBeenCalledExactlyOnceWith({
            ...DEFAULT_DIALOG_DATA,
            onlyMine: true,
            category: 'desserts',
            caloriesFrom: CALORIES_FROM,
            caloriesTo: CALORIES_TO,
        });
    });
});

function filterButton(root: HTMLElement, label: string): HTMLButtonElement {
    const button = Array.from(root.querySelectorAll('button')).find(control => control.textContent.trim() === label);
    if (button === undefined) {
        throw new Error(`Expected recipe filter button "${label}"`);
    }
    return button;
}

function changeNumberField(root: HTMLElement, label: string, value: number): void {
    const field = Array.from(root.querySelectorAll('label')).find(control => control.textContent.trim() === label)?.control;
    if (!(field instanceof HTMLInputElement)) {
        throw new Error(`Expected recipe filter field "${label}"`);
    }
    field.value = String(value);
    field.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
}
