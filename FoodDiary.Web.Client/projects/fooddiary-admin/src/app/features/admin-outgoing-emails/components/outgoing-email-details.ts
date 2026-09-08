import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';

import type { OutgoingEmail } from '../models/outgoing-email';

@Component({
    selector: 'fd-outgoing-email-details',
    imports: [CommonModule, RouterLink, TranslatePipe, FdUiButtonComponent],
    templateUrl: './outgoing-email-details.html',
    styleUrl: './outgoing-email-details.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OutgoingEmailDetailsComponent {
    public readonly mail = input.required<OutgoingEmail>();
    protected readonly sourceMessageId = computed(() => (this.mail().purpose === 'bug_report_received' ? this.mail().correlationId : null));
    public readonly closed = output();
}
