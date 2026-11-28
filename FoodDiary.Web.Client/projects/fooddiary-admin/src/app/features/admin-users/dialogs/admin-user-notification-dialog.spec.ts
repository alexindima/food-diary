import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminUsersService } from '../api/admin-users.service';
import { AdminUserNotificationDialogComponent } from './admin-user-notification-dialog';

let fixture: ComponentFixture<AdminUserNotificationDialogComponent>;
let component: AdminUserNotificationDialogComponent;
let users: { scheduleTestNotification: ReturnType<typeof vi.fn> };
let dialog: { close: ReturnType<typeof vi.fn> };
const DEFAULT_USER = { id: 'selected-user', email: 'notification-test@example.com' };

async function setupAsync(user: { id: string; email?: string; username?: string } = DEFAULT_USER): Promise<void> {
    users = { scheduleTestNotification: vi.fn().mockReturnValue(of(undefined)) };
    dialog = { close: vi.fn() };
    await TestBed.configureTestingModule({
        imports: [AdminUserNotificationDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AdminUsersService, useValue: users },
            { provide: FdUiDialogRef, useValue: dialog },
            { provide: FD_UI_DIALOG_DATA, useValue: user },
        ],
    }).compileComponents();
    fixture = TestBed.createComponent(AdminUserNotificationDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}

async function submitAsync(): Promise<void> {
    const element = fixture.nativeElement as HTMLElement;
    element.querySelector('form')?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await fixture.whenStable();
}

describe('AdminUserNotificationDialogComponent', () => {
    beforeEach(async () => setupAsync());

    it('submits the selected type and delay to the selected user and shows success', async () => {
        component['formModel'].set({ type: 'EatingWindowStarted', delaySeconds: 30 });
        fixture.detectChanges();
        await submitAsync();
        expect(users.scheduleTestNotification).toHaveBeenCalledWith('selected-user', { type: 'EatingWindowStarted', delaySeconds: 30 });
        expect(component['scheduled']()).toBe(true);
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="status"]')?.textContent).toContain(
            'ADMIN_NOTIFICATION.SUCCESS',
        );
        component['close']();
        expect(dialog.close).toHaveBeenCalledWith(true);
    });

    it('preserves selection on failure and allows retry', async () => {
        users.scheduleTestNotification.mockReturnValueOnce(throwError(() => new Error('Unavailable')));
        component['formModel'].set({ type: 'FastingWindowStarted', delaySeconds: 60 });
        fixture.detectChanges();
        await submitAsync();
        expect(component['failed']()).toBe(true);
        expect(component['scheduled']()).toBe(false);
        expect(component['isSubmitting']()).toBe(false);
        expect(component['formModel']()).toEqual({ type: 'FastingWindowStarted', delaySeconds: 60 });
        await submitAsync();
        expect(users.scheduleTestNotification).toHaveBeenCalledTimes(2);
        expect(component['scheduled']()).toBe(true);
    });

    it('blocks duplicate submission and close while scheduling', async () => {
        const pending = new Subject<void>();
        users.scheduleTestNotification.mockReturnValueOnce(pending);
        const first = component['submitAsync']();
        await component['submitAsync']();
        component['close']();
        expect(component['isSubmitting']()).toBe(true);
        expect(users.scheduleTestNotification).toHaveBeenCalledTimes(1);
        expect(dialog.close).not.toHaveBeenCalled();
        pending.next();
        pending.complete();
        await first;
        await component['submitAsync']();
        expect(users.scheduleTestNotification).toHaveBeenCalledTimes(1);
    });
});

it.each([
    { user: { id: 'selected-user', username: 'SelectedUsername' }, recipient: 'SelectedUsername' },
    { user: { id: 'selected-user' }, recipient: 'selected-user' },
])('identifies the notification recipient as $recipient when email is unavailable', async ({ user, recipient }) => {
    await setupAsync(user);
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { ADMIN_NOTIFICATION: { RECIPIENT: 'Notify {{ recipient }}' } });
    await firstValueFrom(translate.use('en'));
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain(`Notify ${recipient}`);
});
