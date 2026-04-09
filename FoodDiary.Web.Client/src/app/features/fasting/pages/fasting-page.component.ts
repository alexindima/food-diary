import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject } from '@angular/core';
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
import {
    CYCLIC_PRESETS,
    FASTING_PROTOCOLS,
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

    public readonly isLoading = this.facade.isLoading;
    public readonly isStarting = this.facade.isStarting;
    public readonly isEnding = this.facade.isEnding;
    public readonly isExtending = this.facade.isExtending;
    public readonly isUpdatingCycle = this.facade.isUpdatingCycle;
    public readonly isActive = this.facade.isActive;
    public readonly currentSession = this.facade.currentSession;
    public readonly stats = this.facade.stats;
    public readonly history = this.facade.history;
    public readonly selectedMode = this.facade.selectedMode;
    public readonly selectedProtocol = this.facade.selectedProtocol;
    public readonly customHours = this.facade.customHours;
    public readonly customIntermittentFastHours = this.facade.customIntermittentFastHours;
    public readonly cyclicFastDays = this.facade.cyclicFastDays;
    public readonly cyclicEatDays = this.facade.cyclicEatDays;
    public readonly cyclicEatDayFastHours = this.facade.cyclicEatDayFastHours;
    public readonly extendHours = this.facade.extendHours;
    public readonly progressPercent = this.facade.progressPercent;
    public readonly elapsedFormatted = this.facade.elapsedFormatted;
    public readonly remainingFormatted = this.facade.remainingFormatted;
    public readonly isOvertime = this.facade.isOvertime;
    public readonly canExtendActiveSession = this.facade.canExtendActiveSession;
    public readonly intermittentProtocols = FASTING_PROTOCOLS.filter(protocol => protocol.category === 'intermittent');
    public readonly extendedProtocols = FASTING_PROTOCOLS.filter(protocol => protocol.category === 'extended');
    public readonly cyclicPresets = CYCLIC_PRESETS;
    public ngOnInit(): void {
        this.facade.initialize();
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

    public extendByDay(): void {
        this.requestExtendByHours(24);
    }

    public extendByCustom(): void {
        this.requestExtendByHours(this.extendHours());
    }

    public skipCyclicFastDay(): void {
        this.openCycleActionDialog('FASTING.SKIP_DAY_CONFIRM_TITLE', 'FASTING.SKIP_DAY_CONFIRM_MESSAGE', 'FASTING.SKIP_DAY', () =>
            this.facade.skipCyclicFastDay(),
        );
    }

    public postponeCyclicFastDay(): void {
        this.openCycleActionDialog(
            'FASTING.POSTPONE_DAY_CONFIRM_TITLE',
            'FASTING.POSTPONE_DAY_CONFIRM_MESSAGE',
            'FASTING.POSTPONE_DAY',
            () => this.facade.postponeCyclicFastDay(),
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
        return this.isCurrentSessionIntermittent() ? 'FASTING.END_FAST' : 'FASTING.INTERRUPT_FAST';
    }

    public canManageCurrentCyclicDay(): boolean {
        const session = this.currentSession();
        return !!session && !session.endedAtUtc && session.planType === 'Cyclic' && session.occurrenceKind === 'FastDay';
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

    private getEndConfirmDialogData(): FastingEndConfirmDialogData {
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
}
