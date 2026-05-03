import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { UnsavedChangesDialogComponent, type UnsavedChangesDialogData } from './unsaved-changes-dialog.component';

describe('UnsavedChangesDialogComponent', () => {
    let component: UnsavedChangesDialogComponent;
    let fixture: ComponentFixture<UnsavedChangesDialogComponent>;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(data: UnsavedChangesDialogData | null = null): void {
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [UnsavedChangesDialogComponent, TranslateModule.forRoot()],
            providers: [
                { provide: FdUiDialogRef, useValue: dialogRefSpy },
                { provide: FD_UI_DIALOG_DATA, useValue: data },
            ],
        });

        fixture = TestBed.createComponent(UnsavedChangesDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should close with save', () => {
        createComponent();
        component.onSave();
        expect(dialogRefSpy.close).toHaveBeenCalledWith('save');
    });

    it('should close with discard', () => {
        createComponent();
        component.onDiscard();
        expect(dialogRefSpy.close).toHaveBeenCalledWith('discard');
    });

    it('should close with stay', () => {
        createComponent();
        component.onStay();
        expect(dialogRefSpy.close).toHaveBeenCalledWith('stay');
    });

    it('should work without data', () => {
        createComponent(null);
        expect(component).toBeTruthy();
        expect(component.data).toBeNull();
    });

    it('should accept custom labels', () => {
        createComponent({
            title: 'Leave page?',
            message: 'You have unsaved work.',
            saveLabel: 'Save & Leave',
            discardLabel: 'Discard Changes',
            stayLabel: 'Keep Editing',
        });
        expect(component.data?.title).toBe('Leave page?');
        expect(component.data?.saveLabel).toBe('Save & Leave');
    });
});
