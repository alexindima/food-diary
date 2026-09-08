import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminMailMessageDialogComponent } from '../dialogs/admin-mail-message-dialog';
import { AdminMailInboxFacade } from '../lib/admin-mail-inbox.facade';
import type { AdminMailInboxMessagePage } from '../models/admin-mail-inbox.data';
import { AdminMailInboxComponent } from './admin-mail-inbox';

describe('AdminMailInboxComponent row activation', () => {
    it.each([null, '2026-09-08T00:00:00Z'])('opens mail from every cell and the keyboard when readAtUtc is %s', async readAtUtc => {
        const open = vi.fn();
        await TestBed.configureTestingModule({
            imports: [AdminMailInboxComponent],
            providers: [
                ...provideTranslateTesting(),
                { provide: FdUiDialogService, useValue: { open } },
                {
                    provide: AdminMailInboxFacade,
                    useValue: {
                        getMessagePage: (): Observable<AdminMailInboxMessagePage> =>
                            of({
                                totalItems: 1,
                                items: [
                                    {
                                        id: 'mail-1',
                                        subject: 'Test message',
                                        fromAddress: 'sender@example.com',
                                        toRecipients: ['bugs@example.com'],
                                        category: 'general',
                                        status: 'received',
                                        isTrustedRelay: true,
                                        receivedAtUtc: '2026-09-07T00:00:00Z',
                                        readAtUtc,
                                    },
                                ],
                            }),
                    },
                },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminMailInboxComponent);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        const row = element.querySelector<HTMLTableRowElement>('tbody tr');
        if (row === null) {
            throw new Error('Expected a mail row');
        }
        for (const cell of row.cells) {
            open.mockClear();
            cell.click();
            expect(open).toHaveBeenCalledTimes(1);
            expect(open.mock.calls[0]?.[0]).toBe(AdminMailMessageDialogComponent);
            expect(open.mock.calls[0]?.[1]).toHaveProperty('data.id', 'mail-1');
        }
        for (const key of ['Enter', ' ']) {
            open.mockClear();
            row.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));
            expect(open).toHaveBeenCalledTimes(1);
        }
        fixture.destroy();
    });
});
