import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { LocalizationService } from '../../../../../services/localization.service';
import type { DietologistRelationship } from '../../../../../shared/models/dietologist.data';
import { createDietologistForm } from '../../user-manage/user-manage-lib/user-manage-form.mapper';
import { UserManageDietologistSummaryComponent } from './user-manage-dietologist-summary.component';

describe('UserManageDietologistSummaryComponent', () => {
    it('builds invite action for empty relationship', async () => {
        const fixture = await createComponentAsync(null);
        const view = getProtectedView(fixture.componentInstance);

        expect(view.labelView()).toEqual({ key: 'USER_MANAGE.DIETOLOGIST_EMPTY_TITLE', params: null });
        expect(view.hintView()).toEqual({ key: 'USER_MANAGE.DIETOLOGIST_EMPTY_HINT', params: null });
        expect(view.summaryAction()).toEqual({
            fill: 'solid',
            variant: 'primary',
            icon: 'person_add',
            labelKey: 'USER_MANAGE.DIETOLOGIST_INVITE_ACTION',
            disabled: true,
            action: 'invite',
        });
        expect(view.showPendingStatus()).toBe(false);
    });

    it('builds pending relationship labels and revoke action', async () => {
        const relationship = createRelationship('Pending');
        const fixture = await createComponentAsync(relationship);
        const view = getProtectedView(fixture.componentInstance);

        expect(view.labelView()).toEqual({ key: 'USER_MANAGE.DIETOLOGIST_PENDING_TITLE', params: null });
        expect(view.hintView()).toEqual({
            key: 'USER_MANAGE.DIETOLOGIST_PENDING_HINT',
            params: { email: relationship.email },
        });
        expect(view.summaryAction().labelKey).toBe('USER_MANAGE.DIETOLOGIST_CANCEL_INVITE');
        expect(view.summaryAction().action).toBe('revoke');
        expect(view.showPendingStatus()).toBe(true);
    });

    it('builds connected relationship label and disconnect action', async () => {
        const relationship = createRelationship('Accepted');
        const fixture = await createComponentAsync(relationship);
        const view = getProtectedView(fixture.componentInstance);

        expect(view.labelView()).toEqual({
            key: 'USER_MANAGE.DIETOLOGIST_CONNECTED_INFO',
            params: { email: relationship.email },
        });
        expect(view.summaryAction().labelKey).toBe('USER_MANAGE.DIETOLOGIST_DISCONNECT_ACTION');
        expect(view.summaryAction().action).toBe('revoke');
        expect(view.showPendingStatus()).toBe(false);
    });
});

type DietologistSummaryActionView = {
    fill: 'outline' | 'solid';
    variant: 'primary' | 'secondary';
    icon: string;
    labelKey: string;
    disabled: boolean;
    action: 'invite' | 'revoke';
};

type DietologistLabelView = {
    key: string;
    params: Record<string, string | null | undefined> | null;
};

function getProtectedView(component: UserManageDietologistSummaryComponent): {
    labelView: () => DietologistLabelView;
    hintView: () => DietologistLabelView;
    summaryAction: () => DietologistSummaryActionView;
    showPendingStatus: () => boolean;
} {
    return component as unknown as {
        labelView: () => DietologistLabelView;
        hintView: () => DietologistLabelView;
        summaryAction: () => DietologistSummaryActionView;
        showPendingStatus: () => boolean;
    };
}

async function createComponentAsync(
    relationship: DietologistRelationship | null,
): Promise<ComponentFixture<UserManageDietologistSummaryComponent>> {
    await TestBed.configureTestingModule({
        imports: [UserManageDietologistSummaryComponent, TranslateModule.forRoot()],
        providers: [{ provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } }],
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManageDietologistSummaryComponent);
    fixture.componentRef.setInput('dietologistForm', createDietologistForm());
    fixture.componentRef.setInput('dietologistRelationship', relationship);
    fixture.componentRef.setInput('dietologistInviteEmailError', null);
    fixture.componentRef.setInput('isSavingDietologist', false);
    fixture.detectChanges();
    return fixture;
}

function createRelationship(status: DietologistRelationship['status']): DietologistRelationship {
    return {
        invitationId: 'invitation-1',
        status,
        email: 'diet@example.com',
        firstName: 'Diet',
        lastName: 'Doctor',
        dietologistUserId: status === 'Accepted' ? 'dietologist-1' : null,
        permissions: {
            shareProfile: true,
            shareMeals: true,
            shareStatistics: true,
            shareWeight: true,
            shareWaist: true,
            shareGoals: true,
            shareHydration: true,
            shareFasting: true,
        },
        createdAtUtc: '2026-05-01T00:00:00Z',
        expiresAtUtc: '2026-05-08T00:00:00Z',
        acceptedAtUtc: status === 'Accepted' ? '2026-05-02T00:00:00Z' : null,
    };
}
