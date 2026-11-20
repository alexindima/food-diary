import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom, type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { FrontendObservabilityService } from '../../../../services/frontend-observability.service';
import { UnsavedChangesService } from '../../../../services/unsaved-changes.service';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { FASTING_REMINDER_PRESETS } from '../../../../shared/lib/fasting-reminder-presets';
import type { DietologistRelationship } from '../../../../shared/models/dietologist.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { Gender, type User } from '../../../../shared/models/user.data';
import { NotificationService, type WebPushSubscriptionItem } from '../../../../shared/notifications/notification.service';
import { PushNotificationService } from '../../../../shared/notifications/push-notification.service';
import { DIETOLOGIST_RELATIONSHIP_ACTIONS } from '../../../dietologist/contracts/relationship-actions';
import { ProfileManageFacade } from '../../lib/profile-manage.facade';
import { DEFAULT_FASTING_CHECK_IN_FOLLOW_UP_REMINDER_HOURS, DEFAULT_FASTING_CHECK_IN_REMINDER_HOURS } from '../../lib/user-manage.config';
import { UserManageComparisonWidgetsComponent } from '../user-manage-sections/comparison-widgets/user-manage-comparison-widgets';
import { UserManageComponent } from './user-manage';
import { UserManageNotificationsFacade } from './user-manage-lib/user-manage-notifications.facade';

let fixture: ComponentFixture<UserManageComponent>;
let component: UserManageComponent;
let dietologistService: DietologistServiceMock;
let facade: ProfileManageFacadeMock;
let dialogService: { open: ReturnType<typeof vi.fn> };
let router: { navigate: ReturnType<typeof vi.fn> };
let notificationService: NotificationServiceMock;
let notificationsFacade: UserManageNotificationsFacadeMock;
let toastService: ReturnType<typeof createToastServiceMock>;
const ABOVE_MAX_PROFILE_HEIGHT = 301;
const FRACTIONAL_PROFILE_HEIGHT = 0.5;
const MAX_PROFILE_HEIGHT = 300;
const INITIAL_PROFILE_HEIGHT = 180;
const INVALID_HEIGHT_FEET = 10;
const CONVERTED_INVALID_HEIGHT = 332.5;
const FUTURE_PROFILE_BIRTH_DATE = '2999-01-01';
const VALID_PROFILE_BIRTH_DATE = '2000-10-02';
const ORIGINAL_SAVE_HEIGHT = 170;
const SUBMITTED_SAVE_HEIGHT = 170.2;

describe('UserManageComponent guided tour', () => {
    it('reveals security feedback after a Telegram callback and keeps the section open for retry', async () => {
        await createComponentAsync(null);
        const section = (fixture.nativeElement as HTMLElement).querySelector('fd-user-manage-security-card')?.closest('details');
        expect(section?.open).toBe(false);
        facade.telegramUnlinkError.set('USER_MANAGE.TELEGRAM_UNLINK_ERROR');
        fixture.detectChanges();
        expect(section?.open).toBe(true);
        facade.telegramUnlinkError.set(null);
        fixture.detectChanges();
        expect(section?.open).toBe(true);
    });

    it('reveals every tour section before starting without opening unrelated accordions', async () => {
        await createComponentAsync(null);
        const host = fixture.nativeElement as HTMLElement;
        const targets = [
            'user-manage-account',
            'user-manage-billing',
            'user-manage-body',
            'user-manage-notifications',
            'user-manage-dietologist',
            'user-manage-privacy',
            'user-manage-tour-help',
        ];
        const hiddenSections = targets
            .map(target => {
                const element = host.querySelector(`[data-tour-id="${target}"]`);
                expect(element).not.toBeNull();
                return element?.closest('details') ?? null;
            })
            .filter((section): section is HTMLDetailsElement => section !== null);
        expect(hiddenSections.length).toBeGreaterThan(0);
        expect(hiddenSections.every(section => !section.open)).toBe(true);
        const unrelated = Array.from(host.querySelectorAll('details')).filter(section => !hiddenSections.includes(section));
        const start = vi.spyOn(TestBed.inject(FdTourService), 'start').mockImplementation(() => {
            expect(hiddenSections.every(section => section.open)).toBe(true);
            expect(unrelated.every(section => !section.open)).toBe(true);
            return true;
        });

        component['startUserManageTour']();

        expect(start).toHaveBeenCalledWith(expect.objectContaining({ id: 'user-manage' }), { force: true });
    });
});

describe('UserManageComponent dietologist invite state', () => {
    it('keeps invite mode when no dietologist relationship exists', async () => {
        await createComponentAsync(null);

        expect(dietologistService.getRelationship).not.toHaveBeenCalled();
        expect(component['hasDietologistRelationship']()).toBe(false);
        expect(component['dietologistForm'].email().disabled()).toBe(false);
        expect(component['dietologistFormModel']().shareProfile).toBe(true);
        expect(component['dietologistFormModel']().shareMeals).toBe(true);
        expect(component['dietologistFormModel']().shareFasting).toBe(true);
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('USER_MANAGE.DIETOLOGIST_INVITE_ACTION');
    });

    it('applies pending relationship state and disables email editing', async () => {
        await createComponentAsync({
            invitationId: entityId<'dietologist-invitation'>('inv-1'),
            status: 'Pending',
            email: 'diet@example.com',
            firstName: null,
            lastName: null,
            dietologistUserId: null,
            permissions: {
                shareProfile: true,
                shareMeals: true,
                shareStatistics: false,
                shareWeight: true,
                shareWaist: false,
                shareGoals: true,
                shareHydration: false,
                shareFasting: true,
            },
            createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
            expiresAtUtc: utcInstant('2026-04-22T00:00:00Z'),
            acceptedAtUtc: null,
        });

        expect(component['hasDietologistRelationship']()).toBe(true);
        expect(component['isDietologistPending']()).toBe(true);
        expect(component['dietologistForm'].email().disabled()).toBe(true);
        expect(component['dietologistFormModel']().email).toBe('diet@example.com');
        expect(component['dietologistFormModel']().shareProfile).toBe(true);
        expect(component['dietologistFormModel']().shareStatistics).toBe(false);
        expect(component['dietologistFormModel']().shareHydration).toBe(false);
        expect(component['dietologistFormModel']().shareFasting).toBe(true);
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('USER_MANAGE.DIETOLOGIST_CANCEL_INVITE');
        expect(host.textContent).not.toContain('USER_MANAGE.DIETOLOGIST_SAVE_PERMISSIONS');
    });
});

describe('Profile dietologist invitation submission', () => {
    it('keeps the invitation draft after a rejected send and renders the pending invitation once retried', async () => {
        await createComponentAsync(null);
        const host = fixture.nativeElement as HTMLElement;
        const summary = host.querySelector('details:has(fd-user-manage-dietologist-card) > summary');
        const email = host.querySelector<HTMLInputElement>('fd-user-manage-dietologist-summary input[type="email"]');
        const send = host.querySelector<HTMLButtonElement>('.user-manage__dietologist-summary-button button');
        const permission = host.querySelector<HTMLButtonElement>('fd-user-manage-dietologist-permissions button[role="switch"]');
        if (email === null || send === null || permission === null || !(summary instanceof HTMLElement)) {
            throw new Error('Expected the profile invitation controls');
        }
        summary.click();
        send.click();
        expect(dietologistService.invite).not.toHaveBeenCalled();
        email.value = 'diet@example.com';
        email.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        expect(send.disabled).toBe(false);
        expect(permission.disabled).toBe(false);

        const request = new Subject<void>();
        const permissions = {
            shareProfile: true,
            shareMeals: true,
            shareStatistics: true,
            shareWeight: true,
            shareWaist: true,
            shareGoals: true,
            shareHydration: true,
            shareFasting: true,
        };
        const relationship: DietologistRelationship = {
            invitationId: entityId<'dietologist-invitation'>('new-invitation'),
            status: 'Pending',
            email: 'diet@example.com',
            firstName: null,
            lastName: null,
            dietologistUserId: null,
            permissions,
            createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
            expiresAtUtc: utcInstant('2026-04-22T00:00:00Z'),
            acceptedAtUtc: null,
        };
        dietologistService.invite.mockReturnValueOnce(request);
        dietologistService.getRelationship.mockReturnValueOnce(of(relationship));
        send.click();
        fixture.detectChanges();
        expect(send.disabled).toBe(true);
        expect(permission.disabled).toBe(true);
        send.click();
        expect(dietologistService.invite).toHaveBeenCalledExactlyOnceWith({ dietologistEmail: email.value, permissions });
        expect(dietologistService.getRelationship).not.toHaveBeenCalled();

        request.error(new Error('Invitation delivery unavailable'));
        fixture.detectChanges();
        expect(host.querySelector('.user-manage__error--dietologist [role="alert"]')?.textContent).toContain(
            'USER_MANAGE.DIETOLOGIST_INVITE_ERROR',
        );
        expect(email.value).toBe('diet@example.com');
        expect(send.disabled).toBe(false);
        expect(permission.disabled).toBe(false);

        send.click();
        fixture.detectChanges();
        expect(dietologistService.getRelationship).toHaveBeenCalledOnce();
        expect(host.textContent).toContain('USER_MANAGE.DIETOLOGIST_STATUS_PENDING');
        expect(host.querySelector('.user-manage__error--dietologist [role="alert"]')).toBeNull();
    });
});

