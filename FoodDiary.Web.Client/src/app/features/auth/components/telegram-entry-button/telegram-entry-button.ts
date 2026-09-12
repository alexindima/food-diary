import { afterNextRender, ChangeDetectionStrategy, Component, computed, inject, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { TelegramWebAppService } from '../../../../shared/auth/telegram-web-app.service';
import { TelegramAuthFacade } from '../../lib/telegram-auth.facade';

@Component({
    selector: 'fd-telegram-entry-button',
    imports: [TranslatePipe, FdUiButtonComponent],
    providers: [TelegramAuthFacade],
    templateUrl: './telegram-entry-button.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TelegramEntryButtonComponent {
    public readonly selected = output();
    private readonly facade = inject(TelegramAuthFacade);
    private readonly telegram = inject(TelegramWebAppService);
    protected readonly available = computed(
        () =>
            this.facade.configuration()?.loginEnabled === true &&
            (this.facade.configuration()?.oidcEnabled === true || this.telegram.isMiniAppLaunch()),
    );

    public constructor() {
        afterNextRender(() => {
            void this.facade.loadConfigurationAsync();
        });
    }
}
