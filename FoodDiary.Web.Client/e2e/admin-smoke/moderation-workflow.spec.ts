import { readFileSync } from 'node:fs';

import { expect, type Locator, type Page, test } from '@playwright/test';

import type { AdminContentReportHttpResponse } from '../../projects/fooddiary-admin/src/app/shared/api/sdk/generated/model/admin-content-report-http-response';
import type { AdminContentReportHttpResponsePagedHttpResponse } from '../../projects/fooddiary-admin/src/app/shared/api/sdk/generated/model/admin-content-report-http-response-paged-http-response';
import type { ApiErrorHttpResponse } from '../../projects/fooddiary-admin/src/app/shared/api/sdk/generated/model/api-error-http-response';
import { fixtureUuid, jsonFixture, OpenApiFixtures } from '../support/openapi-fixtures.mjs';
import { syntheticAuthToken } from '../support/synthetic-auth-token';

// eslint-disable-next-line security/detect-non-literal-fs-filename -- This fixed repository fixture URL contains no external input.
const adminOpenApiDocument: unknown = JSON.parse(readFileSync(new URL('../../api-sdk/admin.openapi.json', import.meta.url), 'utf8'));
const contract = new OpenApiFixtures(adminOpenApiDocument);
const ADMIN_ID = fixtureUuid('moderation-workflow-admin');
const REPORT_ID = fixtureUuid('moderation-workflow-report');
const REPORTER_ID = fixtureUuid('moderation-workflow-reporter');
const TARGET_ID = fixtureUuid('moderation-workflow-recipe');
const OTHER_REPORT_ID = fixtureUuid('moderation-workflow-other-report');
const OTHER_TARGET_ID = fixtureUuid('moderation-workflow-other-recipe');
const REPORT_TITLE = 'Flagged lentil soup';
const OTHER_REPORT_TITLE = 'Unrelated oatmeal';
const ADMIN_NOTE = 'Confirmed the recipe context and moderation policy.';
const RAW_ADMIN_NOTE = `  ${ADMIN_NOTE}\n  `;
const LIST_PATH = '/api/v1/admin/moderation';
const PAGE_SIZE = 20;
const HTTP_NO_CONTENT = 204;
const HTTP_SERVER_ERROR = 500;

type ModerationAction = 'review' | 'dismiss';
type ReportStatus = 'Pending' | 'Reviewed' | 'Dismissed';
type ObservedMutation = { action: ModerationAction; reportId: string; body: unknown };
type ModerationBoundary = {
    status: ReportStatus;
    adminNote: string | null;
    listLoads: number;
    mutations: ObservedMutation[];
};
type BoundaryOptions = { failFirstMutation?: boolean; firstMutationGate?: Promise<void> };

test.describe('admin moderation dialog cancellation', () => {
    for (const dismissal of ['Cancel', 'Escape'] as const) {
        test(`${dismissal} discards the note without mutating or reloading the report`, async ({ page }) => {
            const boundary = await openModerationPageAsync(page);
            const action = dismissal === 'Cancel' ? 'review' : 'dismiss';
            const dialog = await openActionAsync(page, action);
            await dialog.getByRole('textbox', { name: 'Admin note (optional)' }).fill(RAW_ADMIN_NOTE);

            if (dismissal === 'Cancel') {
                await dialog.getByRole('button', { name: 'Cancel', exact: true }).click();
            } else {
                await page.keyboard.press('Escape');
            }

            await expect(dialog).toBeHidden();
            await expect(reportRow(page).getByText('Pending', { exact: true })).toBeVisible();
            const reopened = await openActionAsync(page, action);
            await expect(reopened.getByRole('textbox', { name: 'Admin note (optional)' })).toHaveValue('');
            await reopened.getByRole('button', { name: 'Cancel', exact: true }).click();
            await expect(reopened).toBeHidden();
            expect(boundary.mutations).toEqual([]);
            expect(boundary.listLoads).toBe(1);
        });
    }
});

test.describe('admin moderation confirmed decisions', () => {
    for (const action of ['review', 'dismiss'] as const) {
        test(`${action} submits the selected report and trimmed note once, then reloads its status`, async ({ page }) => {
            const boundary = await openModerationPageAsync(page);
            const dialog = await openActionAsync(page, action);
            await dialog.getByRole('textbox', { name: 'Admin note (optional)' }).fill(RAW_ADMIN_NOTE);

            await confirmDecisionAsync(page, dialog, action, HTTP_NO_CONTENT);

            await expect(dialog).toBeHidden();
            await expect(reportRow(page).getByText(completedStatus(action), { exact: true })).toBeVisible();
            expect(boundary.mutations).toEqual([{ action, reportId: REPORT_ID, body: { adminNote: ADMIN_NOTE } }]);
            expect(boundary.listLoads).toBe(2);
            await reportRow(page).locator('summary').click();
            await expect(reportRow(page).getByText(ADMIN_NOTE, { exact: true })).toBeVisible();
            await expect(otherReportRow(page).getByText('Pending', { exact: true })).toBeVisible();
            await expect(reportRow(page).getByRole('button', { name: 'Review', exact: true })).toHaveCount(0);
            await expect(reportRow(page).getByRole('button', { name: 'Dismiss', exact: true })).toHaveCount(0);
        });
    }
});

