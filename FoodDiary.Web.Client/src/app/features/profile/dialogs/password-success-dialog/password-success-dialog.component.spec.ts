import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { PasswordSuccessDialogComponent } from './password-success-dialog.component';

describe('PasswordSuccessDialogComponent', () => {
    it('closes dialog', async () => {
        const dialogRef = { close: vi.fn() };

        await TestBed.configureTestingModule({
            imports: [PasswordSuccessDialogComponent, TranslateModule.forRoot()],
            providers: [{ provide: FdUiDialogRef, useValue: dialogRef }],
        }).compileComponents();

        const fixture = TestBed.createComponent(PasswordSuccessDialogComponent);

        fixture.componentInstance.close();

        expect(dialogRef.close).toHaveBeenCalledOnce();
    });
});
