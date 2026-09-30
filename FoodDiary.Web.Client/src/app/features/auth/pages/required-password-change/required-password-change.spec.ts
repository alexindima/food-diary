import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { RequiredPasswordChangeComponent } from './required-password-change';

describe('RequiredPasswordChangeComponent', () => {
    it('renders the required password-change route with its dependencies', () => {
        const fixture = TestBed.configureTestingModule({
            imports: [RequiredPasswordChangeComponent],
            providers: [
                { provide: UserFacade, useValue: { changePassword: vi.fn().mockReturnValue(of(true)) } },
                {
                    provide: AuthService,
                    useValue: {
                        completeRequiredPasswordChangeAsync: vi.fn().mockResolvedValue(undefined),
                        onLogoutAsync: vi.fn().mockResolvedValue(void 0),
                    },
                },
                { provide: NavigationService, useValue: { navigateToHomeAsync: vi.fn().mockResolvedValue(void 0) } },
                { provide: TranslateService, useValue: { instant: (key: string): string => key } },
            ],
        })
            .overrideComponent(RequiredPasswordChangeComponent, { set: { template: '' } })
            .createComponent(RequiredPasswordChangeComponent);

        fixture.detectChanges();

        expect(fixture.componentInstance).toBeTruthy();
    });
});

describe('Required password change completion', () => {
    it('retries session renewal after throttling without changing the password again or navigating with an old token', async () => {
        const changePassword = vi.fn().mockReturnValue(of(true));
        const renew = vi
            .fn()
            .mockRejectedValueOnce(new HttpErrorResponse({ status: 429 }))
            .mockResolvedValue(undefined);
        const navigate = vi.fn().mockResolvedValue(undefined);
        const fixture = TestBed.configureTestingModule({
            imports: [RequiredPasswordChangeComponent],
            providers: [
                { provide: UserFacade, useValue: { changePassword } },
                { provide: AuthService, useValue: { completeRequiredPasswordChangeAsync: renew } },
                { provide: NavigationService, useValue: { navigateToHomeAsync: navigate } },
                { provide: TranslateService, useValue: { instant: (key: string): string => key } },
            ],
        })
            .overrideComponent(RequiredPasswordChangeComponent, { set: { template: '' } })
            .createComponent(RequiredPasswordChangeComponent);
        fixture.detectChanges();
        const component = fixture.componentInstance;
        component['formModel'].set({
            currentPassword: 'temporary-password',
            newPassword: 'changed-password',
            confirmPassword: 'changed-password',
        });
        await component['submitAsync']();
        expect(navigate).not.toHaveBeenCalled();
        expect(component['errorMessage']()).toBe('FORM_ERRORS.RATE_LIMITED');
        expect(component['isSubmitting']()).toBe(false);
        await component['submitAsync']();
        expect(changePassword).toHaveBeenCalledOnce();
        expect(renew).toHaveBeenCalledTimes(2);
        expect(navigate).toHaveBeenCalledOnce();
    });
});