describe('UserManageComponent dietologist profile sharing', () => {
    it('asks for confirmation before disabling profile sharing', async () => {
        await createComponentAsync(null, false);

        component['onDietologistProfileToggle'](false);

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(component['dietologistFormModel']().shareProfile).toBe(true);
    });

    it('disables profile sharing after confirmation', async () => {
        await createComponentAsync(null, true);

        component['onDietologistProfileToggle'](false);
        fixture.detectChanges();

        expect(component['dietologistFormModel']().shareProfile).toBe(false);
    });
});

describe('UserManageComponent dietologist permission failure recovery', () => {
    it('keeps the save error visible after restoring failed permission changes', async () => {
        await createComponentAsync({
            invitationId: entityId<'dietologist-invitation'>('inv-1'),
            status: 'Accepted',
            email: 'diet@example.com',
            firstName: null,
            lastName: null,
            dietologistUserId: entityId<'user'>('diet-1'),
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
            createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
            expiresAtUtc: utcInstant('2026-04-22T00:00:00Z'),
            acceptedAtUtc: utcInstant('2026-04-15T01:00:00Z'),
        });
        dietologistService.updatePermissions.mockReturnValueOnce(throwError(() => new Error('Unavailable')));

        component['updateDietologistPermission']('shareMeals', false);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(component['dietologistFormModel']().shareMeals).toBe(true);
        expect(component['dietologistError']()).toBe('USER_MANAGE.DIETOLOGIST_PERMISSIONS_ERROR');
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('USER_MANAGE.DIETOLOGIST_PERMISSIONS_ERROR');

        component['updateDietologistPermission']('shareMeals', false);
        fixture.detectChanges();
        expect(component['dietologistError']()).toBeNull();
        expect(component['dietologistFormModel']().shareMeals).toBe(false);
    });
});

describe('UserManageComponent dietologist permissions', () => {
    it('autosaves permissions when a relationship toggle changes', async () => {
        await createComponentAsync({
            invitationId: entityId<'dietologist-invitation'>('inv-1'),
            status: 'Accepted',
            email: 'diet@example.com',
            firstName: null,
            lastName: null,
            dietologistUserId: entityId<'user'>('diet-1'),
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
            createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
            expiresAtUtc: utcInstant('2026-04-22T00:00:00Z'),
            acceptedAtUtc: utcInstant('2026-04-15T01:00:00Z'),
        });

        component['updateDietologistPermission']('shareFasting', false);

        expect(dietologistService.updatePermissions).toHaveBeenCalledWith({
            shareProfile: true,
            shareMeals: true,
            shareStatistics: true,
            shareWeight: true,
            shareWaist: true,
            shareGoals: true,
            shareHydration: true,
            shareFasting: false,
        });
    });

    it('updates relationship permissions without reloading the dietologist section', async () => {
        await createComponentAsync({
            invitationId: entityId<'dietologist-invitation'>('inv-1'),
            status: 'Accepted',
            email: 'diet@example.com',
            firstName: null,
            lastName: null,
            dietologistUserId: entityId<'user'>('diet-1'),
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
            createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
            expiresAtUtc: utcInstant('2026-04-22T00:00:00Z'),
            acceptedAtUtc: utcInstant('2026-04-15T01:00:00Z'),
        });

        component['updateDietologistPermission']('shareMeals', false);

        expect(dietologistService.getRelationship).not.toHaveBeenCalled();
        expect(facade.dietologistRelationship()?.permissions.shareMeals).toBe(false);
        expect(component['isLoadingDietologist']()).toBe(false);
    });
});

describe('UserManageComponent dietologist disconnect', () => {
    it('asks for confirmation before disconnecting an accepted dietologist relationship', async () => {
        await createComponentAsync(
            {
                invitationId: entityId<'dietologist-invitation'>('inv-1'),
                status: 'Accepted',
                email: 'diet@example.com',
                firstName: null,
                lastName: null,
                dietologistUserId: entityId<'user'>('diet-1'),
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
                createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
                expiresAtUtc: utcInstant('2026-04-22T00:00:00Z'),
                acceptedAtUtc: utcInstant('2026-04-15T01:00:00Z'),
            },
            false,
        );

        component['revokeDietologistRelationship']();

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(dietologistService.revokeRelationship).not.toHaveBeenCalled();
    });

    it('disconnects after confirmation for an accepted relationship', async () => {
        await createComponentAsync(
            {
                invitationId: entityId<'dietologist-invitation'>('inv-1'),
                status: 'Accepted',
                email: 'diet@example.com',
                firstName: null,
                lastName: null,
                dietologistUserId: entityId<'user'>('diet-1'),
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
                createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
                expiresAtUtc: utcInstant('2026-04-22T00:00:00Z'),
                acceptedAtUtc: utcInstant('2026-04-15T01:00:00Z'),
            },
            true,
        );

        component['revokeDietologistRelationship']();

        expect(dietologistService.revokeRelationship).toHaveBeenCalledTimes(1);
    });
});

describe('Profile dietologist writes during notification refresh', () => {
    it('keeps a saved permission when an older notification refresh completes later', async () => {
        const original = dietologistRelationship('Accepted');
        const saved = { ...original, permissions: { ...original.permissions, shareMeals: false } };
        await createComponentAsync(original);
        const card = openDietologistCard();
        const meals = 'button[role="switch"][aria-label="USER_MANAGE.DIETOLOGIST_PERMISSION_MEALS"]';
        const save = new Subject<void>();
        const stale = new Subject<DietologistRelationship | null>();
        dietologistService.updatePermissions.mockReturnValueOnce(save);

        dietologistButton(card, meals).click();
        expect(dietologistService.updatePermissions).toHaveBeenCalledExactlyOnceWith(saved.permissions);
        startDietologistRelationshipRefresh(stale);
        dietologistService.getRelationship.mockReturnValueOnce(of(saved));
        expect(card.textContent).toContain('USER_MANAGE.DIETOLOGIST_LOADING');

        save.next();
        save.complete();
        stale.next(original);
        stale.complete();
        fixture.detectChanges();

        expect(dietologistButton(card, meals).getAttribute('aria-checked')).toBe('false');
        expect(dietologistError(card)).toBeNull();
    });

    it('restores the latest known permissions when a save fails after a notification refresh', async () => {
        const original = dietologistRelationship('Accepted');
        const updated = { ...original, permissions: { ...original.permissions, shareWeight: false } };
        await createComponentAsync(original);
        const card = openDietologistCard();
        const meals = 'button[role="switch"][aria-label="USER_MANAGE.DIETOLOGIST_PERMISSION_MEALS"]';
        const weight = 'button[role="switch"][aria-label="USER_MANAGE.DIETOLOGIST_PERMISSION_WEIGHT"]';
        const save = new Subject<void>();
        dietologistService.updatePermissions.mockReturnValueOnce(save);

        dietologistButton(card, meals).click();
        await refreshDietologistRelationshipAsync(updated);
        expect(dietologistButton(card, weight).getAttribute('aria-checked')).toBe('false');

        save.error(new Error('Permission service temporarily unavailable'));
        fixture.detectChanges();

        expect(dietologistButton(card, meals).getAttribute('aria-checked')).toBe('true');
        expect(dietologistButton(card, weight).getAttribute('aria-checked')).toBe('false');
        expect(dietologistError(card)).toBe('USER_MANAGE.DIETOLOGIST_PERMISSIONS_ERROR');
    });

    it('keeps the invitation form visible when an older notification refresh completes after disconnect', async () => {
        const original = dietologistRelationship('Accepted');
        await createComponentAsync(original, true);
        const card = openDietologistCard();
        const action = '.user-manage__dietologist-summary-button button';
        const disconnect = new Subject<void>();
        const stale = new Subject<DietologistRelationship | null>();
        dietologistService.revokeRelationship.mockReturnValueOnce(disconnect);

        dietologistButton(card, action).click();
        expect(dietologistService.revokeRelationship).toHaveBeenCalledOnce();
        startDietologistRelationshipRefresh(stale);
        dietologistService.getRelationship.mockReturnValueOnce(of(null));
        expect(card.textContent).toContain('USER_MANAGE.DIETOLOGIST_LOADING');

        disconnect.next();
        disconnect.complete();
        stale.next(original);
        stale.complete();
        fixture.detectChanges();

        expect(dietologistButton(card, action).textContent).toContain('USER_MANAGE.DIETOLOGIST_INVITE_ACTION');
        expect(card.querySelector<HTMLInputElement>('input[type="email"]')?.disabled).toBe(false);
        expect(dietologistError(card)).toBeNull();
    });
});

