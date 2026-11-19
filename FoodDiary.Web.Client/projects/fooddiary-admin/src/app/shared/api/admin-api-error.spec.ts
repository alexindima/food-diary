import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { expect, it } from 'vitest';

import { adminApiErrorCode } from './admin-api-error';

it('extracts the API code used to route administrative reauthentication', () => {
    const error = 'Authentication.ReauthenticationRequired';
    const response = new HttpErrorResponse({ status: HttpStatusCode.Forbidden, error: { error } });

    expect(adminApiErrorCode(response)).toBe(error);
});

it.each([
    { name: 'an ordinary runtime exception', thrown: new Error('network is offline') },
    { name: 'an empty response', thrown: new HttpErrorResponse({ status: HttpStatusCode.BadGateway }) },
    { name: 'a gateway text page', thrown: new HttpErrorResponse({ status: HttpStatusCode.BadGateway, error: 'gateway error' }) },
    {
        name: 'different error metadata',
        thrown: new HttpErrorResponse({ status: HttpStatusCode.Forbidden, error: { message: 'Forbidden' } }),
    },
    { name: 'a numeric error code', thrown: new HttpErrorResponse({ status: HttpStatusCode.Forbidden, error: { error: 42 } }) },
])('lets the caller fall back for $name', ({ thrown }) => {
    expect(adminApiErrorCode(thrown)).toBeNull();
});
