import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { PremiumRequiredDialogComponent, type PremiumRequiredDialogData } from './premium-required-dialog.component';

describe('PremiumRequiredDialogComponent', () => {
    let component: PremiumRequiredDialogComponent;
    let fixture: ComponentFixture<PremiumRequiredDialogComponent>;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(data: PremiumRequiredDialogData | null = {}): void {
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [PremiumRequiredDialogComponent, TranslateModule.forRoot()],
            providers: [
                { provide: FdUiDialogRef, useValue: dialogRefSpy },
                { provide: FD_UI_DIALOG_DATA, useValue: data },
            ],
        });

        fixture = TestBed.createComponent(PremiumRequiredDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should close with true on confirm', () => {
        createComponent();
        component.onConfirm();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
    });

    it('should close with false on cancel', () => {
        createComponent();
        component.onCancel();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });

    it('should work without data', () => {
        createComponent(null);
        expect(component).toBeTruthy();
        expect(component.data).toEqual({});
    });

    it('should accept custom labels', () => {
        createComponent({
            title: 'Upgrade Required',
            message: 'This feature requires premium.',
            actionLabel: 'Upgrade Now',
            cancelLabel: 'Maybe Later',
        });
        expect(component.data.title).toBe('Upgrade Required');
        expect(component.data.actionLabel).toBe('Upgrade Now');
    });
});
