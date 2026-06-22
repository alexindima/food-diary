import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import type { FdUiInlineAlertSeverity } from 'fd-ui-kit/inline-alert/fd-ui-inline-alert';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { EMPTY, type Observable } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { resolveAppLocale } from '../../../../shared/lib/locale.constants';
import { HOURS_PER_DAY, MINUTES_PER_HOUR, MS_PER_MINUTE } from '../../../../shared/lib/time.constants';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { FastingCheckInCardComponent } from '../../components/fasting-check-in-card/fasting-check-in-card';
import {
    FastingCheckInChartDialogComponent,
    type FastingCheckInChartDialogData,
} from '../../components/fasting-checkin-chart-dialog/fasting-checkin-chart-dialog';
import { FastingHistoryCardComponent } from '../../components/fasting-history-card/fasting-history-card';
import { FastingInsightsSectionComponent } from '../../components/fasting-insights-section/fasting-insights-section';
import { FastingTimerCardComponent } from '../../components/fasting-timer-card/fasting-timer-card';
import {
    CURRENT_SESSION_RECENT_CHECK_INS_LIMIT,
    DEFAULT_CYCLIC_EAT_DAYS,
    DEFAULT_CYCLIC_EAT_FAST_HOURS,
    DEFAULT_CYCLIC_EAT_WINDOW_HOURS,
    DEFAULT_CYCLIC_FAST_DAYS,
} from '../../lib/fasting.constants';
import { FastingFacade } from '../../lib/fasting.facade';
import {
    FASTING_ENERGY_EMOJI_SCALE,
    FASTING_HUNGER_EMOJI_SCALE,
    FASTING_MOOD_EMOJI_SCALE,
    FASTING_SESSION_CHECK_INS_PAGE_SIZE,
} from '../../lib/fasting-page.constants';
import {
    FASTING_PROTOCOLS,
    type FastingCheckIn,
    type FastingMessage,
    type FastingSession,
    type FastingSessionStatus,
} from '../../models/fasting.data';
import type {
    FastingCheckInViewModel,
    FastingHistorySessionViewModel,
    FastingMessageViewModel,
} from '../fasting-page-lib/fasting-page.types';
import { FASTING_TOUR } from '../fasting-page-lib/fasting-tour';
import { FastingAlertsSectionComponent } from '../fasting-page-sections/alerts-section/fasting-alerts-section';
import { FastingStatsCardComponent } from '../fasting-page-sections/stats-card/fasting-stats-card';

@Component({
    selector: 'fd-fasting-page',
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        SkeletonCardComponent,
        FastingTimerCardComponent,
        FastingHistoryCardComponent,
        FastingInsightsSectionComponent,
        FastingCheckInCardComponent,
        FastingStatsCardComponent,
        FastingAlertsSectionComponent,
    ],
    templateUrl: './fasting-page.html',
    styleUrl: './fasting-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FastingFacade],
})
export class FastingPageComponent {
    private readonly facade = inject(FastingFacade);
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly localizationService = inject(LocalizationService);
    private readonly toastService = inject(FdUiToastService);
    private readonly currentLanguage = signal(this.localizationService.getCurrentLanguage());

