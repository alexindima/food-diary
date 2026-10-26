import assert from 'node:assert/strict';

/** Exercise the actual serving configuration, including regex location precedence. */
export async function assertPwaCachePolicy(origin) {
    const manifestPath = '/assets/manifest.webmanifest';
    const manifest = await (await fetch(`${origin}${manifestPath}`)).json();
    const paths = new Set([manifestPath, '/assets/pwa/app-icon-180x180.png', ...manifest.icons.map(icon => icon.src)]);
    const observations = [];
    for (const pathname of paths) {
        const response = await fetch(`${origin}${pathname}`);
        assert.equal(response.status, 200, `${pathname} must exist.`);
        const cacheControl = response.headers.get('cache-control') ?? '';
        assert.match(cacheControl, /\bno-cache\b/u, `PWA install assets must revalidate: ${pathname}`);
        assert.doesNotMatch(cacheControl, /immutable|max-age=[1-9]/u, `${pathname} must not retain obsolete install metadata.`);
        assert.ok(response.headers.get('content-security-policy'), 'PWA cache overrides must preserve inherited security headers.');
        const etag = response.headers.get('etag');
        assert.ok(etag, 'Revalidation must preserve efficient conditional requests.');
        const conditional = await fetch(`${origin}${pathname}`, { headers: { 'If-None-Match': etag } });
        assert.equal(conditional.status, 304, `${pathname} must support conditional revalidation.`);
        observations.push({ pathname, cacheControl });
    }
    const missing = await fetch(`${origin}/assets/pwa/missing-install-icon.png`);
    assert.equal(missing.status, 404);
    assert.match(missing.headers.get('cache-control') ?? '', /\bno-cache\b/u);
    const otherAsset = await fetch(`${origin}/assets/images/header/logo.svg`);
    assert.match(otherAsset.headers.get('cache-control') ?? '', /immutable/u, 'Other asset caching must remain unchanged.');
    return observations;
}
