import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, type ParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { BehaviorSubject, firstValueFrom, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import type { DietologistInvitationForCurrentUser } from '../../../../shared/models/dietologist.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
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
    refreshSession: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    routeParams = new BehaviorSubject(convertToParamMap({ invitationId: entityId<'dietologist-invitation'>('inv-1') }));
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
        refreshSession: vi.fn().mockReturnValue(of({})),
    };
});

describe('DietologistInvitationPageComponent rendered invitation actions', () => {
    it.each([
        ['Declined', 'DIETOLOGIST_INVITATION.SUCCESS_DECLINE'],
        ['Expired', 'DIETOLOGIST_INVITATION.EXPIRED'],
        ['Revoked', 'DIETOLOGIST_INVITATION.REVOKED'],
    ])('renders a %s invitation and offers to return home', (status, message) => {
        dietologistService.getInvitationForCurrentUser.mockReturnValueOnce(of(createInvitation(status)));
        createComponent();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain(message);
        expect(host.textContent).not.toContain('DIETOLOGIST_INVITATION.ACCEPT');
        expect(host.textContent).not.toContain('DIETOLOGIST_INVITATION.DECLINE');

        invitationButton('DIETOLOGIST_INVITATION.GO_HOME').click();
        expect(navigationService.navigateToHomeAsync).toHaveBeenCalledOnce();
        expect(dietologistService.acceptInvitationForCurrentUser).not.toHaveBeenCalled();
    });

    it('shows the invalid-invitation state without fetching an empty route value', () => {
        routeParams.next(convertToParamMap({ invitationId: '' }));
        createComponent();
        const host = fixture.nativeElement as HTMLElement;

        expect(host.querySelector('[role="alert"]')?.textContent).toContain('DIETOLOGIST_INVITATION.ERROR_INVALID');
        expect(dietologistService.getInvitationForCurrentUser).not.toHaveBeenCalled();
        invitationButton('DIETOLOGIST_INVITATION.GO_HOME').click();
        expect(navigationService.navigateToHomeAsync).toHaveBeenCalledOnce();
    });
});

describe('DietologistInvitationPageComponent invitation lifecycle', () => {
    it('keeps the invitation accepted when the subsequent session refresh fails', () => {
        const invitation = createInvitation('Pending');
        const refresh = new Subject<void>();
        dietologistService.getInvitationForCurrentUser.mockReturnValueOnce(of(invitation));
        dietologistService.acceptInvitationForCurrentUser.mockReturnValueOnce(of(void 0));
        authService.refreshSession.mockReturnValueOnce(refresh);
        createComponent();

        const accept = invitationButton('DIETOLOGIST_INVITATION.ACCEPT');
        accept.click();
        fixture.detectChanges();
        expect(accept.disabled).toBe(true);
        expect(invitationButton('DIETOLOGIST_INVITATION.DECLINE').disabled).toBe(true);
        expect(dietologistService.acceptInvitationForCurrentUser).toHaveBeenCalledExactlyOnceWith(invitation.invitationId);
        expect(authService.refreshSession).toHaveBeenCalledOnce();

        refresh.error(new Error('session temporarily unavailable'));
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('DIETOLOGIST_INVITATION.SUCCESS_ACCEPT');
        invitationButton('DIETOLOGIST_INVITATION.OPEN_CLIENTS').click();
        expect(navigationService.navigateToDietologistAsync).toHaveBeenCalledOnce();
    });

    it('shows the new invitation when a previous acceptance is still pending', () => {
        const pending = new Subject<void>();
        const first = createInvitation('Pending');
        const second = { ...createInvitation('Revoked'), invitationId: entityId<'dietologist-invitation'>('inv-2') };
        dietologistService.getInvitationForCurrentUser.mockReturnValueOnce(of(first)).mockReturnValueOnce(of(second));
        dietologistService.acceptInvitationForCurrentUser.mockReturnValueOnce(pending);
        createComponent();

        invitationButton('DIETOLOGIST_INVITATION.ACCEPT').click();
        expect(dietologistService.acceptInvitationForCurrentUser).toHaveBeenCalledExactlyOnceWith(first.invitationId);
        expect(pending.observed).toBe(true);
        routeParams.next(convertToParamMap({ invitationId: second.invitationId }));
        expect(pending.observed).toBe(false);
        pending.next();
        pending.complete();
        fixture.componentRef.setInput('invitationId', second.invitationId);
        fixture.detectChanges();

        expect(dietologistService.getInvitationForCurrentUser).toHaveBeenLastCalledWith(second.invitationId);
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('DIETOLOGIST_INVITATION.REVOKED');
        expect(authService.refreshSession).not.toHaveBeenCalled();
    });

    it('formats the invitation expiry again when the language changes', async () => {
        const invitation = { ...createInvitation('Pending'), clientFirstName: null, clientLastName: null };
        dietologistService.getInvitationForCurrentUser.mockReturnValueOnce(of(invitation));
        createComponent();
        const translate = TestBed.inject(TranslateService);
        const translation = { DIETOLOGIST_INVITATION: { DESCRIPTION: '{{name}}', EXPIRES: '{{date}}' } };
        translate.setTranslation('en', translation);
        translate.setTranslation('ru', translation);
        await firstValueFrom(translate.use('en'));
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.dietologist-invitation__description')?.textContent).toContain(invitation.clientEmail);
        expect(host.querySelector('.dietologist-invitation__meta')?.textContent).toContain('Apr');

        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();
        expect(host.querySelector('.dietologist-invitation__meta')?.textContent).toContain('апр');
    });
});

