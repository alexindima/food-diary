import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminTemplateHistoryFacade } from '../lib/admin-template-history.facade';
import type { AdminTemplateRevision } from '../models/admin-template-revision';
import { AdminTemplateHistoryComponent } from './admin-template-history';

describe('AdminTemplateHistoryComponent', () => {
    const getRevisions = vi.fn();
    const revision: AdminTemplateRevision = {
        id: 'old',
        subject: 'Previous',
        htmlBody: '<script>unsafe()</script>',
        textBody: 'Previous text',
        isActive: true,
        version: null,
        savedOnUtc: '2026-01-01T00:00:00Z',
        archivedOnUtc: '2026-01-02T00:00:00Z',
    };

    beforeEach(async () => {
        getRevisions.mockReset().mockReturnValue(of([revision]));
        await TestBed.configureTestingModule({
            imports: [AdminTemplateHistoryComponent],
            providers: [...provideTranslateTesting(), { provide: AdminTemplateHistoryFacade, useValue: { getRevisions } }],
        }).compileComponents();
    });

    function create(): ComponentFixture<AdminTemplateHistoryComponent> {
        const fixture = TestBed.createComponent(AdminTemplateHistoryComponent);
        fixture.componentRef.setInput('kind', 'email-templates');
        fixture.componentRef.setInput('templateKey', 'account_created');
        fixture.componentRef.setInput('locale', 'en');
        fixture.detectChanges();
        return fixture;
    }

    it('discards a previous locale request and renders HTML history as text', () => {
        const pending = new Subject<AdminTemplateRevision[]>();
        getRevisions.mockReturnValueOnce(pending);
        const fixture = create();
        fixture.componentRef.setInput('locale', 'ru');
        fixture.detectChanges();
        pending.next([{ ...revision, id: 'stale' }]);
        fixture.detectChanges();
        expect(fixture.componentInstance['revisions']()).toEqual([revision]);
        expect((fixture.nativeElement as HTMLElement).querySelector('script')).toBeNull();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('<script>unsafe()</script>');
    });

    it('retries a failed history request and emits a restore without saving', () => {
        getRevisions.mockReturnValueOnce(throwError(() => new Error('offline')));
        const fixture = create();
        expect(fixture.componentInstance['failed']()).toBe(true);
        fixture.componentInstance['retry']();
        fixture.detectChanges();
        expect(fixture.componentInstance['failed']()).toBe(false);
        const restored = vi.fn();
        fixture.componentInstance.restored.subscribe(restored);
        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('fd-ui-button button')?.click();
        expect(restored).toHaveBeenCalledWith(revision);
        expect(getRevisions).toHaveBeenCalledTimes(2);
    });
});
