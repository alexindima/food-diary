import { Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'fd-landing-steps',
    standalone: true,
    imports: [TranslateModule],
    templateUrl: './landing-steps.component.html',
    styleUrls: ['./landing-steps.component.scss'],
})
export class LandingStepsComponent {}
