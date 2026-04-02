import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { AuthService } from '../../../services/auth.service';
import { LocalizationService } from '../../../services/localization.service';
import { NavigationService } from '../../../services/navigation.service';
import { UserService } from '../../../shared/api/user.service';
import { UpdateUserDto } from '../../../shared/models/user.data';
import { ProfileManageFacade } from './profile-manage.facade';

describe('ProfileManageFacade', () => {
    let facade: ProfileManageFacade;
    let userService: { getInfo: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn>; deleteCurrentUser: ReturnType<typeof vi.fn> };
    let dialogService: { open: ReturnType<typeof vi.fn> };
    let authService: { onLogout: ReturnType<typeof vi.fn>; startAdminSso: ReturnType<typeof vi.fn> };
    let localizationService: { applyLanguagePreference: ReturnType<typeof vi.fn> };
    let navigationService: { navigateToHome: ReturnType<typeof vi.fn> };

    const user = {
        id: 'u1',
        email: 'test@example.com',
        language: 'ru',
        isActive: true,
        isEmailConfirmed: true,
    };

    beforeEach(() => {
        userService = {
            getInfo: vi.fn().mockReturnValue(of(user as any)),
            update: vi.fn().mockReturnValue(of(user as any)),
            deleteCurrentUser: vi.fn().mockReturnValue(of(true)),
        };
        dialogService = {
            open: vi.fn(),
        };
        authService = {
            onLogout: vi.fn().mockResolvedValue(undefined),
            startAdminSso: vi.fn().mockReturnValue(of({ code: 'abc123', expiresAtUtc: '2026-04-02T00:00:00Z' })),
        };
        localizationService = {
            applyLanguagePreference: vi.fn().mockResolvedValue(undefined),
        };
        navigationService = {
            navigateToHome: vi.fn().mockResolvedValue(undefined),
        };

        dialogService.open.mockReturnValue({ afterClosed: () => of(false) });

        TestBed.configureTestingModule({
            providers: [
                ProfileManageFacade,
                { provide: UserService, useValue: userService },
                { provide: FdUiDialogService, useValue: dialogService },
                { provide: AuthService, useValue: authService },
                { provide: LocalizationService, useValue: localizationService },
                { provide: NavigationService, useValue: navigationService },
                {
                    provide: TranslateService,
                    useValue: {
                        instant: vi.fn((key: string) => key),
                    },
                },
            ],
        });

        facade = TestBed.inject(ProfileManageFacade);
    });

    it('loads user and applies language on initialize', () => {
        facade.initialize();

        expect(userService.getInfo).toHaveBeenCalledTimes(1);
        expect(facade.user()).toEqual(user as any);
        expect(localizationService.applyLanguagePreference).toHaveBeenCalledWith('ru');
        expect(facade.globalError()).toBeNull();
    });

    it('sets global error when update returns null', () => {
        userService.update.mockReturnValueOnce(of(null));

        facade.submitUpdate(new UpdateUserDto({ username: 'alex' }));

        expect(facade.globalError()).toBe('USER_MANAGE.UPDATE_ERROR');
    });

    it('shows success dialog and navigates home after successful update', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) });

        facade.submitUpdate(new UpdateUserDto({ username: 'alex' }));

        expect(userService.update).toHaveBeenCalledTimes(1);
        expect(dialogService.open).toHaveBeenCalled();
        expect(navigationService.navigateToHome).toHaveBeenCalled();
    });

    it('opens password success dialog after successful password dialog close', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) }).mockReturnValueOnce({ afterClosed: () => of(undefined) });

        facade.openChangePasswordDialog();

        expect(dialogService.open).toHaveBeenCalledTimes(2);
    });

    it('logs out after confirmed successful account deletion', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) });

        facade.deleteAccount();

        expect(userService.deleteCurrentUser).toHaveBeenCalledTimes(1);
        expect(authService.onLogout).toHaveBeenCalledWith(true);
        expect(facade.isDeleting()).toBe(false);
    });

    it('sets global error when account deletion fails', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) });
        userService.deleteCurrentUser.mockReturnValueOnce(throwError(() => new Error('delete failed')));

        facade.deleteAccount();

        expect(facade.globalError()).toBe('USER_MANAGE.DELETE_ACCOUNT_ERROR');
        expect(facade.isDeleting()).toBe(false);
    });
});
