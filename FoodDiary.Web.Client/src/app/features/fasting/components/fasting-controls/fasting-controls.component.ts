import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { EMPTY, type Observable } from 'rxjs';

import { LocalizationService } from '../../../../services/localization.service';
import { parseIntegerInput } from '../../../../shared/lib/number.utils';
import { FastingFacade } from '../../lib/fasting.facade';
import { FASTING_HARD_STOP_THRESHOLD_HOURS, FASTING_WARNING_THRESHOLD_HOURS } from '../../lib/fasting-page.constants';
import { CYCLIC_PRESETS, FASTING_PROTOCOLS, type FastingMode, type FastingProtocol, type FastingSession } from '../../models/fasting.data';
import {
    FastingEndConfirmDialogComponent,
    type FastingEndConfirmDialogData,
    type FastingEndConfirmDialogResult,
} from '../fasting-end-confirm-dialog/fasting-end-confirm-dialog.component';
import {
    FastingSafetyDialogComponent,
    type FastingSafetyDialogData,
    type FastingSafetyDialogResult,
} from '../fasting-safety-dialog/fasting-safety-dialog.component';

const HOURS_PER_DAY = 24;
const EXTEND_DAY_HOURS = 24;
const EXTEND_DAY_AND_HALF_HOURS = 36;
const REDUCE_SHORT_HOURS = 4;
const REDUCE_LONG_HOURS = 8;
const MIN_FASTING_HOURS = 1;
const EMPTY_DURATION_HOURS = 0;
const CYCLIC_PRESET_SEPARATOR = ':';

@Component({
    selector: 'fd-fasting-controls',
    imports: [FormsModule, TranslatePipe, FdUiSegmentedToggleComponent, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './fasting-controls.component.html',
    styleUrl: './fasting-controls.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.fasting-controls-host--setup]': '!isActive()',
        '[class.fasting-controls-host--page-summary]': 'isActive()',
    },
})
export class FastingControlsComponent {
    private readonly facade = inject(FastingFacade);
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly localizationService = inject(LocalizationService);
    private readonly currentLanguage = signal(this.localizationService.getCurrentLanguage());

    public readonly isActive = this.facade.isActive;
    public readonly currentSession = this.facade.currentSession;
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
    public readonly isStarting = this.facade.isStarting;
    public readonly isEnding = this.facade.isEnding;
    public readonly isExtending = this.facade.isExtending;
    public readonly isReducing = this.facade.isReducing;
    public readonly isUpdatingCycle = this.facade.isUpdatingCycle;
    public readonly canExtendActiveSession = this.facade.canExtendActiveSession;

