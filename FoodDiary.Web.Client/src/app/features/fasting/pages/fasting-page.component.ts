import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiInlineAlertComponent, FdUiInlineAlertSeverity } from 'fd-ui-kit/inline-alert/fd-ui-inline-alert.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { LocalizedDatePipe } from '../../../pipes/localized-date.pipe';
import { LocalizationService } from '../../../services/localization.service';
import {
    FastingEndConfirmDialogComponent,
    FastingEndConfirmDialogData,
    FastingEndConfirmDialogResult,
} from '../components/fasting-end-confirm-dialog/fasting-end-confirm-dialog.component';
import {
    FastingSafetyDialogComponent,
    FastingSafetyDialogData,
    FastingSafetyDialogResult,
} from '../components/fasting-safety-dialog/fasting-safety-dialog.component';
import {
    FastingCheckInChartDialogComponent,
    FastingCheckInChartDialogData,
} from '../components/fasting-checkin-chart-dialog/fasting-checkin-chart-dialog.component';
import { FastingTimerCardComponent } from '../components/fasting-timer-card/fasting-timer-card.component';
import { FastingFacade } from '../lib/fasting.facade';
import {
    FASTING_ENERGY_EMOJI_SCALE,
    FASTING_HARD_STOP_THRESHOLD_HOURS,
    FASTING_HUNGER_EMOJI_SCALE,
    FASTING_MOOD_EMOJI_SCALE,
    FASTING_SESSION_CHECK_INS_PAGE_SIZE,
    FASTING_WARNING_THRESHOLD_HOURS,
} from '../lib/fasting-page.constants';
import { FastingStagePresentation, resolveFastingStage } from '../lib/fasting-stage';
import {
    CYCLIC_PRESETS,
    FastingCheckIn,
    FASTING_PROTOCOLS,
    FASTING_SYMPTOM_OPTIONS,
    FastingMessage,
    FastingMode,
    FastingProtocol,
    FastingSession,
    FastingSessionStatus,
    FastingStats,
} from '../models/fasting.data';

