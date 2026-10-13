export const REPORT_STATUSES = [
    'pending',
    'in_progress',
    'needs_information',
    'not_confirmed',
    'duplicate',
    'draft_ready',
    'failed',
    'content_unavailable',
    'expired',
] as const;

type StatusTone = 'muted' | 'success' | 'warning' | 'danger';
const PREVIEW_LIMIT = 260;
const ELLIPSIS_RESERVE = 3;
const PREVIEW_CONTENT_LIMIT = PREVIEW_LIMIT - ELLIPSIS_RESERVE;

export function reportStatusPresentation(status: string): { key: string; tone: StatusTone } {
    const tones: Record<string, StatusTone> = {
        pending: 'muted',
        in_progress: 'muted',
        needs_information: 'warning',
        not_confirmed: 'muted',
        duplicate: 'muted',
        draft_ready: 'success',
        failed: 'danger',
        content_unavailable: 'warning',
        expired: 'muted',
    };
    return Object.hasOwn(tones, status)
        ? { key: `ADMIN_BUGS.STATUS_${status.toUpperCase()}`, tone: tones[status] ?? 'muted' }
        : { key: 'ADMIN_BUGS.STATUS_UNKNOWN', tone: 'muted' };
}

export function conciseReportSummary(summary: string | null | undefined): string {
    const paragraph =
        summary
            ?.trim()
            .split(/\n\s*\n/u)[0]
            ?.replaceAll(/\s+/gu, ' ') ?? '';
    return paragraph.length <= PREVIEW_LIMIT ? paragraph : `${paragraph.slice(0, PREVIEW_CONTENT_LIMIT).trimEnd()}…`;
}

export function hasReadableReportSubject(subject: string): boolean {
    const value = subject
        .trim()
        .replace(/^bug report[\s:#-]*/iu, '')
        .replace(/^bug[\s:#-]*/iu, '');
    return value.length > 0 && !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/iu.test(value);
}

export function optionalReportText(value: string | null | undefined): string | null {
    const text = value?.trim() ?? '';
    return text.length > 0 ? text : null;
}

export function safeReportPullRequestUrl(value: string | null): string | null {
    if (value === null || value.length === 0) {
        return null;
    }
    try {
        const url = new URL(value);
        return url.protocol === 'https:' && url.username.length === 0 && url.password.length === 0 ? url.href : null;
    } catch {
        return null;
    }
}
