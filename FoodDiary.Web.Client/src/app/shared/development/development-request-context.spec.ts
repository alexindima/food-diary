import { HTTP_INTERCEPTORS, HttpClient, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { LocalWebmcpRequestsService } from '../../composition/webmcp/local-webmcp-requests.service';
import { FrontendObservabilityInterceptor } from '../../interceptor/frontend-observability.interceptor';
import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import { RequestStateController } from '../lib/request-state';
import { developmentRequestContext } from './development-request-context';
import type { DevelopmentRequestCompletion } from './development-tools';
import { DEVELOPMENT_REQUESTS } from './development-tools';

const HTTP_OK = 200;
const INITIAL_HTTP_ID = 11;
const FAILED_HTTP_ID = 12;
const LATE_HTTP_ID = 13;

describe('HTTP response application provenance', () => {
    it('keeps the newer applied response when both HTTP requests return 200 in reverse order', () => {
        TestBed.configureTestingModule({ providers: [
            provideHttpClient(withInterceptorsFromDi()), provideHttpClientTesting(),
            { provide: HTTP_INTERCEPTORS, useClass: FrontendObservabilityInterceptor, multi: true },
            { provide: FrontendObservabilityService, useValue: { recordHttpRequest: vi.fn() } },
            { provide: DEVELOPMENT_REQUESTS, useFactory: (): { observe: (method: string, url: string) => DevelopmentRequestCompletion } => {
                const requests = TestBed.inject(LocalWebmcpRequestsService);
                return { observe: (method: string, url: string): DevelopmentRequestCompletion => requests.begin(method, new URL(url, 'http://localhost')) };
            } },
        ] });
        const owner = new RequestStateController<string>();
        const http = TestBed.inject(HttpClient);
        const testing = TestBed.inject(HttpTestingController);
        const load = (search: string): number => {
            const revision = owner.begin();
            http.get<string>(`/api/v1/products?search=${search}`, { context: developmentRequestContext(owner.bindRequest(revision)) })
                .subscribe(data => owner.succeed(revision, data));
            return revision;
        };
        load('private-old'); const current = load('private-new');
        const old = testing.expectOne('/api/v1/products?search=private-old');
        const latest = testing.expectOne('/api/v1/products?search=private-new');
        latest.flush('new');
        const applied = owner.provenance();
        old.flush('old');
        expect(owner.data()).toBe('new');
        expect(owner.provenance()).toEqual(applied);
        expect(applied).toEqual({ queryRevision: current, lastAppliedQueryRevision: current, lastAppliedRequestId: 2 });
        const history = TestBed.inject(LocalWebmcpRequestsService).read();
        expect(history.map(item => item.status)).toEqual([HTTP_OK, HTTP_OK]);
        expect(JSON.stringify(history)).not.toContain('private-');
        expect(latest.request.headers.keys()).toEqual([]);
        testing.verify();
    });

    it('preserves the last applied response through failure and invalidates it on reset', () => {
        const owner = new RequestStateController<string>();
        const initial = owner.begin(); owner.bindRequest(initial)(INITIAL_HTTP_ID); owner.succeed(initial, 'cached');
        const failed = owner.begin(); owner.bindRequest(failed)(FAILED_HTTP_ID); owner.fail(failed, 'failed');
        expect(owner.provenance().lastAppliedRequestId).toBe(INITIAL_HTTP_ID);
        const late = owner.begin(); owner.bindRequest(late)(LATE_HTTP_ID); owner.reset();
        expect(owner.succeed(late, 'late')).toBe(false);
        expect(owner.provenance().lastAppliedRequestId).toBeNull();
        expect(owner.data()).toBeNull();
    });
});
