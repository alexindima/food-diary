import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

@Component({
    selector: 'fd-not-found',
    imports: [RouterLink, FdUiButtonComponent, FdUiIconComponent, TranslatePipe],
    templateUrl: './not-found.component.html',
    styleUrl: './not-found.component.scss',
})
export class NotFoundComponent {}
