export const ADMIN_DATE_TEXT_LENGTH = 10;
const MAX_ADMIN_PAGE = 10_000;
export function adminPage(value: string | null): number {
    const page = Number(value);
    return Number.isFinite(page) ? Math.min(MAX_ADMIN_PAGE, Math.max(1, Math.trunc(page))) : 1;
}
export function adminQueryValue(value: string): string | null {
    return value.length === 0 ? null : value;
}
