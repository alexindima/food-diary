import { ChangeDetectionStrategy, Component, EventEmitter, Output, input, output } from '@angular/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { TranslatePipe } from '@ngx-translate/core';
import { MatIconModule } from '@angular/material/icon';

export type QuickActionVariant = 'primary' | 'secondary' | 'danger';
export type QuickActionFill = 'solid' | 'outline' | 'text';

@Component({
    selector: 'fd-quick-action-card',
    standalone: true,
    imports: [FdUiCardComponent, FdUiButtonComponent, TranslatePipe, MatIconModule],
    templateUrl: './quick-action-card.component.html',
    styleUrls: ['./quick-action-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuickActionCardComponent {
    public readonly icon = input.required<string>();
    public readonly titleKey = input.required<string>();
    public readonly descriptionKey = input.required<string>();
    public readonly buttonKey = input.required<string>();
    public readonly variant = input.required<QuickActionVariant>();
    public readonly fill = input.required<QuickActionFill>();
    public readonly action = output<void>();
}
