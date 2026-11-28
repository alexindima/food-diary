import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import type { AdminUserLoginEvent, AdminUserRoleAuditEvent } from '../models/admin-user.models';

@Component({
    selector: 'fd-admin-user-details-body',
    imports: [DatePipe, TranslatePipe, AdminLoadErrorComponent],
    templateUrl: './admin-user-details-body.html',
    styleUrl: './admin-user-details-body.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserDetailsBodyComponent {
    public readonly loginEventsLoading = input(false);
    public readonly loginEventsFailed = input(false);
    public readonly roleAuditLoading = input(false);
    public readonly roleAuditFailed = input(false);
    public readonly retryLoginEvents = output();
    public readonly retryRoleAudit = output();
    public readonly roleAuditEvents = input.required<AdminUserRoleAuditEvent[]>();
    public readonly loginEvents = input.required<AdminUserLoginEvent[]>();

    protected describeRoleActor(event: AdminUserRoleAuditEvent): string {
        if (event.actorEmail !== null && event.actorEmail !== undefined && event.actorEmail.trim().length > 0) {
            return event.actorEmail;
        }

        if (event.actorUserId !== null && event.actorUserId !== undefined && event.actorUserId.trim().length > 0) {
            return event.actorUserId;
        }

        return event.source;
    }
}
