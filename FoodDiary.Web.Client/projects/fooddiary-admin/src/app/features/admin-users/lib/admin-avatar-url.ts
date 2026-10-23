/** Keep remote profile images on HTTPS while preserving local and generated images. */
export function resolveAdminAvatarUrl(profileImage: string | null | undefined, baseUrl: string): string | null {
    const source = profileImage?.trim();
    if (source === undefined || source.length === 0) {
        return null;
    }

    try {
        const base = new URL(baseUrl);
        return resolveSupportedImageUrl(source, new URL(source, base), base);
    } catch {
        return null;
    }
}

function resolveSupportedImageUrl(source: string, url: URL, base: URL): string | null {
    if (url.protocol === 'data:') {
        return /^data:image\//iu.test(source) ? source : null;
    }
    if (url.protocol === 'blob:') {
        return url.origin === base.origin ? url.href : null;
    }
    if (url.protocol !== 'http:' && url.protocol !== 'https:') {
        return null;
    }
    return url.protocol === 'https:' || url.origin === base.origin || isLocalHttpImage(url, base) ? url.href : null;
}

function isLocalHttpImage(url: URL, base: URL): boolean {
    return url.protocol === 'http:' && base.protocol === 'http:' && isLoopbackHost(url.hostname) && isLoopbackHost(base.hostname);
}

function isLoopbackHost(hostname: string): boolean {
    return hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '[::1]';
}
