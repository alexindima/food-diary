import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminDailyAdvicesService } from '../api/admin-daily-advices.service';
import { DailyAdviceGroupCardComponent } from '../components/daily-advice-group-card';
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

    async function selectFileAsync(fixture: ComponentFixture<AdminDailyAdvicesComponent>, contents: string): Promise<void> {
        fixture.detectChanges();
        const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('input#advice-import');
        if (input === null) {
            throw new Error('Missing daily advice file input');
        }
        const file = new File([contents], 'advices.json', { type: 'application/json' });
        Object.defineProperty(file, 'text', { value: vi.fn<() => Promise<string>>().mockResolvedValue(contents) });
        Object.defineProperty(input, 'files', { configurable: true, value: [file] });
        input.dispatchEvent(new Event('change', { bubbles: true }));
        await fixture.whenStable();
    }

    it('shows empty state and import result, then reloads the list', async () => {
        const fixture = TestBed.createComponent(AdminDailyAdvicesComponent);
        fixture.detectChanges();
        await fixture.whenStable();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_DAILY_ADVICES.EMPTY');
        await selectFileAsync(fixture, JSON.stringify(DAILY_ADVICE_IMPORT_EXAMPLE));
        fixture.detectChanges();
        expect(importAdvices).toHaveBeenCalledWith(DAILY_ADVICE_IMPORT_EXAMPLE);
        expect(getAll).toHaveBeenCalledTimes(2);
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="status"]')?.textContent).toContain(
            'ADMIN_DAILY_ADVICES.SUCCESS',
        );
    });

    it('rejects invalid JSON and invalid fields before calling the API', async () => {
        const fixture = TestBed.createComponent(AdminDailyAdvicesComponent);
        await selectFileAsync(fixture, '{');
        expect(fixture.componentInstance['importError']()).toBe('ADMIN_DAILY_ADVICES.INVALID_JSON');
        await selectFileAsync(fixture, '{"version":1,"advices":[null]}');
        expect(fixture.componentInstance['importError']()).toBe('ADMIN_DAILY_ADVICES.INVALID_FORMAT');
        expect(importAdvices).not.toHaveBeenCalled();
    });

    it('prevents concurrent submissions and allows retry after an API failure', async () => {
        const pending = new Subject<AdminDailyAdvicesImportResponse>();
        importAdvices.mockReturnValueOnce(pending);
        const fixture = TestBed.createComponent(AdminDailyAdvicesComponent);
        const text = JSON.stringify(DAILY_ADVICE_IMPORT_EXAMPLE);
        await selectFileAsync(fixture, text);
        await selectFileAsync(fixture, text);
        expect(importAdvices).toHaveBeenCalledTimes(1);
        pending.error(new Error('offline'));
        expect(fixture.componentInstance['importing']()).toBe(false);
        expect(fixture.componentInstance['importError']()).toBe('ADMIN_DAILY_ADVICES.IMPORT_FAILED');
        await selectFileAsync(fixture, text);
        expect(importAdvices).toHaveBeenCalledTimes(2);
    });

    it('shows load error and can retry', async () => {
        getAll.mockReturnValueOnce(throwError(() => new Error('offline')));
        const fixture = TestBed.createComponent(AdminDailyAdvicesComponent);
        fixture.detectChanges();
        await fixture.whenStable();
        expect((fixture.nativeElement as HTMLElement).querySelector('fd-admin-load-error')).not.toBeNull();
        fixture.componentInstance['loadAdvices']();
        fixture.detectChanges();
        await fixture.whenStable();
        expect(fixture.componentInstance['loadFailed']()).toBe(false);
    });
});

