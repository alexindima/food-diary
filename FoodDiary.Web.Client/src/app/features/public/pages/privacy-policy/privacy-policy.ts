import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
    selector: 'fd-privacy-policy',
    templateUrl: './privacy-policy.html',
    styleUrls: ['./privacy-policy.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe],
})
export class PrivacyPolicyComponent {}