describe('Profile dietologist requests after the relationship changes', () => {
    it.each(['success', 'error'] as const)('keeps the current permissions after the previous save returns %s', async outcome => {
        const original = dietologistRelationship('Accepted');
        await createComponentAsync(original);
        const card = openDietologistCard();
        const meals = 'button[role="switch"][aria-label="USER_MANAGE.DIETOLOGIST_PERMISSION_MEALS"]';
        const weight = 'button[role="switch"][aria-label="USER_MANAGE.DIETOLOGIST_PERMISSION_WEIGHT"]';
        const pending = new Subject<void>();
        dietologistService.updatePermissions.mockReturnValueOnce(pending);

        dietologistButton(card, meals).click();
        expect(dietologistService.updatePermissions).toHaveBeenCalledExactlyOnceWith({ ...original.permissions, shareMeals: false });
        const replacement = replacementDietologistRelationship();
        await refreshDietologistRelationshipAsync(replacement);
        expect(dietologistButton(card, meals).getAttribute('aria-checked')).toBe('true');
        expect(dietologistButton(card, weight).getAttribute('aria-checked')).toBe('false');

        if (outcome === 'success') {
            pending.next();
            pending.complete();
        } else {
            pending.error(new Error('Previous permission save failed'));
        }
        fixture.detectChanges();

        expect(facade.dietologistRelationship()).toEqual(replacement);
        expect(dietologistButton(card, meals).getAttribute('aria-checked')).toBe('true');
        expect(dietologistButton(card, weight).getAttribute('aria-checked')).toBe('false');
        expect(dietologistError(card)).toBeNull();
    });

    it.each([
        { action: 'disconnect', selector: '.user-manage__dietologist-summary-button button' },
        { action: 'disable profile sharing', selector: 'button[role="switch"][aria-label="USER_MANAGE.DIETOLOGIST_PERMISSION_PROFILE"]' },
    ])('keeps the replacement dietologist settings when confirming an old prompt to $action', async ({ selector }) => {
        await createComponentAsync(dietologistRelationship('Accepted'));
        const card = openDietologistCard();
        const confirmation = new Subject<boolean>();
        dialogService.open.mockReturnValueOnce({ afterClosed: (): Observable<boolean> => confirmation.asObservable() });

        dietologistButton(card, selector).click();
        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(dietologistService.revokeRelationship).not.toHaveBeenCalled();
        const replacement = replacementDietologistRelationship();
        await refreshDietologistRelationshipAsync(replacement);
        confirmation.next(true);
        confirmation.complete();
        fixture.detectChanges();

        expect(facade.dietologistRelationship()).toEqual(replacement);
        expect(dietologistService.revokeRelationship).not.toHaveBeenCalled();
        expect(dietologistService.updatePermissions).not.toHaveBeenCalled();
        expect(
            dietologistButton(card, 'button[role="switch"][aria-label="USER_MANAGE.DIETOLOGIST_PERMISSION_PROFILE"]').getAttribute(
                'aria-checked',
            ),
        ).toBe('true');
    });

    it.each(['success', 'error'] as const)('keeps the current relationship after the previous disconnect returns %s', async outcome => {
        await createComponentAsync(dietologistRelationship('Accepted'), true);
        const card = openDietologistCard();
        const pending = new Subject<void>();
        dietologistService.revokeRelationship.mockReturnValueOnce(pending);

        dietologistButton(card, '.user-manage__dietologist-summary-button button').click();
        expect(dietologistService.revokeRelationship).toHaveBeenCalledOnce();
        const replacement = replacementDietologistRelationship();
        await refreshDietologistRelationshipAsync(replacement);

        if (outcome === 'success') {
            pending.next();
            pending.complete();
        } else {
            pending.error(new Error('Previous disconnect failed'));
        }
        fixture.detectChanges();

        expect(facade.dietologistRelationship()).toEqual(replacement);
        expect(dietologistError(card)).toBeNull();
        expect(dietologistButton(card, '.user-manage__dietologist-summary-button button').textContent).toContain(
            'USER_MANAGE.DIETOLOGIST_DISCONNECT_ACTION',
        );
    });
});

function replacementDietologistRelationship(): DietologistRelationship {
    const relationship = dietologistRelationship('Accepted');
    return {
        ...relationship,
        invitationId: entityId<'dietologist-invitation'>('replacement-dietologist'),
        dietologistUserId: entityId<'user'>('replacement-dietologist-user'),
        firstName: 'New dietologist',
        permissions: { ...relationship.permissions, shareWeight: false },
    };
}

async function refreshDietologistRelationshipAsync(relationship: DietologistRelationship): Promise<void> {
    startDietologistRelationshipRefresh(of(relationship));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(facade.dietologistRelationship()).toEqual(relationship);
}

function startDietologistRelationshipRefresh(response: Observable<DietologistRelationship | null>): void {
    dietologistService.getRelationship.mockReturnValueOnce(response);
    notificationService.notificationsChangedVersion.update(version => version + 1);
    fixture.detectChanges();
}

describe('Profile pending dietologist invitation recovery', () => {
    it('keeps the invitation after cancellation fails, then returns to a clean invitation form on retry', async () => {
        await createComponentAsync(dietologistRelationship('Pending'));
        const card = openDietologistCard();
        const revoke = dietologistButton(card, '.user-manage__dietologist-summary-button button');
        const pendingCancel = new Subject<void>();
        dietologistService.revokeRelationship.mockReturnValueOnce(pendingCancel);
        expect(revoke.textContent).toContain('USER_MANAGE.DIETOLOGIST_CANCEL_INVITE');
        expect(card.querySelector<HTMLInputElement>('input[type="email"]')?.disabled).toBe(true);

        revoke.click();
        expect(revoke.disabled).toBe(false);
        revoke.click();
        expect(dietologistService.revokeRelationship).toHaveBeenCalledOnce();

        fixture.detectChanges();
        expect(dialogService.open).not.toHaveBeenCalled();
        expect(revoke.disabled).toBe(true);
        pendingCancel.error(new Error('Cancellation temporarily unavailable'));
        fixture.detectChanges();
        expect(dietologistError(card)).toBe('USER_MANAGE.DIETOLOGIST_DISCONNECT_ERROR');
        expect(card.textContent).toContain('USER_MANAGE.DIETOLOGIST_STATUS_PENDING');
        expect(revoke.disabled).toBe(false);

        revoke.click();
        fixture.detectChanges();
        expect(dietologistService.revokeRelationship).toHaveBeenCalledTimes(2);
        expect(revoke.textContent).toContain('USER_MANAGE.DIETOLOGIST_INVITE_ACTION');
        expect(card.querySelector<HTMLInputElement>('input[type="email"]')?.disabled).toBe(false);
        expect(dietologistError(card)).toBeNull();
    });

    it('replaces a failed relationship refresh with the accepted relationship when a new notification arrives', async () => {
        await createComponentAsync(dietologistRelationship('Pending'));
        const card = openDietologistCard();
        const pendingRefresh = new Subject<DietologistRelationship | null>();
        const accepted = dietologistRelationship('Accepted');
        dietologistService.getRelationship.mockReturnValueOnce(pendingRefresh).mockReturnValueOnce(of(accepted));

        notificationService.notificationsChangedVersion.update(version => version + 1);
        fixture.detectChanges();
        expect(card.textContent).toContain('USER_MANAGE.DIETOLOGIST_LOADING');
        expect(card.querySelector('.user-manage__dietologist-summary-button button')).toBeNull();
        pendingRefresh.error(new Error('Relationship temporarily unavailable'));
        fixture.detectChanges();
        expect(dietologistError(card)).toBe('USER_MANAGE.DIETOLOGIST_LOAD_ERROR');
        expect(card.textContent).toContain('USER_MANAGE.DIETOLOGIST_STATUS_PENDING');

        notificationService.notificationsChangedVersion.update(version => version + 1);
        fixture.detectChanges();
        expect(dietologistService.getRelationship).toHaveBeenCalledTimes(2);
        expect(dietologistError(card)).toBeNull();
        expect(card.textContent).toContain('USER_MANAGE.DIETOLOGIST_CONNECTED_HINT');
        expect(card.textContent).not.toContain('USER_MANAGE.DIETOLOGIST_STATUS_PENDING');
    });
});