test.describe('admin moderation recoverable failures', () => {
    for (const action of ['review', 'dismiss'] as const) {
        test(`${action} retains the dialog and original note after a server failure and retries the same decision`, async ({ page }) => {
            const boundary = await openModerationPageAsync(page, { failFirstMutation: true });
            const dialog = await openActionAsync(page, action);
            const note = dialog.getByRole('textbox', { name: 'Admin note (optional)' });
            await note.fill(RAW_ADMIN_NOTE);

            await confirmDecisionAsync(page, dialog, action, HTTP_SERVER_ERROR);

            await expect(dialog).toBeVisible();
            await expect(dialog.getByRole('alert')).toHaveText(
                'Could not save the moderation decision. Your note has been kept. Try again.',
            );
            await expect(note).toHaveValue(RAW_ADMIN_NOTE);
            await expect(note).toBeEnabled();
            await expect(confirmButton(dialog, action)).toBeEnabled();
            expect(boundary.status).toBe('Pending');
            expect(boundary.listLoads).toBe(1);
            expect(boundary.mutations).toEqual([{ action, reportId: REPORT_ID, body: { adminNote: ADMIN_NOTE } }]);

            await confirmDecisionAsync(page, dialog, action, HTTP_NO_CONTENT);

            await expect(dialog).toBeHidden();
            await expect(reportRow(page).getByText(completedStatus(action), { exact: true })).toBeVisible();
            expect(boundary.listLoads).toBe(2);
            expect(boundary.mutations).toEqual([
                { action, reportId: REPORT_ID, body: { adminNote: ADMIN_NOTE } },
                { action, reportId: REPORT_ID, body: { adminNote: ADMIN_NOTE } },
            ]);
        });
    }
});

test('admin moderation locks the real dialog while a decision is in flight', async ({ page }) => {
    const gate = createGate();
    const boundary = await openModerationPageAsync(page, { firstMutationGate: gate.wait });
    const dialog = await openActionAsync(page, 'dismiss');
    const note = dialog.getByRole('textbox', { name: 'Admin note (optional)' });
    await note.fill(RAW_ADMIN_NOTE);
    try {
        await confirmButton(dialog, 'dismiss').click();
        await expect.poll(() => boundary.mutations.length).toBe(1);
        await expect(confirmButton(dialog, 'dismiss')).toBeDisabled();
        await expect(dialog.getByRole('button', { name: 'Cancel', exact: true })).toBeDisabled();
        await expect(note).toBeDisabled();
        await expect(dialog.getByRole('button', { name: 'Close', exact: true })).toBeDisabled();
        await page.keyboard.press('Escape');
        await expect(dialog).toBeVisible();
        expect(boundary.listLoads).toBe(1);
        expect(boundary.mutations).toEqual([{ action: 'dismiss', reportId: REPORT_ID, body: { adminNote: ADMIN_NOTE } }]);
    } finally {
        gate.release();
    }
    await expect(dialog).toBeHidden();
    await expect(reportRow(page).getByText('Dismissed', { exact: true })).toBeVisible();
    expect(boundary.listLoads).toBe(2);
    expect(boundary.mutations).toHaveLength(1);
});

