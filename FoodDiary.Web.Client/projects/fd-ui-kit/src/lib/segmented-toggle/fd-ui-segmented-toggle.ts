import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, type ElementRef, inject, input, model, viewChildren } from '@angular/core';
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
    private readonly optionButtons = viewChildren<ElementRef<HTMLButtonElement>>('optionButton');

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
        const options = this.options();
        const selectedIndex = options.findIndex(option => option.value === this.selectedValue());
        return options.map((option, index) => ({
            ...option,
            labelText: option.label ?? (option.labelKey !== undefined ? this.translate.instant(option.labelKey) : ''),
            selected: index === selectedIndex,
            tabIndex: index === Math.max(0, selectedIndex) ? 0 : -1,
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

    protected onKeydown(value: Value, event: KeyboardEvent): void {
        let offset: 1 | -1;
        switch (event.key) {
            case 'ArrowRight':
            case 'ArrowDown': {
                offset = 1;
                break;
            }
            case 'ArrowLeft':
            case 'ArrowUp': {
                offset = -1;
                break;
            }
            default: {
                return;
            }
        }

        const options = this.options();
        const index = options.findIndex(option => option.value === value);
        if (index === -1) {
            return;
        }
        const next = options[(index + offset + options.length) % options.length];
        const button = this.optionButtons().find(option => option.nativeElement.value === next.value)?.nativeElement;
        if (button !== undefined) {
            event.preventDefault();
            button.focus();
            this.select(next.value);
        }
    }
}
