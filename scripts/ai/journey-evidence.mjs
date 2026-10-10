import { redactDiagnostic } from "./diagnostic-redaction.mjs";

export function safeJourneyOutput(value, secrets = []) {
    let text = String(value);
    for (const secret of secrets.filter(
        (item) => typeof item === "string" && item.length > 0,
    ))
        text = text.replaceAll(secret, "[redacted-fixture-secret]");
    return redactDiagnostic(text, 20_000);
}

export function sanitizeJourneyReport(value, secrets = []) {
    if (typeof value === "string") return safeJourneyOutput(value, secrets);
    if (Array.isArray(value))
        return value.map((item) => sanitizeJourneyReport(item, secrets));
    if (value !== null && typeof value === "object")
        return Object.fromEntries(
            Object.entries(value).map(([key, child]) => [
                key,
                sanitizeJourneyReport(child, secrets),
            ]),
        );
    return value;
}