    protected readonly isLoading = this.facade.isLoading;
    protected readonly isEnding = this.facade.isEnding;
    protected readonly isUpdatingCycle = this.facade.isUpdatingCycle;
    protected readonly isSavingCheckIn = this.facade.isSavingCheckIn;
    protected readonly isActive = this.facade.isActive;
    protected readonly currentSession = this.facade.currentSession;
    protected readonly stats = this.facade.stats;
    protected readonly history = this.facade.history;
    protected readonly hungerLevel = this.facade.hungerLevel;
    protected readonly energyLevel = this.facade.energyLevel;
    protected readonly moodLevel = this.facade.moodLevel;
    protected readonly selectedSymptoms = this.facade.selectedSymptoms;
    protected readonly checkInNotes = this.facade.checkInNotes;
    protected readonly hungerEmojiScale = FASTING_HUNGER_EMOJI_SCALE;
    protected readonly energyEmojiScale = FASTING_ENERGY_EMOJI_SCALE;
    protected readonly moodEmojiScale = FASTING_MOOD_EMOJI_SCALE;
    protected readonly alerts = computed(() => this.facade.insightsData().alerts);
    protected readonly insights = computed(() => this.facade.insightsData().insights);
    protected readonly visibleAlertItems = computed(() => {
        this.currentLanguage();
        const session = this.currentSession();
        return this.alerts()
            .filter(alert => this.facade.isPromptVisible(session, alert))
            .map(alert => this.buildMessageViewModel(alert));
    });
    protected readonly insightItems = computed(() => {
        this.currentLanguage();
        return this.insights().map(insight => this.buildMessageViewModel(insight));
    });
    protected readonly sessionCheckInVisibleCount = signal<Record<string, number>>({});
    protected readonly isLoadingMoreHistory = this.facade.isLoadingMoreHistory;
    protected readonly visibleHistory = this.history;
    protected readonly historyItems = computed<FastingHistorySessionViewModel[]>(() => {
        this.currentLanguage();
        this.expandedHistorySessionId();
        this.sessionCheckInVisibleCount();

        return this.visibleHistory().map(session => this.buildHistorySessionViewModel(session));
    });
    protected readonly canLoadMoreHistory = computed(() => this.facade.historyPage() < this.facade.historyTotalPages());
    protected readonly isCheckInExpanded = signal(false);
    protected readonly expandedHistorySessionId = signal<string | null>(null);
    protected readonly currentSessionLatestCheckIn = computed(() => this.getCurrentSessionLatestCheckIn());
    protected readonly currentSessionLatestCheckInView = computed<FastingCheckInViewModel | null>(() => {
        this.currentLanguage();
        const checkIn = this.currentSessionLatestCheckIn();
        return checkIn === null ? null : this.buildCheckInViewModel(checkIn);
    });
    protected readonly currentSessionRecentCheckIns = computed(() => {
        const session = this.currentSession();
        if (session === null) {
            return [];
        }

        return this.getSessionCheckIns(session).slice(0, CURRENT_SESSION_RECENT_CHECK_INS_LIMIT);
    });
    public constructor() {
        this.facade.initialize();

        ((this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.currentLanguage.set(this.localizationService.getCurrentLanguage());
            });

        effect(() => {
            const version = this.facade.checkInSavedVersion();
            if (version <= 0) {
                return;
            }

            untracked(() => {
                this.isCheckInExpanded.set(false);
                this.toastService.success(this.translateService.instant('FASTING.CHECK_IN.SAVED_TOAST'));
            });
        });
    }

    protected saveCheckIn(): void {
        this.facade.saveCheckIn();
    }

    protected openCheckInForm(): void {
        this.isCheckInExpanded.set(true);
    }

    protected closeCheckInForm(): void {
        this.isCheckInExpanded.set(false);
        this.facade.resetCheckInDraft();
    }

    protected startFastingTour(force = true): void {
        this.tourService.start(this.localizedTour.build(FASTING_TOUR), { force });
    }

    protected dismissPrompt(promptId: string): void {
        this.facade.dismissPrompt(promptId);
    }

    protected snoozePrompt(promptId: string): void {
        this.facade.snoozePrompt(promptId);
    }

    protected loadMoreHistory(): void {
        this.facade.loadMoreHistory();
    }

    protected openSessionCheckInChart(session: FastingSession): void {
        if (!this.canViewSessionCheckInChart(session)) {
            return;
        }

        this.dialogService.open<FastingCheckInChartDialogComponent, FastingCheckInChartDialogData, void>(
            FastingCheckInChartDialogComponent,
            {
                size: 'lg',
                width: 'var(--fd-size-dialog-chart-width)',
                maxWidth: 'var(--fd-size-dialog-chart-width)',
                panelClass: 'fd-ui-dialog-panel--chart',
                data: {
                    title: this.translateService.instant('FASTING.CHECK_IN.CHART_TITLE'),
                    subtitle: this.getHistoryChartSubtitle(session),
                    checkIns: this.getSessionCheckIns(session),
                },
            },
        );
    }

    protected getHistoryAccentColor(session: FastingSession): string {
        switch (session.status) {
            case 'Completed': {
                return 'var(--fd-color-green-500)';
            }
            case 'Interrupted': {
                return 'var(--fd-color-orange-500)';
            }
            case 'Skipped': {
                return 'var(--fd-color-sky-500)';
            }
            case 'Postponed': {
                return 'var(--fd-color-ai)';
            }
            case 'Active': {
                return 'var(--fd-color-slate-400)';
            }
        }
    }

