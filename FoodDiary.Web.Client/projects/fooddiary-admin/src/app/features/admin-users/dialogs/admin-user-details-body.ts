import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import type { AdminUserLoginEvent, AdminUserRoleAuditEvent } from '../models/admin-user.models';

export type DetailField = {
    label: string;
    value: string;
};

export type DetailSection = {
    title: string;
    fields: DetailField[];
};

@Component({
    selector: 'fd-admin-user-details-body',
    imports: [DatePipe],
    templateUrl: './admin-user-details-body.html',
    styleUrl: './admin-user-details-body.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserDetailsBodyComponent {
    public readonly roleAuditEvents = input.required<AdminUserRoleAuditEvent[]>();
    public readonly loginEvents = input.required<AdminUserLoginEvent[]>();
    public readonly sections = input.required<DetailSection[]>();

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
