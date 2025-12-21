import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

export type NoticeVariant = 'info' | 'warning' | 'error';

@Component({
    selector: 'fd-notice-banner',
    standalone: true,
    templateUrl: './notice-banner.component.html',
    styleUrl: './notice-banner.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NoticeBannerComponent {
    public readonly type = input<NoticeVariant>('info');
    public readonly title = input<string>('');
    public readonly message = input<string>('');
    public readonly actionLabel = input<string | null>(null);
    public readonly action = output<void>();

    public readonly showAction = computed(() => !!this.actionLabel());

    public onAction(): void {
        if (!this.actionLabel()) {
            return;
        }
        this.action.emit();
    }
}