    protected getHistoryBadgeKey(status: FastingSessionStatus): string {
        switch (status) {
            case 'Completed': {
                return 'FASTING.BADGE_COMPLETED';
            }
            case 'Interrupted': {
                return 'FASTING.BADGE_INTERRUPTED';
            }
            case 'Skipped': {
                return 'FASTING.BADGE_SKIPPED';
            }
            case 'Postponed': {
                return 'FASTING.BADGE_POSTPONED';
            }
            case 'Active': {
                return 'FASTING.BADGE_INCOMPLETE';
            }
        }
    }

    protected getHistoryProtocolLabel(protocol: string): string {
        const option = FASTING_PROTOCOLS.find(item => item.value === protocol);
        return option === undefined ? protocol : this.translateService.instant(option.labelKey);
    }

    protected getHistorySessionTypeLabel(session: FastingSession): string {
        if (session.planType === 'Cyclic') {
            return this.translateService.instant('FASTING.CYCLIC_TYPE');
        }

        const option = FASTING_PROTOCOLS.find(item => item.value === session.protocol);
        if (option === undefined) {
            return this.translateService.instant('FASTING.EXTENDED_TYPE');
        }

        return this.translateService.instant(option.category === 'intermittent' ? 'FASTING.INTERMITTENT_TYPE' : 'FASTING.EXTENDED_TYPE');
    }

    protected getHistoryProtocolDisplay(session: FastingSession): string {
        if (session.planType === 'Cyclic') {
            return this.getCyclicProtocolDisplay(session);
        }

        const option = FASTING_PROTOCOLS.find(item => item.value === session.protocol);
        const hoursLabel = this.translateService.instant('FASTING.HOURS');

        if (option === undefined) {
            return this.formatHistoryDuration(session.initialPlannedDurationHours, session.addedDurationHours, hoursLabel);
        }

        if (option.value === 'CustomIntermittent') {
            return this.getIntermittentRatioLabel(session.initialPlannedDurationHours);
        }

        const baseLabel =
            option.value === 'Custom'
                ? `${session.initialPlannedDurationHours} ${hoursLabel}`
                : this.translateService.instant(option.labelKey);

        const addedHours = session.addedDurationHours;
        if (addedHours === 0) {
            return baseLabel;
        }

        if (addedHours > 0) {
            return `${baseLabel} (+${addedHours} ${hoursLabel})`;
        }

        return `${baseLabel} (${addedHours} ${hoursLabel})`;
    }

    protected hasCheckIn(session: FastingSession): boolean {
        return this.getSessionCheckIns(session).length > 0;
    }

    protected isHistorySessionExpanded(sessionId: string): boolean {
        return this.expandedHistorySessionId() === sessionId;
    }

    protected toggleHistorySession(sessionId: string): void {
        this.expandedHistorySessionId.update(current => (current === sessionId ? null : sessionId));
    }

    protected getHistoryCheckInToggleKey(session: FastingSession): string {
        return this.isHistorySessionExpanded(session.id) ? 'FASTING.HIDE_HISTORY_CHECK_INS' : 'FASTING.SHOW_HISTORY_CHECK_INS';
    }

    protected getLatestSessionCheckIn(session: FastingSession): FastingCheckIn | null {
        const checkIns = this.getSessionCheckIns(session);
        return checkIns.length > 0 ? (checkIns[0] ?? null) : null;
    }

    protected getSessionCheckInCount(session: FastingSession): number {
        return this.getSessionCheckIns(session).length;
    }

    protected canViewSessionCheckInChart(session: FastingSession): boolean {
        return this.getSessionCheckInCount(session) > 1;
    }

    protected getCheckInSummary(hunger: number | null, energy: number | null, mood: number | null): string {
        return this.translateService.instant('FASTING.CHECK_IN.SUMMARY', {
            hunger: this.getHungerSummaryValue(hunger),
            energy: this.getEnergySummaryValue(energy),
            mood: this.getMoodSummaryValue(mood),
        });
    }

    protected hasCurrentSessionTimeline(): boolean {
        return this.currentSessionRecentCheckIns().length > 0;
    }

    protected getCurrentSessionOlderCheckInsCount(): number {
        const session = this.currentSession();
        if (session === null) {
            return 0;
        }

        return Math.max(0, this.getSessionCheckIns(session).length - this.currentSessionRecentCheckIns().length);
    }

