import { TestBed } from '@angular/core/testing';
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
let component: RecipeListFiltersDialogComponent;
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

    const fixture = TestBed.createComponent(RecipeListFiltersDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}

describe('RecipeListFiltersDialogComponent', () => {
    it.each([-1, 0, FRACTIONAL_RECIPE_TIME, MAX_RECIPE_TIME + 1])('rejects unsupported maximum time %s without closing', value => {
        createComponent();
        component['maxTotalTimeValue'] = value;
        component['onApply']();
        expect(dialogRefSpy.close).not.toHaveBeenCalled();
        expect(component['maxTotalTimeError']()).toBe(true);
    });

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
