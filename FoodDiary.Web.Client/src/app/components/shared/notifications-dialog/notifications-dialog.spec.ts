import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { EMPTY, firstValueFrom, of, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { decodeNotificationKind } from '../../../shared/models/notification-kind';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { type NotificationItem, NotificationService } from '../../../shared/notifications/notification.service';
import { NotificationsDialogComponent } from './notifications-dialog';

type NotificationsDialogComponentTestApi = NotificationsDialogComponent & {
    notificationItems: () => Array<{ icon: string }>;
};
type NotificationsDialogTestContext = {
    component: NotificationsDialogComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<NotificationsDialogComponent>;
    notificationService: {
        ensureNotificationsLoaded: ReturnType<typeof vi.fn>;
        markAllRead: ReturnType<typeof vi.fn>;
        markAsRead: ReturnType<typeof vi.fn>;
        notifications: ReturnType<typeof signal<NotificationItem[]>>;
        notificationsLoading: ReturnType<typeof signal<boolean>>;
        notificationsLoaded: ReturnType<typeof signal<boolean>>;
        hasNotificationsLoadError: ReturnType<typeof signal<boolean>>;
    };
    router: { navigateByUrl: ReturnType<typeof vi.fn> };
};

function setupNotificationsDialog(notifications: NotificationItem[]): NotificationsDialogTestContext {
    const dialogRef = { close: vi.fn() };
    const router = { events: EMPTY, navigateByUrl: vi.fn().mockResolvedValue(true) };
    const notificationService = {
        notifications: signal(notifications),
        notificationsLoading: signal(false),
        notificationsLoaded: signal(true),
        hasNotificationsLoadError: signal(false),
        ensureNotificationsLoaded: vi.fn(),
        markAsRead: vi.fn().mockReturnValue(of(void 0)),
        markAllRead: vi.fn().mockReturnValue(of(void 0)),
    };

    TestBed.configureTestingModule({
        imports: [NotificationsDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: NotificationService, useValue: notificationService },
            { provide: Router, useValue: router },
        ],
    });

    const fixture = TestBed.createComponent(NotificationsDialogComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { component, dialogRef, fixture, notificationService, router };
}

describe('NotificationsDialogComponent background reconciliation', () => {
    it('preserves the rendered card while a loaded feed refresh is pending and after it fails', () => {
        const { fixture, notificationService } = setupNotificationsDialog([createInvitationNotification()]);
        const host = fixture.nativeElement as HTMLElement;
        const originalCard = host.querySelector('.notifications-dialog__item');
        expect(originalCard).not.toBeNull();

        notificationService.notificationsLoading.set(true);
        fixture.detectChanges();

        expect(host.querySelector('.notifications-dialog__item')).toBe(originalCard);
        expect(host.querySelector('.notifications-dialog__state')).toBeNull();

        notificationService.notificationsLoading.set(false);
        notificationService.hasNotificationsLoadError.set(true);
        fixture.detectChanges();

        expect(host.querySelector('.notifications-dialog__item')).toBe(originalCard);
        expect(host.querySelector('.notifications-dialog__state')).toBeNull();
        expect(host.querySelector('fd-ui-inline-alert[role="alert"]')).not.toBeNull();
    });

    it('shows initial loading until the first feed load completes', () => {
        const { fixture, notificationService } = setupNotificationsDialog([]);
        const host = fixture.nativeElement as HTMLElement;
        notificationService.notificationsLoaded.set(false);
        notificationService.notificationsLoading.set(true);
        fixture.detectChanges();

        expect(host.querySelector('.notifications-dialog__state')).not.toBeNull();
        expect(host.querySelector('fd-notifications-dialog-list')).toBeNull();

        notificationService.notifications.set([createInvitationNotification()]);
        notificationService.notificationsLoaded.set(true);
        notificationService.notificationsLoading.set(false);
        fixture.detectChanges();

        expect(host.querySelector('.notifications-dialog__item')).not.toBeNull();
        expect(host.querySelector('.notifications-dialog__state')).toBeNull();
    });
});

describe('NotificationsDialogComponent invitations', () => {
    it('keeps a future notification generic and follows its server target rather than its reference', () => {
        const notification: NotificationItem = {
            ...createInvitationNotification(),
            type: decodeNotificationKind('FutureNotification'),
            targetUrl: '/profile?from=notice',
            referenceId: 'inv-1',
        };
        const { component, fixture, notificationService, router } = setupNotificationsDialog([notification]);
        expect((component as NotificationsDialogComponentTestApi).notificationItems()[0].icon).toBe('notifications');
        const host = fixture.nativeElement as HTMLElement;
        host.querySelector<HTMLButtonElement>('.notifications-dialog__item')?.click();
        expect(notificationService.markAsRead).toHaveBeenCalledWith('n1');
        expect(router.navigateByUrl).toHaveBeenCalledWith('/profile?from=notice');
    });

    it('renders a highlighted dietologist invitation card', () => {
        const { component, fixture } = setupNotificationsDialog([createInvitationNotification()]);

        const host = fixture.nativeElement as HTMLElement;
        const card = host.querySelector('.notifications-dialog__item--dietologist');
        expect(card).toBeTruthy();
        expect(host.textContent).toContain('NOTIFICATIONS.DIETOLOGIST_INVITATION_BADGE');
        expect(host.textContent).toContain('NOTIFICATIONS.DIETOLOGIST_INVITATION_ACTION');
        expect((component as NotificationsDialogComponentTestApi).notificationItems()[0].icon).toBe('medical_information');
    });

    it('marks unread invitation as read before navigation', () => {
        const { dialogRef, fixture, notificationService, router } = setupNotificationsDialog([createInvitationNotification()]);

        const host = fixture.nativeElement as HTMLElement;
        const card = host.querySelector<HTMLButtonElement>('.notifications-dialog__item');
        if (card === null) {
            throw new Error('Expected notification card to exist.');
        }

        card.click();
        fixture.detectChanges();

        expect(notificationService.markAsRead).toHaveBeenCalledWith('n1');
        expect(router.navigateByUrl).toHaveBeenCalledWith('/dietologist-invitations/inv-1');
        expect(dialogRef.close).toHaveBeenCalled();
    });
});

describe('NotificationsDialogComponent password reminders', () => {
    it('renders a highlighted password setup reminder card', () => {
        const { component, fixture } = setupNotificationsDialog([createPasswordSetupNotification()]);

        const host = fixture.nativeElement as HTMLElement;
        const card = host.querySelector('.notifications-dialog__item--security');
        expect(card).toBeTruthy();
        expect(host.textContent).toContain('NOTIFICATIONS.PASSWORD_SETUP_BADGE');
        expect(host.textContent).toContain('NOTIFICATIONS.PASSWORD_SETUP_ACTION');
        expect((component as NotificationsDialogComponentTestApi).notificationItems()[0].icon).toBe('password');
    });
});

describe('NotificationsDialogComponent recommendations', () => {
    it('renders recommendation notification as an actionable dietologist card', () => {
        const { component, fixture } = setupNotificationsDialog([createRecommendationNotification()]);

        const host = fixture.nativeElement as HTMLElement;
        const card = host.querySelector('.notifications-dialog__item--dietologist');
        expect(card).toBeTruthy();
        expect(host.textContent).toContain('NOTIFICATIONS.RECOMMENDATION_BADGE');
        expect(host.textContent).toContain('NOTIFICATIONS.RECOMMENDATION_ACTION');
        expect((component as NotificationsDialogComponentTestApi).notificationItems()[0].icon).toBe('medical_information');
    });
});

describe('Notifications inbox bulk acknowledgment', () => {
    it('deduplicates bulk acknowledgment before redraw and permits retry after a failure', () => {
        const items = [createInvitationNotification(), createRecommendationNotification()];
        const { fixture, notificationService } = setupNotificationsDialog(items);
        const host = fixture.nativeElement as HTMLElement;
        const bulkRead = noticeButton(host, '.notifications-dialog__footer button');
        const firstAttempt = new Subject<void>();
        const retry = new Subject<void>();
        notificationService.markAllRead.mockReturnValueOnce(firstAttempt).mockReturnValueOnce(retry);

        expect(bulkRead.disabled).toBe(false);
        bulkRead.click();
        expect(firstAttempt.observed).toBe(true);
        expect(bulkRead.disabled).toBe(false);
        bulkRead.click();
        expect(notificationService.markAllRead).toHaveBeenCalledOnce();
        fixture.detectChanges();
        expect(bulkRead.disabled).toBe(true);
        firstAttempt.error(new Error('Read status unavailable'));
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]:not([hidden])')?.textContent).toContain('NOTIFICATIONS.MARK_ALL_READ_ERROR');
        expect(host.querySelectorAll('.notifications-dialog__item:not(.is-read)')).toHaveLength(items.length);
        expect(bulkRead.disabled).toBe(false);

        bulkRead.click();
        expect(notificationService.markAllRead).toHaveBeenCalledTimes(2);
        expect(retry.observed).toBe(true);
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]:not([hidden])')).toBeNull();
        expect(bulkRead.disabled).toBe(true);
        notificationService.notifications.set(items.map(item => ({ ...item, isRead: true })));
        retry.next();
        retry.complete();
        fixture.detectChanges();
        expect(host.querySelectorAll('.notifications-dialog__item.is-read')).toHaveLength(items.length);
        expect(bulkRead.disabled).toBe(true);
    });
});