    protected getSessionCheckIns(session: FastingSession): FastingCheckIn[] {
        if (session.checkIns.length > 0) {
            return session.checkIns;
        }

        if (session.checkInAtUtc === null) {
            return [];
        }

        return [
            {
                id: `${session.id}:latest`,
                checkedInAtUtc: session.checkInAtUtc,
                hungerLevel: session.hungerLevel ?? 0,
                energyLevel: session.energyLevel ?? 0,
                moodLevel: session.moodLevel ?? 0,
                symptoms: session.symptoms,
                notes: session.checkInNotes,
            },
        ];
    }

    protected getVisibleSessionCheckIns(session: FastingSession): FastingCheckIn[] {
        const allCheckIns = this.getSessionCheckIns(session);
        const visibleCount = this.sessionCheckInVisibleCount()[session.id] ?? FASTING_SESSION_CHECK_INS_PAGE_SIZE;
        return allCheckIns.slice(0, visibleCount);
    }

    protected canLoadMoreSessionCheckIns(session: FastingSession): boolean {
        const allCheckIns = this.getSessionCheckIns(session);
        const visibleCount = this.sessionCheckInVisibleCount()[session.id] ?? FASTING_SESSION_CHECK_INS_PAGE_SIZE;
        return allCheckIns.length > visibleCount;
    }

    protected loadMoreSessionCheckIns(sessionId: string): void {
        this.sessionCheckInVisibleCount.update(current => ({
            ...current,
            [sessionId]: (current[sessionId] ?? FASTING_SESSION_CHECK_INS_PAGE_SIZE) + FASTING_SESSION_CHECK_INS_PAGE_SIZE,
        }));
    }

    protected getHistoryCheckInRegionId(sessionId: string): string {
        return `fasting-history-checkins-${sessionId}`;
    }

    private formatHistoryDuration(baseHours: number, addedHours: number, hoursLabel: string): string {
        if (addedHours === 0) {
            return `${baseHours} ${hoursLabel}`;
        }

        if (addedHours > 0) {
            return `${baseHours} ${hoursLabel} (+${addedHours} ${hoursLabel})`;
        }

        return `${baseHours} ${hoursLabel} (${addedHours} ${hoursLabel})`;
    }

    private getIntermittentRatioLabel(fastHours: number): string {
        return `${fastHours}:${HOURS_PER_DAY - fastHours}`;
    }

    private getCurrentSessionLatestCheckIn(): FastingCheckIn | null {
        const session = this.currentSession();
        if (session === null) {
            return null;
        }

        const checkIns = this.getSessionCheckIns(session);
        return checkIns.length > 0 ? (checkIns[0] ?? null) : null;
    }

    private getSymptomLabel(symptom: string): string {
        return this.translateService.instant(`FASTING.CHECK_IN.SYMPTOMS.${symptom.toUpperCase()}`);
    }

    private buildCheckInViewModel(checkIn: FastingCheckIn): FastingCheckInViewModel {
        return {
            checkIn,
            checkedInAtLabel: this.formatSessionDateLabel(checkIn.checkedInAtUtc),
            relativeCheckedInAt: this.formatRelativeTime(checkIn.checkedInAtUtc),
            summary: this.getCheckInSummary(checkIn.hungerLevel, checkIn.energyLevel, checkIn.moodLevel),
            symptomLabels: checkIn.symptoms.map(symptom => this.getSymptomLabel(symptom)),
        };
    }

    private buildMessageViewModel(message: FastingMessage): FastingMessageViewModel {
        return {
            message,
            severity: this.getAlertSeverity(message),
            title: this.getTranslatedMessage(message, 'titleKey'),
            body: this.getTranslatedMessage(message, 'bodyKey'),
        };
    }

    private buildHistorySessionViewModel(session: FastingSession): FastingHistorySessionViewModel {
        const checkInCount = this.getSessionCheckInCount(session);
        return {
            session,
            startedAtLabel: this.formatSessionDateLabel(session.startedAtUtc),
            accentColor: this.getHistoryAccentColor(session),
            sessionTypeLabel: this.getHistorySessionTypeLabel(session),
            protocolDisplay: this.getHistoryProtocolDisplay(session),
            badgeKey: this.getHistoryBadgeKey(session.status),
            hasCheckIns: checkInCount > 0,
            checkInCount,
            canViewChart: checkInCount > 1,
            isExpanded: this.isHistorySessionExpanded(session.id),
            checkInRegionId: this.getHistoryCheckInRegionId(session.id),
            toggleKey: this.getHistoryCheckInToggleKey(session),
            visibleCheckIns: this.getVisibleSessionCheckIns(session).map(checkIn => this.buildCheckInViewModel(checkIn)),
            canLoadMoreCheckIns: this.canLoadMoreSessionCheckIns(session),
        };
    }

