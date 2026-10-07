import assert from 'node:assert/strict';
import test from 'node:test';

import { normalizeBinaryResponseTypes } from './api-sdk-normalization.mjs';

const source = `class Client {
    download(): Observable<Blob>;
    download(): Observable<any> {
        const localVarHttpHeaderAcceptSelected = 'text/csv';
        let responseType_: 'text' | 'json' | 'blob' = 'json';
        if (localVarHttpHeaderAcceptSelected) {
            if (localVarHttpHeaderAcceptSelected.startsWith('text')) responseType_ = 'text';
            else responseType_ = 'json';
        }
        return this.http.request('get', '/file', { responseType: responseType_ });
    }
}`;

test('keeps binary downloads as Blob while retaining their media type and overloads', () => {
    const result = normalizeBinaryResponseTypes(source, 'client.ts', new Set(['download']));
    assert.match(result, /const responseType_ = 'blob' as const/);
    assert.match(result, /'text\/csv'/);
    assert.match(result, /download\(\): Observable<Blob>;/);
    assert.doesNotMatch(result, /responseType_ = 'text'/);
});

test('does not modify an operation that is not binary', () => {
    assert.equal(normalizeBinaryResponseTypes(source, 'client.ts', new Set()), source);
});

test('preserves the pinned generator template which already requests Blob', () => {
    const binary = "class Client { download() { return this.http.request('get', '/file', { responseType: 'blob' }); } }";
    assert.equal(normalizeBinaryResponseTypes(binary, 'client.ts', new Set(['download'])), binary);
});

test('fails closed when a changed upstream template cannot be normalized', () => {
    assert.throws(
        () => normalizeBinaryResponseTypes('class Client { download() { return null; } }', 'client.ts', new Set(['download'])),
        /Unsupported binary SDK template/,
    );
});
