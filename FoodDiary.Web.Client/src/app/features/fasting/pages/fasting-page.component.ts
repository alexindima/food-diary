import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { LocalizedDatePipe } from '../../../pipes/localized-date.pipe';
import { AdminTelemetryService } from '../../../services/admin-telemetry.service';
import { AuthService } from '../../../services/auth.service';
import { LocalizationService } from '../../../services/localization.service';
import { FastingTelemetrySummary } from '../../../shared/models/admin-telemetry.data';
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
import { FastingTimerCardComponent } from '../components/fasting-timer-card/fasting-timer-card.component';
import { FastingFacade } from '../lib/fasting.facade';
import { FastingStagePresentation, resolveFastingStage } from '../lib/fasting-stage';
import {
    CYCLIC_PRESETS,
    FASTING_PROTOCOLS,
    FASTING_CHECK_IN_SCALE,
    FASTING_SYMPTOM_OPTIONS,
    FastingMessage,
    FastingMode,
    FastingProtocol,
    FastingSession,
    FastingSessionStatus,
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
        FastingTimerCardComponent,
    ],
    templateUrl: './fasting-page.component.html',
    styleUrl: './fasting-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FastingFacade],
})
export class FastingPageComponent implements OnInit {
    private static readonly WarningThresholdHours = 72;
    private static readonly HardStopThresholdHours = 168;

    private readonly facade = inject(FastingFacade);
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly authService = inject(AuthService);
    private readonly adminTelemetryService = inject(AdminTelemetryService);
    private readonly localizationService = inject(LocalizationService);

    public readonly isLoading = this.facade.isLoading;
    public readonly isStarting = this.facade.isStarting;
    public readonly isEnding = this.facade.isEnding;
    public readonly isExtending = this.facade.isExtending;
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
            return '#22c55e';
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
    public readonly checkInScale = FASTING_CHECK_IN_SCALE;
    public readonly symptomOptions = FASTING_SYMPTOM_OPTIONS;
    public readonly insights = computed(() => this.facade.insightsData().insights);
    public readonly fastingTelemetrySummary = signal<FastingTelemetrySummary | null>(null);
    public readonly isLoadingFastingTelemetrySummary = signal(false);
    public readonly visibleCheckInPrompt = computed(() => {
        const prompt = this.facade.insightsData().currentPrompt;
        const session = this.currentSession();
        return this.facade.isPromptVisible(session, prompt) ? prompt : null;
    });

    public ngOnInit(): void {
        this.facade.initialize();
        this.loadFastingTelemetrySummary();
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

    public dismissPrompt(promptId: string): void {
        this.facade.dismissPrompt(promptId);
    }

    public isAdminUser(): boolean {
        return this.authService.isAdmin();
    }

    public loadFastingTelemetrySummary(): void {
        if (!this.isAdminUser() || this.isLoadingFastingTelemetrySummary()) {
            return;
        }

        this.isLoadingFastingTelemetrySummary.set(true);
        this.adminTelemetryService
            .getFastingSummary()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: summary => {
                    this.fastingTelemetrySummary.set(summary);
                    this.isLoadingFastingTelemetrySummary.set(false);
                },
                error: () => {
                    this.fastingTelemetrySummary.set(null);
                    this.isLoadingFastingTelemetrySummary.set(false);
                },
            });
    }

    public snoozePrompt(promptId: string): void {
        this.facade.snoozePrompt(promptId);
    }

    public extendByDay(): void {
        this.requestExtendByHours(24);
    }

    public extendByCustom(): void {
        this.requestExtendByHours(this.extendHours());
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
                return '#22c55e';
            case 'Interrupted':
                return '#f97316';
            case 'Skipped':
                return '#0ea5e9';
            case 'Postponed':
                return '#a855f7';
            default:
                return '#94a3b8';
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
        if (addedHours <= 0) {
            return baseLabel;
        }

        return `${baseLabel} (+${addedHours} ${hoursLabel})`;
    }

    public hasCheckIn(session: FastingSession): boolean {
        return !!session.checkInAtUtc;
    }

    public getCheckInSummary(session: FastingSession): string | null {
        if (!this.hasCheckIn(session)) {
            return null;
        }

        return this.translateService.instant('FASTING.CHECK_IN.SUMMARY', {
            hunger: session.hungerLevel ?? '—',
            energy: session.energyLevel ?? '—',
            mood: session.moodLevel ?? '—',
        });
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
        const normalizedHours = Math.max(1, Math.min(FastingPageComponent.HardStopThresholdHours, additionalHours));
        const currentSession = this.currentSession();
        const currentDuration = currentSession?.plannedDurationHours ?? 0;
        const targetDuration = currentDuration + normalizedHours;

        if (targetDuration > FastingPageComponent.HardStopThresholdHours) {
            this.openSafetyDialog({
                title: this.translateService.instant('FASTING.LIFE_RISK_TITLE'),
                message: this.translateService.instant('FASTING.LIFE_RISK_MESSAGE'),
                cancelLabel: this.translateService.instant('FASTING.CLOSE_ACTION'),
                tone: 'danger',
            });
            return;
        }

        if (targetDuration > FastingPageComponent.WarningThresholdHours) {
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
        if (addedHours <= 0) {
            return `${baseHours} ${hoursLabel}`;
        }

        return `${baseHours} ${hoursLabel} (+${addedHours} ${hoursLabel})`;
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

    public getTranslatedMessage(descriptor: FastingMessage, field: 'titleKey' | 'bodyKey'): string {
        const key = descriptor[field];
        return this.translateService.instant(key, this.resolveMessageParams(descriptor.bodyParams));
    }

    public formatMetric(value: number | null | undefined): string {
        if (value === null || value === undefined || Number.isNaN(value)) {
            return '-';
        }

        return Number.isInteger(value) ? `${value}` : value.toFixed(1);
    }

    public formatDateTime(value: string | null): string | null {
        if (!value) {
            return null;
        }

        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return null;
        }

        return new Intl.DateTimeFormat(this.localizationService.getCurrentLanguage() === 'ru' ? 'ru-RU' : 'en-US', {
            dateStyle: 'medium',
            timeStyle: 'short',
        }).format(date);
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
}
