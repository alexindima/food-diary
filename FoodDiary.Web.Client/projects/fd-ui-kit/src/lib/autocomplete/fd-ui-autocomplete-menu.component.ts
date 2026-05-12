import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { FdUiLoaderComponent } from '../loader/fd-ui-loader.component';
import type { FdUiAutocompleteOption } from './fd-ui-autocomplete.types';

@Component({
    selector: 'fd-ui-autocomplete-menu',
    imports: [FdUiLoaderComponent],
    templateUrl: './fd-ui-autocomplete-menu.component.html',
    styleUrl: './fd-ui-autocomplete.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiAutocompleteMenuComponent<T = unknown> {
    protected readonly isEqual = Object.is;

    public readonly id = input.required<string>();
    public readonly loading = input.required<boolean>();
    public readonly options = input.required<Array<FdUiAutocompleteOption<T>>>();
    public readonly emptyText = input<string>();
    public readonly activeIndex = input.required<number>();
    public readonly selectedValue = input.required<T | null>();

    public readonly optionSelected = output<FdUiAutocompleteOption<T>>();

    protected getOptionId(index: number): string {
        return `${this.id()}-option-${index}`;
    }

    protected getOptionTrack(option: FdUiAutocompleteOption<T>, index: number): string | number {
        return option.id ?? `${String(option.badge ?? '')}:${option.label}:${index}`;
    }
}
