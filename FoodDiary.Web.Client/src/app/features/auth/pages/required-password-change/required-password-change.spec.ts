import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { RequiredPasswordChangeComponent } from './required-password-change';

const PASSWORD_CONTRACT_MAX_LENGTH = 256;

function createBoundaryComponent(): { component: RequiredPasswordChangeComponent; changePassword: ReturnType<typeof vi.fn> } {
    const changePassword = vi.fn().mockReturnValue(of(true));
    const fixture = TestBed.configureTestingModule({
        imports: [RequiredPasswordChangeComponent],
        providers: [
            { provide: UserFacade, useValue: { changePassword } },
            { provide: AuthService, useValue: { completeRequiredPasswordChangeAsync: vi.fn().mockResolvedValue(undefined) } },
            { provide: NavigationService, useValue: { navigateToHomeAsync: vi.fn().mockResolvedValue(void 0) } },
            { provide: TranslateService, useValue: { instant: (key: string): string => key } },
        ],
    })
        .overrideComponent(RequiredPasswordChangeComponent, { set: { template: '' } })
        .createComponent(RequiredPasswordChangeComponent);
    fixture.detectChanges();
    return { component: fixture.componentInstance, changePassword };
}

describe('Required password change boundaries', () => {
    it.each(['currentPassword', 'newPassword', 'confirmPassword'] as const)('blocks an oversized %s before the API', async field => {
        const { component, changePassword } = createBoundaryComponent();
        component['formModel'].set({ currentPassword: 'current-password', newPassword: 'new-password', confirmPassword: 'new-password' });
        component['form'][field]().value.set('p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH + 1));

        await component['submitAsync']();

        expect(component['form'][field]().getError('maxLength')).toBeDefined();
        expect(changePassword).not.toHaveBeenCalled();
    });

    it('submits boundary passwords with literal whitespace preserved', async () => {
        const { component, changePassword } = createBoundaryComponent();
        const currentPassword = ` ${'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH - 2)} `;
        const newPassword = ` ${'q'.repeat(PASSWORD_CONTRACT_MAX_LENGTH - 2)} `;
        component['formModel'].set({ currentPassword, newPassword, confirmPassword: newPassword });

        await component['submitAsync']();

        expect(changePassword).toHaveBeenCalledWith({ currentPassword, newPassword });
    });
});

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