describe('Profile dietologist client profile sharing', () => {
    it('keeps sharing active if confirmation is canceled or saving fails and lets sharing be turned off and restored', async () => {
        const relationship = dietologistRelationship('Accepted');
        await createComponentAsync(relationship);
        const card = openDietologistCard();
        const share = dietologistButton(card, 'button[role="switch"][aria-label="USER_MANAGE.DIETOLOGIST_PERMISSION_PROFILE"]');
        expect(share.getAttribute('aria-checked')).toBe('true');

        share.click();
        fixture.detectChanges();
        expect(dialogService.open).toHaveBeenCalledExactlyOnceWith(FdUiConfirmDialogComponent, {
            preset: 'confirm',
            data: {
                title: 'USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_TITLE',
                message: 'USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_MESSAGE',
                confirmLabel: 'USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_CONFIRM',
                cancelLabel: 'USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_CANCEL',
            },
        });
        expect(share.getAttribute('aria-checked')).toBe('true');
        expect(dietologistService.updatePermissions).not.toHaveBeenCalled();

        const confirmation = new Subject<boolean>();
        const save = new Subject<void>();
        dialogService.open.mockReturnValueOnce({ afterClosed: (): Observable<boolean> => confirmation.asObservable() });
        dietologistService.updatePermissions.mockReturnValueOnce(save);
        share.click();
        expect(dietologistService.updatePermissions).not.toHaveBeenCalled();
        confirmation.next(true);
        confirmation.complete();
        expect(share.disabled).toBe(false);
        share.click();
        expect(dialogService.open).toHaveBeenCalledTimes(2);

        fixture.detectChanges();
        expect(dietologistService.updatePermissions).toHaveBeenCalledExactlyOnceWith({ ...relationship.permissions, shareProfile: false });
        expect(share.getAttribute('aria-checked')).toBe('false');
        expect(share.disabled).toBe(true);

        save.error(new Error('Permission service temporarily unavailable'));
        fixture.detectChanges();
        expect(share.getAttribute('aria-checked')).toBe('true');
        expect(share.disabled).toBe(false);
        expect(dietologistError(card)).toBe('USER_MANAGE.DIETOLOGIST_PERMISSIONS_ERROR');

        dialogService.open.mockReturnValueOnce({ afterClosed: (): Observable<boolean> => of(true) });
        share.click();
        fixture.detectChanges();
        expect(share.getAttribute('aria-checked')).toBe('false');
        expect(dietologistError(card)).toBeNull();
        share.click();
        fixture.detectChanges();
        expect(share.getAttribute('aria-checked')).toBe('true');
        expect(dietologistService.updatePermissions).toHaveBeenLastCalledWith(relationship.permissions);
    });

    it('changes invitation draft permissions without contacting an existing dietologist', async () => {
        await createComponentAsync(null, true);
        const card = openDietologistCard();
        const share = dietologistButton(card, 'button[role="switch"][aria-label="USER_MANAGE.DIETOLOGIST_PERMISSION_PROFILE"]');
        const meals = dietologistButton(card, 'button[role="switch"][aria-label="USER_MANAGE.DIETOLOGIST_PERMISSION_MEALS"]');
        share.click();
        meals.click();
        fixture.detectChanges();
        expect(share.getAttribute('aria-checked')).toBe('false');
        expect(meals.getAttribute('aria-checked')).toBe('false');

        share.click();
        fixture.detectChanges();
        expect(share.getAttribute('aria-checked')).toBe('true');
        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(dietologistService.updatePermissions).not.toHaveBeenCalled();
    });
});

describe('Profile dietologist invitation draft controls', () => {
    it('keeps the permission draft stable and sends only one invitation before the controls lock', async () => {
        await createComponentAsync(null);
        const card = openDietologistCard();
        const email = card.querySelector<HTMLInputElement>('input[type="email"]');
        if (email === null) {
            throw new Error('Expected the dietologist invitation email field');
        }
        email.value = 'diet@example.com';
        email.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        const shareMeals = dietologistButton(card, 'button[role="switch"][aria-label="USER_MANAGE.DIETOLOGIST_PERMISSION_MEALS"]');
        const invite = dietologistButton(card, '.user-manage__dietologist-summary-button button');
        const permissions = dietologistRelationship('Pending').permissions;
        const pending = new Subject<void>();
        dietologistService.invite.mockReturnValueOnce(pending);
        invite.click();
        expect(invite.disabled || shareMeals.disabled).toBe(false);
        invite.click();
        shareMeals.click();
        expect(dietologistService.invite).toHaveBeenCalledExactlyOnceWith({ dietologistEmail: email.value, permissions });

        fixture.detectChanges();

        expect(invite.disabled).toBe(true);
        expect(shareMeals.disabled).toBe(true);
        expect(shareMeals.getAttribute('aria-checked')).toBe('true');

        pending.error(new Error('Invitation temporarily unavailable'));
        fixture.detectChanges();
        shareMeals.click();
        fixture.detectChanges();
        expect(shareMeals.getAttribute('aria-checked')).toBe('false');
        expect(dietologistService.updatePermissions).not.toHaveBeenCalled();
    });

    it('retranslates an invalid invitation email without losing the draft or sending it', async () => {
        await createComponentAsync(null);
        const translate = TestBed.inject(TranslateService);
        vi.spyOn(translate, 'instant').mockRestore();
        const emailErrorEn = 'Incorrect email';
        const emailErrorRu = 'Некорректный адрес электронной почты';
        translate.setTranslation('en', { FORM_ERRORS: { EMAIL: emailErrorEn } });
        translate.setTranslation('ru', { FORM_ERRORS: { EMAIL: emailErrorRu } });
        const card = openDietologistCard();
        const email = card.querySelector<HTMLInputElement>('input[type="email"]');
        if (email === null) {
            throw new Error('Expected the dietologist invitation email field');
        }
        email.value = 'invalid-address';
        email.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        dietologistButton(card, '.user-manage__dietologist-summary-button button').click();
        fixture.detectChanges();

        expect(card.querySelector('[role="alert"]')?.textContent).toContain(emailErrorEn);
        expect(document.activeElement).toBe(email);
        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();

        expect(card.querySelector('[role="alert"]')?.textContent).toContain(emailErrorRu);
        expect(email.value).toBe('invalid-address');
        expect(dietologistService.invite).not.toHaveBeenCalled();
    });
});

describe('Profile dietologist relationship without contact details', () => {
    it('keeps the connected permissions editable when the dietologist has no exposed email', async () => {
        const relationship: DietologistRelationship = { ...dietologistRelationship('Accepted'), email: null };
        await createComponentAsync(relationship);
        const card = openDietologistCard();
        const shareMeals = dietologistButton(card, 'button[role="switch"][aria-label="USER_MANAGE.DIETOLOGIST_PERMISSION_MEALS"]');
        expect(card.querySelector('input[type="email"]')).toBeNull();
        expect(dietologistButton(card, '.user-manage__dietologist-summary-button button').textContent).toContain(
            'USER_MANAGE.DIETOLOGIST_DISCONNECT_ACTION',
        );

        shareMeals.click();
        fixture.detectChanges();
        expect(dietologistService.updatePermissions).toHaveBeenCalledExactlyOnceWith({ ...relationship.permissions, shareMeals: false });
        expect(shareMeals.getAttribute('aria-checked')).toBe('false');
        expect(dietologistError(card)).toBeNull();
    });
});
function dietologistRelationship(status: 'Accepted' | 'Pending'): DietologistRelationship {
    const accepted = status === 'Accepted';
    return {
        invitationId: entityId<'dietologist-invitation'>('dietologist-relationship'),
        status,
        email: 'diet@example.com',
        firstName: accepted ? 'Anne' : null,
        lastName: null,
        dietologistUserId: accepted ? entityId<'user'>('dietologist-user') : null,
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
        createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
        expiresAtUtc: utcInstant('2026-04-22T00:00:00Z'),
        acceptedAtUtc: accepted ? utcInstant('2026-04-15T01:00:00Z') : null,
    };
}

function openDietologistCard(): HTMLElement {
    const host = fixture.nativeElement as HTMLElement;
    const card = host.querySelector('fd-user-manage-dietologist-card');
    const section = card?.closest('details')?.querySelector('summary');
    if (!(card instanceof HTMLElement) || !(section instanceof HTMLElement)) {
        throw new Error('Expected the dietologist details section');
    }
    section.click();
    fixture.detectChanges();
    return card;
}

function dietologistButton(card: HTMLElement, selector: string): HTMLButtonElement {
    const button = card.querySelector(selector);
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error(`Expected the dietologist control ${selector}`);
    }
    return button;
}

function dietologistError(card: HTMLElement): string | null {
    return card.querySelector('.user-manage__error--dietologist [role="alert"]')?.textContent.trim() ?? null;
}

describe('UserManageComponent notification relationship refresh', () => {
    it('reloads dietologist relationship when notifications realtime changes', async () => {
        await createComponentAsync(null);
        expect(dietologistService.getRelationship).toHaveBeenCalledTimes(0);

        notificationService.notificationsChangedVersion.set(1);
        fixture.detectChanges();

        expect(dietologistService.getRelationship).toHaveBeenCalledTimes(1);
    });
});

