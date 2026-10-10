import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiStopActivationDirective } from 'fd-ui-kit';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

@Component({
    selector: 'fd-entity-card-actions',
    imports: [FdUiStopActivationDirective, FdUiIconComponent],
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

    protected emitCardAction(): void {
        this.action.emit();
    }
}
