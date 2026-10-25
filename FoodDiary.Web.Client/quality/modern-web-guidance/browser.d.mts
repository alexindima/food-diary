import type { Page } from '@playwright/test';
import type { BrowserInspection, InspectionScenario } from './browser-platform.mjs';

export const browserChecks: Set<string>;
export function inspectPage(page: Page, check: string, scenario: InspectionScenario): Promise<BrowserInspection>;
export function runBrowser(options: Record<string, unknown>): Promise<unknown>;
