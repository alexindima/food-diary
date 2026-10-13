import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { type FdUiDialogConfig, FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { type PublicAuthDialogRef, PublicAuthDialogService } from './public-auth-dialog.service';

describe('Public account-access dialog', () => {
    it('does not render an interactive overlay during server rendering', async () => {
        const open = vi.fn();
        TestBed.configureTestingModule({ providers: [provideTranslateTesting(),
            { provide: PLATFORM_ID, useValue: 'server' }, { provide: FdUiDialogService, useValue: { open } }] });
        const result = await TestBed.inject(PublicAuthDialogService).openAsync({ mode: 'login' });
        expect(result).toBeNull();
        expect(open).not.toHaveBeenCalled();
    });
    it.each(['login', 'register'] as const)('names the %s dialog while preserving its focus and return destination', async mode => {
        const open = vi.fn<(component: unknown, config: FdUiDialogConfig) => PublicAuthDialogRef>().mockReturnValue({
            afterClosed: () => of(undefined),
        });
        TestBed.configureTestingModule({
            providers: [provideTranslateTesting(), { provide: FdUiDialogService, useValue: { open } }],
        });
        await TestBed.inject(PublicAuthDialogService).openAsync({ mode, returnUrl: '/explore' });
        expect(open).toHaveBeenCalledWith(
            expect.any(Function),
            expect.objectContaining({
                ariaLabel: 'AUTH.DIALOG_TITLE',
                autoFocus: mode === 'login' ? '#auth-login-email' : '#auth-register-email',
            }),
        );
        expect(open.mock.calls[0][1].data).toMatchObject({ mode, returnUrl: '/explore' });
    });
});
