import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

@Component({
    selector: 'fd-entity-card-actions',
    imports: [DecimalPipe, TranslatePipe, FdUiButtonComponent],
    templateUrl: './entity-card-actions.component.html',
    styleUrl: './entity-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EntityCardActionsComponent {
    public readonly calories = input.required<number>();
    public readonly showAction = input.required<boolean>();
    public readonly actionIcon = input.required<string>();
    public readonly actionAriaLabel = input<string | null>(null);

    public readonly action = output();

    public handleAction(event: Event): void {
        event.stopPropagation();
        this.action.emit();
    }
}
