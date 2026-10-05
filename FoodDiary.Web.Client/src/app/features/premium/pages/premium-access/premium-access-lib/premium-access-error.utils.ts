export function resolvePremiumErrorMessage(_error: unknown, fallbackMessage: string): string {
    // Provider messages are diagnostics; the caller supplies localized, actionable copy.
    return fallbackMessage;
}
