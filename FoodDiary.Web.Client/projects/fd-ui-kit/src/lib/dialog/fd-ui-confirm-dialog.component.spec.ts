import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { FdUiConfirmDialogComponent, type FdUiConfirmDialogData } from './fd-ui-confirm-dialog.component';

describe('FdUiConfirmDialogComponent', () => {
    let component: FdUiConfirmDialogComponent;
    let fixture: ComponentFixture<FdUiConfirmDialogComponent>;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(data: FdUiConfirmDialogData): void {
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [FdUiConfirmDialogComponent, TranslateModule.forRoot()],
            providers: [
                { provide: FD_UI_DIALOG_DATA, useValue: data },
                { provide: FdUiDialogRef, useValue: dialogRefSpy },
                provideNoopAnimations(),
            ],
        });

        fixture = TestBed.createComponent(FdUiConfirmDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create', () => {
        createComponent({ title: 'Confirm', message: 'Are you sure?' });
        expect(component).toBeTruthy();
    });

    it('should display title and message', () => {
        createComponent({ title: 'Delete Item', message: 'This action cannot be undone.' });
        const nativeEl = fixture.nativeElement as HTMLElement;
        const titleEl = nativeEl.querySelector('.fd-ui-dialog__title');
        const messageEl = nativeEl.querySelector('.fd-ui-confirm-dialog__message');
        expect(titleEl).toBeTruthy();
        expect(titleEl?.textContent).toContain('Delete Item');
        expect(messageEl).toBeTruthy();
        expect(messageEl?.textContent).toContain('This action cannot be undone.');
    });

    it('should close with true on confirm', () => {
        createComponent({ title: 'Confirm', message: 'Proceed?' });
        component.onConfirm();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
    });

    it('should close with false on cancel', () => {
        createComponent({ title: 'Confirm', message: 'Proceed?' });
        component.onCancel();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });

    it('should use danger variant when danger is true', () => {
        createComponent({ title: 'Delete', message: 'Delete this?', danger: true });
        const nativeEl = fixture.nativeElement as HTMLElement;
        const confirmBtn = nativeEl.querySelector('.fd-ui-dialog__footer fd-ui-button:last-child button');
        expect(confirmBtn).toBeTruthy();
        expect(confirmBtn?.classList).toContain('fd-ui-button--danger');
    });

    it('should use custom button labels when provided', () => {
        createComponent({
            title: 'Confirm',
            message: 'Sure?',
            confirmLabel: 'Yes, do it',
            cancelLabel: 'No, go back',
        });
        const nativeEl = fixture.nativeElement as HTMLElement;
        const buttons = nativeEl.querySelectorAll('fd-ui-button');
        expect(buttons[0].textContent).toContain('No, go back');
        expect(buttons[1].textContent).toContain('Yes, do it');
    });

    it('should use default button texts when labels are not provided', () => {
        createComponent({ title: 'Confirm', message: 'Sure?' });
        const nativeEl = fixture.nativeElement as HTMLElement;
        const buttons = nativeEl.querySelectorAll('fd-ui-button');
        // With TranslateModule.forRoot() and no translations loaded, keys are returned as-is
        expect(buttons.length).toBe(2);
        expect(buttons[0].textContent.trim()).toBeTruthy();
        expect(buttons[1].textContent.trim()).toBeTruthy();
    });
});
