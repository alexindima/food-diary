import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, type ParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { BehaviorSubject, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import type { DietologistInvitationForCurrentUser } from '../../../../shared/models/dietologist.data';
import { DietologistFacade } from '../../lib/dietologist.facade';
import { DietologistInvitationPageComponent } from './dietologist-invitation-page';

let fixture: ComponentFixture<DietologistInvitationPageComponent>;
let component: DietologistInvitationPageComponent;
let routeParams: BehaviorSubject<ParamMap>;
let dietologistService: {
    acceptInvitationForCurrentUser: ReturnType<typeof vi.fn>;
    declineInvitationForCurrentUser: ReturnType<typeof vi.fn>;
    getInvitationForCurrentUser: ReturnType<typeof vi.fn>;
};
let navigationService: {
    navigateToDietologistAsync: ReturnType<typeof vi.fn>;
    navigateToHomeAsync: ReturnType<typeof vi.fn>;
};
let authService: {
    refreshToken: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    routeParams = new BehaviorSubject(convertToParamMap({ invitationId: 'inv-1' }));
    dietologistService = {
        getInvitationForCurrentUser: vi.fn(),
        acceptInvitationForCurrentUser: vi.fn(),
        declineInvitationForCurrentUser: vi.fn(),
    };
    navigationService = {
        navigateToDietologistAsync: vi.fn().mockResolvedValue(void 0),
        navigateToHomeAsync: vi.fn().mockResolvedValue(void 0),
    };
    authService = {
        refreshToken: vi.fn().mockReturnValue(of({})),
    };
});

describe('DietologistInvitationPageComponent accepted state', () => {
    it('shows accepted state when invitation is already accepted', () => {
        dietologistService.getInvitationForCurrentUser.mockReturnValue(
            of({
                invitationId: 'inv-1',
                clientUserId: 'client-1',
                clientEmail: 'client@example.com',
                clientFirstName: 'Client',
                clientLastName: 'Name',
                status: 'Accepted',
                createdAtUtc: '2026-04-15T00:00:00Z',
                expiresAtUtc: '2026-04-22T00:00:00Z',
            }),
        );

        createComponent();

        expect(component['state']()).toBe('accepted');
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('DIETOLOGIST_INVITATION.SUCCESS_ACCEPT');
    });
});

describe('DietologistInvitationPageComponent route changes', () => {
    it('loads the new invitation and ignores a late response from the old one', () => {
        const oldRequest = new Subject<DietologistInvitationForCurrentUser>();
        const newInvitation: DietologistInvitationForCurrentUser = {
            invitationId: 'inv-2',
            clientUserId: 'client-2',
            clientEmail: 'client2@example.invalid',
            clientFirstName: 'New',
            clientLastName: null,
            status: 'Pending',
            createdAtUtc: '2026-04-15T00:00:00Z',
            expiresAtUtc: '2026-04-22T00:00:00Z',
        };
        dietologistService.getInvitationForCurrentUser.mockReturnValueOnce(oldRequest).mockReturnValueOnce(of(newInvitation));
        createComponent();
        routeParams.next(convertToParamMap({ invitationId: 'inv-2' }));
        oldRequest.next({ ...newInvitation, invitationId: 'inv-1', status: 'Revoked' });
        fixture.detectChanges();
        expect(component['state']()).toBe('ready');
        expect(component['invitation']()?.invitationId).toBe('inv-2');
        expect(dietologistService.getInvitationForCurrentUser).toHaveBeenLastCalledWith('inv-2');
    });
});

describe('DietologistInvitationPageComponent error state', () => {
    it('shows error state when invitation request fails', () => {
        dietologistService.getInvitationForCurrentUser.mockReturnValue(throwError(() => new Error('load failed')));

        createComponent();

        expect(component['state']()).toBe('error');
        expect(component['errorMessage']()).toBe('DIETOLOGIST_INVITATION.ERROR_LOAD');
    });
});

describe('DietologistInvitationPageComponent action recovery', () => {
    it.each([
        { action: 'accept', method: 'acceptInvitationForCurrentUser', success: 'accepted', error: 'DIETOLOGIST_INVITATION.ERROR_ACCEPT' },
        {
            action: 'decline',
            method: 'declineInvitationForCurrentUser',
            success: 'declined',
            error: 'DIETOLOGIST_INVITATION.ERROR_DECLINE',
        },
    ] as const)('keeps the invitation actionable after a failed $action and allows retry', ({ action, method, success, error }) => {
        dietologistService.getInvitationForCurrentUser.mockReturnValue(
            of({
                invitationId: 'inv-1',
                clientUserId: 'client-1',
                clientEmail: 'client@example.com',
                clientFirstName: 'Client',
                clientLastName: null,
                status: 'Pending',
                createdAtUtc: '2026-04-15T00:00:00Z',
                expiresAtUtc: '2026-04-22T00:00:00Z',
            }),
        );
        dietologistService[method]
            .mockReturnValueOnce(throwError(() => new Error('temporarily unavailable')))
            .mockReturnValueOnce(of(void 0));
        createComponent();
        component[action]();
        fixture.detectChanges();
        expect(component['state']()).toBe('ready');
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="alert"]')?.textContent).toContain(error);
        expect(host.textContent).toContain('DIETOLOGIST_INVITATION.ACCEPT');
        expect(host.textContent).toContain('DIETOLOGIST_INVITATION.DECLINE');
        expect(component['isSubmitting']()).toBe(false);
        component[action]();
        fixture.detectChanges();
        expect(component['state']()).toBe(success);
        expect(component['errorMessage']()).toBeNull();
        expect(dietologistService[method]).toHaveBeenCalledTimes(2);
    });
});

function createComponent(): void {
    TestBed.configureTestingModule({
        imports: [DietologistInvitationPageComponent],
        providers: [
            provideTranslateTesting(),
            { provide: DietologistFacade, useValue: dietologistService },
            { provide: NavigationService, useValue: navigationService },
            { provide: AuthService, useValue: authService },
            {
                provide: ActivatedRoute,
                useValue: {
                    paramMap: routeParams.asObservable(),
                    snapshot: {
                        paramMap: convertToParamMap({ invitationId: 'inv-1' }),
                    },
                },
            },
        ],
    });

    const translateService = TestBed.inject(TranslateService);
    vi.spyOn(translateService, 'instant').mockImplementation((key: string) => key);

    fixture = TestBed.createComponent(DietologistInvitationPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}
