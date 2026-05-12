import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { catchError, EMPTY, finalize, type Observable, of, switchMap } from 'rxjs';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import type { DietologistInvitationForCurrentUser } from '../../../../shared/models/dietologist.data';
import { DietologistService } from '../../api/dietologist.service';

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
    private readonly currentLanguage = signal(this.resolveCurrentLanguage());

    public readonly state = signal<InvitationPageState>('loading');
    public readonly invitation = signal<DietologistInvitationForCurrentUser | null>(null);
    public readonly errorMessage = signal<string | null>(null);
    public readonly isSubmitting = signal(false);
    public readonly invitationView = computed<DietologistInvitationView | null>(() => {
        this.currentLanguage();
        const invitation = this.invitation();
        if (invitation === null) {
            return null;
        }

        const displayName = [invitation.clientFirstName, invitation.clientLastName]
            .filter((value): value is string => value !== null && value.length > 0)
            .join(' ')
            .trim();

        return {
            invitation,
            displayName: displayName.length > 0 ? displayName : invitation.clientEmail,
            expiresDateLabel: this.formatMediumDate(invitation.expiresAtUtc),
        };
    });

    public constructor() {
        ((this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.currentLanguage.set(this.resolveCurrentLanguage());
            });
        this.loadInvitation();
    }

    public accept(): void {
        const invitationId = this.invitation()?.invitationId;
        if (invitationId === undefined || invitationId.length === 0 || this.isSubmitting()) {
            return;
        }

        this.isSubmitting.set(true);
        this.dietologistService
            .acceptInvitationForCurrentUser(invitationId)
            .pipe(
                switchMap(() => this.authService.refreshToken().pipe(catchError(() => of(null)))),
                finalize(() => {
                    this.isSubmitting.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: () => {
                    this.state.set('accepted');
                },
                error: () => {
                    this.state.set('error');
                    this.errorMessage.set(this.translateService.instant('DIETOLOGIST_INVITATION.ERROR_ACCEPT'));
                },
            });
    }

    public decline(): void {
        const invitationId = this.invitation()?.invitationId;
        if (invitationId === undefined || invitationId.length === 0 || this.isSubmitting()) {
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
        if (invitationId === null || invitationId.length === 0) {
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

    private resolveCurrentLanguage(): string {
        const current = this.translateService.getCurrentLang() as string | null | undefined;
        if (current !== null && current !== undefined && current.length > 0) {
            return current;
        }

        const fallback = this.translateService.getFallbackLang() as string | null | undefined;
        return fallback !== null && fallback !== undefined && fallback.length > 0 ? fallback : 'en';
    }
}

interface DietologistInvitationView {
    invitation: DietologistInvitationForCurrentUser;
    displayName: string;
    expiresDateLabel: string;
}
