import { HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { developmentOperation } from './local-webmcp-request-metadata';
import { LocalWebmcpRequestsService } from './local-webmcp-requests.service';

describe('local request provenance', () => {
    it('clears session history without reusing IDs that a live owner may already reference', () => {
        const service = TestBed.inject(LocalWebmcpRequestsService);
        const first = service.begin('GET', new URL('http://localhost/api/v1/statistics/summary'));
        service.clear();
        const second = service.begin('GET', new URL('http://localhost/api/v1/statistics/summary'));
        first(HttpStatusCode.Ok);
        second(HttpStatusCode.Ok);
        expect(second.requestId).toBe((first.requestId ?? 0) + 1);
        expect(service.read()).toHaveLength(1);
        expect(service.read()[0].id).toBe(second.requestId);
    });
    it('identifies cycle and client operations without retaining IDs or query values', () => {
        expect(developmentOperation('GET', '/api/v1/cycles/current/nutrition-summary')).toBe(
            'GET /api/v{version}/cycles/current/nutrition-summary',
        );
        expect(developmentOperation('PUT', '/api/v1/cycles/59050212-47cf-4c78-903b-882809d3ef19/settings')).toBe(
            'PUT /api/v{version}/cycles/{cycleProfileId}/settings',
        );
        expect(developmentOperation('GET', '/api/v1/dietologist/clients/59050212-47cf-4c78-903b-882809d3ef19/dashboard')).toBe(
            'GET /api/v{version}/dietologist/clients/{clientUserId}/dashboard',
        );
        expect(developmentOperation('GET', '/api/v1/cycles/private-cycle/unknown-secret')).toBeNull();
        expect(developmentOperation('GET', '/api/v1/meals/overview')).toBe('GET /api/v{version}/meals/overview');
        expect(developmentOperation('GET', '/api/v1/recipes/public/categories')).toBe('GET /api/v{version}/recipes/public/categories');
        expect(developmentOperation('GET', '/api/v1/meals/not-a-uuid')).toBeNull();
    });

    it('retains independently observed API identities and rejects unexpected header contents', () => {
        const service = TestBed.inject(LocalWebmcpRequestsService);
        const fingerprintLength = 64;
        const build = 'a'.repeat(fingerprintLength);
        const instance = '59050212-47cf-4c78-903b-882809d3ef19';
        service.begin('GET', new URL('http://localhost/api/v1/cycles/current?token=private-query'))(HttpStatusCode.Ok, {
            apiBuild: build,
            apiInstance: instance,
        });
        service.begin('GET', new URL('http://localhost/api/v1/cycles/private-cycle/settings'))(HttpStatusCode.InternalServerError, {
            apiBuild: 'private-header',
            apiInstance: instance,
        });
        service.begin('GET', new URL('http://localhost/api/v1/auth/private-auth'))(HttpStatusCode.Ok, {
            apiBuild: build,
            apiInstance: instance,
        });
        expect(service.read()).toHaveLength(2);
        expect(service.read()[0]).toMatchObject({
            resource: 'cycles',
            operation: 'GET /api/v{version}/cycles/current',
            apiRuntime: { binaryFingerprint: build, instanceId: instance },
        });
        expect(service.read()[1]).toMatchObject({ status: 500, apiRuntime: null });
        expect(JSON.stringify(service.read())).not.toMatch(/private-query|private-cycle|private-header|private-auth/);
        service.clear();
        expect(service.read()).toEqual([]);
    });
});
