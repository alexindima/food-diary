import { ChangeDetectionStrategy, Component, computed, contentChild, input } from '@angular/core';

import { FdUiCardActionsDirective } from './fd-ui-card-actions.directive';

export type FdUiCardAppearance = 'default' | 'product' | 'recipe' | 'info' | 'general' | 'entry';
export type FdUiCardTone = 'default' | 'editor' | 'editor-white' | 'editor-gradient' | 'profile';
export type FdUiCardDensity = 'default' | 'relaxed' | 'compact' | 'profile';
export type FdUiCardAccent = 'default' | 'primary' | 'success';
export type FdUiCardHeaderAlign = 'start' | 'center';

@Component({
    selector: 'fd-ui-card',
    standalone: true,
    imports: [],
    templateUrl: './fd-ui-card.component.html',
    styleUrls: ['./fd-ui-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiCardComponent {
    public readonly title = input<string>();
    public readonly subtle = input(false);
    public readonly meta = input<string>();
    public readonly appearance = input<FdUiCardAppearance>('default');
    public readonly tone = input<FdUiCardTone>('default');
    public readonly density = input<FdUiCardDensity>('default');
    public readonly accent = input<FdUiCardAccent>('default');
    public readonly headerAlign = input<FdUiCardHeaderAlign>('start');

    public readonly headerActions = contentChild(FdUiCardActionsDirective);

    public readonly cardClass = computed(() => {
        const classes = ['fd-ui-card'];
        if (this.subtle()) {
            classes.push('fd-ui-card--subtle');
        }
        classes.push(`fd-ui-card--appearance-${this.appearance()}`);
        classes.push(`fd-ui-card--tone-${this.tone()}`);
        classes.push(`fd-ui-card--density-${this.density()}`);
        classes.push(`fd-ui-card--accent-${this.accent()}`);
        classes.push(`fd-ui-card--header-align-${this.headerAlign()}`);
        return classes.join(' ');
    });
}
