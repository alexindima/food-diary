const API_SEGMENT = 'api';
const AUTH_SEGMENT = 'auth';
const VERSION_PREFIX = 'v';

export function buildRealtimeHubUrl(authBaseUrl: string, hubPath: string): string {
    const baseUrl = removeTrailingSlashes(authBaseUrl);
    const hubUrlBase = removeAuthApiSuffix(baseUrl);
    return `${hubUrlBase}${normalizeHubPath(hubPath)}`;
}

function removeAuthApiSuffix(value: string): string {
    const segments = value.split('/');
    const authIndex = segments.length - 1;
    const apiOrVersionIndex = authIndex - 1;
    const apiIndex = apiOrVersionIndex - 1;

    if (segments[authIndex] !== AUTH_SEGMENT) {
        return value;
    }

    if (segments[apiOrVersionIndex] === API_SEGMENT) {
        return segments.slice(0, apiOrVersionIndex).join('/');
    }

    if (isApiVersionSegment(segments[apiOrVersionIndex]) && segments[apiIndex] === API_SEGMENT) {
        return segments.slice(0, apiIndex).join('/');
    }

    return value;
}

function isApiVersionSegment(segment: string | undefined): boolean {
    if (segment === undefined || !segment.startsWith(VERSION_PREFIX) || segment.length === VERSION_PREFIX.length) {
        return false;
    }

    let hasDigit = false;
    let currentPartHasDigit = false;
    for (const char of segment.slice(VERSION_PREFIX.length)) {
        if (char === '.') {
            if (!currentPartHasDigit) {
                return false;
            }

            currentPartHasDigit = false;
            continue;
        }

        if (!isDigit(char)) {
            return false;
        }

        hasDigit = true;
        currentPartHasDigit = true;
    }

    return hasDigit && currentPartHasDigit;
}

function isDigit(char: string): boolean {
    return char >= '0' && char <= '9';
}

function normalizeHubPath(hubPath: string): string {
    let firstNonSlashIndex = 0;
    while (hubPath[firstNonSlashIndex] === '/') {
        firstNonSlashIndex++;
    }

    return `/${hubPath.slice(firstNonSlashIndex)}`;
}

function removeTrailingSlashes(value: string): string {
    let endIndex = value.length;
    while (endIndex > 0 && value[endIndex - 1] === '/') {
        endIndex--;
    }

    return value.slice(0, endIndex);
}
