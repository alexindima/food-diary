import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FastingEndConfirmDialogComponent, type FastingEndConfirmDialogData } from './fasting-end-confirm-dialog.component';

const data: FastingEndConfirmDialogData = {
    title: 'End fast?',
    message: 'This will complete the session.',
    confirmLabel: 'End',
    cancelLabel: 'Keep fasting',
};

describe('FastingEndConfirmDialogComponent', () => {
    let dialogRef: { close: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        dialogRef = { close: vi.fn() };
        TestBed.configureTestingModule({
            imports: [FastingEndConfirmDialogComponent],
            providers: [
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: FD_UI_DIALOG_DATA, useValue: data },
            ],
        });
    });

    it('renders dialog copy', () => {
        const fixture = createComponent();
        const text = getElement(fixture).textContent;

        expect(text).toContain(data.title);
        expect(text).toContain(data.message);
        expect(text).toContain(data.confirmLabel);
        expect(text).toContain(data.cancelLabel);
    });

    it('closes with confirm or cancel result', () => {
        const fixture = createComponent();

        fixture.componentInstance.onConfirm();
        fixture.componentInstance.onCancel();

        expect(dialogRef.close).toHaveBeenNthCalledWith(1, 'confirm');
        expect(dialogRef.close).toHaveBeenNthCalledWith(2, 'cancel');
    });
});

function createComponent(): ComponentFixture<FastingEndConfirmDialogComponent> {
    const fixture = TestBed.createComponent(FastingEndConfirmDialogComponent);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<FastingEndConfirmDialogComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}
