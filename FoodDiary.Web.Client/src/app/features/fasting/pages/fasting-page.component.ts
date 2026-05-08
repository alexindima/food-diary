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
import { FastingControlsComponent } from '../components/fasting-controls/fasting-controls.component';
import { FastingTimerCardComponent } from '../components/fasting-timer-card/fasting-timer-card.component';
import { FastingFacade } from '../lib/fasting.facade';
import {
    FASTING_ENERGY_EMOJI_SCALE,
    FASTING_HUNGER_EMOJI_SCALE,
    FASTING_MOOD_EMOJI_SCALE,
    FASTING_SESSION_CHECK_INS_PAGE_SIZE,
    type FastingEmojiScaleOption,
} from '../lib/fasting-page.constants';
import { type FastingStagePresentation, resolveFastingStage } from '../lib/fasting-stage';
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
        FastingControlsComponent,
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
    public readonly progressPercent = this.facade.progressPercent;
    public readonly elapsedFormatted = this.facade.elapsedFormatted;
    public readonly remainingFormatted = this.facade.remainingFormatted;
    public readonly currentStage = computed<FastingStagePresentation | null>(() => {
        const session = this.currentSession();
        if (!session) {
            return null;
        }

        if (session.planType === 'Cyclic' && session.occurrenceKind === 'EatDay' && !session.endedAtUtc) {
            return null;
        }

        return resolveFastingStage(this.facade.elapsedMs(), session.plannedDurationHours);
    });
    public readonly currentRingColor = computed(() => {
        const session = this.currentSession();
        if (session?.planType === 'Cyclic' && session.occurrenceKind === 'EatDay' && !session.endedAtUtc) {
            return 'var(--fd-color-green-500)';
        }

        return this.currentStage()?.color ?? null;
    });
    public readonly nextStageFormatted = computed(() => {
        const stage = this.currentStage();
        if (!stage?.nextInMs) {
            return null;
        }

        return this.formatDuration(stage.nextInMs);
    });
    public readonly isOvertime = this.facade.isOvertime;
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
    public readonly sessionCheckInVisibleCount = signal<Record<string, number>>({});
    public readonly isLoadingMoreHistory = this.facade.isLoadingMoreHistory;
    public readonly visibleHistory = this.history;
    public readonly canLoadMoreHistory = computed(() => this.facade.historyPage() < this.facade.historyTotalPages());
    public readonly isCheckInExpanded = signal(false);
    public readonly expandedHistorySessionId = signal<string | null>(null);
    public readonly hasCurrentCheckIn = computed(() => this.getCurrentSessionLatestCheckIn() !== null);
    public readonly currentSessionLatestCheckIn = computed(() => this.getCurrentSessionLatestCheckIn());
    public readonly currentSessionRecentCheckIns = computed(() => {
        const session = this.currentSession();
        if (!session) {
            return [];
        }

        return this.getSessionCheckIns(session).slice(0, 3);
    });
    public readonly visibleAlerts = computed(() => {
        const session = this.currentSession();
        return this.alerts().filter(alert => this.facade.isPromptVisible(session, alert));
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

    public getCurrentCheckInCtaKey(): string {
        return this.hasCurrentCheckIn() ? 'FASTING.CHECK_IN.UPDATE_ACTION' : 'FASTING.CHECK_IN.ADD_ACTION';
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

    public getCurrentCardLabelKey(): string {
        const session = this.currentSession();
        if (!session) {
            return 'FASTING.WIDGET_LABEL';
        }

        switch (session.planType) {
            case 'Cyclic':
                return 'FASTING.CYCLIC_TYPE';
            case 'Extended':
                return 'FASTING.EXTENDED_TYPE';
            default:
                return 'FASTING.INTERMITTENT_TYPE';
        }
    }

    public getCurrentCardStateLabel(): string | null {
        const session = this.currentSession();
        if (!session?.endedAtUtc) {
            return this.getOccurrenceKindLabel(session?.occurrenceKind);
        }

        return null;
    }

    public getCurrentCardDetailLabel(): string | null {
        const session = this.currentSession();
        if (!session) {
            return null;
        }

        if (session.planType === 'Cyclic') {
            const cycleLabel = this.translateService.instant('FASTING.CYCLE_PATTERN', {
                fast: session.cyclicFastDays ?? 1,
                eat: session.cyclicEatDays ?? 1,
            });
            const eatWindowLabel = this.translateService.instant('FASTING.EAT_WINDOW_PATTERN', {
                fast: session.cyclicEatDayFastHours ?? 16,
                eat: session.cyclicEatDayEatingWindowHours ?? 8,
            });

            return `${cycleLabel} · ${eatWindowLabel}`;
        }

        return null;
    }

    public getCurrentCardSummaryDetailLabel(): string | null {
        const session = this.currentSession();
        if (!session) {
            return null;
        }

        return this.getHistoryProtocolDisplay(session);
    }

    public getCurrentCardMetaLabel(): string | null {
        const session = this.currentSession();
        if (!session || session.planType !== 'Cyclic') {
            return null;
        }

        return this.getCyclicPhaseProgressLabel(session);
    }

    public getCurrentRemainingLabelKey(): string {
        const session = this.currentSession();
        if (!session) {
            return 'FASTING.REMAINING';
        }

        if (session.planType === 'Intermittent' && session.occurrenceKind === 'FastingWindow') {
            return 'FASTING.UNTIL_EATING_WINDOW';
        }

        return 'FASTING.REMAINING';
    }

    private formatDuration(ms: number): string {
        const totalSeconds = Math.floor(ms / 1000);
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;
        return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
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

    private getOccurrenceKindLabel(kind?: FastingSession['occurrenceKind']): string | null {
        switch (kind) {
            case 'FastDay':
                return this.translateService.instant('FASTING.FAST_DAY');
            case 'EatDay':
                return this.translateService.instant('FASTING.EAT_DAY');
            case 'FastingWindow':
                return this.translateService.instant('FASTING.FASTING_WINDOW');
            case 'EatingWindow':
                return this.translateService.instant('FASTING.EATING_WINDOW');
            default:
                return null;
        }
    }

    private getCyclicPhaseProgressLabel(session: FastingSession): string | null {
        const dayNumber = session.cyclicPhaseDayNumber;
        const dayTotal = session.cyclicPhaseDayTotal;
        if (!dayNumber || !dayTotal) {
            return this.getOccurrenceKindLabel(session.occurrenceKind);
        }

        if (session.occurrenceKind === 'EatDay') {
            return this.translateService.instant('FASTING.CYCLIC_EAT_PHASE_DAY_PROGRESS', {
                current: dayNumber,
                total: dayTotal,
            });
        }

        const stage = this.currentStage();
        if (stage) {
            return this.translateService.instant('FASTING.CYCLIC_FAST_PHASE_STAGE_PROGRESS', {
                current: dayNumber,
                total: dayTotal,
                stage: stage.index,
                stageTotal: stage.total,
            });
        }

        return this.translateService.instant('FASTING.CYCLIC_FAST_PHASE_DAY_PROGRESS', { current: dayNumber, total: dayTotal });
    }

    private getSymptomLabel(symptom: string): string {
        return this.translateService.instant(`FASTING.CHECK_IN.SYMPTOMS.${symptom.toUpperCase()}`);
    }

    private getHistoryChartSubtitle(session: FastingSession): string {
        return `${this.formatSessionDateLabel(session.startedAtUtc)} · ${this.getHistorySessionTypeLabel(session)} · ${this.getHistoryProtocolDisplay(session)}`;
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

    public getTranslatedMessage(descriptor: FastingMessage, field: 'titleKey' | 'bodyKey'): string {
        const key = descriptor[field];
        return this.translateService.instant(key, this.resolveMessageParams(descriptor.bodyParams));
    }

    public getAlertSeverity(message: FastingMessage): FdUiInlineAlertSeverity {
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
