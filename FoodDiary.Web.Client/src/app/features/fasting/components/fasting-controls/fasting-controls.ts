import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import type { FdUiSegmentedToggleOption } from 'fd-ui-kit';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import type { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import type { Observable } from 'rxjs';

import { LocalizationService } from '../../../../shared/i18n/localization.service';
import {
    EXTEND_DAY_AND_HALF_HOURS,
    EXTEND_DAY_HOURS,
    MIN_FASTING_HOURS,
    REDUCE_LONG_HOURS,
    REDUCE_SHORT_HOURS,
} from '../../../../shared/lib/fasting.constants';
import { parseIntegerInput } from '../../../../shared/lib/number.utils';
import { HOURS_PER_DAY } from '../../../../shared/lib/time.constants';
import {
    CYCLIC_PRESETS,
    FASTING_PROTOCOL_CATALOG,
    FASTING_PROTOCOLS,
    type FastingMode,
    type FastingProtocol,
    type FastingSession,
} from '../../../../shared/models/fasting.data';
import { FastingFacade } from '../../lib/fasting.facade';
import { FASTING_HARD_STOP_THRESHOLD_HOURS, FASTING_WARNING_THRESHOLD_HOURS } from '../../lib/fasting-page.constants';
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

@Component({
    selector: 'fd-fasting-controls',
    imports: [FastingSetupControlsComponent, FastingActiveExtendedControlsComponent, FastingActiveBasicControlsComponent],
    templateUrl: './fasting-controls.html',
    styleUrl: './fasting-controls.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.fasting-controls-host--setup]': '!isActive()',
        '[class.fasting-controls-host--page-summary]': 'isActive()',
        '[class.fasting-controls-host--dialog]': "presentation() === 'dialog'",
    },
})
export class FastingControlsComponent {
    private readonly facade = inject(FastingFacade);
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly localizationService = inject(LocalizationService);
    private readonly currentLanguage = signal(this.localizationService.getCurrentLanguage());

