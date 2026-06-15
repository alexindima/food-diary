import { TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { PasswordSuccessDialogComponent } from './password-success-dialog';

describe('PasswordSuccessDialogComponent', () => {
    it('closes dialog', async () => {
        const dialogRef = { close: vi.fn() };

        await TestBed.configureTestingModule({
            imports: [PasswordSuccessDialogComponent],
            providers: [provideTranslateTesting(), { provide: FdUiDialogRef, useValue: dialogRef }],
        }).compileComponents();

        const fixture = TestBed.createComponent(PasswordSuccessDialogComponent);

        fixture.componentInstance['close']();

        expect(dialogRef.close).toHaveBeenCalledOnce();
    });
});
