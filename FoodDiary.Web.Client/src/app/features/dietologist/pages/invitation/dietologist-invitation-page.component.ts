import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { EMPTY, finalize, type Observable } from 'rxjs';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { DietologistService } from '../../api/dietologist.service';
import type { DietologistInvitationForCurrentUser } from '../../models/dietologist.data';

type InvitationPageState = 'loading' | 'ready' | 'accepted' | 'declined' | 'expired' | 'revoked' | 'error';

@Component({
    selector: 'fd-dietologist-invitation-page',
    imports: [TranslateModule, FdUiButtonComponent, FdUiCardComponent, FdUiFormErrorComponent],
    templateUrl: './dietologist-invitation-page.component.html',
    styleUrl: './dietologist-invitation-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DietologistInvitationPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly dietologistService = inject(DietologistService);
    private readonly navigationService = inject(NavigationService);
    private readonly authService = inject(AuthService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly currentLanguage = signal((this.translateService.getCurrentLang() || this.translateService.getFallbackLang()) ?? 'en');

    public readonly state = signal<InvitationPageState>('loading');
    public readonly invitation = signal<DietologistInvitationForCurrentUser | null>(null);
    public readonly errorMessage = signal<string | null>(null);
    public readonly isSubmitting = signal(false);
    public readonly invitationView = computed<DietologistInvitationView | null>(() => {
        this.currentLanguage();
        const invitation = this.invitation();
        if (!invitation) {
            return null;
        }

        const displayName = [invitation.clientFirstName, invitation.clientLastName].filter(Boolean).join(' ').trim();

        return {
            invitation,
            displayName: displayName || invitation.clientEmail,
            expiresDateLabel: this.formatMediumDate(invitation.expiresAtUtc),
        };
    });

    public constructor() {
        ((this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.currentLanguage.set((this.translateService.getCurrentLang() || this.translateService.getFallbackLang()) ?? 'en');
            });
        this.loadInvitation();
    }

    public accept(): void {
        const invitationId = this.invitation()?.invitationId;
        if (!invitationId || this.isSubmitting()) {
            return;
        }

        this.isSubmitting.set(true);
        this.dietologistService
            .acceptInvitationForCurrentUser(invitationId)
            .pipe(
                finalize(() => {
                    this.isSubmitting.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: () => {
                    this.authService
                        .refreshToken()
                        .pipe(takeUntilDestroyed(this.destroyRef))
                        .subscribe({
                            next: () => {
                                this.state.set('accepted');
                            },
                            error: () => {
                                this.state.set('accepted');
                            },
                        });
                },
                error: () => {
                    this.state.set('error');
                    this.errorMessage.set(this.translateService.instant('DIETOLOGIST_INVITATION.ERROR_ACCEPT'));
                },
            });
    }

    public decline(): void {
        const invitationId = this.invitation()?.invitationId;
        if (!invitationId || this.isSubmitting()) {
            return;
        }

        this.isSubmitting.set(true);
        this.dietologistService
            .declineInvitationForCurrentUser(invitationId)
            .pipe(
                finalize(() => {
                    this.isSubmitting.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: () => {
                    this.state.set('declined');
                },
                error: () => {
                    this.state.set('error');
                    this.errorMessage.set(this.translateService.instant('DIETOLOGIST_INVITATION.ERROR_DECLINE'));
                },
            });
    }

    public goToClients(): void {
        void this.navigationService.navigateToDietologistAsync();
    }

    public goHome(): void {
        void this.navigationService.navigateToHomeAsync();
    }

    private loadInvitation(): void {
        const invitationId = this.route.snapshot.paramMap.get('invitationId');
        if (!invitationId) {
            this.state.set('error');
            this.errorMessage.set(this.translateService.instant('DIETOLOGIST_INVITATION.ERROR_INVALID'));
            return;
        }

        this.state.set('loading');
        this.dietologistService
            .getInvitationForCurrentUser(invitationId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: invitation => {
                    this.invitation.set(invitation);
                    this.state.set(this.resolveState(invitation.status));
                },
                error: () => {
                    this.state.set('error');
                    this.errorMessage.set(this.translateService.instant('DIETOLOGIST_INVITATION.ERROR_LOAD'));
                },
            });
    }

    private resolveState(status: string): InvitationPageState {
        switch (status) {
            case 'Accepted':
                return 'accepted';
            case 'Declined':
                return 'declined';
            case 'Expired':
                return 'expired';
            case 'Revoked':
                return 'revoked';
            default:
                return 'ready';
        }
    }

    private formatMediumDate(value: string): string {
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return value;
        }

        return new Intl.DateTimeFormat(this.currentLanguage() === 'ru' ? 'ru-RU' : 'en-US', {
            dateStyle: 'medium',
        }).format(date);
    }
}

interface DietologistInvitationView {
    invitation: DietologistInvitationForCurrentUser;
    displayName: string;
    expiresDateLabel: string;
}
