import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-admin-load-error',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './admin-load-error.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLoadErrorComponent {
    public readonly retry = output();
}
