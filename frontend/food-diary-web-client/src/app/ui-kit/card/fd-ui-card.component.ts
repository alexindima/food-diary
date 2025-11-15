import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    ContentChild,
    Input,
} from '@angular/core';
import { FdUiCardActionsDirective } from './fd-ui-card-actions.directive';

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

    @ContentChild(FdUiCardActionsDirective) public headerActions?: FdUiCardActionsDirective;
}

