import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { type NotificationItem, NotificationService } from '../../../services/notification.service';
import { NotificationsDialogComponent } from './notifications-dialog.component';

type NotificationsDialogComponentTestApi = NotificationsDialogComponent & {
    getNotificationIcon(notification: NotificationItem): string;
};

describe('NotificationsDialogComponent', () => {
    let fixture: ComponentFixture<NotificationsDialogComponent>;
    let component: NotificationsDialogComponent;
    let dialogRef: { close: ReturnType<typeof vi.fn> };
    let router: { navigateByUrl: ReturnType<typeof vi.fn> };
    let notificationService: {
        notifications: ReturnType<typeof signal<NotificationItem[]>>;
        notificationsLoading: ReturnType<typeof signal<boolean>>;
        ensureNotificationsLoaded: ReturnType<typeof vi.fn>;
        markAsRead: ReturnType<typeof vi.fn>;
        markAllRead: ReturnType<typeof vi.fn>;
    };

    function createComponent(notifications: NotificationItem[]): void {
        dialogRef = { close: vi.fn() };
        router = { navigateByUrl: vi.fn().mockResolvedValue(true) };
        notificationService = {
            notifications: signal(notifications),
            notificationsLoading: signal(false),
            ensureNotificationsLoaded: vi.fn(),
            markAsRead: vi.fn().mockReturnValue(of(undefined)),
            markAllRead: vi.fn().mockReturnValue(of(undefined)),
        };

        TestBed.configureTestingModule({
            imports: [NotificationsDialogComponent, TranslateModule.forRoot()],
            providers: [
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: NotificationService, useValue: notificationService },
                { provide: Router, useValue: router },
            ],
        });

        fixture = TestBed.createComponent(NotificationsDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('renders a highlighted dietologist invitation card', () => {
        createComponent([
            {
                id: 'n1',
                type: 'DietologistInvitationReceived',
                title: 'Dietologist invitation',
                body: 'Client invited you',
                targetUrl: '/dietologist-invitations/inv-1',
                referenceId: 'inv-1',
                isRead: false,
                createdAtUtc: '2026-04-15T00:00:00Z',
            },
        ]);

        const host: HTMLElement = fixture.nativeElement;
        const card = host.querySelector('.notifications-dialog__item--dietologist');
        expect(card).toBeTruthy();
        expect(host.textContent).toContain('NOTIFICATIONS.DIETOLOGIST_INVITATION_BADGE');
        expect(host.textContent).toContain('NOTIFICATIONS.DIETOLOGIST_INVITATION_ACTION');
        expect((component as NotificationsDialogComponentTestApi).getNotificationIcon(notificationService.notifications()[0])).toBe(
            'medical_information',
        );
    });

    it('marks unread invitation as read before navigation', () => {
        createComponent([
            {
                id: 'n1',
                type: 'DietologistInvitationReceived',
                title: 'Dietologist invitation',
                body: 'Client invited you',
                targetUrl: '/dietologist-invitations/inv-1',
                referenceId: 'inv-1',
                isRead: false,
                createdAtUtc: '2026-04-15T00:00:00Z',
            },
        ]);

        const card = fixture.nativeElement.querySelector('.notifications-dialog__item') as HTMLButtonElement;
        card.click();
        fixture.detectChanges();

        expect(notificationService.markAsRead).toHaveBeenCalledWith('n1');
        expect(router.navigateByUrl).toHaveBeenCalledWith('/dietologist-invitations/inv-1');
        expect(dialogRef.close).toHaveBeenCalled();
    });

    it('renders a highlighted password setup reminder card', () => {
        createComponent([
            {
                id: 'n2',
                type: 'PasswordSetupSuggested',
                title: 'Add a backup password',
                body: 'Set a password to keep a backup sign-in method besides Google.',
                targetUrl: '/profile?intent=set-password',
                referenceId: 'password-setup:user-1',
                isRead: false,
                createdAtUtc: '2026-04-15T00:00:00Z',
            },
        ]);

        const host: HTMLElement = fixture.nativeElement;
        const card = host.querySelector('.notifications-dialog__item--security');
        expect(card).toBeTruthy();
        expect(host.textContent).toContain('NOTIFICATIONS.PASSWORD_SETUP_BADGE');
        expect(host.textContent).toContain('NOTIFICATIONS.PASSWORD_SETUP_ACTION');
        expect((component as NotificationsDialogComponentTestApi).getNotificationIcon(notificationService.notifications()[0])).toBe(
            'password',
        );
    });
});
