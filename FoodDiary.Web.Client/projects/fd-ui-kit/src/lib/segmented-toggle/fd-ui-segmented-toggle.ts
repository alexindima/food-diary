import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, model } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';

export type FdUiSegmentedToggleOption<Value extends string = string> = {
    label?: string;
    labelKey?: string;
    value: Value;
};

export type FdUiSegmentedToggleAppearance = 'default' | 'soft';
export type FdUiSegmentedToggleSize = 'compact' | 'sm' | 'md';

@Component({
    selector: 'fd-ui-segmented-toggle',
    imports: [CommonModule],
    templateUrl: './fd-ui-segmented-toggle.html',
    styleUrls: ['./fd-ui-segmented-toggle.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.fd-ui-segmented-toggle-host--full-width]': 'fullWidth()',
        '[class.fd-ui-segmented-toggle-host--fit-content]': 'fitContent()',
    },
})
export class FdUiSegmentedToggleComponent<Value extends string = string> {
    private readonly translate = inject(TranslateService);
    private readonly languageChange = toSignal(this.translate.onLangChange);

    public readonly options = input<Array<FdUiSegmentedToggleOption<Value>>>([]);
    public readonly selectedValue = model.required<Value>();
    public readonly ariaLabel = input<string | null>(null);
    public readonly appearance = input<FdUiSegmentedToggleAppearance>('default');
    public readonly size = input<FdUiSegmentedToggleSize>('md');
    public readonly fullWidth = input(false);
    public readonly fitContent = input(false);
    public readonly shrinkItems = input(false);
    public readonly stackOnNarrow = input(true);
    public readonly wrapOnNarrow = input(false);
    protected readonly optionViewModels = computed(() => {
        this.languageChange();
        return this.options().map(option => ({
            ...option,
            labelText: option.label ?? (option.labelKey !== undefined ? this.translate.instant(option.labelKey) : ''),
        }));
    });

    protected readonly containerClass = computed(() => {
        const classes = [
            'fd-ui-segmented-toggle',
            `fd-ui-segmented-toggle--appearance-${this.appearance()}`,
            `fd-ui-segmented-toggle--size-${this.size()}`,
        ];
        if (this.fullWidth()) {
            classes.push('fd-ui-segmented-toggle--full-width');
        }
        if (this.shrinkItems()) {
            classes.push('fd-ui-segmented-toggle--shrink-items');
        }
        if (this.stackOnNarrow()) {
            classes.push('fd-ui-segmented-toggle--stack-on-narrow');
        }
        if (this.wrapOnNarrow()) {
            classes.push('fd-ui-segmented-toggle--wrap-on-narrow');
        }
        return classes.join(' ');
    });

    protected select(value: Value): void {
        if (value === this.selectedValue()) {
            return;
        }

        this.selectedValue.set(value);
    }
}