    public readonly presentation = input<'default' | 'dialog'>('default');
    public readonly showSetupStartAction = input(true);

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
    protected readonly maxReducibleHours = this.facade.maxReducibleHours;
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
    protected readonly activeExtendedSession = computed(() => {
        const session = this.currentSession();
        return session?.planType === 'Extended' && this.isActive() ? session : null;
    });
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
    protected readonly endConfirmationKeys = {
        Intermittent: {
            titleKey: 'FASTING.END_CONFIRM_TITLE',
            messageKey: 'FASTING.END_CONFIRM_MESSAGE',
            actionKey: 'FASTING.END_FAST',
        },
        Extended: {
            titleKey: 'FASTING.INTERRUPT_CONFIRM_TITLE',
            messageKey: 'FASTING.INTERRUPT_CONFIRM_MESSAGE',
            actionKey: 'FASTING.INTERRUPT_FAST',
        },
        Cyclic: {
            titleKey: 'FASTING.STOP_CYCLE_CONFIRM_TITLE',
            messageKey: 'FASTING.STOP_CYCLE_CONFIRM_MESSAGE',
            actionKey: 'FASTING.STOP_CYCLE',
        },
    } satisfies Record<FastingSession['planType'], { titleKey: string; messageKey: string; actionKey: string }>;
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
        this.buildSegmentedToggleOptions<FastingMode>([
            { labelKey: 'FASTING.MODE_INTERMITTENT', value: 'intermittent' },
            { labelKey: 'FASTING.MODE_EXTENDED', value: 'extended' },
            { labelKey: 'FASTING.MODE_CYCLIC', value: 'cyclic' },
        ]),
    );
    protected readonly intermittentProtocolOptions = computed(() => this.buildSegmentedToggleOptions(this.intermittentProtocols));
    protected readonly extendedProtocolOptions = computed(() => this.buildSegmentedToggleOptions(this.extendedProtocols));
    protected readonly cyclicEatDayProtocolOptions = computed(() => this.buildSegmentedToggleOptions(this.cyclicEatDayProtocols));
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
    protected readonly setupDescriptionKey = computed(() => `FASTING.REDESIGN.MODE_DESCRIPTION_${this.selectedMode().toUpperCase()}`);
    protected readonly setupSummary = computed(() => {
        this.currentLanguage();
        const mode = this.selectedMode();
        if (mode === 'cyclic') {
            return this.translateService.instant('FASTING.REDESIGN.CYCLIC_SELECTION_SUMMARY', {
                fastDays: this.cyclicFastDays(),
                eatDays: this.cyclicEatDays(),
                fastHours: this.cyclicEatDayFastHours(),
                eatHours: this.cyclicEatDayEatingWindowHours(),
            });
        }

        const protocol = this.selectedProtocol();
        const hours =
            mode === 'intermittent' && protocol === 'CustomIntermittent'
                ? this.customIntermittentFastHours()
                : mode === 'extended' && protocol === 'Custom'
                  ? this.customHours()
                  : FASTING_PROTOCOL_CATALOG[protocol].hours;
        const key = mode === 'extended' ? 'FASTING.REDESIGN.EXTENDED_SELECTION_SUMMARY' : 'FASTING.REDESIGN.INTERMITTENT_SELECTION_SUMMARY';
        return this.translateService.instant(key, { hours, eatHours: Math.max(0, HOURS_PER_DAY - hours) });
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.currentLanguage.set(this.localizationService.getCurrentLanguage());
        });
    }

    protected onModeChange(mode: FastingMode): void {
        this.facade.selectMode(mode);
    }

    protected onProtocolChange(protocol: FastingProtocol): void {
        this.facade.selectProtocol(protocol);
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

    protected onCyclicEatDayProtocolChange(protocol: FastingProtocol): void {
        this.facade.selectCyclicEatDayProtocol(protocol);
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

    protected endFasting(sessionId: FastingSession['id']): void {
        const session = this.currentSession();
        if (session?.id !== sessionId || session.endedAtUtc !== null) {
            return;
        }

        const dialog = this.dialogService.open<
            FastingEndConfirmDialogComponent,
            FastingEndConfirmDialogData,
            FastingEndConfirmDialogResult
        >(FastingEndConfirmDialogComponent, {
            preset: 'confirm',
            data: this.getEndConfirmDialogData(session.planType),
        });
        this.observeOwnedDialog(dialog)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                if (result === 'confirm' && this.matchesActiveSession(sessionId)) {
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

    protected extendByDay(sessionId: FastingSession['id']): void {
        this.isCustomExtendExpanded.set(false);
        this.requestExtendByHours(sessionId, EXTEND_DAY_HOURS);
    }

    protected extendBy36Hours(sessionId: FastingSession['id']): void {
        this.isCustomExtendExpanded.set(false);
        this.requestExtendByHours(sessionId, EXTEND_DAY_AND_HALF_HOURS);
    }

    protected extendByCustom(sessionId: FastingSession['id']): void {
        this.requestExtendByHours(sessionId, this.extendHours());
    }

    protected showCustomExtend(): void {
        this.isExtendPanelExpanded.set(true);
        this.isCustomExtendExpanded.set(true);
    }

    protected toggleExtendPanel(): void {
        this.isExtendPanelExpanded.update(isExpanded => !isExpanded);
    }

    protected reduceBy4Hours(sessionId: FastingSession['id']): void {
        this.isCustomReduceExpanded.set(false);
        this.requestReduceByHours(sessionId, REDUCE_SHORT_HOURS);
    }

    protected reduceBy8Hours(sessionId: FastingSession['id']): void {
        this.isCustomReduceExpanded.set(false);
        this.requestReduceByHours(sessionId, REDUCE_LONG_HOURS);
    }

    protected reduceByCustom(sessionId: FastingSession['id']): void {
        this.requestReduceByHours(sessionId, this.reduceHours());
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

    private buildSegmentedToggleOptions<Value extends string>(
        items: ReadonlyArray<{ labelKey: string; value: Value }>,
    ): Array<FdUiSegmentedToggleOption<Value>> {
        this.currentLanguage();

        return items.map(item => ({
            label: this.translateService.instant(item.labelKey),
            value: item.value,
        }));
    }

    private getCyclicPresetSelectionValue(fastDays: number, eatDays: number): string {
        return `${fastDays}${CYCLIC_PRESET_SEPARATOR}${eatDays}`;
    }

    private getEndConfirmDialogData(planType: FastingSession['planType']): FastingEndConfirmDialogData {
        const { titleKey, messageKey, actionKey } = this.endConfirmationKeys[planType];
        return {
            title: this.translateService.instant(titleKey),
            message: this.translateService.instant(messageKey),
            confirmLabel: this.translateService.instant(actionKey),
            cancelLabel: this.translateService.instant('FASTING.CANCEL_ACTION'),
        };
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

    private requestExtendByHours(sessionId: FastingSession['id'], additionalHours: number): void {
        const session = this.activeExtendedSession();
        if (session?.id !== sessionId) {
            return;
        }

        const normalizedHours = Math.max(MIN_FASTING_HOURS, Math.min(FASTING_HARD_STOP_THRESHOLD_HOURS, additionalHours));
        const currentDuration = session.plannedDurationHours;
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
                    if (result === 'confirm' && this.matchesActiveSession(sessionId, currentDuration)) {
                        this.facade.extendByHours(normalizedHours);
                    }
                });
            return;
        }

        this.facade.extendByHours(normalizedHours);
    }

    private requestReduceByHours(sessionId: FastingSession['id'], reducedHours: number): void {
        if (this.activeExtendedSession()?.id !== sessionId) {
            return;
        }

        if (reducedHours < MIN_FASTING_HOURS || reducedHours > this.maxReducibleHours()) {
            return;
        }

        this.facade.reduceTargetByHours(reducedHours);
    }

    private openCycleActionDialog(titleKey: string, messageKey: string, confirmLabelKey: string, action: () => void): void {
        const sessionId = this.currentSession()?.id;
        const dialog = this.dialogService.open<
            FastingEndConfirmDialogComponent,
            FastingEndConfirmDialogData,
            FastingEndConfirmDialogResult
        >(FastingEndConfirmDialogComponent, {
            preset: 'confirm',
            data: {
                title: this.translateService.instant(titleKey),
                message: this.translateService.instant(messageKey),
                confirmLabel: this.translateService.instant(confirmLabelKey),
                cancelLabel: this.translateService.instant('FASTING.CANCEL_ACTION'),
            },
        });
        this.observeOwnedDialog(dialog)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                if (result === 'confirm' && this.matchesActiveSession(sessionId)) {
                    action();
                }
            });
    }

    private matchesActiveSession(sessionId: FastingSession['id'] | undefined, plannedDurationHours?: number): boolean {
        const session = this.currentSession();
        return (
            session !== null &&
            session.id === sessionId &&
            session.endedAtUtc === null &&
            (plannedDurationHours === undefined || session.plannedDurationHours === plannedDurationHours)
        );
    }

    private openSafetyDialog(data: FastingSafetyDialogData): Observable<FastingSafetyDialogResult | undefined> {
        const dialog = this.dialogService.open<FastingSafetyDialogComponent, FastingSafetyDialogData, FastingSafetyDialogResult>(
            FastingSafetyDialogComponent,
            { preset: 'confirm', data },
        );
        return this.observeOwnedDialog(dialog);
    }

    private observeOwnedDialog<T, R>(dialog: FdUiDialogRef<T, R>): Observable<R | undefined> {
        const unregisterCleanup = this.destroyRef.onDestroy(() => {
            dialog.close();
        });
        const closed = dialog.afterClosed();
        closed.subscribe({ complete: unregisterCleanup });
        return closed;
    }
}
