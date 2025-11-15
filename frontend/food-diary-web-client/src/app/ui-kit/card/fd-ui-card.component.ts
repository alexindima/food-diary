import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    ContentChild,
    Input,
} from '@angular/core';
import { FdUiCardActionsDirective } from './fd-ui-card-actions.directive';

export type FdUiCardAppearance = 'default' | 'product' | 'recipe' | 'info' | 'general';

@Component({
    selector: 'fd-ui-card',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './fd-ui-card.component.html',
    styleUrls: ['./fd-ui-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiCardComponent {
    @Input() public title?: string;
    @Input() public subtle = false;
    @Input() public meta?: string;
    @Input() public appearance: FdUiCardAppearance = 'default';

    @ContentChild(FdUiCardActionsDirective) public headerActions?: FdUiCardActionsDirective;

    public get appearanceClass(): string {
        return `fd-ui-card--appearance-${this.appearance}`;
    }
}
