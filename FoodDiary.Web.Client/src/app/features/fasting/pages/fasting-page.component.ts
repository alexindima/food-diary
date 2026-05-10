import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiChipSelectComponent, type FdUiChipSelectOption, FdUiEmojiPickerComponent, type FdUiEmojiPickerOption } from 'fd-ui-kit';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInlineAlertComponent, type FdUiInlineAlertSeverity } from 'fd-ui-kit/inline-alert/fd-ui-inline-alert.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { EMPTY, type Observable } from 'rxjs';

import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { SkeletonCardComponent } from '../../../components/shared/skeleton-card/skeleton-card.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { LocalizedDatePipe } from '../../../pipes/localized-date.pipe';
import { LocalizationService } from '../../../services/localization.service';
import {
    FastingCheckInChartDialogComponent,
    type FastingCheckInChartDialogData,
} from '../components/fasting-checkin-chart-dialog/fasting-checkin-chart-dialog.component';
import { FastingTimerCardComponent } from '../components/fasting-timer-card/fasting-timer-card.component';
import { FastingFacade } from '../lib/fasting.facade';
import {
    FASTING_ENERGY_EMOJI_SCALE,
    FASTING_HUNGER_EMOJI_SCALE,
    FASTING_MOOD_EMOJI_SCALE,
    FASTING_SESSION_CHECK_INS_PAGE_SIZE,
    type FastingEmojiScaleOption,
} from '../lib/fasting-page.constants';
import {
    FASTING_PROTOCOLS,
    FASTING_SYMPTOM_OPTIONS,
    type FastingCheckIn,
    type FastingMessage,
    type FastingSession,
    type FastingSessionStatus,
    type FastingStats,
} from '../models/fasting.data';

@Component({
    selector: 'fd-fasting-page',
    standalone: true,
    imports: [
        DecimalPipe,
        FormsModule,
        TranslatePipe,
        FdUiChipSelectComponent,
        FdUiEmojiPickerComponent,
        LocalizedDatePipe,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        SkeletonCardComponent,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiAccentSurfaceComponent,
        FdUiInlineAlertComponent,
        FastingTimerCardComponent,
    ],
    templateUrl: './fasting-page.component.html',
    styleUrl: './fasting-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FastingFacade],
})
export class FastingPageComponent {
    private readonly facade = inject(FastingFacade);
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly localizationService = inject(LocalizationService);
    private readonly toastService = inject(FdUiToastService);
    private readonly currentLanguage = signal(this.localizationService.getCurrentLanguage());

