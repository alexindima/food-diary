import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { DietologistService } from '../../api/dietologist.service';
import { DietologistInvitationPageComponent } from './dietologist-invitation-page.component';

describe('DietologistInvitationPageComponent', () => {
    let fixture: ComponentFixture<DietologistInvitationPageComponent>;
    let component: DietologistInvitationPageComponent;
    let dietologistService: {
        getInvitationForCurrentUser: ReturnType<typeof vi.fn>;
        acceptInvitationForCurrentUser: ReturnType<typeof vi.fn>;
        declineInvitationForCurrentUser: ReturnType<typeof vi.fn>;
    };
    let navigationService: {
        navigateToDietologistAsync: ReturnType<typeof vi.fn>;
        navigateToHomeAsync: ReturnType<typeof vi.fn>;
    };
    let authService: {
        refreshToken: ReturnType<typeof vi.fn>;
    };

    function createComponent(): void {
        TestBed.configureTestingModule({
            imports: [DietologistInvitationPageComponent, TranslateModule.forRoot()],
            providers: [
                { provide: DietologistService, useValue: dietologistService },
                { provide: NavigationService, useValue: navigationService },
                { provide: AuthService, useValue: authService },
                {
                    provide: ActivatedRoute,
                    useValue: {
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

    beforeEach(() => {
        dietologistService = {
            getInvitationForCurrentUser: vi.fn(),
            acceptInvitationForCurrentUser: vi.fn(),
            declineInvitationForCurrentUser: vi.fn(),
        };
        navigationService = {
            navigateToDietologistAsync: vi.fn().mockResolvedValue(undefined),
            navigateToHomeAsync: vi.fn().mockResolvedValue(undefined),
        };
        authService = {
            refreshToken: vi.fn().mockReturnValue(of({})),
        };
    });

    it('shows accepted state when invitation is already accepted', () => {
        dietologistService = {
            getInvitationForCurrentUser: vi.fn().mockReturnValue(
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
            ),
            acceptInvitationForCurrentUser: vi.fn(),
            declineInvitationForCurrentUser: vi.fn(),
        };

        createComponent();

        expect(component.state()).toBe('accepted');
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('DIETOLOGIST_INVITATION.SUCCESS_ACCEPT');
    });

    it('shows error state when invitation request fails', () => {
        dietologistService = {
            getInvitationForCurrentUser: vi.fn().mockReturnValue(throwError(() => new Error('load failed'))),
            acceptInvitationForCurrentUser: vi.fn(),
            declineInvitationForCurrentUser: vi.fn(),
        };

        createComponent();

        expect(component.state()).toBe('error');
        expect(component.errorMessage()).toBe('DIETOLOGIST_INVITATION.ERROR_LOAD');
    });
});
