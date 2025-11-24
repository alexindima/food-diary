import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { TranslatePipe } from '@ngx-translate/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
    selector: 'fd-quick-action-card',
    standalone: true,
    imports: [FdUiCardComponent, TranslatePipe, MatIconModule],
    templateUrl: './quick-action-card.component.html',
    styleUrls: ['./quick-action-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuickActionCardComponent {
    public readonly icon = input.required<string>();
    public readonly titleKey = input.required<string>();
    public readonly descriptionKey = input.required<string>();
    public readonly action = output<void>();
}
