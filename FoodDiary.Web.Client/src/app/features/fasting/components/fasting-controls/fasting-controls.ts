import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import type { FdUiSegmentedToggleOption } from 'fd-ui-kit';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { EMPTY, type Observable } from 'rxjs';

import { LocalizationService } from '../../../../services/localization.service';
import { parseIntegerInput } from '../../../../shared/lib/number.utils';
import { HOURS_PER_DAY } from '../../../../shared/lib/time.constants';
import {
    EMPTY_FASTING_DURATION_HOURS,
    EXTEND_DAY_AND_HALF_HOURS,
    EXTEND_DAY_HOURS,
    MIN_FASTING_HOURS,
    REDUCE_LONG_HOURS,
    REDUCE_SHORT_HOURS,
} from '../../lib/fasting.constants';
import { FastingFacade } from '../../lib/fasting.facade';
import { FASTING_HARD_STOP_THRESHOLD_HOURS, FASTING_WARNING_THRESHOLD_HOURS } from '../../lib/fasting-page.constants';
import { CYCLIC_PRESETS, FASTING_PROTOCOLS, type FastingMode, type FastingProtocol, type FastingSession } from '../../models/fasting.data';
import {
    FastingEndConfirmDialogComponent,
    type FastingEndConfirmDialogData,
    type FastingEndConfirmDialogResult,
} from '../fasting-end-confirm-dialog/fasting-end-confirm-dialog';
import {
    FastingSafetyDialogComponent,
    type FastingSafetyDialogData,
    type FastingSafetyDialogResult,
} from '../fasting-safety-dialog/fasting-safety-dialog';
import { FastingActiveBasicControlsComponent } from './active-basic/fasting-active-basic-controls';
import { FastingActiveExtendedControlsComponent } from './active-extended/fasting-active-extended-controls';
import { FastingSetupControlsComponent } from './setup/fasting-setup-controls';

const CYCLIC_PRESET_SEPARATOR = ':';
const FASTING_MODES = new Set<FastingMode>(['intermittent', 'extended', 'cyclic']);

