import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { environment } from '../../../../environments/environment';
import { DAILY_ADVICE_IMPORT_EXAMPLE, MAX_ADVICE_IMPORT_BYTES } from '../lib/daily-advice-import';
import { AdminDailyAdvicesComponent } from './admin-daily-advices';

const baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/daily-advices`;
let fixture: ComponentFixture<AdminDailyAdvicesComponent>;
let root: HTMLElement;
let http: HttpTestingController;

beforeEach(async () => {
    await TestBed.configureTestingModule({
        imports: [AdminDailyAdvicesComponent],
        providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(), ...provideTranslateTesting()],
    }).compileComponents();
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { ADMIN_DAILY_ADVICES: { SUCCESS: 'Imported {{imported}}, skipped {{skipped}}' } });
    await firstValueFrom(translate.use('en'));
    fixture = TestBed.createComponent(AdminDailyAdvicesComponent);
    root = fixture.nativeElement as HTMLElement;
    http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.method === 'GET' && request.url === `${baseUrl}/groups`).flush([]);
    fixture.detectChanges();
});

afterEach(() => {
    vi.restoreAllMocks();
    http.verify();
});

function button(key: string): HTMLButtonElement {
    const control = Array.from(root.querySelectorAll('button')).find(item => item.textContent.trim() === `ADMIN_DAILY_ADVICES.${key}`);
    if (control === undefined) {
        throw new Error(`Missing daily advice action ${key}`);
    }
    return control;
}

async function selectFileAsync(contents?: string, size?: number): Promise<void> {
    const input = root.querySelector<HTMLInputElement>('input#advice-import');
    if (input === null) {
        throw new Error('Missing daily advice file chooser');
    }
    const files: File[] = [];
    if (contents !== undefined) {
        const file = new File([contents], 'advices.json', { type: 'application/json' });
        Object.defineProperty(file, 'text', { value: vi.fn<() => Promise<string>>().mockResolvedValue(contents) });
        if (size !== undefined) {
            Object.defineProperty(file, 'size', { value: size });
        }
        files.push(file);
    }
    Object.defineProperty(input, 'files', { configurable: true, value: files });
    input.dispatchEvent(new Event('change', { bubbles: true }));
    await fixture.whenStable();
    fixture.detectChanges();
}

async function readExampleAsync(value: Blob | MediaSource | undefined): Promise<string> {
    if (!(value instanceof Blob)) {
        throw new Error('Daily advice example download did not produce a file');
    }
    return new Promise<string>((resolve, reject) => {
        const reader = new FileReader();
        reader.addEventListener('loadend', () => {
            if (typeof reader.result === 'string') {
                resolve(reader.result);
            } else {
                reject(new Error('Could not read the daily advice example download'));
            }
        });
        reader.readAsText(value);
    });
}

it('downloads a reusable bilingual example, imports it through the SDK and renders the refreshed advice', async () => {
    const blobUrl = 'blob:daily-advice-example';
    const makeUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue(blobUrl);
    const revokeUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    const link = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
    expect(root.textContent).toContain('ADMIN_DAILY_ADVICES.EMPTY');
    button('EXAMPLE').click();
    expect(link.mock.contexts[0]).toMatchObject({ download: 'fooddiary-daily-advices.example.json', href: blobUrl });
    expect(revokeUrl).toHaveBeenCalledExactlyOnceWith(blobUrl);
    const contents = await readExampleAsync(makeUrl.mock.calls[0]?.[0]);
    expect(JSON.parse(contents)).toEqual(DAILY_ADVICE_IMPORT_EXAMPLE);

    await selectFileAsync(contents);
    const request = http.expectOne({ method: 'POST', url: `${baseUrl}/groups/import` });
    expect(request.request.body).toEqual(DAILY_ADVICE_IMPORT_EXAMPLE);
    expect(request.request.headers.get('Idempotency-Key')).toBeTruthy();
    expect(button('IMPORTING').disabled).toBe(true);
    request.flush({ importedCount: 2, skippedCount: 1 });
    http.expectOne(item => item.method === 'GET' && item.url === `${baseUrl}/groups`).flush(DAILY_ADVICE_IMPORT_EXAMPLE.advices);
    fixture.detectChanges();
    expect(root.querySelector('[role="status"]')?.textContent).toContain('Imported 2, skipped 1');
    expect(root.querySelector('[lang="en"]')?.textContent).toBe('Drink water throughout the day.');
    expect(button('IMPORT').disabled).toBe(false);
});

it('ignores an empty selection, rejects an oversized file, and recovers with a legacy import', async () => {
    await selectFileAsync();
    expect(root.querySelector('[role="alert"]')).toBeNull();
    await selectFileAsync('{}', MAX_ADVICE_IMPORT_BYTES + 1);
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_DAILY_ADVICES.FILE_TOO_LARGE');
    http.expectNone({ method: 'POST' });

    const legacy = { version: 1, advices: [{ value: 'Build a routine', locale: 'en-US' }] };
    await selectFileAsync(JSON.stringify(legacy));
    const request = http.expectOne({ method: 'POST', url: `${baseUrl}/import` });
    expect(root.querySelector('[role="alert"]')).toBeNull();
    expect(request.request.body).toEqual(legacy);
    request.flush({ importedCount: 1, skippedCount: 0 });
    http.expectOne(item => item.method === 'GET' && item.url === `${baseUrl}/groups`).flush([
        { id: 'legacy-advice', en: 'Build a routine', weight: 1 },
    ]);
    fixture.detectChanges();

    expect(root.querySelector('[role="status"]')?.textContent).toContain('Imported 1, skipped 0');
    expect(root.querySelector('[lang="en"]')?.textContent).toBe('Build a routine');
    expect(root.querySelector('[lang="ru"]')?.textContent).toContain('ADMIN_DAILY_ADVICES.MISSING');
});
