import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  input,
  contentChild
} from '@angular/core';
import { FdUiCardActionsDirective } from './fd-ui-card-actions.directive';

export type FdUiCardAppearance = 'default' | 'product' | 'recipe' | 'info' | 'general' | 'entry';

@Component({
    selector: 'fd-ui-card',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './fd-ui-card.component.html',
    styleUrls: ['./fd-ui-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiCardComponent {
    public readonly title = input<string>();
    public readonly subtle = input(false);
    public readonly meta = input<string>();
    public readonly appearance = input<FdUiCardAppearance>('default');

    public readonly headerActions = contentChild(FdUiCardActionsDirective);

    public get appearanceClass(): string {
        return `fd-ui-card--appearance-${this.appearance()}`;
    }
}
