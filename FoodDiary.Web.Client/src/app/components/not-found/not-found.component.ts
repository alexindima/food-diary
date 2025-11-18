import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

@Component({
    selector: 'fd-not-found',
    imports: [RouterLink, FdUiButtonComponent, TranslatePipe],
    templateUrl: './not-found.component.html',
    styleUrl: './not-found.component.less'
})
export class NotFoundComponent {}
