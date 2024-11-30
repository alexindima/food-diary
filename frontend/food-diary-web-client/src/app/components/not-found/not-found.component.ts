import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TuiButton } from '@taiga-ui/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
    selector: 'app-not-found',
    imports: [RouterLink, TuiButton, TranslatePipe],
    templateUrl: './not-found.component.html',
    styleUrl: './not-found.component.less'
})
export class NotFoundComponent {}
