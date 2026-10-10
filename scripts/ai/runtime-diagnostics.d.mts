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
