import { ChangeDetectionStrategy, Component, computed, input, linkedSignal, model, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiInputComponent, type FdUiInputValue } from 'fd-ui-kit/input/fd-ui-input';

import { normalizeFastingHours } from '../../../lib/fasting-input-normalization';
import type { FastingCustomActionState } from '../fasting-controls.types';

@Component({
    selector: 'fd-fasting-active-extended-controls',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './fasting-active-extended-controls.html',
    styleUrl: '../fasting-controls.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class FastingActiveExtendedControlsComponent {
    protected readonly normalizeCustomHours = (value: number | null): number | null =>
        value === null ? null : normalizeFastingHours(value);

    public readonly canExtendActiveSession = input.required<boolean>();
    public readonly isExtendPanelExpanded = input.required<boolean>();
    public readonly isReducePanelExpanded = input.required<boolean>();
    public readonly isCustomExtendExpanded = input.required<boolean>();
    public readonly isCustomReduceExpanded = input.required<boolean>();
    public readonly extendHours = model.required<number>();
    public readonly reduceHours = model.required<number>();
    public readonly maxReducibleHours = input.required<number>();
    public readonly isExtending = input.required<boolean>();
    public readonly isReducing = input.required<boolean>();
    public readonly isEnding = input.required<boolean>();
    public readonly isUpdatingCycle = input.required<boolean>();
    public readonly endActionLabelKey = input.required<string>();

    public readonly extendPanelToggle = output();
    public readonly reducePanelToggle = output();
    public readonly extendDay = output();
    public readonly extend36Hours = output();
    public readonly customExtendShow = output();
    public readonly extendCustom = output();
    public readonly reduce4Hours = output();
    public readonly reduce8Hours = output();
    public readonly customReduceShow = output();
    public readonly reduceCustom = output();
    public readonly end = output();

    protected readonly extendHoursDraft = linkedSignal<number | null>(() => this.extendHours());
    protected readonly reduceHoursDraft = linkedSignal<number | null>(() => this.reduceHours());
    protected readonly extendPanelToggleLabel = computed(() => (this.isExtendPanelExpanded() ? '-' : '+'));
    protected readonly reducePanelToggleLabel = computed(() => (this.isReducePanelExpanded() ? '-' : '+'));
    protected readonly customExtendActionState = computed<FastingCustomActionState>(() =>
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
    protected readonly customReduceActionState = computed<FastingCustomActionState>(() =>
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
    protected readonly extendActionsDisabled = (): boolean => this.isExtending() || this.isReducing() || this.isEnding();
    protected readonly reduceActionsDisabled = (): boolean => this.isReducing() || this.isExtending() || this.isEnding();
    protected readonly endDisabled = (): boolean => this.isEnding() || this.isReducing() || this.isExtending() || this.isUpdatingCycle();

    protected isReduceActionDisabled(hours: number | null): boolean {
        return this.getReduceDisabledReason(hours) !== null;
    }

    protected getExtendCustomDisabledReason(): string | null {
        if (this.extendActionsDisabled()) {
            return 'DISABLED_HINTS.FASTING_BUSY';
        }
        return this.extendHoursDraft() === null ? 'FORM_ERRORS.REQUIRED' : null;
    }

    protected getReduceDisabledReason(hours: number | null): string | null {
        if (this.reduceActionsDisabled()) {
            return 'DISABLED_HINTS.FASTING_BUSY';
        }
        if (hours === null) {
            return 'FORM_ERRORS.REQUIRED';
        }

        if (hours > this.maxReducibleHours()) {
            return 'DISABLED_HINTS.FASTING_REDUCE_UNAVAILABLE';
        }

        return null;
    }

    protected updateExtendHours(value: FdUiInputValue): void {
        const hours = typeof value === 'number' ? value : null;
        this.extendHoursDraft.set(hours);
        if (hours !== null) {
            this.extendHours.set(hours);
        }
    }

    protected updateReduceHours(value: FdUiInputValue): void {
        const hours = typeof value === 'number' ? value : null;
        this.reduceHoursDraft.set(hours);
        if (hours !== null) {
            this.reduceHours.set(hours);
        }
    }
}
