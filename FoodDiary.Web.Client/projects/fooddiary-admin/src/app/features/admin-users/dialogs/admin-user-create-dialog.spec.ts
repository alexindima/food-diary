import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminUsersFacade } from '../lib/admin-users.facade';
import { AdminUserCreateDialogComponent } from './admin-user-create-dialog';

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

        form.temporaryPassword().value.set('QaPassword123!');
        fixture.detectChanges();
        expect(createButton().disabled).toBe(false);

        form.temporaryPassword().value.set('');
        checkbox('admin-user-create-generate-password').click();
        fixture.detectChanges();
        expect(createButton().disabled).toBe(false);
    });
});
