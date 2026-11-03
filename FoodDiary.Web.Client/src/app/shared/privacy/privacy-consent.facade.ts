import { inject, Service, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiDialogService } from 'fd-ui-kit';
import { concatMap, firstValueFrom, map } from 'rxjs';

import type { HealthConsentDialogData } from '../../components/shared/health-consent-dialog/health-consent-dialog';
import { AuthService } from '../../services/auth.service';
import { ExportService } from '../api/export.service';
import { type UserPrivacyState, UserService } from '../api/user.service';
import { SessionEventsService } from '../auth/session-events.service';
import { MarketingConsentService } from '../marketing/marketing-consent.service';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { PrivacyConsentPurpose } from './privacy-consent-purpose';
import { PRIVACY_NOTICE_VERSION } from './privacy-notice-version';

@Service()
export class PrivacyConsentFacade {
    private readonly users = inject(UserService);
    private readonly exporter = inject(ExportService);
    private readonly dialogs = inject(FdUiDialogService);
    private readonly sessions = inject(SessionEventsService);
    private readonly auth = inject(AuthService);
    private readonly marketing = inject(MarketingConsentService);
    private readonly storage = inject(BrowserStorageService);
    public readonly state = signal<UserPrivacyState | null>(null);
    public readonly busy = signal(false);
    public readonly failed = signal(false);
    public readonly needsReload = signal(false);
    private sessionVersion = 0;
    private healthReview: Promise<boolean> | null = null;

    public constructor() {
        this.marketing.changed
            .pipe(
                takeUntilDestroyed(),
                map(allowed => ({ allowed, session: this.sessionVersion })),
                concatMap(async choice => this.syncMarketingAsync(choice.allowed, choice.session)),
            )
            .subscribe();
        this.sessions.authenticated$.pipe(takeUntilDestroyed()).subscribe(() => {
            this.reset();
        });
        this.sessions.sessionEnded$.pipe(takeUntilDestroyed()).subscribe(() => {
            this.reset();
        });
    }

    private async syncMarketingAsync(allowed: boolean, session: number): Promise<void> {
        if (session !== this.sessionVersion || !this.auth.isAuthenticated()) {
            return;
        }
        const saved = await this.chooseAsync(PrivacyConsentPurpose.MarketingAttribution, allowed);
        if (session === this.sessionVersion) {
            this.marketing.accountSyncFailed.set(!saved);
        }
    }

    public async downloadAsync(): Promise<void> {
        const session = this.sessionVersion;
        this.busy.set(true);
        this.failed.set(false);
        try {
            await firstValueFrom(this.exporter.exportPersonalData());
        } catch {
            if (session === this.sessionVersion) {
                this.failed.set(true);
            }
        } finally {
            if (session === this.sessionVersion) {
                this.busy.set(false);
            }
        }
    }

    private reset(): void {
        this.sessionVersion++;
        this.healthReview = null;
        this.state.set(null);
        this.failed.set(false);
        this.busy.set(false);
        this.needsReload.set(false);
        this.marketing.accountSyncFailed.set(false);
    }

    public async flushPendingWithdrawalAsync(): Promise<void> {
        const userId = this.users.user()?.id;
        if (userId !== undefined && this.storage.getItem('local', `fd_marketing_withdrawal:${userId}`) === 'pending') {
            this.marketing.accountSyncFailed.set(!(await this.chooseAsync(PrivacyConsentPurpose.MarketingAttribution, false)));
        }
    }

    public async loadAsync(): Promise<UserPrivacyState> {
        const session = this.sessionVersion;
        const state = await firstValueFrom(this.users.getPrivacy());
        if (session === this.sessionVersion) {
            this.state.set(state);
            this.needsReload.set(state.noticeVersion !== PRIVACY_NOTICE_VERSION);
        }
        return state;
    }

    public async chooseAsync(purpose: PrivacyConsentPurpose, granted: boolean): Promise<boolean> {
        const session = this.sessionVersion;
        const userId = this.users.user()?.id;
        this.markPendingWithdrawal(purpose, granted, userId);
        this.busy.set(true);
        this.failed.set(false);
        try {
            return await this.saveChoiceAsync(purpose, granted, session, userId);
        } catch {
            this.failCurrentSession(session);
            return false;
        } finally {
            if (session === this.sessionVersion) {
                this.busy.set(false);
            }
        }
    }

