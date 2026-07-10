import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { ConfirmDeleteDialogComponent, type ConfirmDeleteDialogData } from './confirm-delete-dialog';

describe('ConfirmDeleteDialogComponent', () => {
    let component: ConfirmDeleteDialogComponent;
    let fixture: ComponentFixture<ConfirmDeleteDialogComponent>;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(data: ConfirmDeleteDialogData = {}): void {
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [ConfirmDeleteDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: dialogRefSpy },
                { provide: FD_UI_DIALOG_DATA, useValue: data },
            ],
        });

        fixture = TestBed.createComponent(ConfirmDeleteDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should close with true on confirm', () => {
        createComponent();
        component['onConfirm']();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
    });

    it('should close with false on cancel', () => {
        createComponent();
        component['onCancel']();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });

    it('should display custom title from data', () => {
        createComponent({ title: 'Remove item?' });
        expect(component['data'].title).toBe('Remove item?');
    });

    it('should use the configured confirm icon and localized close label', () => {
        createComponent({ confirmIcon: 'logout' });

        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('.fd-ui-button--danger .fd-ui-icon__glyph')?.textContent).toContain('logout');
        expect(element.querySelector('.fd-ui-dialog__close-button')?.getAttribute('aria-label')).toBe('COMMON.CLOSE');
    });

    it('should have default empty data when none provided', () => {
        createComponent();
        expect(component['data'].title).toBeUndefined();
        expect(component['data'].name).toBeUndefined();
    });

    it('should accept entityType and name in data', () => {
        createComponent({ entityType: 'product', name: 'Apple' });
        expect(component['data'].entityType).toBe('product');
        expect(component['data'].name).toBe('Apple');
    });
});