describe('daily advices pagination', () => {
    const getAll = vi.fn();
    beforeEach(async () => {
        getAll.mockReset().mockReturnValue(of([]));
        await TestBed.configureTestingModule({
            providers: [
                provideRouter(
                    [{ path: 'daily-advices', component: AdminDailyAdvicesComponent }],
                    withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
                ),
                ...provideTranslateTesting(),
                { provide: AdminDailyAdvicesService, useValue: { getAll } },
            ],
        }).compileComponents();
    });

    it('keeps URL paging on reload and recovers a stale page after the list shrinks', async () => {
        const items: AdminDailyAdvice[] = Array.from({ length: 26 }, (_, index) => ({
            id: adminId<'daily-advice'>(`advice-${index}`),
            ru: `Совет ${index}`,
            en: `Advice ${index}`,
            weight: 1,
            tag: null,
        }));
        getAll.mockReturnValue(of(items));
        const router = TestBed.inject(Router);
        const harness = await RouterTestingHarness.create();
        const component = await harness.navigateByUrl('/daily-advices?page=2&page=1', AdminDailyAdvicesComponent);
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(component['pageIndex']()).toBe(1);
        expect(component['pageItems']()[0]?.id).toBe('advice-25');
        expect(new URL(router.url, 'http://localhost').searchParams.get('page')).toBe('2');
        const host = harness.routeNativeElement;
        const previous = host?.querySelector<HTMLButtonElement>('fd-ui-pagination button:first-child');
        expect(previous).not.toBeNull();
        previous?.click();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(host?.querySelector('fd-admin-daily-advice-group-card [lang="en"]')?.textContent).toBe('Advice 0');
        expect(new URL(router.url, 'http://localhost').searchParams.get('page')).toBe('1');
        await harness.navigateByUrl('/daily-advices');
        await harness.fixture.whenStable();
        expect(component['pageItems']()[0]?.id).toBe('advice-0');
        expect(getAll).toHaveBeenCalledOnce();
        await harness.navigateByUrl('/daily-advices?page=2');
        await harness.fixture.whenStable();
        getAll.mockReturnValueOnce(of(items.slice(0, 1)));
        component['loadAdvices']();
        harness.detectChanges();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(getAll).toHaveBeenCalledTimes(2);
        expect(component['pageItems']()[0]?.id).toBe('advice-0');
        expect(new URL(router.url, 'http://localhost').searchParams.get('page')).toBe('1');
    });
});

describe('daily advice mutation refresh', () => {
    it.each(['success', 'error'])('preserves another card draft while the refreshed list %s settles', async outcome => {
        const items: AdminDailyAdvice[] = ['A', 'B'].map(key => ({
            id: adminId<'daily-advice'>(key),
            ru: `${key} Russian`,
            en: `${key} English`,
            weight: 1,
            tag: null,
        }));
        const mutation = new Subject<AdminDailyAdvice>();
        const refresh = new Subject<AdminDailyAdvice[]>();
        const getAll = vi.fn().mockReturnValueOnce(of(items)).mockReturnValueOnce(refresh);
        await TestBed.configureTestingModule({
            imports: [AdminDailyAdvicesComponent],
            providers: [
                provideRouter([]),
                ...provideTranslateTesting(),
                { provide: AdminDailyAdvicesService, useValue: { getAll, update: (): Subject<AdminDailyAdvice> => mutation } },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminDailyAdvicesComponent);
        fixture.detectChanges();
        await fixture.whenStable();
        const cards = (): DailyAdviceGroupCardComponent[] =>
            fixture.debugElement
                .queryAll(By.directive(DailyAdviceGroupCardComponent))
                .map(node => node.componentInstance as DailyAdviceGroupCardComponent);
        const [first, second] = cards();
        first['edit']();
        const saving = first['saveAsync']();
        second['edit']();
        second['formModel'].update(value => ({ ...value, en: 'Newer unsaved draft' }));
        mutation.next(items[0]);
        await saving;
        fixture.detectChanges();
        expect(cards()[1]).toBe(second);
        expect(second['formModel']().en).toBe('Newer unsaved draft');
        if (outcome === 'error') {
            refresh.error(new Error('offline'));
        } else {
            refresh.next(items.map(item => ({ ...item })));
            refresh.complete();
        }
        await fixture.whenStable();
        expect(cards()[1]).toBe(second);
        expect(second['editing']()).toBe(true);
        expect(second['formModel']().en).toBe('Newer unsaved draft');
    });
});
