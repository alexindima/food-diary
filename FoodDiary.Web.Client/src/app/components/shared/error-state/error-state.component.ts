import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-error-state',
    standalone: true,
    imports: [TranslatePipe, FdUiIconComponent, FdUiButtonComponent],
    templateUrl: './error-state.component.html',
    styleUrl: './error-state.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorStateComponent {
    public readonly titleKey = input<string>('ERRORS.LOAD_FAILED_TITLE');
    public readonly messageKey = input<string>('ERRORS.LOAD_FAILED_MESSAGE');
    public readonly icon = input<string>('error_outline');
    public readonly showRetry = input<boolean>(true);
    public readonly retryKey = input<string>('ERRORS.RETRY');

    public readonly retry = output<void>();

    public onRetry(): void {
        this.retry.emit();
    }
}
