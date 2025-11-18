import { Component } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
    selector: 'fd-ui-loader',
    standalone: true,
    imports: [MatProgressSpinnerModule],
    template: `
        <div class="fd-ui-loader">
            <mat-progress-spinner mode="indeterminate" diameter="32"></mat-progress-spinner>
        </div>
    `,
    styleUrls: ['./fd-ui-loader.component.scss'],
})
export class FdUiLoaderComponent {}