    private getCyclicProtocolDisplay(session: FastingSession): string {
        const fastDays = session.cyclicFastDays ?? DEFAULT_CYCLIC_FAST_DAYS;
        const eatDays = session.cyclicEatDays ?? DEFAULT_CYCLIC_EAT_DAYS;
        const eatWindowHours = session.cyclicEatDayEatingWindowHours ?? DEFAULT_CYCLIC_EAT_WINDOW_HOURS;
        const eatFastHours = session.cyclicEatDayFastHours ?? DEFAULT_CYCLIC_EAT_FAST_HOURS;

        return `${fastDays}:${eatDays} (${eatFastHours}:${eatWindowHours})`;
    }

    private getHistoryChartSubtitle(session: FastingSession): string {
        return `${this.formatSessionDateLabel(session.startedAtUtc)} · ${this.getHistorySessionTypeLabel(session)} · ${this.getHistoryProtocolDisplay(session)}`;
    }

    private formatSessionDateLabel(value: string): string {
        return new Intl.DateTimeFormat(resolveAppLocale(this.localizationService.getCurrentLanguage()), {
            day: 'numeric',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        }).format(new Date(value));
    }

    private getTranslatedMessage(descriptor: FastingMessage, field: 'titleKey' | 'bodyKey'): string {
        const key = descriptor[field];
        return this.translateService.instant(key, this.resolveMessageParams(descriptor.bodyParams));
    }

    private getAlertSeverity(message: FastingMessage): FdUiInlineAlertSeverity {
        switch (message.tone) {
            case 'warning': {
                return 'warning';
            }
            case 'positive': {
                return 'success';
            }
            case 'neutral': {
                return 'info';
            }
        }
    }

    protected getEnergyEmoji(level: number | null | undefined): string {
        return this.energyEmojiScale.find(option => option.value === level)?.emoji ?? '—';
    }

    protected getHungerEmoji(level: number | null | undefined): string {
        return this.hungerEmojiScale.find(option => option.value === level)?.emoji ?? '—';
    }

    protected getMoodEmoji(level: number | null | undefined): string {
        return this.moodEmojiScale.find(option => option.value === level)?.emoji ?? '—';
    }

    protected formatRelativeTime(value: string | null): string | null {
        if (value === null || value.length === 0) {
            return null;
        }

        const timestamp = new Date(value).getTime();
        if (Number.isNaN(timestamp)) {
            return null;
        }

        const diffMs = timestamp - Date.now();
        const diffMinutes = Math.round(diffMs / MS_PER_MINUTE);
        const locale = resolveAppLocale(this.localizationService.getCurrentLanguage());
        const formatter = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' });

        if (Math.abs(diffMinutes) < MINUTES_PER_HOUR) {
            return formatter.format(diffMinutes, 'minute');
        }

        const diffHours = Math.round(diffMinutes / MINUTES_PER_HOUR);
        if (Math.abs(diffHours) < HOURS_PER_DAY) {
            return formatter.format(diffHours, 'hour');
        }

        const diffDays = Math.round(diffHours / HOURS_PER_DAY);
        return formatter.format(diffDays, 'day');
    }

    private resolveMessageParams(params: Record<string, string> | null): Record<string, string> | undefined {
        if (params === null) {
            return undefined;
        }

        return Object.fromEntries(
            Object.entries(params).map(([key, value]) => [
                key,
                value.startsWith('FASTING.') ? this.translateService.instant(value) : value,
            ]),
        );
    }

    private getHungerSummaryValue(level: number | null): string {
        if (level === null) {
            return '—';
        }

        return this.getHungerEmoji(level);
    }

    private getEnergySummaryValue(level: number | null): string {
        if (level === null) {
            return '—';
        }

        return this.getEnergyEmoji(level);
    }

    private getMoodSummaryValue(level: number | null): string {
        if (level === null) {
            return '—';
        }

        return this.getMoodEmoji(level);
    }
}
