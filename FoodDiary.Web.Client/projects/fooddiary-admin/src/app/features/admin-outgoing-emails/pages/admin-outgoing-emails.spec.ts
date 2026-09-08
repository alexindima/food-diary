import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminOutgoingEmailsFacade } from '../lib/admin-outgoing-emails.facade';
import type { OutgoingEmailPage } from '../models/outgoing-email';
import { AdminOutgoingEmailsComponent } from './admin-outgoing-emails';

describe('AdminOutgoingEmailsComponent', () => {
    it('shows a failed request and recovers to the empty state after refresh', async () => {
        const first = new Subject<OutgoingEmailPage>();
        const retry = new Subject<OutgoingEmailPage>();
        const getPage = vi.fn().mockReturnValueOnce(first).mockReturnValueOnce(retry);
        await TestBed.configureTestingModule({
            imports: [AdminOutgoingEmailsComponent],
            providers: [provideRouter([]), ...provideTranslateTesting(), { provide: AdminOutgoingEmailsFacade, useValue: { getPage } }],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminOutgoingEmailsComponent);
        const element = fixture.nativeElement as HTMLElement;
        fixture.detectChanges();
        expect(element.querySelector('[role="status"]')).not.toBeNull();
        first.error(new Error('Unavailable'));
        fixture.detectChanges();
        expect(element.querySelector('[role="alert"]')).not.toBeNull();
        const refresh = element.querySelector<HTMLButtonElement>('fd-ui-button button');
        expect(refresh).not.toBeNull();
        refresh?.click();
        await fixture.whenStable();
        retry.next({ items: [], totalItems: 0 });
        fixture.detectChanges();
        expect(element.querySelector('[role="alert"]')).toBeNull();
        expect(element.textContent).toContain('ADMIN_OUTGOING.EMPTY');
        expect(getPage).toHaveBeenLastCalledWith(1, '', '', { recipient: '', id: '', correlationId: '' });
        fixture.destroy();
    });
});