describe('UserManageComponent browser unload protection', () => {
    it('guards browser unload only while profile edits remain unsaved', async () => {
        await createComponentAsync(null);

        const cleanUnload = new Event('beforeunload', { cancelable: true });
        window.dispatchEvent(cleanUnload);
        expect(cleanUnload.defaultPrevented).toBe(false);

        component['userForm'].firstName().value.set('Unsaved name');
        fixture.detectChanges();
        const dirtyUnload = new Event('beforeunload', { cancelable: true });
        window.dispatchEvent(dirtyUnload);
        expect(dirtyUnload.defaultPrevented).toBe(true);
        expect(facade.saveProfileAsync).not.toHaveBeenCalled();

        component['discardUserFormChanges']();
        fixture.detectChanges();
        const discardedUnload = new Event('beforeunload', { cancelable: true });
        window.dispatchEvent(discardedUnload);
        expect(discardedUnload.defaultPrevented).toBe(false);
    });
});

describe('UserManageComponent profile height validation', () => {
    it.each([0, -1, ABOVE_MAX_PROFILE_HEIGHT])('blocks saving height %s outside the API range', async heightCm => {
        await createComponentAsync(null);
        component['userForm'].heightCm().value.set(heightCm);
        fixture.detectChanges();

        await component['onSubmit']();

        expect(component['userForm']().invalid()).toBe(true);
        expect(facade.saveProfileAsync).not.toHaveBeenCalled();
        const button = (fixture.nativeElement as HTMLElement).querySelector('fd-unsaved-changes-bar fd-ui-button:last-child button');
        expect(button?.hasAttribute('disabled')).toBe(false);
    });

    it.each([null, FRACTIONAL_PROFILE_HEIGHT, MAX_PROFILE_HEIGHT])('allows saving optional or valid height %s', async heightCm => {
        await createComponentAsync(null);
        component['userForm'].heightCm().value.set(heightCm);
        fixture.detectChanges();

        await component['onSubmit']();

        expect(component['userForm']().invalid()).toBe(false);
        expect(facade.saveProfileAsync).toHaveBeenCalledTimes(1);
    });
});

describe('UserManageComponent imperial height validation', () => {
    it('validates the entered imperial height instead of retaining the previous value', async () => {
        await createComponentAsync(null);
        component['userForm'].heightCm().value.set(INITIAL_PROFILE_HEIGHT);
        fixture.detectChanges();
        const widgets = fixture.debugElement.query(By.directive(UserManageComparisonWidgetsComponent))
            .componentInstance as UserManageComparisonWidgetsComponent;

        widgets['onImperialHeightChange']('feet', INVALID_HEIGHT_FEET);
        fixture.detectChanges();
        await component['onSubmit']();

        expect(component['userForm'].heightCm().value()).toBe(CONVERTED_INVALID_HEIGHT);
        expect(component['userForm']().invalid()).toBe(true);
        expect(facade.saveProfileAsync).not.toHaveBeenCalled();
    });
});

describe('UserManageComponent invalid birthday drafts', () => {
    it('retains the last valid birthday when manual input becomes invalid', async () => {
        await createComponentAsync(null);
        editProfileBirthDate(VALID_PROFILE_BIRTH_DATE);
        editProfileBirthDate(FUTURE_PROFILE_BIRTH_DATE);

        await component['onSubmit']();

        expect(component['userForm'].birthDate().value()).toBe(VALID_PROFILE_BIRTH_DATE);
        expect(facade.saveProfileAsync).not.toHaveBeenCalled();
    });

    it('blocks saving another edited field while the birthday input is invalid', async () => {
        await createComponentAsync(null);
        editProfileBirthDate(FUTURE_PROFILE_BIRTH_DATE);
        component['userForm'].firstName().value.set('Edited name');
        fixture.detectChanges();

        await component['onSubmit']();

        expect(component['userForm']().invalid()).toBe(true);
        expect(facade.saveProfileAsync).not.toHaveBeenCalled();
        expect(profileSaveButton()?.disabled).toBe(false);
    });

    it('guards an invalid birthday draft even when no other field changes', async () => {
        await createComponentAsync(null);
        editProfileBirthDate(FUTURE_PROFILE_BIRTH_DATE);

        expect(component['hasUnsavedProfileChanges']()).toBe(true);
        expect(profileSaveButton()?.disabled).toBe(false);
    });

    it('discards the invalid manual draft and its error', async () => {
        await createComponentAsync(null);
        editProfileBirthDate(FUTURE_PROFILE_BIRTH_DATE);

        component['discardUserFormChanges']();
        fixture.detectChanges();

        expect(profileBirthDateInput().value).toBe('');
        expect(profileBirthDateInput().getAttribute('aria-invalid')).toBeNull();
        expect(component['hasUnsavedProfileChanges']()).toBe(false);
    });

    it('allows saving after the invalid birthday is corrected', async () => {
        await createComponentAsync(null);
        editProfileBirthDate(FUTURE_PROFILE_BIRTH_DATE);
        editProfileBirthDate(VALID_PROFILE_BIRTH_DATE);

        await component['onSubmit']();

        expect(component['userForm']().invalid()).toBe(false);
        expect(component['userForm'].birthDate().value()).toBe(VALID_PROFILE_BIRTH_DATE);
        expect(facade.saveProfileAsync).toHaveBeenCalledTimes(1);
    });
});

describe('UserManageComponent invalid navigation saves', () => {
    it.each(['height', 'birthDate'])('refuses save-and-leave with invalid %s', async field => {
        await createComponentAsync(null);
        if (field === 'height') {
            component['userForm'].heightCm().value.set(0);
            fixture.detectChanges();
        } else {
            editProfileBirthDate(FUTURE_PROFILE_BIRTH_DATE);
        }

        expect(TestBed.inject(UnsavedChangesService).getHandler()?.save()).toBe(false);
        expect(facade.saveProfileAsync).not.toHaveBeenCalled();
    });
});

function profileBirthDateInput(): HTMLInputElement {
    const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('fd-ui-date-input input');
    if (input === null) {
        throw new Error('Expected profile birthday input');
    }
    return input;
}

function editProfileBirthDate(value: string): void {
    const input = profileBirthDateInput();
    input.value = new Intl.DateTimeFormat(TestBed.inject(TranslateService).getCurrentLang() ?? 'en', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
    }).format(new Date(`${value}T00:00:00`));
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
}

