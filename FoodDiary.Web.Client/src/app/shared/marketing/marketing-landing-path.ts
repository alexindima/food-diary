const MARKETING_QUERY_KEYS = new Set(['utm_source', 'utm_medium', 'utm_campaign', 'utm_content', 'utm_term']);

/** Attribution needs campaign parameters, never authentication or nested navigation URLs. */
export function sanitizeMarketingLandingPath(value: string): string {
    try {
        const url = new URL(value, 'https://marketing.invalid');
        if (url.protocol !== 'https:' && url.protocol !== 'http:') {
            return '/';
        }

        const query = new URLSearchParams();
        url.searchParams.forEach((parameterValue, key) => {
            if (MARKETING_QUERY_KEYS.has(key)) {
                query.append(key, parameterValue);
            }
        });
        const search = query.toString();
        return search.length > 0 ? `${url.pathname}?${search}` : url.pathname;
    } catch {
        return '/';
    }
}
