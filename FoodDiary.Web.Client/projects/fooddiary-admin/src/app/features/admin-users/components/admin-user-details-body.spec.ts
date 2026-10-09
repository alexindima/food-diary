import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminUserRoleAuditEvent } from '../models/admin-user.models';
import { AdminUserDetailsBodyComponent } from './admin-user-details-body';

const roleAuditEvent: AdminUserRoleAuditEvent = {
    id: adminId<'role-audit-event'>('role-1'),
    userId: adminId<'user'>('user-1'),
    roleName: 'Support',
    action: 'Added',
    actorUserId: adminId<'user'>('actor-1'),
    actorEmail: 'admin@example.com',
    source: 'AdminPanel',
    occurredAtUtc: adminUtcInstant('2026-02-03T00:00:00Z'),
};

function createComponent(): AdminUserDetailsBodyComponent {
    TestBed.configureTestingModule({ imports: [AdminUserDetailsBodyComponent] });
    return TestBed.createComponent(AdminUserDetailsBodyComponent).componentInstance;
}

describe('AdminUserDetailsBodyComponent', () => {
    it('describes the role actor with email, user id then source fallback', () => {
        const component = createComponent();

        expect(component['describeRoleActor'](roleAuditEvent)).toBe('admin@example.com');
        expect(component['describeRoleActor']({ ...roleAuditEvent, actorEmail: '', actorUserId: adminId<'user'>('actor-1') })).toBe(
            'actor-1',
        );
        expect(component['describeRoleActor']({ ...roleAuditEvent, actorEmail: '', actorUserId: adminId<'user'>('') })).toBe('AdminPanel');
    });
});