@Component({
    selector: 'fd-fasting-page',
    standalone: true,
    imports: [
        DecimalPipe,
        FormsModule,
        TranslatePipe,
        LocalizedDatePipe,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiInputComponent,
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

    public readonly isLoading = this.facade.isLoading;
    public readonly isStarting = this.facade.isStarting;
    public readonly isEnding = this.facade.isEnding;
    public readonly isExtending = this.facade.isExtending;
    public readonly isReducing = this.facade.isReducing;
    public readonly isUpdatingCycle = this.facade.isUpdatingCycle;
    public readonly isSavingCheckIn = this.facade.isSavingCheckIn;
    public readonly isActive = this.facade.isActive;
    public readonly currentSession = this.facade.currentSession;
    public readonly stats = this.facade.stats;
    public readonly history = this.facade.history;
    public readonly selectedMode = this.facade.selectedMode;
    public readonly selectedProtocol = this.facade.selectedProtocol;
    public readonly customHours = this.facade.customHours;
    public readonly customIntermittentFastHours = this.facade.customIntermittentFastHours;
    public readonly cyclicEatDayProtocol = this.facade.cyclicEatDayProtocol;
    public readonly cyclicFastDays = this.facade.cyclicFastDays;
    public readonly cyclicEatDays = this.facade.cyclicEatDays;
    public readonly cyclicUsesCustomPreset = this.facade.cyclicUsesCustomPreset;
    public readonly cyclicEatDayFastHours = this.facade.cyclicEatDayFastHours;
    public readonly extendHours = this.facade.extendHours;
    public readonly reduceHours = this.facade.reduceHours;
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
    public readonly canExtendActiveSession = this.facade.canExtendActiveSession;
    public readonly intermittentProtocols = FASTING_PROTOCOLS.filter(protocol => protocol.category === 'intermittent');
    public readonly cyclicEatDayProtocols = FASTING_PROTOCOLS.filter(protocol => protocol.category === 'intermittent');
    public readonly extendedProtocols = FASTING_PROTOCOLS.filter(protocol => protocol.category === 'extended');
    public readonly cyclicPresets = CYCLIC_PRESETS;
    public readonly hungerEmojiScale = FASTING_HUNGER_EMOJI_SCALE;
    public readonly energyEmojiScale = FASTING_ENERGY_EMOJI_SCALE;
    public readonly moodEmojiScale = FASTING_MOOD_EMOJI_SCALE;
    public readonly symptomOptions = FASTING_SYMPTOM_OPTIONS;
    public readonly alerts = computed(() => this.facade.insightsData().alerts);
    public readonly insights = computed(() => this.facade.insightsData().insights);
    public readonly sessionCheckInVisibleCount = signal<Record<string, number>>({});
    public readonly isLoadingMoreHistory = this.facade.isLoadingMoreHistory;
    public readonly visibleHistory = this.history;
    public readonly canLoadMoreHistory = computed(() => this.facade.historyPage() < this.facade.historyTotalPages());
    public readonly isCheckInExpanded = signal(false);
    public readonly isCustomExtendExpanded = signal(false);
    public readonly isCustomReduceExpanded = signal(false);
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

        effect(() => {
            const version = this.facade.checkInSavedVersion();
            if (version <= 0) {
                return;
            }

            this.isCheckInExpanded.set(false);
            this.toastService.success(this.translateService.instant('FASTING.CHECK_IN.SAVED_TOAST'));
        });
    }

    public selectMode(mode: FastingMode): void {
        this.facade.selectMode(mode);
    }

    public selectProtocol(protocol: FastingProtocol): void {
        this.facade.selectProtocol(protocol);
    }

    public onCustomHoursChange(value: string | number): void {
        const hours = typeof value === 'number' ? value : parseInt(value, 10);
        if (!isNaN(hours)) {
            this.facade.setCustomHours(hours);
        }
    }

    public onCustomIntermittentFastHoursChange(value: string | number): void {
        const hours = typeof value === 'number' ? value : parseInt(value, 10);
        if (!isNaN(hours)) {
            this.facade.setCustomIntermittentFastHours(hours);
        }
    }

    public selectCyclicPreset(fastDays: number, eatDays: number): void {
        this.facade.setCyclicPreset(fastDays, eatDays);
    }

    public selectCustomCyclicPreset(): void {
        this.facade.selectCustomCyclicPreset();
    }

    public onCyclicFastDaysChange(value: string | number): void {
        const days = typeof value === 'number' ? value : parseInt(value, 10);
        if (!isNaN(days)) {
            this.facade.setCyclicFastDays(days);
        }
    }

    public onCyclicEatDaysChange(value: string | number): void {
        const days = typeof value === 'number' ? value : parseInt(value, 10);
        if (!isNaN(days)) {
            this.facade.setCyclicEatDays(days);
        }
    }

    public selectCyclicEatDayProtocol(protocol: FastingProtocol): void {
        this.facade.selectCyclicEatDayProtocol(protocol);
    }

    public onCyclicEatDayFastHoursChange(value: string | number): void {
        const hours = typeof value === 'number' ? value : parseInt(value, 10);
        if (!isNaN(hours)) {
            this.facade.setCyclicEatDayFastHours(hours);
        }
    }

    public startFasting(): void {
        this.facade.startFasting();
    }

    public endFasting(): void {
        const data = this.getEndConfirmDialogData();
        this.dialogService
            .open<FastingEndConfirmDialogComponent, FastingEndConfirmDialogData, FastingEndConfirmDialogResult>(
                FastingEndConfirmDialogComponent,
                {
                    size: 'sm',
                    data,
                },
            )
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                if (result === 'confirm') {
                    this.facade.endFasting();
                }
            });
    }

    public onExtendHoursChange(value: string | number): void {
        const hours = typeof value === 'number' ? value : parseInt(value, 10);
        if (!isNaN(hours)) {
            this.facade.setExtendHours(hours);
        }
    }

    public onReduceHoursChange(value: string | number): void {
        const hours = typeof value === 'number' ? value : parseInt(value, 10);
        if (!isNaN(hours)) {
            this.facade.setReduceHours(hours);
        }
    }

    public setHungerLevel(level: number): void {
        this.facade.setHungerLevel(level);
    }

    public setEnergyLevel(level: number): void {
        this.facade.setEnergyLevel(level);
    }

    public setMoodLevel(level: number): void {
        this.facade.setMoodLevel(level);
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
                width: 'min(1440px, calc(100vw - 40px))',
                maxWidth: 'min(1440px, calc(100vw - 40px))',
                panelClass: 'fd-ui-dialog-panel--chart',
                data: {
                    title: this.translateService.instant('FASTING.CHECK_IN.CHART_TITLE'),
                    subtitle: this.getHistoryChartSubtitle(session),
                    checkIns: this.getSessionCheckIns(session),
                },
            },
        );
    }

    public extendByDay(): void {
        this.isCustomExtendExpanded.set(false);
        this.requestExtendByHours(24);
    }

    public extendBy36Hours(): void {
        this.isCustomExtendExpanded.set(false);
        this.requestExtendByHours(36);
    }

    public extendByCustom(): void {
        this.requestExtendByHours(this.extendHours());
    }

    public showCustomExtend(): void {
        this.isCustomExtendExpanded.set(true);
    }

    public reduceBy4Hours(): void {
        this.isCustomReduceExpanded.set(false);
        this.requestReduceByHours(4);
    }

    public reduceBy8Hours(): void {
        this.isCustomReduceExpanded.set(false);
        this.requestReduceByHours(8);
    }

    public reduceByCustom(): void {
        this.requestReduceByHours(this.reduceHours());
    }

    public showCustomReduce(): void {
        this.isCustomReduceExpanded.set(true);
    }

    public skipCyclicDay(): void {
        this.openCycleActionDialog(
            this.getSkipCycleConfirmTitleKey(),
            this.getSkipCycleConfirmMessageKey(),
            this.getSkipCycleActionLabelKey(),
            () => this.facade.skipCyclicDay(),
        );
    }

    public postponeCyclicDay(): void {
        this.openCycleActionDialog(
            this.getPostponeCycleConfirmTitleKey(),
            this.getPostponeCycleConfirmMessageKey(),
            this.getPostponeCycleActionLabelKey(),
            () => this.facade.postponeCyclicDay(),
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
                return '#a855f7';
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

    public getEndActionLabelKey(): string {
        if (this.isCurrentSessionCyclic()) {
            return 'FASTING.STOP_CYCLE';
        }

        return this.isCurrentSessionIntermittent() ? 'FASTING.END_FAST' : 'FASTING.INTERRUPT_FAST';
    }

    public canManageCurrentCyclicDay(): boolean {
        const session = this.currentSession();
        return (
            !!session &&
            !session.endedAtUtc &&
            session.planType === 'Cyclic' &&
            (session.occurrenceKind === 'FastDay' || session.occurrenceKind === 'EatDay')
        );
    }

    public getSkipCycleActionLabelKey(): string {
        return this.getCurrentCyclicOccurrenceKind() === 'EatDay' ? 'FASTING.START_FAST_NOW' : 'FASTING.SKIP_FASTING_PERIOD';
    }

    public getPostponeCycleActionLabelKey(): string {
        return this.getCurrentCyclicOccurrenceKind() === 'EatDay' ? 'FASTING.SKIP_DAY' : 'FASTING.SKIP_DAY';
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

    public isSelectedCyclicPreset(fastDays: number, eatDays: number): boolean {
        return !this.cyclicUsesCustomPreset() && this.cyclicFastDays() === fastDays && this.cyclicEatDays() === eatDays;
    }

    public isCustomCyclicPresetSelected(): boolean {
        return this.cyclicUsesCustomPreset();
    }

    private formatDuration(ms: number): string {
        const totalSeconds = Math.floor(ms / 1000);
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;
        return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
    }

    private getEndConfirmDialogData(): FastingEndConfirmDialogData {
        if (this.isCurrentSessionCyclic()) {
            return {
                title: this.translateService.instant('FASTING.STOP_CYCLE_CONFIRM_TITLE'),
                message: this.translateService.instant('FASTING.STOP_CYCLE_CONFIRM_MESSAGE'),
                confirmLabel: this.translateService.instant('FASTING.STOP_CYCLE'),
                cancelLabel: this.translateService.instant('FASTING.CANCEL_ACTION'),
            };
        }

        const isIntermittent = this.isCurrentSessionIntermittent();
        return {
            title: this.translateService.instant(isIntermittent ? 'FASTING.END_CONFIRM_TITLE' : 'FASTING.INTERRUPT_CONFIRM_TITLE'),
            message: this.translateService.instant(isIntermittent ? 'FASTING.END_CONFIRM_MESSAGE' : 'FASTING.INTERRUPT_CONFIRM_MESSAGE'),
            confirmLabel: this.translateService.instant(isIntermittent ? 'FASTING.END_FAST' : 'FASTING.INTERRUPT_FAST'),
            cancelLabel: this.translateService.instant('FASTING.CANCEL_ACTION'),
        };
    }

    private isCurrentSessionIntermittent(): boolean {
        const session = this.currentSession();
        if (!session) {
            return true;
        }

        return session.planType !== 'Extended';
    }

    private isCurrentSessionCyclic(): boolean {
        return this.currentSession()?.planType === 'Cyclic';
    }

    private getSkipCycleConfirmTitleKey(): string {
        return this.getCurrentCyclicOccurrenceKind() === 'EatDay'
            ? 'FASTING.START_FAST_NOW_CONFIRM_TITLE'
            : 'FASTING.SKIP_FASTING_PERIOD_CONFIRM_TITLE';
    }

    private getSkipCycleConfirmMessageKey(): string {
        return this.getCurrentCyclicOccurrenceKind() === 'EatDay'
            ? 'FASTING.START_FAST_NOW_CONFIRM_MESSAGE'
            : 'FASTING.SKIP_FASTING_PERIOD_CONFIRM_MESSAGE';
    }

    private getPostponeCycleConfirmTitleKey(): string {
        return this.getCurrentCyclicOccurrenceKind() === 'EatDay'
            ? 'FASTING.SKIP_EATING_DAY_CONFIRM_TITLE'
            : 'FASTING.SKIP_FAST_DAY_CONFIRM_TITLE';
    }

    private getPostponeCycleConfirmMessageKey(): string {
        return this.getCurrentCyclicOccurrenceKind() === 'EatDay'
            ? 'FASTING.SKIP_EATING_DAY_CONFIRM_MESSAGE'
            : 'FASTING.SKIP_FAST_DAY_CONFIRM_MESSAGE';
    }

    private getCurrentCyclicOccurrenceKind(): FastingSession['occurrenceKind'] | null {
        const session = this.currentSession();
        if (!session || session.planType !== 'Cyclic' || session.endedAtUtc) {
            return null;
        }

        return session.occurrenceKind;
    }

    private requestExtendByHours(additionalHours: number): void {
        const normalizedHours = Math.max(1, Math.min(FASTING_HARD_STOP_THRESHOLD_HOURS, additionalHours));
        const currentSession = this.currentSession();
        const currentDuration = currentSession?.plannedDurationHours ?? 0;
        const targetDuration = currentDuration + normalizedHours;

        if (targetDuration > FASTING_HARD_STOP_THRESHOLD_HOURS) {
            this.openSafetyDialog({
                title: this.translateService.instant('FASTING.LIFE_RISK_TITLE'),
                message: this.translateService.instant('FASTING.LIFE_RISK_MESSAGE'),
                cancelLabel: this.translateService.instant('FASTING.CLOSE_ACTION'),
                tone: 'danger',
            });
            return;
        }

        if (targetDuration > FASTING_WARNING_THRESHOLD_HOURS) {
            this.openSafetyDialog({
                title: this.translateService.instant('FASTING.EXTEND_WARNING_TITLE'),
                message: this.translateService.instant('FASTING.EXTEND_WARNING_MESSAGE'),
                confirmLabel: this.translateService.instant('FASTING.CONFIRM_ADD_ACTION'),
                cancelLabel: this.translateService.instant('FASTING.CANCEL_ACTION'),
                tone: 'warning',
            })
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe(result => {
                    if (result === 'confirm') {
                        this.facade.extendByHours(normalizedHours);
                    }
                });
            return;
        }

        this.facade.extendByHours(normalizedHours);
    }

    private requestReduceByHours(reducedHours: number): void {
        const session = this.currentSession();
        if (!session) {
            return;
        }

        const maxReducibleHours = Math.max(0, session.plannedDurationHours - 1);
        const normalizedHours = Math.max(1, Math.min(maxReducibleHours, reducedHours));
        if (normalizedHours <= 0) {
            return;
        }

        this.facade.reduceTargetByHours(normalizedHours);
    }

    private openCycleActionDialog(titleKey: string, messageKey: string, confirmLabelKey: string, action: () => void): void {
        this.dialogService
            .open<FastingEndConfirmDialogComponent, FastingEndConfirmDialogData, FastingEndConfirmDialogResult>(
                FastingEndConfirmDialogComponent,
                {
                    size: 'sm',
                    data: {
                        title: this.translateService.instant(titleKey),
                        message: this.translateService.instant(messageKey),
                        confirmLabel: this.translateService.instant(confirmLabelKey),
                        cancelLabel: this.translateService.instant('FASTING.CANCEL_ACTION'),
                    },
                },
            )
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                if (result === 'confirm') {
                    action();
                }
            });
    }

    private openSafetyDialog(data: FastingSafetyDialogData): Observable<FastingSafetyDialogResult | undefined> {
        return this.dialogService
            .open<FastingSafetyDialogComponent, FastingSafetyDialogData, FastingSafetyDialogResult>(FastingSafetyDialogComponent, {
                size: 'sm',
                data,
            })
            .afterClosed();
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

    public getCustomIntermittentEatingWindowHours(): number {
        return Math.max(1, 24 - this.customIntermittentFastHours());
    }

    public getCyclicEatDayEatingWindowHours(): number {
        return Math.max(1, 24 - this.cyclicEatDayFastHours());
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

        const key = session.occurrenceKind === 'EatDay' ? 'FASTING.CYCLIC_EAT_PHASE_PROGRESS' : 'FASTING.CYCLIC_FAST_PHASE_PROGRESS';

        return this.translateService.instant(key, { current: dayNumber, total: dayTotal });
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
