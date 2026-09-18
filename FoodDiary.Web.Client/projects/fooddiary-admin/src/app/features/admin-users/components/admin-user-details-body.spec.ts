import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import type { AdminUserRoleAuditEvent } from '../models/admin-user.models';
import { AdminUserDetailsBodyComponent } from './admin-user-details-body';

const roleAuditEvent: AdminUserRoleAuditEvent = {
    id: 'role-1',
    userId: 'user-1',
    roleName: 'Support',
    action: 'Added',
    actorUserId: 'actor-1',
    actorEmail: 'admin@example.com',
    source: 'AdminPanel',
    occurredAtUtc: '2026-02-03T00:00:00Z',
};

function createComponent(): AdminUserDetailsBodyComponent {
    TestBed.configureTestingModule({ imports: [AdminUserDetailsBodyComponent] });
    return TestBed.createComponent(AdminUserDetailsBodyComponent).componentInstance;
}

describe('AdminUserDetailsBodyComponent', () => {
    it('describes the role actor with email, user id then source fallback', () => {
        const component = createComponent();

        expect(component['describeRoleActor'](roleAuditEvent)).toBe('admin@example.com');
        expect(component['describeRoleActor']({ ...roleAuditEvent, actorEmail: '', actorUserId: 'actor-1' })).toBe('actor-1');
        expect(component['describeRoleActor']({ ...roleAuditEvent, actorEmail: '', actorUserId: '' })).toBe('AdminPanel');
    });
});