    public readonly isCustomExtendExpanded = signal(false);
    public readonly isCustomReduceExpanded = signal(false);
    public readonly isExtendPanelExpanded = signal(false);
    public readonly isReducePanelExpanded = signal(false);
    public readonly extendPanelToggleLabel = computed(() => (this.isExtendPanelExpanded() ? '-' : '+'));
    public readonly reducePanelToggleLabel = computed(() => (this.isReducePanelExpanded() ? '-' : '+'));
    public readonly customExtendActionState = computed<FastingCustomActionState>(() =>
        this.isCustomExtendExpanded()
            ? {
                  variant: 'primary',
                  fill: 'solid',
              }
            : {
                  variant: 'secondary',
                  fill: 'outline',
              },
    );
    public readonly customReduceActionState = computed<FastingCustomActionState>(() =>
        this.isCustomReduceExpanded()
            ? {
                  variant: 'danger',
                  fill: 'solid',
              }
            : {
                  variant: 'secondary',
                  fill: 'outline',
              },
    );
    public readonly isActiveExtendedSession = computed(() => this.currentSession()?.planType === 'Extended' && this.isActive());
    public readonly intermittentProtocols = FASTING_PROTOCOLS.filter(protocol => protocol.category === 'intermittent');
    public readonly cyclicEatDayProtocols = FASTING_PROTOCOLS.filter(protocol => protocol.category === 'intermittent');
    public readonly extendedProtocols = FASTING_PROTOCOLS.filter(protocol => protocol.category === 'extended');
    public readonly cyclicPresets = CYCLIC_PRESETS;
    public readonly customIntermittentEatingWindowHours = computed(() =>
        Math.max(MIN_FASTING_HOURS, HOURS_PER_DAY - this.customIntermittentFastHours()),
    );
    public readonly cyclicEatDayEatingWindowHours = computed(() =>
        Math.max(MIN_FASTING_HOURS, HOURS_PER_DAY - this.cyclicEatDayFastHours()),
    );
    public readonly endActionLabelKey = computed(() => {
        if (this.isCurrentSessionCyclic()) {
            return 'FASTING.STOP_CYCLE';
        }

        return this.isCurrentSessionIntermittent() ? 'FASTING.END_FAST' : 'FASTING.INTERRUPT_FAST';
    });
    public readonly canManageCurrentCyclicDay = computed(() => {
        const session = this.currentSession();
        return (
            session !== null &&
            session.endedAtUtc === null &&
            session.planType === 'Cyclic' &&
            (session.occurrenceKind === 'FastDay' || session.occurrenceKind === 'EatDay')
        );
    });
    public readonly skipCycleActionLabelKey = computed(() =>
        this.getCurrentCyclicOccurrenceKind() === 'EatDay' ? 'FASTING.START_FAST_NOW' : 'FASTING.SKIP_FASTING_PERIOD',
    );
    public readonly postponeCycleActionLabelKey = computed(() => 'FASTING.SKIP_DAY');
    public readonly isCustomCyclicPresetSelected = computed(() => this.cyclicUsesCustomPreset());
    public readonly modeOptions = computed(() =>
        this.buildSegmentedToggleOptions([
            { labelKey: 'FASTING.MODE_INTERMITTENT', value: 'intermittent' },
            { labelKey: 'FASTING.MODE_EXTENDED', value: 'extended' },
            { labelKey: 'FASTING.MODE_CYCLIC', value: 'cyclic' },
        ]),
    );
    public readonly intermittentProtocolOptions = computed(() => this.buildProtocolOptions(this.intermittentProtocols));
    public readonly extendedProtocolOptions = computed(() => this.buildProtocolOptions(this.extendedProtocols));
    public readonly cyclicEatDayProtocolOptions = computed(() => this.buildProtocolOptions(this.cyclicEatDayProtocols));
    public readonly cyclicPresetOptions = computed(() => {
        this.currentLanguage();

        return [
            ...this.cyclicPresets.map<FdUiSegmentedToggleOption>(preset => ({
                label: preset.label,
                value: this.getCyclicPresetSelectionValue(preset.fastDays, preset.eatDays),
            })),
            {
                label: this.translateService.instant('FASTING.CUSTOM_CYCLE'),
                value: 'custom',
            },
        ];
    });
    public readonly selectedCyclicPresetValue = computed(() => {
        if (this.isCustomCyclicPresetSelected()) {
            return 'custom';
        }

        return this.getCyclicPresetSelectionValue(this.cyclicFastDays(), this.cyclicEatDays());
    });

    public constructor() {
        ((this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.currentLanguage.set(this.localizationService.getCurrentLanguage());
            });
    }

    public onModeChange(mode: string): void {
        this.facade.selectMode(mode as FastingMode);
    }

    public onProtocolChange(protocol: string): void {
        this.facade.selectProtocol(protocol as FastingProtocol);
    }

    public onCustomHoursChange(value: string | number): void {
        const hours = parseIntegerInput(value);
        if (hours !== null) {
            this.facade.setCustomHours(hours);
        }
    }

    public onCustomIntermittentFastHoursChange(value: string | number): void {
        const hours = parseIntegerInput(value);
        if (hours !== null) {
            this.facade.setCustomIntermittentFastHours(hours);
        }
    }

    public onCyclicPresetChange(value: string): void {
        if (value === 'custom') {
            this.facade.selectCustomCyclicPreset();
            return;
        }

        const [fastDaysRaw, eatDaysRaw] = value.split(CYCLIC_PRESET_SEPARATOR);
        const fastDays = Number.parseInt(fastDaysRaw, 10);
        const eatDays = Number.parseInt(eatDaysRaw, 10);

        if (Number.isNaN(fastDays) || Number.isNaN(eatDays)) {
            return;
        }

        this.facade.setCyclicPreset(fastDays, eatDays);
    }

    public onCyclicFastDaysChange(value: string | number): void {
        const days = parseIntegerInput(value);
        if (days !== null) {
            this.facade.setCyclicFastDays(days);
        }
    }

    public onCyclicEatDaysChange(value: string | number): void {
        const days = parseIntegerInput(value);
        if (days !== null) {
            this.facade.setCyclicEatDays(days);
        }
    }

    public onCyclicEatDayProtocolChange(protocol: string): void {
        this.facade.selectCyclicEatDayProtocol(protocol as FastingProtocol);
    }

    public onCyclicEatDayFastHoursChange(value: string | number): void {
        const hours = parseIntegerInput(value);
        if (hours !== null) {
            this.facade.setCyclicEatDayFastHours(hours);
        }
    }

    public startFasting(): void {
        this.facade.startFasting();
    }

