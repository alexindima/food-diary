import { TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { UpdateSuccessDialogComponent } from './update-success-dialog';

describe('UpdateSuccessDialogComponent', () => {
    it('closes dialog with redirect flag', async () => {
        const dialogRef = { close: vi.fn() };

        await TestBed.configureTestingModule({
            imports: [UpdateSuccessDialogComponent],
            providers: [provideTranslateTesting(), { provide: FdUiDialogRef, useValue: dialogRef }],
        }).compileComponents();

        const fixture = TestBed.createComponent(UpdateSuccessDialogComponent);

        fixture.componentInstance['close'](true);

        expect(dialogRef.close).toHaveBeenCalledWith(true);
    });
});