function createInvitation(status: DietologistInvitationForCurrentUser['status']): DietologistInvitationForCurrentUser {
    return {
        invitationId: entityId<'dietologist-invitation'>('inv-1'),
        clientUserId: entityId<'user'>('client-1'),
        clientEmail: 'client@example.com',
        clientFirstName: 'Client',
        clientLastName: 'Name',
        status,
        createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
        expiresAtUtc: utcInstant('2026-04-22T00:00:00Z'),
    };
}

function invitationButton(label: string): HTMLButtonElement {
    const host = fixture.nativeElement as HTMLElement;
    const button = Array.from(host.querySelectorAll('button')).find(candidate => candidate.textContent.trim() === label);
    if (button === undefined) {
        throw new Error(`Invitation button "${label}" was not found`);
    }
    return button;
}

describe('DietologistInvitationPageComponent accepted state', () => {
    it('shows accepted state when invitation is already accepted', () => {
        dietologistService.getInvitationForCurrentUser.mockReturnValue(
            of({
                invitationId: entityId<'dietologist-invitation'>('inv-1'),
                clientUserId: entityId<'user'>('client-1'),
                clientEmail: 'client@example.com',
                clientFirstName: 'Client',
                clientLastName: 'Name',
                status: 'Accepted',
                createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
                expiresAtUtc: utcInstant('2026-04-22T00:00:00Z'),
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
            invitationId: entityId<'dietologist-invitation'>('inv-2'),
            clientUserId: entityId<'user'>('client-2'),
            clientEmail: 'client2@example.invalid',
            clientFirstName: 'New',
            clientLastName: null,
            status: 'Pending',
            createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
            expiresAtUtc: utcInstant('2026-04-22T00:00:00Z'),
        };
        dietologistService.getInvitationForCurrentUser.mockReturnValueOnce(oldRequest).mockReturnValueOnce(of(newInvitation));
        createComponent();
        routeParams.next(convertToParamMap({ invitationId: entityId<'dietologist-invitation'>('inv-2') }));
        fixture.componentRef.setInput('invitationId', newInvitation.invitationId);
        oldRequest.next({ ...newInvitation, invitationId: entityId<'dietologist-invitation'>('inv-1'), status: 'Revoked' });
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
                invitationId: entityId<'dietologist-invitation'>('inv-1'),
                clientUserId: entityId<'user'>('client-1'),
                clientEmail: 'client@example.com',
                clientFirstName: 'Client',
                clientLastName: null,
                status: 'Pending',
                createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
                expiresAtUtc: utcInstant('2026-04-22T00:00:00Z'),
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
                useValue: { paramMap: routeParams.asObservable() },
            },
        ],
    });

    fixture = TestBed.createComponent(DietologistInvitationPageComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('invitationId', routeParams.value.get('invitationId') ?? undefined);
    fixture.detectChanges();
}
