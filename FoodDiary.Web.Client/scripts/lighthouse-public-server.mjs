import { createServer, request as httpRequest } from 'node:http';
import { createSecureServer } from 'node:http2';
import { createHash, X509Certificate } from 'node:crypto';
import { request as httpsRequest } from 'node:https';
import { readFileSync, realpathSync, statSync } from 'node:fs';
import path from 'node:path';
import { gzipSync } from 'node:zlib';

import { normalizeRoute, validateOrigin } from './lighthouse-public.mjs';

const contentTypes = new Map([
    ['.css', 'text/css; charset=utf-8'],
    ['.html', 'text/html; charset=utf-8'],
    ['.ico', 'image/x-icon'],
    ['.js', 'text/javascript; charset=utf-8'],
    ['.json', 'application/json; charset=utf-8'],
    ['.png', 'image/png'],
    ['.svg', 'image/svg+xml'],
    ['.webp', 'image/webp'],
    ['.jpg', 'image/jpeg'],
    ['.woff2', 'font/woff2'],
    ['.txt', 'text/plain; charset=utf-8'],
]);

export function prerenderFile(root, route) {
    const normalized = normalizeRoute(route);
    const file = path.join(root, normalized.slice(1), 'index.html');
    if (!statSync(file).isFile()) throw new Error(`Missing prerendered HTML: ${route}`);
    return file;
}

export async function startStaticServer(staticRoot, apiOrigin, { transport = 'http1' } = {}) {
    if (!['http1', 'http2'].includes(transport)) throw new Error('Unknown static server transport.');
    const root = realpathSync(staticRoot);
    const api = apiOrigin ? new URL(validateOrigin(apiOrigin)) : undefined;
    const cache = new Map();
    const listener = (request, response) => {
        try {
            const requestUrl = request.url ?? '/';
            const decoded = decodeURIComponent(requestUrl.split(/[?#]/)[0]);
            if (
                !decoded.startsWith('/') ||
                decoded.startsWith('//') ||
                decoded.includes('\\') ||
                decoded.includes('\0') ||
                decoded.split('/').some(segment => segment === '..' || segment === '.')
            ) {
                response.writeHead(400).end('Invalid path');
                return;
            }
            const url = new URL(requestUrl, 'http://localhost');
            if (api && url.pathname.startsWith('/api/')) {
                const headers = { ...request.headers, host: api.host };
                for (const name of Object.keys(headers)) if (name.startsWith(':')) delete headers[name];
                delete headers.authorization;
                delete headers.cookie;
                const forward = (api.protocol === 'https:' ? httpsRequest : httpRequest)(
                    new URL(`${url.pathname}${url.search}`, api),
                    {
                        method: request.method,
                        headers,
                        timeout: 15000,
                    },
                    upstream => {
                        const responseHeaders = { ...upstream.headers };
                        delete responseHeaders['set-cookie'];
                        for (const name of [
                            'connection',
                            'keep-alive',
                            'proxy-connection',
                            'transfer-encoding',
                            'upgrade',
                            'http2-settings',
                        ])
                            delete responseHeaders[name];
                        response.writeHead(upstream.statusCode ?? 502, responseHeaders);
                        upstream.pipe(response);
                    },
                );
                forward.on('timeout', () => forward.destroy(new Error('API proxy timed out.')));
                forward.on('error', () => {
                    if (!response.headersSent) response.writeHead(502);
                    response.end('API proxy unavailable');
                });
                request.on('aborted', () => forward.destroy());
                request.pipe(forward);
                return;
            }
            if (!['GET', 'HEAD'].includes(request.method ?? '')) {
                response.writeHead(405).end();
                return;
            }
            const pathname = decoded;
            const relative = pathname.slice(1);
            const routePath = path.extname(relative) ? relative : path.join(relative, 'index.html');
            const file = realpathSync(path.resolve(root, routePath));
            if (!file.startsWith(`${root}${path.sep}`) || !statSync(file).isFile()) {
                response.writeHead(404).end('Not found');
                return;
            }
            let entry = cache.get(file);
            if (!entry) {
                const plain = readFileSync(file);
                const compressible = ['.css', '.html', '.js', '.json', '.svg', '.txt'].includes(path.extname(file));
                entry = { plain, gzip: compressible ? gzipSync(plain) : undefined };
                cache.set(file, entry);
            }
            const gzip = entry.gzip && /(?:^|,)\s*gzip\s*(?:,|$)/i.test(request.headers['accept-encoding'] ?? '');
            const body = gzip ? entry.gzip : entry.plain;
            response.writeHead(200, {
                'Content-Type': contentTypes.get(path.extname(file)) ?? 'application/octet-stream',
                'Content-Length': body.length,
                'Cache-Control': 'no-store',
                Vary: 'Accept-Encoding',
                ...(gzip ? { 'Content-Encoding': 'gzip' } : {}),
            });
            response.end(request.method === 'HEAD' ? undefined : body);
        } catch {
            response.writeHead(404).end('Not found');
        }
    };
    let certificate, certificateSPKI;
    let server;
    const sessions = new Set();
    if (transport === 'http2') {
        const { generate } = await import('selfsigned');
        const pems = await generate([{ name: 'commonName', value: 'localhost' }], {
            keySize: 2048,
            algorithm: 'sha256',
            notBeforeDate: new Date(Date.now() - 60_000),
            notAfterDate: new Date(Date.now() + 3_600_000),
        });
        certificate = pems.cert;
        certificateSPKI = createHash('sha256')
            .update(new X509Certificate(certificate).publicKey.export({ type: 'spki', format: 'der' }))
            .digest('base64');
        server = createSecureServer({ cert: certificate, key: pems.private, allowHTTP1: true }, listener);
        server.on('session', session => {
            sessions.add(session);
            session.once('close', () => sessions.delete(session));
        });
    } else server = createServer(listener);
    await new Promise((resolve, reject) => {
        server.once('error', reject);
        server.listen(0, '127.0.0.1', resolve);
    });
    const address = server.address();
    if (!address || typeof address === 'string') throw new Error('Cannot determine audit server port.');
    return {
        origin: `${transport === 'http2' ? 'https' : 'http'}://127.0.0.1:${address.port}`,
        certificate,
        certificateSPKI,
        close: () =>
            new Promise((resolve, reject) => {
                server.close(error => (error ? reject(error) : resolve()));
                for (const session of sessions) session.destroy();
                server.closeAllConnections?.();
            }),
    };
}
