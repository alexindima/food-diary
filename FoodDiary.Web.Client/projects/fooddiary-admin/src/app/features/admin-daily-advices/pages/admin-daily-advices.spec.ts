import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminDailyAdvicesService } from '../api/admin-daily-advices.service';
import { DAILY_ADVICE_IMPORT_EXAMPLE } from '../lib/daily-advice-import';
import type { AdminDailyAdvice, AdminDailyAdvicesImportResponse } from '../models/admin-daily-advice.models';
import { AdminDailyAdvicesComponent } from './admin-daily-advices';

describe('daily advices page', () => {
    const getAll = vi.fn();
    const importAdvices = vi.fn();
    beforeEach(async () => {
        getAll.mockReset().mockReturnValue(of([]));
        importAdvices.mockReset().mockReturnValue(of({ importedCount: 2, skippedCount: 0 }));
        await TestBed.configureTestingModule({
            imports: [AdminDailyAdvicesComponent],
            providers: [
                provideRouter([]),
                ...provideTranslateTesting(),
                { provide: AdminDailyAdvicesService, useValue: { getAll, importAdvices } },
            ],
        }).compileComponents();
    });

    function fileEvent(text: string): Event {
        const input = document.createElement('input');
        input.type = 'file';
        Object.defineProperty(input, 'files', { value: [{ size: text.length, text: vi.fn().mockResolvedValue(text) }] });
        const event = new Event('change');
        Object.defineProperty(event, 'target', { value: input });
        return event;
    }

    it('shows empty state and import result, then reloads the list', async () => {
        const fixture = TestBed.createComponent(AdminDailyAdvicesComponent);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_DAILY_ADVICES.EMPTY');
        await fixture.componentInstance['importFileAsync'](fileEvent(JSON.stringify(DAILY_ADVICE_IMPORT_EXAMPLE)));
        fixture.detectChanges();
        expect(importAdvices).toHaveBeenCalledWith(DAILY_ADVICE_IMPORT_EXAMPLE);
        expect(getAll).toHaveBeenCalledTimes(2);
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="status"]')?.textContent).toContain(
            'ADMIN_DAILY_ADVICES.SUCCESS',
        );
    });

    it('rejects invalid JSON and invalid fields before calling the API', async () => {
        const fixture = TestBed.createComponent(AdminDailyAdvicesComponent);
        await fixture.componentInstance['importFileAsync'](fileEvent('{'));
        expect(fixture.componentInstance['importError']()).toBe('ADMIN_DAILY_ADVICES.INVALID_JSON');
        await fixture.componentInstance['importFileAsync'](fileEvent('{"version":1,"advices":[null]}'));
        expect(fixture.componentInstance['importError']()).toBe('ADMIN_DAILY_ADVICES.INVALID_FORMAT');
        expect(importAdvices).not.toHaveBeenCalled();
    });

    it('prevents concurrent submissions and allows retry after an API failure', async () => {
        const pending = new Subject<AdminDailyAdvicesImportResponse>();
        importAdvices.mockReturnValueOnce(pending);
        const fixture = TestBed.createComponent(AdminDailyAdvicesComponent);
        const text = JSON.stringify(DAILY_ADVICE_IMPORT_EXAMPLE);
        await fixture.componentInstance['importFileAsync'](fileEvent(text));
        await fixture.componentInstance['importFileAsync'](fileEvent(text));
        expect(importAdvices).toHaveBeenCalledTimes(1);
        pending.error(new Error('offline'));
        expect(fixture.componentInstance['importing']()).toBe(false);
        expect(fixture.componentInstance['importError']()).toBe('ADMIN_DAILY_ADVICES.IMPORT_FAILED');
        await fixture.componentInstance['importFileAsync'](fileEvent(text));
        expect(importAdvices).toHaveBeenCalledTimes(2);
    });

    it('shows load error and can retry', () => {
        getAll.mockReturnValueOnce(throwError(() => new Error('offline')));
        const fixture = TestBed.createComponent(AdminDailyAdvicesComponent);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('fd-admin-load-error')).not.toBeNull();
        fixture.componentInstance['loadAdvices']();
        expect(fixture.componentInstance['loadFailed']()).toBe(false);
    });
});

describe('daily advices pagination', () => {
    const getAll = vi.fn();
    beforeEach(async () => {
        getAll.mockReset().mockReturnValue(of([]));
        await TestBed.configureTestingModule({
            imports: [AdminDailyAdvicesComponent],
            providers: [provideRouter([]), ...provideTranslateTesting(), { provide: AdminDailyAdvicesService, useValue: { getAll } }],
        }).compileComponents();
    });

    it('keeps URL paging on reload and recovers a stale page after the list shrinks', async () => {
        const items: AdminDailyAdvice[] = Array.from({ length: 26 }, (_, index) => ({
            id: `advice-${index}`,
            ru: `Совет ${index}`,
            en: `Advice ${index}`,
            weight: 1,
            tag: null,
        }));
        getAll.mockReturnValue(of(items));
        const router = TestBed.inject(Router);
        await router.navigateByUrl('/?page=2');
        const fixture = TestBed.createComponent(AdminDailyAdvicesComponent);
        await fixture.whenStable();
        fixture.detectChanges();
        expect(fixture.componentInstance['page']()).toBe(1);
        expect(fixture.componentInstance['pageItems']()[0]?.id).toBe('advice-25');
        expect(new URL(router.url, 'http://localhost').searchParams.get('page')).toBe('2');
        getAll.mockReturnValueOnce(of(items.slice(0, 1)));
        fixture.componentInstance['loadAdvices']();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(fixture.componentInstance['pageItems']()[0]?.id).toBe('advice-0');
        expect(new URL(router.url, 'http://localhost').searchParams.get('page')).toBe('1');
    });
});
