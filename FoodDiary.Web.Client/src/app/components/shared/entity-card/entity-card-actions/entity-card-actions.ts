import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

@Component({
    selector: 'fd-entity-card-actions',
    imports: [FdUiIconComponent],
    templateUrl: './entity-card-actions.html',
    styleUrl: '../entity-card.scss',
    host: {
        class: 'entity-card-actions-host',
    },
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EntityCardActionsComponent {
    public readonly actionIcon = input.required<string>();
    public readonly actionAriaLabel = input<string | null>(null);

    public readonly action = output();

    protected emitCardAction(event: Event): void {
        event.stopPropagation();
        this.action.emit();
    }
}