    public endFasting(): void {
        this.dialogService
            .open<FastingEndConfirmDialogComponent, FastingEndConfirmDialogData, FastingEndConfirmDialogResult>(
                FastingEndConfirmDialogComponent,
                {
                    preset: 'confirm',
                    data: this.getEndConfirmDialogData(),
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
        const hours = parseIntegerInput(value);
        if (hours !== null) {
            this.facade.setExtendHours(hours);
        }
    }

    public onReduceHoursChange(value: string | number): void {
        const hours = parseIntegerInput(value);
        if (hours !== null) {
            this.facade.setReduceHours(hours);
        }
    }

    public extendByDay(): void {
        this.isCustomExtendExpanded.set(false);
        this.requestExtendByHours(EXTEND_DAY_HOURS);
    }

    public extendBy36Hours(): void {
        this.isCustomExtendExpanded.set(false);
        this.requestExtendByHours(EXTEND_DAY_AND_HALF_HOURS);
    }

    public extendByCustom(): void {
        this.requestExtendByHours(this.extendHours());
    }

    public showCustomExtend(): void {
        this.isExtendPanelExpanded.set(true);
        this.isCustomExtendExpanded.set(true);
    }

    public toggleExtendPanel(): void {
        this.isExtendPanelExpanded.update(isExpanded => !isExpanded);
    }

    public reduceBy4Hours(): void {
        this.isCustomReduceExpanded.set(false);
        this.requestReduceByHours(REDUCE_SHORT_HOURS);
    }

    public reduceBy8Hours(): void {
        this.isCustomReduceExpanded.set(false);
        this.requestReduceByHours(REDUCE_LONG_HOURS);
    }

    public reduceByCustom(): void {
        this.requestReduceByHours(this.reduceHours());
    }

    public showCustomReduce(): void {
        this.isReducePanelExpanded.set(true);
        this.isCustomReduceExpanded.set(true);
    }

    public toggleReducePanel(): void {
        this.isReducePanelExpanded.update(isExpanded => !isExpanded);
    }

    public skipCyclicDay(): void {
        this.openCycleActionDialog(
            this.getSkipCycleConfirmTitleKey(),
            this.getSkipCycleConfirmMessageKey(),
            this.skipCycleActionLabelKey(),
            () => {
                this.facade.skipCyclicDay();
            },
        );
    }

    public postponeCyclicDay(): void {
        this.openCycleActionDialog(
            this.getPostponeCycleConfirmTitleKey(),
            this.getPostponeCycleConfirmMessageKey(),
            this.postponeCycleActionLabelKey(),
            () => {
                this.facade.postponeCyclicDay();
            },
        );
    }

    private buildProtocolOptions(protocols: readonly { labelKey: string; value: FastingProtocol }[]): FdUiSegmentedToggleOption[] {
        this.currentLanguage();

        return protocols.map(protocol => ({
            label: this.translateService.instant(protocol.labelKey),
            value: protocol.value,
        }));
    }

    private buildSegmentedToggleOptions(items: readonly { labelKey: string; value: string }[]): FdUiSegmentedToggleOption[] {
        this.currentLanguage();

        return items.map(item => ({
            label: this.translateService.instant(item.labelKey),
            value: item.value,
        }));
    }

    private getCyclicPresetSelectionValue(fastDays: number, eatDays: number): string {
        return `${fastDays}${CYCLIC_PRESET_SEPARATOR}${eatDays}`;
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
        if (session === null) {
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
        if (session?.planType !== 'Cyclic' || session.endedAtUtc !== null) {
            return null;
        }

        return session.occurrenceKind;
    }

    private requestExtendByHours(additionalHours: number): void {
        const normalizedHours = Math.max(MIN_FASTING_HOURS, Math.min(FASTING_HARD_STOP_THRESHOLD_HOURS, additionalHours));
        const currentSession = this.currentSession();
        const currentDuration = currentSession?.plannedDurationHours ?? EMPTY_DURATION_HOURS;
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
        if (session === null) {
            return;
        }

        const maxReducibleHours = Math.max(EMPTY_DURATION_HOURS, session.plannedDurationHours - MIN_FASTING_HOURS);
        const normalizedHours = Math.max(MIN_FASTING_HOURS, Math.min(maxReducibleHours, reducedHours));
        if (normalizedHours <= EMPTY_DURATION_HOURS) {
            return;
        }

        this.facade.reduceTargetByHours(normalizedHours);
    }

    private openCycleActionDialog(titleKey: string, messageKey: string, confirmLabelKey: string, action: () => void): void {
        this.dialogService
            .open<FastingEndConfirmDialogComponent, FastingEndConfirmDialogData, FastingEndConfirmDialogResult>(
                FastingEndConfirmDialogComponent,
                {
                    preset: 'confirm',
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
                preset: 'confirm',
                data,
            })
            .afterClosed();
    }
}

interface FastingCustomActionState {
    variant: 'primary' | 'secondary' | 'danger';
    fill: 'solid' | 'outline';
}
