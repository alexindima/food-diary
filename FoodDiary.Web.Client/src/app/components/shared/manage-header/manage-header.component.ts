import { ChangeDetectionStrategy, Component, booleanAttribute, input, output } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

@Component({
    selector: 'fd-manage-header',
    imports: [FdUiButtonComponent],
    templateUrl: './manage-header.component.html',
    styleUrls: ['./manage-header.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.fd-manage-header--mobile-back-only]': 'mobileBackOnly()',
    },
})
export class ManageHeaderComponent {
    public readonly title = input.required<string>();
    public readonly subtitle = input<string | null>(null);
    public readonly mobileBackOnly = input(true, { transform: booleanAttribute });
    public readonly backAriaLabel = input('Back');
    public readonly back = output<void>();

    public onBackClick(): void {
        this.back.emit();
    }
}
