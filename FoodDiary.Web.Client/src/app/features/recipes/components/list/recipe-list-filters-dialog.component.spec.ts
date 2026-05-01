import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { RecipeListFiltersDialogComponent, RecipeListFiltersDialogData } from './recipe-list-filters-dialog.component';

describe('RecipeListFiltersDialogComponent', () => {
    let component: RecipeListFiltersDialogComponent;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(data: RecipeListFiltersDialogData = { onlyMine: false }): void {
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [RecipeListFiltersDialogComponent, TranslateModule.forRoot()],
            providers: [
                provideNoopAnimations(),
                { provide: FdUiDialogRef, useValue: dialogRefSpy },
                { provide: FD_UI_DIALOG_DATA, useValue: data },
            ],
        });

        const fixture = TestBed.createComponent(RecipeListFiltersDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should initialize visibility from data when onlyMine is false', () => {
        createComponent({ onlyMine: false });
        expect(component.visibilityValue).toBe('all');
    });

    it('should initialize visibility from data when onlyMine is true', () => {
        createComponent({ onlyMine: true });
        expect(component.visibilityValue).toBe('mine');
    });

    it('should change visibility', () => {
        createComponent();
        component.onVisibilityChange('mine');
        expect(component.visibilityValue).toBe('mine');

        component.onVisibilityChange('all');
        expect(component.visibilityValue).toBe('all');
    });

    it('should close with result on apply', () => {
        createComponent({ onlyMine: false });
        component.onVisibilityChange('mine');
        component.onApply();
        expect(dialogRefSpy.close).toHaveBeenCalledWith({ onlyMine: true });
    });

    it('should close with null on cancel', () => {
        createComponent();
        component.onCancel();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(null);
    });
});