async function openModerationPageAsync(page: Page, options: BoundaryOptions = {}): Promise<ModerationBoundary> {
    const token = syntheticAuthToken(ADMIN_ID, 'Admin');
    await page.addInitScript(value => {
        localStorage.setItem('authToken', value);
    }, token);
    const boundary: ModerationBoundary = { status: 'Pending', adminNote: null, listLoads: 0, mutations: [] };
    await page.route('**/api/**', async route => {
        const request = route.request();
        const url = new URL(request.url());
        const method = request.method();
        expect(request.headers()['authorization']).toBe(`Bearer ${token}`);
        if (method === 'GET' && url.pathname === LIST_PATH) {
            boundary.listLoads++;
            expect(url.searchParams.get('page')).toBe('1');
            expect(url.searchParams.get('limit')).toBe(String(PAGE_SIZE));
            expect(url.searchParams.has('status')).toBe(false);
            await route.fulfill(jsonFixture(contract, method, request.url(), reportsPage(boundary)));
            return;
        }
        const action = requestAction(url.pathname);
        if (action !== null && method === 'POST') {
            const body: unknown = request.postDataJSON();
            contract.assertSchema('AdminReportActionHttpRequest', body);
            boundary.mutations.push({ action, reportId: REPORT_ID, body });
            expect(body).toEqual({ adminNote: ADMIN_NOTE });
            if (boundary.mutations.length === 1 && options.failFirstMutation === true) {
                await route.fulfill(jsonFixture(contract, method, request.url(), serverFailure(), HTTP_SERVER_ERROR));
                return;
            }
            if (boundary.mutations.length === 1 && options.firstMutationGate !== undefined) {
                await options.firstMutationGate;
            }
            boundary.status = completedStatus(action);
            boundary.adminNote = ADMIN_NOTE;
            contract.assertResponse(method, request.url(), HTTP_NO_CONTENT, undefined);
            await route.fulfill({ status: HTTP_NO_CONTENT });
            return;
        }
        throw new Error(`Unexpected moderation fixture operation: ${method} ${url.pathname}`);
    });
    await page.goto('/moderation?period=all&status=');
    await expect(reportRow(page).getByText('Pending', { exact: true })).toBeVisible();
    await expect(otherReportRow(page).getByText('Pending', { exact: true })).toBeVisible();
    expect(boundary.listLoads).toBe(1);
    return boundary;
}

function reportsPage(boundary: ModerationBoundary): AdminContentReportHttpResponsePagedHttpResponse {
    const report: AdminContentReportHttpResponse = {
        id: REPORT_ID,
        reporterId: REPORTER_ID,
        targetId: TARGET_ID,
        targetType: 'Recipe',
        targetTitle: REPORT_TITLE,
        targetExcerpt: 'Recipe context for a synthetic moderation report.',
        reason: 'Spam report for misleading recipe claims.',
        status: boundary.status,
        adminNote: boundary.adminNote,
        createdAtUtc: '2026-10-21T10:00:00Z',
        reviewedAtUtc: boundary.status === 'Pending' ? null : '2026-10-22T12:00:00Z',
        reviewedByUserId: boundary.status === 'Pending' ? null : ADMIN_ID,
    };
    const other: AdminContentReportHttpResponse = {
        ...report,
        id: OTHER_REPORT_ID,
        targetId: OTHER_TARGET_ID,
        targetTitle: OTHER_REPORT_TITLE,
        status: 'Pending',
        adminNote: null,
        reviewedAtUtc: null,
        reviewedByUserId: null,
    };
    return { data: [other, report], page: 1, limit: PAGE_SIZE, totalItems: 2, totalPages: 1 };
}

function serverFailure(): ApiErrorHttpResponse {
    return { error: 'InternalServerError', message: 'Synthetic moderation failure.', traceId: fixtureUuid('moderation-workflow-failure') };
}

function requestAction(pathname: string): ModerationAction | null {
    if (pathname === `${LIST_PATH}/${REPORT_ID}/review`) {
        return 'review';
    }
    return pathname === `${LIST_PATH}/${REPORT_ID}/dismiss` ? 'dismiss' : null;
}

function completedStatus(action: ModerationAction): 'Reviewed' | 'Dismissed' {
    return action === 'review' ? 'Reviewed' : 'Dismissed';
}

function reportRow(page: Page): Locator {
    return page.getByRole('table', { name: 'Moderation reports' }).getByRole('row').filter({ hasText: REPORT_TITLE });
}

function otherReportRow(page: Page): Locator {
    return page.getByRole('table', { name: 'Moderation reports' }).getByRole('row').filter({ hasText: OTHER_REPORT_TITLE });
}

async function openActionAsync(page: Page, action: ModerationAction): Promise<Locator> {
    await reportRow(page)
        .getByRole('button', { name: action === 'review' ? 'Review' : 'Dismiss', exact: true })
        .click();
    const dialog = page.getByRole('dialog', { name: action === 'review' ? 'Review Report' : 'Dismiss Report', exact: true });
    await expect(dialog).toBeVisible();
    return dialog;
}

function confirmButton(dialog: Locator, action: ModerationAction): Locator {
    return dialog.getByRole('button', { name: action === 'review' ? 'Mark Reviewed' : 'Dismiss', exact: true });
}

async function confirmDecisionAsync(page: Page, dialog: Locator, action: ModerationAction, status: number): Promise<void> {
    await Promise.all([
        page.waitForResponse(response => response.url().endsWith(`${REPORT_ID}/${action}`) && response.status() === status),
        confirmButton(dialog, action).click(),
    ]);
}

function createGate(): { wait: Promise<void>; release: () => void } {
    let complete: (() => void) | undefined;
    const wait = new Promise<void>(resolve => {
        complete = resolve;
    });
    return {
        wait,
        release: (): void => {
            if (complete === undefined) {
                throw new Error('The moderation transport gate was not initialized.');
            }
            complete();
        },
    };
}
