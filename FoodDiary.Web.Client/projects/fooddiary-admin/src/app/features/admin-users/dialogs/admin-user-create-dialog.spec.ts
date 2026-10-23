import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminUsersFacade } from '../lib/admin-users.facade';
import { AdminUserCreateDialogComponent } from './admin-user-create-dialog';

const PASSWORD_MAXIMUM = 256;

describe('AdminUserCreateDialogComponent credential options', () => {
    let fixture: ComponentFixture<AdminUserCreateDialogComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AdminUserCreateDialogComponent],
            providers: [
                { provide: FdUiDialogRef, useValue: { close: vi.fn() } },
                { provide: AdminUsersFacade, useValue: {} },
            ],
        }).compileComponents();
        fixture = TestBed.createComponent(AdminUserCreateDialogComponent);
        fixture.detectChanges();
    });

    function checkbox(id: string): HTMLInputElement {
        const element = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(`input#${id}`);
        if (element === null) {
            throw new Error(`Missing checkbox ${id}`);
        }
        return element;
    }

    it('requires a password change when credential email is enabled again', () => {
        const send = checkbox('admin-user-create-send-credentials');
        const requireChange = checkbox('admin-user-create-require-password-change');
        send.click();
        fixture.detectChanges();
        requireChange.click();
        fixture.detectChanges();
        expect(requireChange.checked).toBe(false);
        send.click();
        fixture.detectChanges();
        expect(send.checked).toBe(true);
        expect(requireChange.checked).toBe(true);
    });

    it('disables creation for empty or short manual passwords and recovers when valid', () => {
        const form = fixture.componentInstance['form'];
        form.email().value.set('admin-qa@example.com');
        checkbox('admin-user-create-generate-password').click();
        fixture.detectChanges();

        const createButton = (): HTMLButtonElement => {
            const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button');
            const button = [...buttons].find(element => element.textContent.includes('Create user'));
            if (button === undefined) {
                throw new Error('Missing create user button');
            }
            return button;
        };

        expect(createButton().disabled).toBe(true);
        form.temporaryPassword().value.set('12345');
        fixture.detectChanges();
        expect(createButton().disabled).toBe(true);

        for (const password of ['       ', '   a   ', ' 12345 ']) {
            form.temporaryPassword().value.set(password);
            fixture.detectChanges();
            expect(createButton().disabled).toBe(true);
        }

        form.temporaryPassword().value.set(' 123456 ');
        fixture.detectChanges();
        expect(createButton().disabled).toBe(false);

        form.temporaryPassword().value.set('QaPassword123!');
        fixture.detectChanges();
        expect(createButton().disabled).toBe(false);

        form.temporaryPassword().value.set('');
        checkbox('admin-user-create-generate-password').click();
        fixture.detectChanges();
        expect(createButton().disabled).toBe(false);
    });
});

describe('AdminUserCreateDialogComponent manual password maximum', () => {
    it('accepts the API maximum and blocks a longer manual password before submission', async () => {
        const createUser = vi.fn();
        await TestBed.configureTestingModule({
            imports: [AdminUserCreateDialogComponent],
            providers: [
                { provide: FdUiDialogRef, useValue: { close: vi.fn() } },
                { provide: AdminUsersFacade, useValue: { createUser } },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminUserCreateDialogComponent);
        const component = fixture.componentInstance;
        component['formModel'].update(value => ({ ...value, email: 'admin-qa@example.com', generatePassword: false }));
        component['form'].temporaryPassword().value.set('q'.repeat(PASSWORD_MAXIMUM));
        fixture.detectChanges();
        expect(component['form']().invalid()).toBe(false);
        const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('input[type="password"]');
        expect(input?.maxLength).toBe(PASSWORD_MAXIMUM);

        component['form'].temporaryPassword().value.set('q'.repeat(PASSWORD_MAXIMUM + 1));
        fixture.detectChanges();
        expect(component['form']().invalid()).toBe(true);
        component['submit']();
        expect(createUser).not.toHaveBeenCalled();
        component['form'].generatePassword().value.set(true);
        fixture.detectChanges();
        expect(component['form']().invalid()).toBe(false);
    });
});

describe('AdminUserCreateDialogComponent pending creation', () => {
    it('locks fields and manual credential options until a failed request returns', async () => {
        const pending = new Subject<never>();
        const createUser = vi.fn().mockReturnValue(pending);
        const close = vi.fn();
        await TestBed.configureTestingModule({
            imports: [AdminUserCreateDialogComponent],
            providers: [
                { provide: FdUiDialogRef, useValue: { close } },
                { provide: AdminUsersFacade, useValue: { createUser } },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminUserCreateDialogComponent);
        const component = fixture.componentInstance;
        component['formModel'].update(value => ({ ...value, email: 'admin-qa@example.com', sendCredentialsEmail: false }));
        component['submit']();
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        for (const control of element.querySelectorAll<HTMLInputElement>('input')) {
            expect(control.disabled).toBe(true);
        }
        component['toggleRole']('Admin');
        component['onCredentialsOptionChange']('sendCredentialsEmail', true);
        component['close']();
        expect(component['hasRole']('Admin')).toBe(false);
        expect(component['formModel']().sendCredentialsEmail).toBe(false);
        expect(close).not.toHaveBeenCalled();

        pending.error(new Error('offline'));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(element.querySelector<HTMLInputElement>('input')?.disabled).toBe(false);
        });
        expect(component['formModel']().email).toBe('admin-qa@example.com');
        expect(component['submitError']()).not.toBeNull();
    });
});