    private markPendingWithdrawal(purpose: PrivacyConsentPurpose, granted: boolean, userId: string | undefined): void {
        if (!granted && userId !== undefined && purpose === PrivacyConsentPurpose.MarketingAttribution) {
            this.storage.setItem('local', `fd_marketing_withdrawal:${userId}`, 'pending');
        }
    }

    private async saveChoiceAsync(
        purpose: PrivacyConsentPurpose,
        granted: boolean,
        session: number,
        userId: string | undefined,
    ): Promise<boolean> {
        const state = await this.loadAsync();
        if (session !== this.sessionVersion || (granted && !this.checkNotice(state))) {
            return false;
        }
        await firstValueFrom(this.users.updatePrivacy(purpose, granted, PRIVACY_NOTICE_VERSION));
        if (userId !== undefined && purpose === PrivacyConsentPurpose.MarketingAttribution) {
            this.storage.removeItem('local', `fd_marketing_withdrawal:${userId}`);
        }
        if (session !== this.sessionVersion) {
            return false;
        }
        const confirmed = await this.loadAsync();
        return session === this.sessionVersion && isGranted(confirmed, purpose) === granted;
    }

    private checkNotice(state: UserPrivacyState): boolean {
        if (state.noticeVersion === PRIVACY_NOTICE_VERSION) {
            return true;
        }
        this.failed.set(true);
        return false;
    }

    private failCurrentSession(session: number): void {
        if (session === this.sessionVersion) {
            this.failed.set(true);
        }
    }

    public async ensureHealthConsentAsync(forceReview = false, data?: HealthConsentDialogData): Promise<boolean> {
        if (this.healthReview !== null) {
            return this.healthReview;
        }
        const review = this.checkHealthConsentAsync(forceReview, data);
        this.healthReview = review;
        try {
            return await review;
        } finally {
            if (this.healthReview === review) {
                this.healthReview = null;
            }
        }
    }

    private async checkHealthConsentAsync(forceReview: boolean, data?: HealthConsentDialogData): Promise<boolean> {
        const session = this.sessionVersion;
        try {
            this.failed.set(false);
            const state = await this.loadAsync();
            if (session !== this.sessionVersion) {
                return false;
            }
            if (!this.checkNotice(state)) {
                return false;
            }
            if (state.healthAcceptedAtUtc !== null) {
                return true;
            }
            if (!forceReview && state.healthDeclinedAtUtc !== null) {
                return false;
            }
            return await this.reviewHealthAsync(session, data);
        } catch {
            if (session === this.sessionVersion) {
                this.failed.set(true);
            }
            return false;
        }
    }
    private async reviewHealthAsync(session: number, data?: HealthConsentDialogData): Promise<boolean> {
        const { HealthConsentDialogComponent } = await import('../../components/shared/health-consent-dialog/health-consent-dialog');
        const accepted = await firstValueFrom(
            this.dialogs
                .open<InstanceType<typeof HealthConsentDialogComponent>, HealthConsentDialogData, boolean>(HealthConsentDialogComponent, {
                    data,
                    size: 'lg',
                    disableClose: true,
                })
                .afterClosed(),
        );
        if (session !== this.sessionVersion) {
            return false;
        }
        const saved = await this.chooseAsync(PrivacyConsentPurpose.HealthTracking, accepted === true);
        return saved && accepted === true && session === this.sessionVersion;
    }
}

function isGranted(state: UserPrivacyState, purpose: PrivacyConsentPurpose): boolean {
    switch (purpose) {
        case PrivacyConsentPurpose.HealthTracking: {
            return state.healthAcceptedAtUtc !== null;
        }
        case PrivacyConsentPurpose.AiProcessing: {
            return state.aiAcceptedAtUtc !== null;
        }
        case PrivacyConsentPurpose.MarketingAttribution: {
            return state.marketingAcceptedAtUtc !== null;
        }
    }
}
