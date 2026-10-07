import assert from 'node:assert/strict';
import test from 'node:test';

import { findApiClientUsageViolations } from './check-api-client-usage.mjs';

const http = `import { HttpClient, HttpBackend, HttpRequest } from '@angular/common/http';
declare const client: HttpClient;
declare const backend: HttpBackend;`;
const fixtures = [
    [
        'mapped HttpClient type',
        'src/app/shared/api/pick.ts',
        `${http} declare const selected: Pick<HttpClient, 'get'>; selected.get('/api/v1/users');`,
        true,
    ],
    [
        'typed detached HTTP function',
        'src/app/shared/api/typed-function.ts',
        `${http} declare const send: HttpClient['get']; send('/api/v1/users');`,
        true,
    ],
    [
        'nullable HttpClient',
        'src/app/shared/api/nullable.ts',
        `${http} declare const nullable: HttpClient | null; nullable?.get('/api/v1/users');`,
        true,
    ],
    [
        'dynamic bracket method',
        'src/app/shared/api/dynamic.ts',
        `${http} declare const method: 'get' | 'delete'; client[method]('/api/v1/users');`,
        true,
    ],
    [
        'namespace httpResource',
        'src/app/shared/api/namespace.ts',
        `import * as http from '@angular/common/http'; http.httpResource('/api/v1/users');`,
        true,
    ],
    ['dynamic HTTP library import', 'src/app/shared/api/import.ts', `import('axios');`, true],
    [
        'approved auth retry forwarding',
        'src/app/interceptor/auth.interceptor.ts',
        `${http} import { HttpHandler } from '@angular/common/http'; class AuthInterceptor { refreshRequest(req: HttpRequest<unknown>, next: HttpHandler) { return next.handle(req); } }`,
        false,
    ],
    [
        'new dispatch in interceptor file',
        'src/app/interceptor/auth.interceptor.ts',
        `${http} import { HttpHandler } from '@angular/common/http'; class AuthInterceptor { legacy(req: HttpRequest<unknown>, next: HttpHandler) { return next.handle(req); } }`,
        true,
    ],
    [
        'copied interceptor name',
        'src/app/interceptor/legacy.interceptor.ts',
        `${http} import { HttpHandler } from '@angular/common/http'; class AuthInterceptor { intercept(req: HttpRequest<unknown>, next: HttpHandler) { return next.handle(req); } }`,
        true,
    ],
    [
        'direct GET with a dynamic API URL',
        'src/app/features/products/api/legacy.ts',
        `${http} client.get(environment.apiUrls.products);`,
        true,
    ],
    [
        'renamed import and detached method',
        'src/app/features/products/api/alias.ts',
        `import { HttpClient as Transport } from '@angular/common/http'; declare const connection: Transport; const send = connection.post;`,
        true,
    ],
    ['bracket HTTP method', 'src/app/features/products/api/bracket.ts', `${http} client['patch']('/api/v1/products');`, true],
    ['destructured HTTP method', 'src/app/features/products/api/destructure.ts', `${http} const { get: query } = client;`, true],
    ['raw request object', 'src/app/features/products/api/request.ts', `${http} new HttpRequest('GET', '/api/v1/products');`, true],
    [
        'backend bypass',
        'src/app/features/products/api/backend.ts',
        `${http} backend.handle(new HttpRequest('GET', '/api/v1/products'));`,
        true,
    ],
    ['native fetch alias', 'src/app/shared/api/native.ts', `const send = fetch; send('/api/v1/users');`, true],
    ['window fetch', 'src/app/shared/api/window.ts', `window.fetch('/api/v1/users');`, true],
    ['destructured window fetch', 'src/app/shared/api/window-alias.ts', `const { fetch: send } = window;`, true],
    ['XMLHttpRequest', 'src/app/shared/api/xhr.ts', `new XMLHttpRequest();`, true],
    ['beacon HTTP', 'src/app/shared/api/beacon.ts', `navigator.sendBeacon('/api/v1/logs', '{}');`, true],
    [
        'alternative HTTP library',
        'projects/fooddiary-admin/src/app/shared/api/legacy.ts',
        `import axios from 'axios'; axios.get('/api/v1/admin/users');`,
        true,
    ],
    [
        'Angular httpResource',
        'src/app/shared/api/resource.ts',
        `import { httpResource as resource } from '@angular/common/http'; resource('/api/v1/users');`,
        true,
    ],
    [
        'generated-looking feature directory',
        'src/app/features/products/generated/legacy.ts',
        `${http} client.get('/api/v1/products');`,
        true,
    ],
    [
        'actual generated output',
        'src/app/shared/api/sdk/generated/api/products.service.ts',
        `${http} client.get('/api/v1/products');`,
        false,
    ],
    ['local method named fetch', 'src/app/features/products/lib/list.ts', `class List { fetch() {} load() { this.fetch(); } }`, false],
    [
        'generated method call',
        'src/app/features/products/api/product.service.ts',
        `class ProductsSdk { getProductById() {} } new ProductsSdk().getProductById();`,
        false,
    ],
    [
        'signed upload',
        'src/app/shared/api/image-upload.service.ts',
        `${http} class ImageUploadService { uploadToPresignedUrl(uploadUrl: string, file: Blob) { return client.put(uploadUrl, file); } }`,
        false,
    ],
    [
        'new API call inside upload file',
        'src/app/shared/api/image-upload.service.ts',
        `${http} class ImageUploadService { getLegacy() { return client.get('/api/v1/images'); } }`,
        true,
    ],
    [
        'API target in signed upload method',
        'src/app/shared/api/image-upload.service.ts',
        `${http} class ImageUploadService { uploadToPresignedUrl(uploadUrl: string, file: Blob) { return client.put('/api/v1/images', file); } }`,
        true,
    ],
    [
        'translation assets',
        'src/app/shared/i18n/food-diary-translation.loader.ts',
        http +
            'class FoodDiaryTranslationLoader { loadBundle(lang: string) { const url = `./assets/i18n/${lang}/core.json`; return client.get(url); } }',
        false,
    ],
    [
        'API target in translation loader',
        'src/app/shared/i18n/food-diary-translation.loader.ts',
        `${http} class FoodDiaryTranslationLoader { loadBundle(lang: string) { const url = '/api/v1/users'; return client.get(url); } }`,
        true,
    ],
    [
        'generated transport forwarding',
        'src/app/shared/api/sdk/sdk-connection.ts',
        `${http} function createSdkConnection(request: HttpRequest<unknown>, headers: unknown, params: unknown) { return client.request(request.clone({ headers, params })); }`,
        false,
    ],
    [
        'new URL in generated transport',
        'src/app/shared/api/sdk/sdk-connection.ts',
        `${http} function createSdkConnection() { return client.request('GET', '/api/v1/users'); }`,
        true,
    ],
    [
        'URL rewrite in generated transport',
        'src/app/shared/api/sdk/sdk-connection.ts',
        `${http} function createSdkConnection(request: HttpRequest<unknown>) { return client.request(request.clone({ url: '/api/v1/users' })); }`,
        true,
    ],
    [
        'admin signed upload',
        'projects/fooddiary-admin/src/app/features/admin-ai-prompts/api/admin-ai-prompts.service.ts',
        `${http} class AdminAiPromptsService { uploadImage(upload: { uploadUrl: string }, file: Blob) { return client.put(upload.uploadUrl, file); } }`,
        false,
    ],
    [
        'extra API in admin upload file',
        'projects/fooddiary-admin/src/app/features/admin-ai-prompts/api/admin-ai-prompts.service.ts',
        `${http} class AdminAiPromptsService { legacy() { return client.post('/api/v1/admin/ai-prompts', {}); } }`,
        true,
    ],
];

for (const [name, path, content, rejected] of fixtures) {
    test(`${rejected ? 'rejects' : 'allows'} ${name}`, () => {
        const violations = findApiClientUsageViolations([{ path, content }]);
        assert.equal(violations.length > 0, rejected, violations.join('\n'));
    });
}