describe('Notifications inbox row actions', () => {
    it.each([null, '  '])('acknowledges a notice without navigating when its destination is %s', targetUrl => {
        const notice = { ...createRecommendationNotification(), targetUrl };
        const { fixture, dialogRef, notificationService, router } = setupNotificationsDialog([notice]);
        const host = fixture.nativeElement as HTMLElement;
        const row = noticeButton(host, '.notifications-dialog__item');
        row.click();
        expect(notificationService.markAsRead).toHaveBeenCalledExactlyOnceWith(notice.id);

        notificationService.notifications.set([{ ...notice, isRead: true }]);
        fixture.detectChanges();
        expect(row.classList.contains('is-read')).toBe(true);
        row.click();
        expect(notificationService.markAsRead).toHaveBeenCalledOnce();
        expect(router.navigateByUrl).not.toHaveBeenCalled();
        expect(dialogRef.close).not.toHaveBeenCalled();
    });

    it('opens the chosen destination after the read acknowledgment fails', () => {
        const notice = createRecommendationNotification();
        const { fixture, dialogRef, notificationService, router } = setupNotificationsDialog([notice]);
        const pending = new Subject<void>();
        notificationService.markAsRead.mockReturnValueOnce(pending);
        noticeButton(fixture.nativeElement as HTMLElement, '.notifications-dialog__item').click();
        expect(notificationService.markAsRead).toHaveBeenCalledExactlyOnceWith(notice.id);
        expect(router.navigateByUrl).not.toHaveBeenCalled();
        expect(dialogRef.close).not.toHaveBeenCalled();

        pending.error(new Error('Read acknowledgment unavailable'));
        expect(router.navigateByUrl).toHaveBeenCalledExactlyOnceWith(notice.targetUrl);
        expect(dialogRef.close).toHaveBeenCalledOnce();
    });

    it('opens the destination of an already-read notification immediately', () => {
        const notice = { ...createPasswordSetupNotification(), isRead: true };
        const { fixture, dialogRef, notificationService, router } = setupNotificationsDialog([notice]);
        noticeButton(fixture.nativeElement as HTMLElement, '.notifications-dialog__item').click();

        expect(router.navigateByUrl).toHaveBeenCalledExactlyOnceWith(notice.targetUrl);
        expect(dialogRef.close).toHaveBeenCalledOnce();
        expect(notificationService.markAsRead).not.toHaveBeenCalled();
    });
    it('cancels a pending read acknowledgment if the user dismisses the inbox', () => {
        const { fixture, notificationService, router } = setupNotificationsDialog([createInvitationNotification()]);
        const pending = new Subject<void>();
        notificationService.markAsRead.mockReturnValueOnce(pending);
        noticeButton(fixture.nativeElement as HTMLElement, '.notifications-dialog__item').click();
        expect(pending.observed).toBe(true);
        fixture.destroy();
        expect(pending.observed).toBe(false);
        pending.next();
        pending.complete();
        expect(router.navigateByUrl).not.toHaveBeenCalled();
    });
});

