import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiFocusWhenDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { environment } from '../../../../../../environments/environment';

@Component({
    selector: 'fd-user-manage-telegram-connection',
    imports: [RouterLink, TranslatePipe, FdUiButtonComponent, FdUiIconComponent, FdUiFocusWhenDirective],
    templateUrl: './user-manage-telegram-connection.html',
    styleUrl: '../../user-manage/user-manage.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageTelegramConnectionComponent {
    public readonly hasTelegramIdentity = input(false);
    public readonly canUnlinkTelegram = input(false);
    public readonly isUnlinkingTelegram = input(false);
    public readonly unlinkError = input<string | null>(null);
    public readonly telegramUnlink = output();
    protected readonly botUrl = /^[A-Za-z0-9_]{5,32}$/.test(environment.telegramBotUsername ?? '')
        ? `https://t.me/${environment.telegramBotUsername}`
        : null;
}
