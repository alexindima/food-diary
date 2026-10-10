// Diagnostics keep locations and error categories, never request bodies or credentials.
export function redactDiagnostic(value, maximum = 2000) {
    return String(value)
        .replace(/\b(?:Bearer|Basic)\s+[^\s"']+/giu, "[redacted-auth]")
        .replace(
            /\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b/gu,
            "[redacted-token]",
        )
        .replace(
            /((?:password|secret|token|api[_-]?key|authorization|cookie|connectionstring)\s*["']?\s*[:=]\s*["']?)[^\s,;"'}]+/giu,
            "$1[redacted]",
        )
        .replace(/(https?:\/\/)[^\s/@]+:[^\s/@]+@/giu, "$1[redacted]@")
        .replace(
            /\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b/giu,
            "[redacted-email]",
        )
        .replace(/([?&])[A-Za-z0-9_.%-]+=[^\s&"']*/gu, "$1[redacted-query]")
        .slice(0, maximum);
}

export function errorLines(value, limit = 12) {
    return String(value)
        .split(/\r?\n/u)
        .filter((line) =>
            /error|fail|exception|timeout|not found|cannot|refus/iu.test(line),
        )
        .slice(0, limit)
        .map((line) => redactDiagnostic(line));
}
