import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
    selector: 'fd-auth-google-section',
    imports: [TranslatePipe],
    templateUrl: './auth-google-section.component.html',
    styleUrl: './auth.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthGoogleSectionComponent {
    public readonly isReady = input.required<boolean>();
    public readonly unavailableKey = input.required<string>();
}
