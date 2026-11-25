import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox';

import { INCOMING_MAILBOXES, type IncomingMailbox } from '../lib/admin-mail-inbox-preferences';

@Component({
    selector: 'fd-admin-mailbox-views',
    imports: [FdUiButtonComponent, FdUiCheckboxComponent, TranslatePipe],
    templateUrl: './admin-mailbox-views.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminMailboxViewsComponent {
    public readonly recipient = input.required<string>();
    public readonly hiddenRecipients = input.required<readonly IncomingMailbox[]>();
    public readonly recipientSelected = output<string>();
    public readonly recipientVisibilityChanged = output<{ recipient: IncomingMailbox; hidden: boolean }>();

    protected readonly incomingMailboxes = INCOMING_MAILBOXES;
    protected readonly showPreferences = signal(false);
    protected readonly selectedRecipient = computed(() => this.recipient().toLowerCase());
}
