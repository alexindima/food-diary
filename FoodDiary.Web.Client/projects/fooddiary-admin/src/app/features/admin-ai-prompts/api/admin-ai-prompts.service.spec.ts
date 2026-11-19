import { DOCUMENT } from '@angular/common';
import { type HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, onTestFinished } from 'vitest';

import { COLLECTION_PAGE_SIZE } from '../../../shared/api/load-paged-collection';
import type { AdminAiPromptScenarioHttpResponse } from '../../../shared/api/sdk/generated/model/admin-ai-prompt-scenario-http-response';
import { AdminAiPromptsFacade } from '../lib/admin-ai-prompts.facade';
import type { AdminAiPromptDraft } from '../models/admin-ai-prompt-scenario';
import { AdminAiPromptsService } from './admin-ai-prompts.service';

const testAuthInterceptor: HttpInterceptorFn = (request, next) =>
    next(request.clone({ setHeaders: { Authorization: 'Bearer test-only' } }));
const promptsUrl = 'http://localhost:5300/api/v1/admin/ai-prompts';

beforeEach(() =>
    TestBed.configureTestingModule({
        providers: [provideHttpClient(withInterceptors([testAuthInterceptor])), provideHttpClientTesting()],
    }),
);
afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
});

describe('AdminAiPromptsService sample upload', () => {
    it('uploads photo bytes without forwarding the admin bearer token to the signed storage URL', () => {
        const service = TestBed.inject(AdminAiPromptsService);
        const http = TestBed.inject(HttpTestingController);
        const file = new File(['image bytes'], 'meal.png', { type: 'image/png' });
        let result: string | undefined;
        service.uploadImage(file).subscribe(value => {
            result = value.assetId;
        });
        const request = http.expectOne('http://localhost:5300/api/v1/images/upload-url');
        expect(request.request.headers.has('Authorization')).toBe(true);
        request.flush({ uploadUrl: 'https://storage.example.test/signed', assetId: 'asset' });
        const upload = http.expectOne('https://storage.example.test/signed');
        expect(upload.request.headers.has('Authorization')).toBe(false);
        expect(upload.request.body).toBe(file);
        upload.flush('');
        const confirm = http.expectOne('http://localhost:5300/api/v1/images/asset/confirm');
        expect(confirm.request.headers.has('Authorization')).toBe(true);
        confirm.flush({ assetId: 'asset' });
        expect(result).toBe('asset');
    });
});

describe('AdminAiPromptsService secure browser requirement', () => {
    it('fails before a billed test request when secure UUID generation is unavailable', () => {
        const service = TestBed.inject(AdminAiPromptsService);
        const http = TestBed.inject(HttpTestingController);
        const crypto = TestBed.inject(DOCUMENT).defaultView?.crypto;
        if (crypto === undefined) {
            throw new Error('Expected a browser Crypto object.');
        }
        const descriptor = Object.getOwnPropertyDescriptor(crypto, 'randomUUID');
        Object.defineProperty(crypto, 'randomUUID', { configurable: true, value: undefined });
        onTestFinished(() => {
            if (descriptor === undefined) {
                Reflect.deleteProperty(crypto, 'randomUUID');
            } else {
                Object.defineProperty(crypto, 'randomUUID', descriptor);
            }
        });

        const draft: AdminAiPromptDraft = { key: 'text-parse', locale: 'en', promptText: 'Parse {{userText}}', text: 'an apple' };
        expect(() => service.test(draft)).toThrow('A secure browser context is required.');
        http.expectNone(`${promptsUrl}/test`);
    });
});

