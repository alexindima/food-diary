export function runtimeDiagnosticState(
    name: string,
    root?: string,
): {
    status: string;
    staleSources: boolean;
    frontendUrl: string;
    apiUrl: string;
    processes: { role: string; alive: boolean }[];
};

export function collectMailDiagnostics(
    name: string,
    root?: string,
): Promise<{
    schemaVersion: number;
    name: string;
    observedAtUtc: string;
    configurationSource: string;
    provider: { processAlive: boolean; health: string; mode: string };
    backgroundJobs: string;
    applicationOutbox: string;
    smtpDelivery: string;
    endToEndDelivery: string;
}>;
