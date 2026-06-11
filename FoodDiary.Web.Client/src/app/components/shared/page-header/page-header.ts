import { booleanAttribute, ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { HeaderActionsOverflowComponent } from '../header-actions-overflow/header-actions-overflow';

@Component({
    selector: 'fd-page-header',
    imports: [FdUiHintDirective, FdUiButtonComponent, HeaderActionsOverflowComponent],
    templateUrl: './page-header.html',
    styleUrls: ['./page-header.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.fd-page-header--compact-actions]': 'compactActions()',
        '[class.fd-page-header--with-back]': 'backVisible()',
    },
})
export class PageHeaderComponent {
    public readonly title = input.required<string>();
    public readonly subtitle = input<string>();
    public readonly compactActions = input(false);
    public readonly backVisible = input(false, { transform: booleanAttribute });
    public readonly backAriaLabel = input('Back');
    public readonly back = output();

    protected onBackClick(): void {
        this.back.emit();
    }
}