@Component({
    selector: 'fd-fasting-controls',
    imports: [FastingSetupControlsComponent, FastingActiveExtendedControlsComponent, FastingActiveBasicControlsComponent],
    templateUrl: './fasting-controls.html',
    styleUrl: './fasting-controls.scss',
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

    protected readonly isActive = this.facade.isActive;
    protected readonly currentSession = this.facade.currentSession;
    protected readonly selectedMode = this.facade.selectedMode;
    protected readonly selectedProtocol = this.facade.selectedProtocol;
    protected readonly customHours = this.facade.customHours;
    protected readonly customIntermittentFastHours = this.facade.customIntermittentFastHours;
    protected readonly cyclicEatDayProtocol = this.facade.cyclicEatDayProtocol;
    protected readonly cyclicFastDays = this.facade.cyclicFastDays;
    protected readonly cyclicEatDays = this.facade.cyclicEatDays;
    protected readonly cyclicUsesCustomPreset = this.facade.cyclicUsesCustomPreset;
    protected readonly cyclicEatDayFastHours = this.facade.cyclicEatDayFastHours;
    protected readonly extendHours = this.facade.extendHours;
    protected readonly reduceHours = this.facade.reduceHours;
    protected readonly isStarting = this.facade.isStarting;
    protected readonly isEnding = this.facade.isEnding;
    protected readonly isExtending = this.facade.isExtending;
    protected readonly isReducing = this.facade.isReducing;
    protected readonly isUpdatingCycle = this.facade.isUpdatingCycle;
    protected readonly canExtendActiveSession = this.facade.canExtendActiveSession;

    protected readonly isCustomExtendExpanded = signal(false);
    protected readonly isCustomReduceExpanded = signal(false);
    protected readonly isExtendPanelExpanded = signal(false);
    protected readonly isReducePanelExpanded = signal(false);
    protected readonly isActiveExtendedSession = computed(() => this.currentSession()?.planType === 'Extended' && this.isActive());
    protected readonly intermittentProtocols = FASTING_PROTOCOLS.filter(protocol => protocol.category === 'intermittent');
    protected readonly cyclicEatDayProtocols = FASTING_PROTOCOLS.filter(protocol => protocol.category === 'intermittent');
    protected readonly extendedProtocols = FASTING_PROTOCOLS.filter(protocol => protocol.category === 'extended');
    protected readonly cyclicPresets = CYCLIC_PRESETS;
    protected readonly customIntermittentEatingWindowHours = computed(() =>
        Math.max(MIN_FASTING_HOURS, HOURS_PER_DAY - this.customIntermittentFastHours()),
    );
    protected readonly cyclicEatDayEatingWindowHours = computed(() =>
        Math.max(MIN_FASTING_HOURS, HOURS_PER_DAY - this.cyclicEatDayFastHours()),
    );
    protected readonly endActionLabelKey = computed(() => {
        if (this.isCurrentSessionCyclic()) {
            return 'FASTING.STOP_CYCLE';
        }

        return this.isCurrentSessionIntermittent() ? 'FASTING.END_FAST' : 'FASTING.INTERRUPT_FAST';
    });
    protected readonly canManageCurrentCyclicDay = computed(() => {
        const session = this.currentSession();
        return (
            session !== null &&
            session.endedAtUtc === null &&
            session.planType === 'Cyclic' &&
            (session.occurrenceKind === 'FastDay' || session.occurrenceKind === 'EatDay')
        );
    });
    protected readonly skipCycleActionLabelKey = computed(() =>
        this.getCurrentCyclicOccurrenceKind() === 'EatDay' ? 'FASTING.START_FAST_NOW' : 'FASTING.SKIP_FASTING_PERIOD',
    );
    protected readonly postponeCycleActionLabelKey = computed(() => 'FASTING.SKIP_DAY');
    protected readonly isCustomCyclicPresetSelected = computed(() => this.cyclicUsesCustomPreset());
    protected readonly modeOptions = computed(() =>
        this.buildSegmentedToggleOptions([
            { labelKey: 'FASTING.MODE_INTERMITTENT', value: 'intermittent' },
            { labelKey: 'FASTING.MODE_EXTENDED', value: 'extended' },
            { labelKey: 'FASTING.MODE_CYCLIC', value: 'cyclic' },
        ]),
    );
    protected readonly intermittentProtocolOptions = computed(() => this.buildProtocolOptions(this.intermittentProtocols));
    protected readonly extendedProtocolOptions = computed(() => this.buildProtocolOptions(this.extendedProtocols));
    protected readonly cyclicEatDayProtocolOptions = computed(() => this.buildProtocolOptions(this.cyclicEatDayProtocols));
    protected readonly cyclicPresetOptions = computed(() => {
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
    protected readonly selectedCyclicPresetValue = computed(() => {
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

    protected onModeChange(mode: string): void {
        if (this.isFastingMode(mode)) {
            this.facade.selectMode(mode);
        }
    }

    protected onProtocolChange(protocol: string): void {
        if (this.isFastingProtocol(protocol)) {
            this.facade.selectProtocol(protocol);
        }
    }

    protected onCustomHoursChange(value: string | number): void {
        const hours = parseIntegerInput(value);
        if (hours !== null) {
            this.facade.setCustomHours(hours);
        }
    }

    protected onCustomIntermittentFastHoursChange(value: string | number): void {
        const hours = parseIntegerInput(value);
        if (hours !== null) {
            this.facade.setCustomIntermittentFastHours(hours);
        }
    }

    protected onCyclicPresetChange(value: string): void {
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

    protected onCyclicFastDaysChange(value: string | number): void {
        const days = parseIntegerInput(value);
        if (days !== null) {
            this.facade.setCyclicFastDays(days);
        }
    }

    protected onCyclicEatDaysChange(value: string | number): void {
        const days = parseIntegerInput(value);
        if (days !== null) {
            this.facade.setCyclicEatDays(days);
        }
    }

    protected onCyclicEatDayProtocolChange(protocol: string): void {
        if (this.isFastingProtocol(protocol)) {
            this.facade.selectCyclicEatDayProtocol(protocol);
        }
    }

    protected onCyclicEatDayFastHoursChange(value: string | number): void {
        const hours = parseIntegerInput(value);
        if (hours !== null) {
            this.facade.setCyclicEatDayFastHours(hours);
        }
    }

    protected startFasting(): void {
        this.facade.startFasting();
    }

    protected endFasting(): void {
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

    protected onExtendHoursChange(value: string | number): void {
        const hours = parseIntegerInput(value);
        if (hours !== null) {
            this.facade.setExtendHours(hours);
        }
    }

    protected onReduceHoursChange(value: string | number): void {
        const hours = parseIntegerInput(value);
        if (hours !== null) {
            this.facade.setReduceHours(hours);
        }
    }

    protected extendByDay(): void {
        this.isCustomExtendExpanded.set(false);
        this.requestExtendByHours(EXTEND_DAY_HOURS);
    }

    protected extendBy36Hours(): void {
        this.isCustomExtendExpanded.set(false);
        this.requestExtendByHours(EXTEND_DAY_AND_HALF_HOURS);
    }

    protected extendByCustom(): void {
        this.requestExtendByHours(this.extendHours());
    }

    protected showCustomExtend(): void {
        this.isExtendPanelExpanded.set(true);
        this.isCustomExtendExpanded.set(true);
    }

    protected toggleExtendPanel(): void {
        this.isExtendPanelExpanded.update(isExpanded => !isExpanded);
    }

    protected reduceBy4Hours(): void {
        this.isCustomReduceExpanded.set(false);
        this.requestReduceByHours(REDUCE_SHORT_HOURS);
    }

    protected reduceBy8Hours(): void {
        this.isCustomReduceExpanded.set(false);
        this.requestReduceByHours(REDUCE_LONG_HOURS);
    }

    protected reduceByCustom(): void {
        this.requestReduceByHours(this.reduceHours());
    }

    protected showCustomReduce(): void {
        this.isReducePanelExpanded.set(true);
        this.isCustomReduceExpanded.set(true);
    }

    protected toggleReducePanel(): void {
        this.isReducePanelExpanded.update(isExpanded => !isExpanded);
    }

    protected skipCyclicDay(): void {
        this.openCycleActionDialog(
            this.getSkipCycleConfirmTitleKey(),
            this.getSkipCycleConfirmMessageKey(),
            this.skipCycleActionLabelKey(),
            () => {
                this.facade.skipCyclicDay();
            },
        );
    }

    protected postponeCyclicDay(): void {
        this.openCycleActionDialog(
            this.getPostponeCycleConfirmTitleKey(),
            this.getPostponeCycleConfirmMessageKey(),
            this.postponeCycleActionLabelKey(),
            () => {
                this.facade.postponeCyclicDay();
            },
        );
    }

    private buildProtocolOptions(protocols: ReadonlyArray<{ labelKey: string; value: FastingProtocol }>): FdUiSegmentedToggleOption[] {
        this.currentLanguage();

        return protocols.map(protocol => ({
            label: this.translateService.instant(protocol.labelKey),
            value: protocol.value,
        }));
    }

    private buildSegmentedToggleOptions(items: ReadonlyArray<{ labelKey: string; value: string }>): FdUiSegmentedToggleOption[] {
        this.currentLanguage();

        return items.map(item => ({
            label: this.translateService.instant(item.labelKey),
            value: item.value,
        }));
    }

    private isFastingMode(value: string): value is FastingMode {
        return (FASTING_MODES as ReadonlySet<string>).has(value);
    }

    private isFastingProtocol(value: string): value is FastingProtocol {
        return FASTING_PROTOCOLS.some(protocol => protocol.value === value);
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
        const currentDuration = currentSession?.plannedDurationHours ?? EMPTY_FASTING_DURATION_HOURS;
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

        const maxReducibleHours = Math.max(EMPTY_FASTING_DURATION_HOURS, session.plannedDurationHours - MIN_FASTING_HOURS);
        const normalizedHours = Math.max(MIN_FASTING_HOURS, Math.min(maxReducibleHours, reducedHours));
        if (normalizedHours <= EMPTY_FASTING_DURATION_HOURS) {
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
