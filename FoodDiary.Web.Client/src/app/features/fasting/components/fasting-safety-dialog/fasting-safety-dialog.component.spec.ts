import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FastingSafetyDialogComponent, type FastingSafetyDialogData } from './fasting-safety-dialog.component';

const data: FastingSafetyDialogData = {
    title: 'Safety warning',
    message: 'Long fasts need extra care.',
    confirmLabel: 'Continue',
    cancelLabel: 'Cancel',
    tone: 'danger',
};

describe('FastingSafetyDialogComponent', () => {
    let dialogRef: { close: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        dialogRef = { close: vi.fn() };
        TestBed.configureTestingModule({
            imports: [FastingSafetyDialogComponent],
            providers: [
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: FD_UI_DIALOG_DATA, useValue: data },
            ],
        });
    });

    it('renders warning data and danger tone', () => {
        const fixture = createComponent();
        const element = getElement(fixture);

        expect(element.textContent).toContain(data.title);
        expect(element.textContent).toContain(data.message);
        expect(element.querySelector('.fasting-safety-dialog__message--danger')).not.toBeNull();
    });

    it('closes with explicit confirm and cancel results', () => {
        const fixture = createComponent();

        fixture.componentInstance.onConfirm();
        fixture.componentInstance.onCancel();

        expect(dialogRef.close).toHaveBeenNthCalledWith(1, 'confirm');
        expect(dialogRef.close).toHaveBeenNthCalledWith(2, 'cancel');
    });

    it('uses close result for informational dialogs without confirm action', () => {
        TestBed.resetTestingModule();
        dialogRef = { close: vi.fn() };
        TestBed.configureTestingModule({
            imports: [FastingSafetyDialogComponent],
            providers: [
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: FD_UI_DIALOG_DATA, useValue: { title: 'Info', message: 'Too long.', cancelLabel: 'Ok' } },
            ],
        });
        const fixture = createComponent();

        fixture.componentInstance.onClose();

        expect(dialogRef.close).toHaveBeenCalledWith('close');
    });
});

function createComponent(): ComponentFixture<FastingSafetyDialogComponent> {
    const fixture = TestBed.createComponent(FastingSafetyDialogComponent);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<FastingSafetyDialogComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}