function profileSaveButton(): HTMLButtonElement | null {
    return (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('fd-unsaved-changes-bar fd-ui-button:last-child button');
}

describe('UserManageComponent save-and-leave completion', () => {
    it.each([false, true])('waits for save-and-leave completion and returns the save result %s', async saved => {
        await createComponentAsync(null);
        component['userForm'].firstName().value.set('QA navigation draft');
        fixture.detectChanges();
        let finishSave: ((value: boolean) => void) | undefined;
        const pendingSave = new Promise<boolean>(resolve => {
            finishSave = resolve;
        });
        facade.saveProfileAsync.mockReturnValueOnce(pendingSave);
        let settled = false;

        const saving = TestBed.inject(UnsavedChangesService).getHandler()?.save();
        if (!(saving instanceof Promise)) {
            throw new Error('Expected asynchronous profile save');
        }
        void saving.then(() => {
            settled = true;
        });
        await fixture.whenStable();

        expect(settled).toBe(false);
        expect(component['hasUnsavedProfileChanges']()).toBe(true);
        expect(component['userForm'].firstName().value()).toBe('QA navigation draft');
        expect(facade.saveProfileAsync).toHaveBeenCalledOnce();

        finishSave?.(saved);
        expect(await saving).toBe(saved);
        if (!saved) {
            expect(component['hasUnsavedProfileChanges']()).toBe(true);
            expect(component['userForm'].firstName().value()).toBe('QA navigation draft');
        }
    });
});

describe('UserManageComponent edits during profile saving', () => {
    it('preserves newer edits and discards them to the last successful save', async () => {
        await createComponentAsync(null, false, profileSaveUser());
        const finishSave = deferProfileSave({ heightCm: SUBMITTED_SAVE_HEIGHT });
        component['onUserFormPatch']({ heightCm: SUBMITTED_SAVE_HEIGHT });

        const saving = TestBed.inject(UnsavedChangesService).getHandler()?.save();
        component['onUserFormPatch']({ heightCm: INITIAL_PROFILE_HEIGHT });
        finishSave(true);
        const saved = await saving;
        fixture.detectChanges();
        await fixture.whenStable();

        expect(saved).toBe(false);
        expect(component['userForm'].heightCm().value()).toBe(INITIAL_PROFILE_HEIGHT);
        expect(component['hasUnsavedProfileChanges']()).toBe(true);
        component['discardUserFormChanges']();
        expect(component['userForm'].heightCm().value()).toBe(SUBMITTED_SAVE_HEIGHT);
        expect(component['hasUnsavedProfileChanges']()).toBe(false);
    });

    it('rejects a duplicate submit without replacing the active save snapshot', async () => {
        await createComponentAsync(null, false, profileSaveUser());
        const finishSave = deferProfileSave({ firstName: 'Saved' });
        component['onUserFormPatch']({ firstName: 'Saved' });

        const saving = component['onSubmit']();
        const duplicate = component['onSubmit']();
        finishSave(true);
        const saved = await saving;
        fixture.detectChanges();
        await fixture.whenStable();

        expect(duplicate).toBe(false);
        expect(facade.saveProfileAsync).toHaveBeenCalledOnce();
        expect(saved).toBe(true);
        expect(component['hasUnsavedProfileChanges']()).toBe(false);
    });

    it('applies authoritative normalization when no newer edits exist', async () => {
        await createComponentAsync(null, false, profileSaveUser());
        const finishSave = deferProfileSave({ firstName: 'Saved' });
        component['onUserFormPatch']({ firstName: '  Saved  ' });

        const saving = TestBed.inject(UnsavedChangesService).getHandler()?.save();
        finishSave(true);
        fixture.detectChanges();
        const saved = await saving;
        fixture.detectChanges();
        await fixture.whenStable();

        expect(saved).toBe(true);
        expect(component['userForm'].firstName().value()).toBe('Saved');
        expect(component['hasUnsavedProfileChanges']()).toBe(false);
    });

    it('keeps the newer draft and original discard baseline when saving fails', async () => {
        await createComponentAsync(null, false, profileSaveUser());
        const finishSave = deferProfileSave({ firstName: 'Saved' });
        component['onUserFormPatch']({ firstName: 'Saved' });

        const saving = component['onSubmit']();
        component['onUserFormPatch']({ firstName: 'Newer draft' });
        finishSave(false);
        const saved = await saving;
        fixture.detectChanges();
        await fixture.whenStable();

        expect(saved).toBe(false);
        expect(component['userForm'].firstName().value()).toBe('Newer draft');
        expect(component['hasUnsavedProfileChanges']()).toBe(true);
        component['discardUserFormChanges']();
        expect(component['userForm'].firstName().value()).toBe('Before');
        expect(component['hasUnsavedProfileChanges']()).toBe(false);
    });
});

function profileSaveUser(): User {
    return {
        id: entityId<'user'>('u1'),
        email: 'user@example.com',
        hasPassword: true,
        firstName: 'Before',
        heightCm: ORIGINAL_SAVE_HEIGHT,
        pushNotificationsEnabled: true,
        fastingPushNotificationsEnabled: true,
        socialPushNotificationsEnabled: false,
        fastingCheckInReminderHours: DEFAULT_FASTING_CHECK_IN_REMINDER_HOURS,
        fastingCheckInFollowUpReminderHours: DEFAULT_FASTING_CHECK_IN_FOLLOW_UP_REMINDER_HOURS,
        isActive: true,
        isEmailConfirmed: true,
    };
}

function deferProfileSave(savedFields: Partial<User>): (saved: boolean) => void {
    let finishSave: ((saved: boolean) => void) | undefined;
    const pendingSave = new Promise<boolean>(resolve => {
        finishSave = resolve;
    });
    facade.saveProfileAsync.mockImplementationOnce(async () => {
        facade.isSavingProfile.set(true);
        const saved = await pendingSave;
        if (saved) {
            facade.user.update(current => (current === null ? current : { ...current, ...savedFields }));
            facade.profileSavedVersion.update(version => version + 1);
        }
        facade.isSavingProfile.set(false);
        return saved;
    });
    return saved => finishSave?.(saved);
}

describe('UserManageComponent explicit profile save feedback', () => {
    it('should prevent native profile form submit when saving now', async () => {
        await createComponentAsync(null);

        const form = (fixture.nativeElement as HTMLElement).querySelector('form');
        expect(form).not.toBeNull();

        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = form?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(facade.saveProfileAsync).toHaveBeenCalledTimes(1);
    });

    it('does not save when editable user fields change', async () => {
        await createComponentAsync(null);

        component['userForm'].firstName().markAsDirty();
        component['userForm'].firstName().value.set('Alex');
        fixture.detectChanges();

        expect(facade.saveProfileAsync).not.toHaveBeenCalled();
        expect(component['hasUnsavedProfileChanges']()).toBe(true);
    });

    it('clears unsaved state after the profile save succeeds', async () => {
        await createComponentAsync(null, false, {
            id: entityId<'user'>('u1'),
            email: 'user@example.com',
            hasPassword: true,
            firstName: 'Before',
            pushNotificationsEnabled: true,
            fastingPushNotificationsEnabled: true,
            socialPushNotificationsEnabled: false,
            fastingCheckInReminderHours: 4,
            fastingCheckInFollowUpReminderHours: 8,
            isActive: true,
            isEmailConfirmed: true,
        });

        component['userForm'].firstName().value.set('Alex');
        fixture.detectChanges();
        expect(component['hasUnsavedProfileChanges']()).toBe(true);

        facade.user.update(current => (current === null ? current : { ...current, firstName: 'Alex' }));
        facade.profileSavedVersion.update(version => version + 1);
        fixture.detectChanges();
        await fixture.whenStable();

        expect(component['hasUnsavedProfileChanges']()).toBe(false);
        const savedUnload = new Event('beforeunload', { cancelable: true });
        window.dispatchEvent(savedUnload);
        expect(savedUnload.defaultPrevented).toBe(false);
    });

    it('ignores bubbled input events outside profile fields', async () => {
        await createComponentAsync(null);
        const unrelatedInput = document.createElement('input');
        unrelatedInput.value = 'ignored';
        const inputEvent = new Event('input', { bubbles: true });
        Object.defineProperty(inputEvent, 'target', { value: unrelatedInput });

        component['onUserFormInput'](inputEvent);

        expect(facade.saveProfileAsync).not.toHaveBeenCalled();
    });

    it('keeps select changes local until save', async () => {
        await createComponentAsync(null, false, {
            id: entityId<'user'>('u1'),
            email: 'user@example.com',
            hasPassword: true,
            gender: 'M',
            theme: 'dark',
            uiStyle: 'modern',
            pushNotificationsEnabled: true,
            fastingPushNotificationsEnabled: true,
            socialPushNotificationsEnabled: false,
            fastingCheckInReminderHours: 4,
            fastingCheckInFollowUpReminderHours: 8,
            isActive: true,
            isEmailConfirmed: true,
        });

        component['onUserFormPatch']({ gender: Gender.Female });

        expect(facade.saveProfileAsync).not.toHaveBeenCalled();
        expect(component['userForm'].gender().value()).toBe('F');
        expect(component['hasUnsavedProfileChanges']()).toBe(true);
    });
});

describe('UserManageComponent profile normalization and intents', () => {
    it('shows Google linking success and clears the transient query parameter', async () => {
        await createComponentAsync(null, false, null, { googleLink: 'success' });

        expect(toastService.success).toHaveBeenCalledWith('USER_MANAGE.GOOGLE_LINK_SUCCESS');
        expect(router.navigate).toHaveBeenCalledOnce();
        const navigateOptions: unknown = router.navigate.mock.calls[0]?.[1];
        expect(navigateOptions).toMatchObject({
            queryParams: { googleLink: null },
            queryParamsHandling: 'merge',
            replaceUrl: true,
        });
    });

    it('shows Google linking failure and clears the transient query parameter', async () => {
        await createComponentAsync(null, false, null, { googleLink: 'failed' });

        expect(toastService.error).toHaveBeenCalledWith('USER_MANAGE.GOOGLE_LINK_ERROR');
    });

    it('normalizes legacy profile select values from user overview', async () => {
        await createComponentAsync(null, false, {
            id: entityId<'user'>('u1'),
            email: 'user@example.com',
            hasPassword: true,
            username: 'alexi',
            firstName: 'Alex',
            lastName: 'User',
            gender: 'female',
            language: 'en-US',
            theme: 'default',
            uiStyle: 'classic',
            pushNotificationsEnabled: true,
            fastingPushNotificationsEnabled: true,
            socialPushNotificationsEnabled: false,
            fastingCheckInReminderHours: 4,
            fastingCheckInFollowUpReminderHours: 8,
            isActive: true,
            isEmailConfirmed: true,
        });

        expect(component['userFormModel']().gender).toBe('F');
        expect(component['userFormModel']().language).toBe('en');
        expect(component['userFormModel']().theme).toBe('ocean');
        expect(component['userFormModel']().uiStyle).toBe('classic');
    });

    it('opens set password dialog from notifications intent for google-only account', async () => {
        await createComponentAsync(
            null,
            false,
            {
                id: entityId<'user'>('u1'),
                email: 'user@example.com',
                hasPassword: false,
                pushNotificationsEnabled: true,
                fastingPushNotificationsEnabled: true,
                socialPushNotificationsEnabled: true,
                fastingCheckInReminderHours: 4,
                fastingCheckInFollowUpReminderHours: 8,
                isActive: true,
                isEmailConfirmed: true,
            },
            { intent: 'set-password' },
        );

        expect(facade.openChangePasswordDialog).toHaveBeenCalledTimes(1);
        const navigateOptions = router.navigate.mock.calls[0]?.[1] as
            { queryParams?: Record<string, unknown>; queryParamsHandling?: string; replaceUrl?: boolean } | undefined;
        if (navigateOptions === undefined) {
            throw new Error('Expected router navigation options.');
        }

        expect(router.navigate.mock.calls[0]?.[0]).toEqual([]);
        expect(navigateOptions.queryParams).toEqual({ intent: null });
        expect(navigateOptions.queryParamsHandling).toBe('merge');
        expect(navigateOptions.replaceUrl).toBe(true);
    });
});

describe('Profile draft and security actions after account updates', () => {
    const profile: User = {
        id: entityId<'user'>('fbc0f99e-fca7-419c-8f6b-1eec02c19313'),
        email: 'profile@example.com',
        lastName: 'Saved surname',
        hasPassword: true,
        hasTelegramIdentity: true,
        hasGoogleIdentity: true,
        pushNotificationsEnabled: false,
        fastingPushNotificationsEnabled: false,
        socialPushNotificationsEnabled: false,
        fastingCheckInReminderHours: DEFAULT_FASTING_CHECK_IN_REMINDER_HOURS,
        fastingCheckInFollowUpReminderHours: DEFAULT_FASTING_CHECK_IN_FOLLOW_UP_REMINDER_HOURS,
        isActive: true,
        isEmailConfirmed: true,
    };

    it('keeps manual edits through a notification preference update and lets navigation discard them', async () => {
        await createComponentAsync(null, false, profile);
        const host = fixture.nativeElement as HTMLElement;
        const surname = host.querySelector<HTMLInputElement>('[data-user-field="lastName"] input');
        const navigationHandler = TestBed.inject(UnsavedChangesService).getHandler();
        if (surname === null || navigationHandler === null) {
            throw new Error('Expected the surname input and its navigation protection');
        }
        expect(surname.value).toBe('Saved surname');
        surname.value = 'Edited surname';
        surname.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        expect(navigationHandler.hasChanges()).toBe(true);

        facade.user.set({ ...profile, pushNotificationsEnabled: true });
        fixture.detectChanges();
        expect(notificationsFacade.syncFromUser).toHaveBeenLastCalledWith(expect.objectContaining({ pushNotificationsEnabled: true }));
        expect(surname.value).toBe('Edited surname');
        expect(profileSaveButton()).not.toBeNull();

        navigationHandler.discard();
        fixture.detectChanges();
        expect(surname.value).toBe('Saved surname');
        expect(navigationHandler.hasChanges()).toBe(false);
        expect(profileSaveButton()).toBeNull();
        expect(facade.saveProfileAsync).not.toHaveBeenCalled();
    });

    it('clears an old set-password intent when the account already has a password and keeps the explicit action available', async () => {
        await createComponentAsync(null, false, null, { intent: 'set-password', section: 'security' });
        expect(router.navigate).not.toHaveBeenCalled();

        facade.user.set(profile);
        fixture.detectChanges();
        expect(router.navigate).toHaveBeenCalledExactlyOnceWith(
            [],
            expect.objectContaining({ queryParams: { intent: null }, queryParamsHandling: 'merge', replaceUrl: true }),
        );
        expect(facade.openChangePasswordDialog).not.toHaveBeenCalled();

        securityButton('USER_MANAGE.CHANGE_PASSWORD').click();
        expect(facade.openChangePasswordDialog).toHaveBeenCalledOnce();
    });

    it('forwards the enabled Telegram disconnect button to the profile account flow', async () => {
        await createComponentAsync(null, false, profile);
        const disconnect = securityButton('USER_MANAGE.TELEGRAM_UNLINK');
        expect(disconnect.disabled).toBe(false);

        disconnect.click();
        expect(facade.unlinkTelegramAsync).toHaveBeenCalledOnce();
        expect(facade.openChangePasswordDialog).not.toHaveBeenCalled();
        expect(facade.saveProfileAsync).not.toHaveBeenCalled();
    });
});

function securityButton(label: string): HTMLButtonElement {
    const host = fixture.nativeElement as HTMLElement;
    const section = host.querySelector('fd-user-manage-security-card')?.closest('details');
    section?.querySelector('summary')?.click();
    fixture.detectChanges();
    const button = [...(section?.querySelectorAll('button') ?? [])].find(candidate => candidate.textContent.trim() === label);
    if (button === undefined || section?.open !== true) {
        throw new Error('Expected an action in the open security section');
    }
    return button;
}

type DietologistServiceMock = {
    getRelationship: ReturnType<typeof vi.fn>;
    invite: ReturnType<typeof vi.fn>;
    updatePermissions: ReturnType<typeof vi.fn>;
    revokeRelationship: ReturnType<typeof vi.fn>;
};

type NotificationServiceMock = {
    scheduleTestNotification: ReturnType<typeof vi.fn>;
    notificationsChangedVersion: ReturnType<typeof signal<number>>;
};

type UserManageNotificationsFacadeMock = {
    notificationPermission: ReturnType<typeof signal<NotificationPermission | 'unsupported'>>;
    notificationsChangedVersion: ReturnType<typeof signal<number>>;
    isUpdatingNotifications: ReturnType<typeof signal<boolean>>;
    isSchedulingTestNotification: ReturnType<typeof signal<boolean>>;
    pushNotificationsEnabled: ReturnType<typeof signal<boolean>>;
    fastingPushNotificationsEnabled: ReturnType<typeof signal<boolean>>;
    socialPushNotificationsEnabled: ReturnType<typeof signal<boolean>>;
    fastingCheckInReminderHours: ReturnType<typeof signal<number>>;
    fastingCheckInFollowUpReminderHours: ReturnType<typeof signal<number>>;
    fastingReminderPresets: typeof FASTING_REMINDER_PRESETS;
    pushNotificationsSupported: ReturnType<typeof signal<boolean>>;
    pushNotificationsSubscribed: ReturnType<typeof signal<boolean>>;
    pushNotificationsBusy: ReturnType<typeof signal<boolean>>;
    currentSubscriptionEndpoint: ReturnType<typeof signal<string | null>>;
    connectedDeviceItems: ReturnType<typeof signal<[]>>;
    isLoadingConnectedDevices: ReturnType<typeof signal<boolean>>;
    removingConnectedDeviceEndpoint: ReturnType<typeof signal<string | null>>;
    syncFromUser: ReturnType<typeof vi.fn>;
    togglePushNotificationsAsync: ReturnType<typeof vi.fn>;
    enableDeviceNotificationsAsync: ReturnType<typeof vi.fn>;
    toggleFastingPushNotificationsAsync: ReturnType<typeof vi.fn>;
    toggleSocialPushNotificationsAsync: ReturnType<typeof vi.fn>;
    applyFastingReminderPreset: ReturnType<typeof vi.fn>;
    onFastingReminderHoursChange: ReturnType<typeof vi.fn>;
    saveFastingReminderHoursAsync: ReturnType<typeof vi.fn>;
    scheduleTestNotification: ReturnType<typeof vi.fn>;
    removeConnectedDeviceAsync: ReturnType<typeof vi.fn>;
};

type ProfileManageFacadeMock = {
    user: WritableSignal<User | null>;
    globalError: ReturnType<typeof signal<string | null>>;
    isSavingProfile: ReturnType<typeof signal<boolean>>;
    profileSavedVersion: ReturnType<typeof signal<number>>;
    isLinkingGoogle: ReturnType<typeof signal<boolean>>;
    isUnlinkingTelegram: ReturnType<typeof signal<boolean>>;
    telegramUnlinkError: ReturnType<typeof signal<string | null>>;
    backupEmailSentTo: WritableSignal<string | null>;
    backupEmailResendAt: WritableSignal<number>;
    isRequestingBackupEmail: WritableSignal<boolean>;
    requestBackupEmailAsync: ReturnType<typeof vi.fn>;
    isUpdatingNotifications: ReturnType<typeof signal<boolean>>;
    webPushSubscriptions: WritableSignal<WebPushSubscriptionItem[]>;
    dietologistRelationship: WritableSignal<DietologistRelationship | null>;
    currentWeight: WritableSignal<number | null>;
    currentWaist: WritableSignal<number | null>;
    isLoadingWebPushSubscriptions: ReturnType<typeof signal<boolean>>;
    removingWebPushSubscriptionEndpoint: ReturnType<typeof signal<string | null>>;
    initialize: ReturnType<typeof vi.fn>;
    clearGlobalError: ReturnType<typeof vi.fn>;
    saveProfileAsync: ReturnType<typeof vi.fn<ProfileManageFacade['saveProfileAsync']>>;
    openChangePasswordDialog: ReturnType<typeof vi.fn>;
    unlinkTelegramAsync: ReturnType<typeof vi.fn<ProfileManageFacade['unlinkTelegramAsync']>>;
    linkGoogle: ReturnType<typeof vi.fn>;
    updateNotificationPreferences: ReturnType<typeof vi.fn>;
    refreshWebPushSubscriptions: ReturnType<typeof vi.fn>;
    removeWebPushSubscription: ReturnType<typeof vi.fn>;
    openAdminPanel: ReturnType<typeof vi.fn>;
};

async function createComponentAsync(
    relationship: DietologistRelationship | null,
    dialogResult = false,
    user: User | null = null,
    queryParams: Record<string, string> = {},
): Promise<void> {
    facade = createFacadeMock(relationship, user);
    dietologistService = createDietologistServiceMock(relationship);
    dialogService = createDialogServiceMock(dialogResult);
    router = {
        navigate: vi.fn().mockResolvedValue(true),
    };
    notificationService = {
        scheduleTestNotification: vi.fn().mockReturnValue(of(void 0)),
        notificationsChangedVersion: signal(0),
    };
    notificationsFacade = createUserManageNotificationsFacadeMock(notificationService.notificationsChangedVersion);
    toastService = createToastServiceMock();

    await TestBed.configureTestingModule({
        imports: [UserManageComponent],
        providers: [...createTestingProviders(queryParams), provideTranslateTesting()],
    })
        .overrideComponent(UserManageComponent, {
            remove: { providers: [ProfileManageFacade, UserManageNotificationsFacade] },
            add: {
                providers: [
                    { provide: ProfileManageFacade, useValue: facade },
                    { provide: UserManageNotificationsFacade, useValue: notificationsFacade },
                ],
            },
        })
        .compileComponents();

    configureTranslateService();

    fixture = TestBed.createComponent(UserManageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}

function createTestingProviders(queryParams: Record<string, string>): unknown[] {
    return [
        { provide: DIETOLOGIST_RELATIONSHIP_ACTIONS, useValue: dietologistService },
        { provide: AuthService, useValue: { isAdmin: vi.fn(() => false) } },
        { provide: ActivatedRoute, useValue: { queryParamMap: of(convertToParamMap(queryParams)) } },
        { provide: Router, useValue: router },
        { provide: LocalizationService, useValue: createLocalizationServiceMock() },
        { provide: NotificationService, useValue: notificationService },
        { provide: PushNotificationService, useValue: createPushNotificationServiceMock() },
        { provide: FdUiToastService, useValue: toastService },
        { provide: FdUiDialogService, useValue: dialogService },
        { provide: FrontendObservabilityService, useValue: createFrontendObservabilityServiceMock() },
    ];
}

function createDietologistServiceMock(relationship: DietologistRelationship | null): DietologistServiceMock {
    return {
        getRelationship: vi.fn().mockReturnValue(of(relationship)),
        invite: vi.fn().mockReturnValue(of(void 0)),
        updatePermissions: vi.fn().mockReturnValue(of(void 0)),
        revokeRelationship: vi.fn().mockReturnValue(of(void 0)),
    };
}

function createDialogServiceMock(dialogResult: boolean): { open: ReturnType<typeof vi.fn> } {
    return {
        open: vi.fn().mockReturnValue({
            afterClosed: () => of(dialogResult),
        }),
    };
}

function createLocalizationServiceMock(): {
    applyLanguagePreferenceAsync: ReturnType<typeof vi.fn>;
    getCurrentLanguage: ReturnType<typeof vi.fn>;
} {
    return {
        applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(void 0),
        getCurrentLanguage: vi.fn(() => 'en'),
    };
}

function createPushNotificationServiceMock(): {
    isSupported: ReturnType<typeof signal<boolean>>;
    isSubscribed: ReturnType<typeof signal<boolean>>;
    isBusy: ReturnType<typeof signal<boolean>>;
    currentSubscriptionEndpoint: ReturnType<typeof signal<string | null>>;
    ensureSubscriptionAsync: ReturnType<typeof vi.fn>;
    removeSubscriptionAsync: ReturnType<typeof vi.fn>;
} {
    return {
        isSupported: signal(false),
        isSubscribed: signal(false),
        isBusy: signal(false),
        currentSubscriptionEndpoint: signal<string | null>(null),
        ensureSubscriptionAsync: vi.fn().mockResolvedValue('unsupported'),
        removeSubscriptionAsync: vi.fn().mockResolvedValue(true),
    };
}

function createUserManageNotificationsFacadeMock(
    notificationsChangedVersion: ReturnType<typeof signal<number>>,
): UserManageNotificationsFacadeMock {
    return {
        notificationPermission: signal('unsupported'),
        notificationsChangedVersion,
        isUpdatingNotifications: signal(false),
        isSchedulingTestNotification: signal(false),
        pushNotificationsEnabled: signal(false),
        fastingPushNotificationsEnabled: signal(true),
        socialPushNotificationsEnabled: signal(true),
        fastingCheckInReminderHours: signal(DEFAULT_FASTING_CHECK_IN_REMINDER_HOURS),
        fastingCheckInFollowUpReminderHours: signal(DEFAULT_FASTING_CHECK_IN_FOLLOW_UP_REMINDER_HOURS),
        fastingReminderPresets: FASTING_REMINDER_PRESETS,
        pushNotificationsSupported: signal(false),
        pushNotificationsSubscribed: signal(false),
        pushNotificationsBusy: signal(false),
        currentSubscriptionEndpoint: signal<string | null>(null),
        connectedDeviceItems: signal([]),
        isLoadingConnectedDevices: signal(false),
        removingConnectedDeviceEndpoint: signal<string | null>(null),
        syncFromUser: vi.fn(),
        togglePushNotificationsAsync: vi.fn().mockResolvedValue(void 0),
        enableDeviceNotificationsAsync: vi.fn().mockResolvedValue(void 0),
        toggleFastingPushNotificationsAsync: vi.fn().mockResolvedValue(void 0),
        toggleSocialPushNotificationsAsync: vi.fn().mockResolvedValue(void 0),
        applyFastingReminderPreset: vi.fn(),
        onFastingReminderHoursChange: vi.fn(),
        saveFastingReminderHoursAsync: vi.fn().mockResolvedValue(void 0),
        scheduleTestNotification: vi.fn(),
        removeConnectedDeviceAsync: vi.fn().mockResolvedValue(void 0),
    };
}

function createToastServiceMock(): {
    success: ReturnType<typeof vi.fn>;
    info: ReturnType<typeof vi.fn>;
    error: ReturnType<typeof vi.fn>;
} {
    return {
        success: vi.fn(),
        info: vi.fn(),
        error: vi.fn(),
    };
}

function createFrontendObservabilityServiceMock(): {
    recordNotificationSettingsViewed: ReturnType<typeof vi.fn>;
    recordNotificationPreferenceChanged: ReturnType<typeof vi.fn>;
    recordNotificationSubscriptionEvent: ReturnType<typeof vi.fn>;
    recordFastingReminderPresetSelected: ReturnType<typeof vi.fn>;
    recordFastingReminderTimingSaved: ReturnType<typeof vi.fn>;
} {
    return {
        recordNotificationSettingsViewed: vi.fn(),
        recordNotificationPreferenceChanged: vi.fn(),
        recordNotificationSubscriptionEvent: vi.fn(),
        recordFastingReminderPresetSelected: vi.fn(),
        recordFastingReminderTimingSaved: vi.fn(),
    };
}

function configureTranslateService(): void {
    const translateService = TestBed.inject(TranslateService);
    vi.spyOn(translateService, 'instant').mockImplementation((key: string) => key);
    translateService.setFallbackLang('en');
    translateService.use('en');
}

function createFacadeMock(relationship: DietologistRelationship | null, user: User | null = null): ProfileManageFacadeMock {
    return {
        user: signal(user),
        globalError: signal<string | null>(null),
        isSavingProfile: signal(false),
        profileSavedVersion: signal(0),
        isLinkingGoogle: signal(false),
        isUnlinkingTelegram: signal(false),
        telegramUnlinkError: signal<string | null>(null),
        backupEmailSentTo: signal<string | null>(null),
        backupEmailResendAt: signal(0),
        isRequestingBackupEmail: signal(false),
        requestBackupEmailAsync: vi.fn().mockResolvedValue(void 0),
        isUpdatingNotifications: signal(false),
        webPushSubscriptions: signal([]),
        dietologistRelationship: signal(relationship),
        currentWeight: signal<number | null>(null),
        currentWaist: signal<number | null>(null),
        isLoadingWebPushSubscriptions: signal(false),
        removingWebPushSubscriptionEndpoint: signal<string | null>(null),
        initialize: vi.fn(),
        clearGlobalError: vi.fn(),
        saveProfileAsync: vi.fn<ProfileManageFacade['saveProfileAsync']>().mockResolvedValue(true),
        openChangePasswordDialog: vi.fn(),
        unlinkTelegramAsync: vi.fn<ProfileManageFacade['unlinkTelegramAsync']>().mockResolvedValue(undefined),
        linkGoogle: vi.fn(),
        updateNotificationPreferences: vi.fn(),
        refreshWebPushSubscriptions: vi.fn(),
        removeWebPushSubscription: vi.fn(),
        openAdminPanel: vi.fn(),
    };
}
