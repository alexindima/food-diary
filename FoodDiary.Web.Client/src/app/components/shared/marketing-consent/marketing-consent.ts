import {
    afterNextRender,
    afterRenderEffect,
    ChangeDetectionStrategy,
    Component,
    computed,
    type ElementRef,
    inject,
    input,
    output,
    signal,
    viewChild,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent } from 'fd-ui-kit';

import { MARKETING_ATTRIBUTION_ENABLED } from '../../../shared/marketing/marketing-attribution-enabled';
import { MarketingConsentService } from '../../../shared/marketing/marketing-consent.service';

@Component({
    selector: 'fd-marketing-consent',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent],
    templateUrl: './marketing-consent.html',
    styleUrl: './marketing-consent.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MarketingConsentComponent {
    public readonly settingsOpen = input(false);
    public readonly closed = output();
    protected readonly consent = inject(MarketingConsentService);
    private readonly enabled = inject(MARKETING_ATTRIBUTION_ENABLED);
    private readonly ready = signal(false);
    private readonly notice = viewChild<ElementRef<HTMLElement>>('notice');
    protected readonly visible = computed(
        () => this.enabled && this.ready() && (this.settingsOpen() || this.consent.currentDecision() === null),
    );

    public constructor() {
        afterNextRender(() => {
            this.ready.set(true);
        });
        afterRenderEffect(() => {
            if (this.settingsOpen()) {
                this.notice()?.nativeElement.focus({ preventScroll: true });
            }
        });
    }

    protected choose(allowed: boolean): void {
        this.consent.decide(allowed);
        this.closed.emit();
    }
}