describe('Notifications inbox destination ownership', () => {
    it.each(['success', 'failure'] as const)('ignores the former destination when its acknowledgement ends in %s', outcome => {
        const older = createInvitationNotification();
        const newer = createRecommendationNotification();
        const { fixture, dialogRef, notificationService, router } = setupNotificationsDialog([older, newer]);
        const oldAcknowledgement = new Subject<void>();
        const newAcknowledgement = new Subject<void>();
        notificationService.markAsRead.mockReturnValueOnce(oldAcknowledgement).mockReturnValueOnce(newAcknowledgement);
        const host = fixture.nativeElement as HTMLElement;

        noticeButton(host, `.notifications-dialog__item[aria-label^="${older.title}"]`).click();
        noticeButton(host, `.notifications-dialog__item[aria-label^="${newer.title}"]`).click();
        expect(notificationService.markAsRead.mock.calls).toEqual([[older.id], [newer.id]]);
        expect(router.navigateByUrl).not.toHaveBeenCalled();

        if (outcome === 'success') {
            oldAcknowledgement.next();
            oldAcknowledgement.complete();
        } else {
            oldAcknowledgement.error(new Error('Earlier read request failed'));
        }

        expect(router.navigateByUrl).not.toHaveBeenCalled();
        expect(dialogRef.close).not.toHaveBeenCalled();

        newAcknowledgement.next();
        newAcknowledgement.complete();
        expect(router.navigateByUrl).toHaveBeenCalledExactlyOnceWith(newer.targetUrl);
        expect(dialogRef.close).toHaveBeenCalledOnce();
    });

    it('preserves a pending destination when the next action only marks a targetless row read', () => {
        const destination = createInvitationNotification();
        const notice = { ...createRecommendationNotification(), targetUrl: null };
        const { fixture, dialogRef, notificationService, router } = setupNotificationsDialog([destination, notice]);
        const pending = new Subject<void>();
        notificationService.markAsRead.mockReturnValueOnce(pending);
        const host = fixture.nativeElement as HTMLElement;

        noticeButton(host, `.notifications-dialog__item[aria-label^="${destination.title}"]`).click();
        noticeButton(host, `.notifications-dialog__item[aria-label^="${notice.title}"]`).click();
        expect(notificationService.markAsRead.mock.calls).toEqual([[destination.id], [notice.id]]);
        expect(router.navigateByUrl).not.toHaveBeenCalled();

        pending.next();
        pending.complete();
        expect(router.navigateByUrl).toHaveBeenCalledExactlyOnceWith(destination.targetUrl);
        expect(dialogRef.close).toHaveBeenCalledOnce();
    });
});

