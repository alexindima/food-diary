import { afterNextRender, ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent } from 'fd-ui-kit';

import { MarketingConsentService } from '../../../shared/marketing/marketing-consent.service';

@Component({
    selector: 'fd-marketing-consent',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent],
    templateUrl: './marketing-consent.html',
    styleUrl: './marketing-consent.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MarketingConsentComponent {
    protected readonly consent = inject(MarketingConsentService);
    protected readonly expanded = signal(false);

    public constructor() {
        afterNextRender(() => {
            this.expanded.set(this.consent.currentDecision() === null);
        });
    }

    protected choose(allowed: boolean): void {
        this.consent.decide(allowed);
        this.expanded.set(false);
    }
}
