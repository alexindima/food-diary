import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, linkedSignal, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { catchError, filter, finalize, map, type Observable, of, switchMap, takeUntil } from 'rxjs';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import type { DietologistInvitationForCurrentUser } from '../../../../shared/models/dietologist.data';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { DietologistFacade } from '../../lib/dietologist.facade';
import { formatDietologistDate } from '../../lib/dietologist-date.utils';

type InvitationPageState = 'loading' | 'ready' | 'accepted' | 'declined' | 'expired' | 'revoked' | 'error';

@Component({
    selector: 'fd-dietologist-invitation-page',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent, FdUiFormErrorComponent],
    templateUrl: './dietologist-invitation-page.html',
    styleUrl: './dietologist-invitation-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DietologistInvitationPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly dietologistFacade = inject(DietologistFacade);
    private readonly navigationService = inject(NavigationService);
    private readonly authService = inject(AuthService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly currentLanguage = signal(this.resolveCurrentLanguage());

    private readonly invitationId = toSignal(this.route.paramMap.pipe(map(params => params.get('invitationId'))), { initialValue: null });
    private readonly invitationResource = rxResource({
        params: () => {
            const id = this.invitationId();
            return id !== null && id.length > 0 ? id : undefined;
        },
        stream: ({ params }) => this.dietologistFacade.getInvitationForCurrentUser(entityId<'dietologist-invitation'>(params)),
    });
    protected readonly invitation = computed(() => (this.invitationResource.hasValue() ? this.invitationResource.value() : null));
    private readonly actionState = linkedSignal<string | null, 'accepted' | 'declined' | null>({
        source: this.invitationId,
        computation: () => null,
    });
    private readonly actionError = linkedSignal<string | null, string | null>({
        source: this.invitationId,
        computation: () => null,
    });
    protected readonly state = computed<InvitationPageState>(() => {
        const invitation = this.invitation();
        if (invitation === null) {
            return this.errorMessage() === null ? 'loading' : 'error';
        }
        return this.actionState() ?? this.resolveState(invitation.status);
    });
    protected readonly errorMessage = computed(() => {
        if (this.invitationResource.status() === 'idle') {
            return this.translateService.instant('DIETOLOGIST_INVITATION.ERROR_INVALID');
        }
        if (this.invitationResource.error() !== undefined) {
            return this.translateService.instant('DIETOLOGIST_INVITATION.ERROR_LOAD');
        }
        return this.actionError();
    });
    protected readonly isSubmitting = signal(false);

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.currentLanguage.set(this.resolveCurrentLanguage());
        });
    }

    protected describeInvitation(invitation: DietologistInvitationForCurrentUser): DietologistInvitationView {
        const language = this.currentLanguage();
        const displayName = [invitation.clientFirstName, invitation.clientLastName]
            .filter((value): value is string => value !== null && value.length > 0)
            .join(' ')
            .trim();

        return {
            displayName:
                displayName.length > 0
                    ? displayName
                    : (invitation.clientEmail ?? this.translateService.instant('ATTENTION.UNNAMED_CLIENT')),
            expiresDateLabel: formatDietologistDate(invitation.expiresAtUtc, language),
        };
    }

    protected accept(): void {
        const invitationId = this.invitation()?.invitationId;
        if (invitationId === undefined || invitationId.length === 0 || this.isSubmitting()) {
            return;
        }

        this.actionError.set(null);
        this.isSubmitting.set(true);
        this.dietologistFacade
            .acceptInvitationForCurrentUser(invitationId)
            .pipe(
                switchMap(() => this.authService.refreshSession().pipe(catchError(() => of(null)))),
                takeUntil(this.invitationChanged(invitationId)),
                finalize(() => {
                    this.isSubmitting.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: () => {
                    this.actionState.set('accepted');
                },
                error: () => {
                    this.actionError.set(this.translateService.instant('DIETOLOGIST_INVITATION.ERROR_ACCEPT'));
                },
            });
    }

    protected decline(): void {
        const invitationId = this.invitation()?.invitationId;
        if (invitationId === undefined || invitationId.length === 0 || this.isSubmitting()) {
            return;
        }

        this.actionError.set(null);
        this.isSubmitting.set(true);
        this.dietologistFacade
            .declineInvitationForCurrentUser(invitationId)
            .pipe(
                takeUntil(this.invitationChanged(invitationId)),
                finalize(() => {
                    this.isSubmitting.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: () => {
                    this.actionState.set('declined');
                },
                error: () => {
                    this.actionError.set(this.translateService.instant('DIETOLOGIST_INVITATION.ERROR_DECLINE'));
                },
            });
    }

    protected goToClients(): void {
        void this.navigationService.navigateToDietologistAsync();
    }

    protected goHome(): void {
        void this.navigationService.navigateToHomeAsync();
    }

    private invitationChanged(invitationId: string): Observable<unknown> {
        return this.route.paramMap.pipe(filter(params => params.get('invitationId') !== invitationId));
    }

    private resolveState(status: string): InvitationPageState {
        switch (status) {
            case 'Accepted': {
                return 'accepted';
            }
            case 'Declined': {
                return 'declined';
            }
            case 'Expired': {
                return 'expired';
            }
            case 'Revoked': {
                return 'revoked';
            }
            default: {
                return 'ready';
            }
        }
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

type DietologistInvitationView = {
    displayName: string;
    expiresDateLabel: string;
};