it('loads stored prompt versions across SDK pages through the existing facade', async () => {
    const facade = TestBed.inject(AdminAiPromptsFacade);
    const http = TestBed.inject(HttpTestingController);
    const storedPrompt = {
        id: 'nutrition-en',
        key: 'nutrition',
        locale: 'en',
        promptText: 'Estimate nutrition',
        version: 2,
        isActive: true,
        createdOnUtc: '2026-10-01T00:00:00Z',
    };
    const saved = firstValueFrom(facade.getAll());
    const first = http.expectOne(request => request.method === 'GET' && request.url === promptsUrl && request.params.get('page') === '1');
    expect(first.request.params.get('limit')).toBe(String(COLLECTION_PAGE_SIZE));
    const firstPage = Array.from({ length: COLLECTION_PAGE_SIZE }, (_, index) => ({ ...storedPrompt, id: `saved-${index}` }));
    first.flush(firstPage);
    const second = http.expectOne(request => request.method === 'GET' && request.url === promptsUrl && request.params.get('page') === '2');
    const translated = { ...storedPrompt, id: 'nutrition-ru', locale: 'ru', promptText: 'Оцени пищевую ценность' };
    second.flush([translated]);
    expect(await saved).toEqual([...firstPage, translated].map(item => ({ ...item, updatedOnUtc: null })));
});

describe('AdminAiPromptsService editor API', () => {
    it('loads the inherited scenario and sends only the selected locale’s draft on save', async () => {
        const service = TestBed.inject(AdminAiPromptsService);
        const http = TestBed.inject(HttpTestingController);
        const inherited: AdminAiPromptScenarioHttpResponse = {
            key: 'nutrition',
            locale: 'ru',
            promptText: 'English fallback',
            source: 'english',
            sourceLocale: 'en',
            inheritedPromptText: 'English fallback',
            inheritedSource: 'english',
            responseFormatJson: '{}',
            variables: ['foodName'],
        };

        const scenarios = firstValueFrom(service.getScenarios());
        http.expectOne({ method: 'GET', url: `${promptsUrl}/scenarios` }).flush([inherited]);
        expect(await scenarios).toEqual([{ ...inherited, template: null }]);

        const promptText = 'Оцени пищевую ценность';
        const saved = firstValueFrom(service.save('nutrition', 'ru', promptText, true));
        const request = http.expectOne({ method: 'PUT', url: `${promptsUrl}/nutrition/ru` });
        expect(request.request.body).toEqual({ promptText, isActive: true });
        const response = {
            id: 'nutrition-ru',
            key: 'nutrition',
            locale: 'ru',
            promptText,
            isActive: true,
            version: 2,
            createdOnUtc: '2026-10-01T00:00:00Z',
        };
        request.flush(response);
        expect(await saved).toEqual({ ...response, updatedOnUtc: null });
    });

    it('uses a separate idempotency key for every explicit test and keeps preview side effect free', async () => {
        const service = TestBed.inject(AdminAiPromptsService);
        const http = TestBed.inject(HttpTestingController);
        const draft: AdminAiPromptDraft = { key: 'text-parse', locale: 'en', promptText: 'Parse {{userText}}', text: 'an apple' };
        const preview = firstValueFrom(service.preview(draft));
        const previewRequest = http.expectOne({ method: 'POST', url: `${promptsUrl}/preview` });
        expect(previewRequest.request.body).toEqual(draft);
        expect(previewRequest.request.headers.has('Idempotency-Key')).toBe(false);
        previewRequest.flush({ text: 'Parse an apple' });
        expect(await preview).toEqual({ text: 'Parse an apple' });

        const first = firstValueFrom(service.test(draft));
        const second = firstValueFrom(service.test(draft));
        const testRequests = http.match({ method: 'POST', url: `${promptsUrl}/test` });
        expect(testRequests).toHaveLength(2);
        const keys = testRequests.map(request => request.request.headers.get('Idempotency-Key'));
        expect(keys).not.toContain(null);
        expect(new Set(keys).size).toBe(2);
        for (const request of testRequests) {
            expect(request.request.body).toEqual(draft);
            request.flush({ text: '{"items":[]}' });
        }
        expect(await Promise.all([first, second])).toEqual([{ text: '{"items":[]}' }, { text: '{"items":[]}' }]);
    });
});