describe('Notifications inbox locale', () => {
    it('reformats a retained row when the language changes without loading the inbox again', async () => {
        const entry = {
            ...createRecommendationNotification(),
            title: '  New recommendation  ',
            body: '  Review the new suggestion.  ',
            createdAtUtc: utcInstant('2026-04-15T12:30:00Z'),
        };
        const { fixture, notificationService } = setupNotificationsDialog([entry]);
        const translate = TestBed.inject(TranslateService);
        const originalRow = noticeButton(fixture.nativeElement as HTMLElement, '.notifications-dialog__item');
        await firstValueFrom(translate.use('en'));
        fixture.detectChanges();
        const date = originalRow.querySelector('.notifications-dialog__item-date');
        const originalDate = date?.textContent.trim();
        expect(originalDate).toBe(formatNoticeTimestamp(entry.createdAtUtc, 'en-US'));
        expect(originalRow.getAttribute('aria-label')).toBe('New recommendation. Review the new suggestion.');

        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();
        expect(date?.textContent.trim()).toBe(formatNoticeTimestamp(entry.createdAtUtc, 'ru-RU'));
        expect(date?.textContent.trim()).not.toBe(originalDate);
        expect(noticeButton(fixture.nativeElement as HTMLElement, '.notifications-dialog__item')).toBe(originalRow);
        expect(notificationService.ensureNotificationsLoaded).toHaveBeenCalledOnce();
    });

    it('retains the notification when a stored timestamp cannot be formatted', () => {
        const notice = { ...createPasswordSetupNotification(), createdAtUtc: utcInstant('timestamp unavailable') };
        const { fixture, router } = setupNotificationsDialog([notice]);
        const row = noticeButton(fixture.nativeElement as HTMLElement, '.notifications-dialog__item');
        expect(row.querySelector('.notifications-dialog__item-date')?.textContent.trim()).toBe(notice.createdAtUtc);
        row.click();
        expect(router.navigateByUrl).toHaveBeenCalledExactlyOnceWith(notice.targetUrl);
    });
});

function noticeButton(root: HTMLElement, selector: string): HTMLButtonElement {
    const button = root.querySelector(selector);
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error(`Expected the inbox control ${selector}`);
    }
    return button;
}

function formatNoticeTimestamp(value: string, locale: string): string {
    return new Intl.DateTimeFormat(locale, {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    }).format(new Date(value));
}

function createInvitationNotification(): NotificationItem {
    return {
        id: entityId<'notification'>('n1'),
        type: decodeNotificationKind('DietologistInvitationReceived'),
        title: 'Dietologist invitation',
        body: 'Client invited you',
        targetUrl: '/dietologist-invitations/inv-1',
        referenceId: 'inv-1',
        isRead: false,
        createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
    };
}

function createPasswordSetupNotification(): NotificationItem {
    return {
        id: entityId<'notification'>('n2'),
        type: decodeNotificationKind('PasswordSetupSuggested'),
        title: 'Add a backup password',
        body: 'Set a password to keep a backup sign-in method besides Google.',
        targetUrl: '/profile?intent=set-password',
        referenceId: 'password-setup:user-1',
        isRead: false,
        createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
    };
}

function createRecommendationNotification(): NotificationItem {
    return {
        id: entityId<'notification'>('n3'),
        type: decodeNotificationKind('NewRecommendation'),
        title: 'New recommendation',
        body: null,
        targetUrl: '/recommendations?recommendationId=rec-1',
        referenceId: 'rec-1',
        isRead: false,
        createdAtUtc: utcInstant('2026-04-15T00:00:00Z'),
    };
}
