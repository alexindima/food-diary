import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

@Component({
    selector: 'fd-not-found',
    imports: [RouterLink, FdUiButtonComponent, FdUiIconComponent, TranslatePipe],
    templateUrl: './not-found.html',
    styleUrl: './not-found.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundComponent {}
