import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

export type NoticeVariant = 'info' | 'warning' | 'error';

@Component({
    selector: 'fd-notice-banner',
    standalone: true,
    imports: [FdUiButtonComponent],
    templateUrl: './notice-banner.component.html',
    styleUrl: './notice-banner.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NoticeBannerComponent {
    public readonly type = input<NoticeVariant>('info');
    public readonly title = input<string>('');
    public readonly message = input<string>('');
    public readonly actionLabel = input<string | null>(null);
    public readonly action = output();

    public readonly showAction = computed(() => {
        const actionLabel = this.actionLabel();
        return actionLabel !== null && actionLabel.length > 0;
    });

    public onAction(): void {
        const actionLabel = this.actionLabel();
        if (actionLabel === null || actionLabel.length === 0) {
            return;
        }
        this.action.emit();
    }
}