    public readonly isLoading = this.facade.isLoading;
    public readonly isEnding = this.facade.isEnding;
    public readonly isUpdatingCycle = this.facade.isUpdatingCycle;
    public readonly isSavingCheckIn = this.facade.isSavingCheckIn;
    public readonly isActive = this.facade.isActive;
    public readonly currentSession = this.facade.currentSession;
    public readonly stats = this.facade.stats;
    public readonly history = this.facade.history;
    public readonly hungerLevel = this.facade.hungerLevel;
    public readonly energyLevel = this.facade.energyLevel;
    public readonly moodLevel = this.facade.moodLevel;
    public readonly selectedSymptoms = this.facade.selectedSymptoms;
    public readonly checkInNotes = this.facade.checkInNotes;
    public readonly hungerEmojiScale = FASTING_HUNGER_EMOJI_SCALE;
    public readonly energyEmojiScale = FASTING_ENERGY_EMOJI_SCALE;
    public readonly moodEmojiScale = FASTING_MOOD_EMOJI_SCALE;
    public readonly hungerEmojiOptions = computed(() => this.buildEmojiPickerOptions('FASTING.CHECK_IN.HUNGER', this.hungerEmojiScale));
    public readonly energyEmojiOptions = computed(() => this.buildEmojiPickerOptions('FASTING.CHECK_IN.ENERGY', this.energyEmojiScale));
    public readonly moodEmojiOptions = computed(() => this.buildEmojiPickerOptions('FASTING.CHECK_IN.MOOD', this.moodEmojiScale));
    public readonly symptomOptions = FASTING_SYMPTOM_OPTIONS;
    public readonly symptomChipOptions = computed(() => {
        this.currentLanguage();

        return this.symptomOptions.map<FdUiChipSelectOption>(symptom => {
            const label = this.translateService.instant(symptom.labelKey);
            return {
                value: symptom.value,
                label,
                ariaLabel: label,
                hint: label,
            };
        });
    });
    public readonly alerts = computed(() => this.facade.insightsData().alerts);
    public readonly insights = computed(() => this.facade.insightsData().insights);
    public readonly statsView = computed<FastingStatsViewModel | null>(() => {
        this.currentLanguage();
        const stats = this.stats();
        if (!stats) {
            return null;
        }

        return {
            stats,
            topSymptomLabel: this.getTopSymptomLabel(stats.topSymptom),
        };
    });
    public readonly visibleAlertItems = computed(() => {
        this.currentLanguage();
        const session = this.currentSession();
        return this.alerts()
            .filter(alert => this.facade.isPromptVisible(session, alert))
            .map(alert => this.buildMessageViewModel(alert));
    });
    public readonly insightItems = computed(() => {
        this.currentLanguage();
        return this.insights().map(insight => this.buildMessageViewModel(insight));
    });
    public readonly sessionCheckInVisibleCount = signal<Record<string, number>>({});
    public readonly isLoadingMoreHistory = this.facade.isLoadingMoreHistory;
    public readonly visibleHistory = this.history;
    public readonly canLoadMoreHistory = computed(() => this.facade.historyPage() < this.facade.historyTotalPages());
    public readonly isCheckInExpanded = signal(false);
    public readonly expandedHistorySessionId = signal<string | null>(null);
    public readonly hasCurrentCheckIn = computed(() => this.getCurrentSessionLatestCheckIn() !== null);
    public readonly currentSessionLatestCheckIn = computed(() => this.getCurrentSessionLatestCheckIn());
    public readonly currentSessionLatestCheckInView = computed<FastingCheckInViewModel | null>(() => {
        this.currentLanguage();
        const checkIn = this.currentSessionLatestCheckIn();
        return checkIn ? this.buildCheckInViewModel(checkIn) : null;
    });
    public readonly currentCheckInCtaKey = computed(() =>
        this.hasCurrentCheckIn() ? 'FASTING.CHECK_IN.UPDATE_ACTION' : 'FASTING.CHECK_IN.ADD_ACTION',
    );
    public readonly currentSessionRecentCheckIns = computed(() => {
        const session = this.currentSession();
        if (!session) {
            return [];
        }

        return this.getSessionCheckIns(session).slice(0, 3);
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

            this.isCheckInExpanded.set(false);
            this.toastService.success(this.translateService.instant('FASTING.CHECK_IN.SAVED_TOAST'));
        });
    }

    public toggleSymptom(symptom: string): void {
        this.facade.toggleSymptom(symptom);
    }

    public onCheckInNotesChange(value: string): void {
        this.facade.setCheckInNotes(value);
    }

    public saveCheckIn(): void {
        this.facade.saveCheckIn();
    }

    public openCheckInForm(): void {
        this.isCheckInExpanded.set(true);
    }

    public closeCheckInForm(): void {
        this.isCheckInExpanded.set(false);
        this.facade.resetCheckInDraft();
    }

    public dismissPrompt(promptId: string): void {
        this.facade.dismissPrompt(promptId);
    }

    public snoozePrompt(promptId: string): void {
        this.facade.snoozePrompt(promptId);
    }

    public loadMoreHistory(): void {
        this.facade.loadMoreHistory();
    }

    public openSessionCheckInChart(session: FastingSession): void {
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

    public getHistoryAccentColor(session: FastingSession): string {
        switch (session.status) {
            case 'Completed':
                return 'var(--fd-color-green-500)';
            case 'Interrupted':
                return 'var(--fd-color-orange-500)';
            case 'Skipped':
                return 'var(--fd-color-sky-500)';
            case 'Postponed':
                return 'var(--fd-color-ai)';
            default:
                return 'var(--fd-color-slate-400)';
        }
    }

    public getHistoryBadgeKey(status: FastingSessionStatus): string {
        switch (status) {
            case 'Completed':
                return 'FASTING.BADGE_COMPLETED';
            case 'Interrupted':
                return 'FASTING.BADGE_INTERRUPTED';
            case 'Skipped':
                return 'FASTING.BADGE_SKIPPED';
            case 'Postponed':
                return 'FASTING.BADGE_POSTPONED';
            default:
                return 'FASTING.BADGE_INCOMPLETE';
        }
    }

    public getHistoryProtocolLabel(protocol: string): string {
        const option = FASTING_PROTOCOLS.find(item => item.value === protocol);
        return option ? this.translateService.instant(option.labelKey) : protocol;
    }

    public getHistorySessionTypeLabel(session: FastingSession): string {
        if (session.planType === 'Cyclic') {
            return this.translateService.instant('FASTING.CYCLIC_TYPE');
        }

        const option = FASTING_PROTOCOLS.find(item => item.value === session.protocol);
        if (!option) {
            return this.translateService.instant('FASTING.EXTENDED_TYPE');
        }

        return this.translateService.instant(option.category === 'intermittent' ? 'FASTING.INTERMITTENT_TYPE' : 'FASTING.EXTENDED_TYPE');
    }

    public getHistoryProtocolDisplay(session: FastingSession): string {
        if (session.planType === 'Cyclic') {
            const cycleLabel =
                session.cyclicFastDays && session.cyclicEatDays ? `${session.cyclicFastDays}:${session.cyclicEatDays}` : '1:1';
            const eatWindowHours = session.cyclicEatDayEatingWindowHours ?? 8;
            const eatFastHours = session.cyclicEatDayFastHours ?? 16;
            return `${cycleLabel} (${eatFastHours}:${eatWindowHours})`;
        }

        const option = FASTING_PROTOCOLS.find(item => item.value === session.protocol);
        const hoursLabel = this.translateService.instant('FASTING.HOURS');

        if (!option) {
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

    public hasPersonalSummary(stats: FastingStats | null): boolean {
        return (
            !!stats &&
            (stats.completionRateLast30Days > 0 || stats.checkInRateLast30Days > 0 || !!stats.lastCheckInAtUtc || !!stats.topSymptom)
        );
    }

    public getTopSymptomLabel(symptom: string | null): string {
        return symptom ? this.getSymptomLabel(symptom) : this.translateService.instant('FASTING.PERSONAL_SUMMARY.NO_SYMPTOM');
    }

    public hasCheckIn(session: FastingSession): boolean {
        return this.getSessionCheckIns(session).length > 0;
    }

    public isHistorySessionExpanded(sessionId: string): boolean {
        return this.expandedHistorySessionId() === sessionId;
    }

    public toggleHistorySession(sessionId: string): void {
        this.expandedHistorySessionId.update(current => (current === sessionId ? null : sessionId));
    }

    public getHistoryCheckInToggleKey(session: FastingSession): string {
        return this.isHistorySessionExpanded(session.id) ? 'FASTING.HIDE_HISTORY_CHECK_INS' : 'FASTING.SHOW_HISTORY_CHECK_INS';
    }

    public getLatestSessionCheckIn(session: FastingSession): FastingCheckIn | null {
        const checkIns = this.getSessionCheckIns(session);
        return checkIns.length > 0 ? (checkIns[0] ?? null) : null;
    }

    public getSessionCheckInCount(session: FastingSession): number {
        return this.getSessionCheckIns(session).length;
    }

    public canViewSessionCheckInChart(session: FastingSession): boolean {
        return this.getSessionCheckInCount(session) > 1;
    }

    public getCheckInSummary(hunger: number | null, energy: number | null, mood: number | null): string {
        return this.translateService.instant('FASTING.CHECK_IN.SUMMARY', {
            hunger: this.getHungerSummaryValue(hunger),
            energy: this.getEnergySummaryValue(energy),
            mood: this.getMoodSummaryValue(mood),
        });
    }

    public hasCurrentSessionTimeline(): boolean {
        return this.currentSessionRecentCheckIns().length > 0;
    }

    public getCurrentSessionOlderCheckInsCount(): number {
        const session = this.currentSession();
        if (!session) {
            return 0;
        }

        return Math.max(0, this.getSessionCheckIns(session).length - this.currentSessionRecentCheckIns().length);
    }

    public getSessionCheckIns(session: FastingSession): FastingCheckIn[] {
        if (session.checkIns.length > 0) {
            return session.checkIns;
        }

        if (!session.checkInAtUtc) {
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

    public getVisibleSessionCheckIns(session: FastingSession): FastingCheckIn[] {
        const allCheckIns = this.getSessionCheckIns(session);
        const visibleCount = this.sessionCheckInVisibleCount()[session.id] ?? FASTING_SESSION_CHECK_INS_PAGE_SIZE;
        return allCheckIns.slice(0, visibleCount);
    }

    public canLoadMoreSessionCheckIns(session: FastingSession): boolean {
        const allCheckIns = this.getSessionCheckIns(session);
        const visibleCount = this.sessionCheckInVisibleCount()[session.id] ?? FASTING_SESSION_CHECK_INS_PAGE_SIZE;
        return allCheckIns.length > visibleCount;
    }

    public loadMoreSessionCheckIns(sessionId: string): void {
        this.sessionCheckInVisibleCount.update(current => ({
            ...current,
            [sessionId]: (current[sessionId] ?? FASTING_SESSION_CHECK_INS_PAGE_SIZE) + FASTING_SESSION_CHECK_INS_PAGE_SIZE,
        }));
    }

    public getHistoryCheckInRegionId(sessionId: string): string {
        return `fasting-history-checkins-${sessionId}`;
    }

    private buildEmojiPickerOptions(labelKey: string, scale: FastingEmojiScaleOption[]): FdUiEmojiPickerOption<number>[] {
        this.currentLanguage();

        const label = this.translateService.instant(labelKey);
        return scale.map(option => {
            const text = `${label} ${option.value}/5`;
            return {
                value: option.value,
                emoji: option.emoji,
                ariaLabel: text,
                hint: text,
            };
        });
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
        return `${fastHours}:${24 - fastHours}`;
    }

    private getCurrentSessionLatestCheckIn(): FastingCheckIn | null {
        const session = this.currentSession();
        if (!session) {
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

    private getHistoryChartSubtitle(session: FastingSession): string {
        return `${this.formatSessionDateLabel(session.startedAtUtc)} Â· ${this.getHistorySessionTypeLabel(session)} Â· ${this.getHistoryProtocolDisplay(session)}`;
    }

    private formatSessionDateLabel(value: string): string {
        return new Intl.DateTimeFormat(this.localizationService.getCurrentLanguage() === 'ru' ? 'ru-RU' : 'en-US', {
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
            case 'warning':
                return 'warning';
            case 'positive':
                return 'success';
            default:
                return 'info';
        }
    }

    public getEnergyEmoji(level: number | null | undefined): string {
        return this.energyEmojiScale.find(option => option.value === level)?.emoji ?? '—';
    }

    public getHungerEmoji(level: number | null | undefined): string {
        return this.hungerEmojiScale.find(option => option.value === level)?.emoji ?? '—';
    }

    public getMoodEmoji(level: number | null | undefined): string {
        return this.moodEmojiScale.find(option => option.value === level)?.emoji ?? '—';
    }

    public formatRelativeTime(value: string | null): string | null {
        if (!value) {
            return null;
        }

        const timestamp = new Date(value).getTime();
        if (Number.isNaN(timestamp)) {
            return null;
        }

        const diffMs = timestamp - Date.now();
        const diffMinutes = Math.round(diffMs / 60000);
        const locale = this.localizationService.getCurrentLanguage() === 'ru' ? 'ru-RU' : 'en-US';
        const formatter = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' });

        if (Math.abs(diffMinutes) < 60) {
            return formatter.format(diffMinutes, 'minute');
        }

        const diffHours = Math.round(diffMinutes / 60);
        if (Math.abs(diffHours) < 24) {
            return formatter.format(diffHours, 'hour');
        }

        const diffDays = Math.round(diffHours / 24);
        return formatter.format(diffDays, 'day');
    }

    private resolveMessageParams(params: Record<string, string> | null): Record<string, string> | undefined {
        if (!params) {
            return undefined;
        }

        return Object.fromEntries(
            Object.entries(params).map(([key, value]) => [
                key,
                value.startsWith('FASTING.') ? this.translateService.instant(value) : value,
            ]),
        );
    }

    private getEnergyDisplay(level: number | null): string {
        if (level === null) {
            return '—';
        }

        return `${this.getEnergyEmoji(level)} ${level}/5`;
    }

    private getMoodDisplay(level: number | null): string {
        if (level === null) {
            return '—';
        }

        return `${this.getMoodEmoji(level)} ${level}/5`;
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

interface FastingStatsViewModel {
    stats: FastingStats;
    topSymptomLabel: string;
}

interface FastingMessageViewModel {
    message: FastingMessage;
    severity: FdUiInlineAlertSeverity;
    title: string;
    body: string;
}

interface FastingCheckInViewModel {
    checkIn: FastingCheckIn;
    relativeCheckedInAt: string | null;
    summary: string;
    symptomLabels: string[];
}
