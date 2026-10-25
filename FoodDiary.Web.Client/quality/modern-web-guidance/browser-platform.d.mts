import type { Page } from '@playwright/test';

export interface BrowserInspection {
    observations: number;
    findings: { message: string; targets?: string[]; url?: string }[];
    detail?: string;
    manual?: { id: string; reason: string }[];
    exemptions?: string[];
}
export interface InspectionScenario {
    kind?: 'public' | 'login' | 'register';
    locale?: string;
    openDialog?: string;
}
export const platformBrowserChecks: Set<string>;
export function inspectPlatformPage(page: Page, check: string, scenario: InspectionScenario): Promise<BrowserInspection>;
