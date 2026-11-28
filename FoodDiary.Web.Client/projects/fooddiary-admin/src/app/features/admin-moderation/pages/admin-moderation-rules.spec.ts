import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { environment } from '../../../../environments/environment';
import type { RecipeModerationPolicyVersion } from '../../../shared/api/sdk/generated/model/recipe-moderation-policy-version';
import type { RecipeModerationPolicyView } from '../../../shared/api/sdk/generated/model/recipe-moderation-policy-view';
import { AdminModerationRulesComponent } from './admin-moderation-rules';

const POLICY_API = `${environment.apiUrls.auth.replace(/\/auth$/u, '')}/admin/moderation/rules`;
const DEFAULT_ID = 'c0a17afe-0737-4fc4-9cf7-4ce3090be7a6';
const ACTIVE_ID = '00000000-0000-4000-8000-000000000001';
const HISTORICAL_ID = '00000000-0000-4000-8000-000000000002';
const PUBLISHED_ID = '00000000-0000-4000-8000-000000000003';
const MISSING_ID = '00000000-0000-4000-8000-000000000004';
const PUBLISHER = '00000000-0000-4000-8000-000000000007';
const INITIAL_SAMPLE = 15;
const DEFAULT_SAMPLE = 10;
const DEFAULT_IMAGE_CATEGORIES = ['self-harm', 'self-harm/intent', 'self-harm/instructions', 'sexual', 'violence', 'violence/graphic'];
const DEFAULT_RULES = {
    id: DEFAULT_ID,
    publishedAt: '1970-01-01T00:00:00Z',
    publishedBy: null,
    rules: {
        textReviewCategories: [
            ...DEFAULT_IMAGE_CATEGORIES,
            'harassment',
            'harassment/threatening',
            'hate',
            'hate/threatening',
            'illicit',
            'illicit/violent',
            'sexual/minors',
        ],
        imageReviewCategories: DEFAULT_IMAGE_CATEGORIES,
        mistralSamplePercent: DEFAULT_SAMPLE,
    },
} satisfies RecipeModerationPolicyVersion;
const ACTIVE_RULES = {
    id: ACTIVE_ID,
    publishedAt: '2026-08-01T12:00:00Z',
    publishedBy: PUBLISHER,
    rules: { textReviewCategories: ['harassment'], imageReviewCategories: ['violence'], mistralSamplePercent: INITIAL_SAMPLE },
} satisfies RecipeModerationPolicyVersion;
const POLICY: RecipeModerationPolicyView = {
    active: ACTIVE_RULES,
    history: [ACTIVE_RULES, DEFAULT_RULES],
    mistralAvailable: false,
};
const HISTORICAL = {
    id: HISTORICAL_ID,
    publishedAt: '2026-07-01T12:00:00Z',
    publishedBy: PUBLISHER,
    rules: { textReviewCategories: ['hate'], imageReviewCategories: ['sexual'], mistralSamplePercent: 10 },
} satisfies RecipeModerationPolicyVersion;

type RulesFixture = ComponentFixture<AdminModerationRulesComponent>;
let http: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        imports: [AdminModerationRulesComponent],
        providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(), ...provideTranslateTesting()],
    });
    http = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    http.verify();
});

describe('Reading and validating Admin recipe moderation rules', () => {
    it('announces a failed read, then retries and renders the default policy and provider availability', async () => {
        const fixture = mountRules();
        expect(host(fixture).querySelector('[role="status"]')?.textContent).toContain('ADMIN_COMMON.LOADING');
        http.expectOne({ method: 'GET', url: POLICY_API }).flush(null, {
            status: HttpStatusCode.ServiceUnavailable,
            statusText: 'Service Unavailable',
        });
        await fixture.whenStable();
        fixture.detectChanges();
        expect(host(fixture).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_COMMON.LOAD_ERROR');
        expect(host(fixture).querySelector('fieldset')).toBeNull();

        const retry = button(host(fixture), 'ADMIN_COMMON.RETRY');
        retry.click();
        expect(retry.isConnected).toBe(true);
        retry.click();
        fixture.detectChanges();
        http.expectOne({ method: 'GET', url: POLICY_API }).flush({ ...POLICY, active: DEFAULT_RULES, history: [DEFAULT_RULES] });
        await fixture.whenStable();
        fixture.detectChanges();
        expect(host(fixture).querySelector('[role="alert"]')).toBeNull();
        expect(host(fixture).querySelector('section')?.textContent).toContain(DEFAULT_ID);
        expect(host(fixture).textContent).toContain('ADMIN_RECIPE_MODERATION.DEFAULT_RULES');
        expect(host(fixture).textContent).toContain('ADMIN_RECIPE_MODERATION.MISTRAL_UNAVAILABLE');
        expect(sample(fixture).value).toBe(String(DEFAULT_SAMPLE));
    });

    it.each(['', '-1', '0.5', '101'])('refuses to publish sample value %j and recovers at the supported limits', async value => {
        const fixture = await mountLoadedRulesAsync();
        const publish = publishButton(fixture);
        enterSample(fixture, value);
        expect(publish.disabled).toBe(false);
        publish.click();
        http.expectNone({ method: 'PUT', url: POLICY_API });
        fixture.detectChanges();
        expect(publish.disabled).toBe(true);

        changeSample(fixture, '0');
        expect(publish.disabled).toBe(false);
        changeSample(fixture, '100');
        expect(publish.disabled).toBe(false);
    });

    it('requires at least one text category and one image category', async () => {
        const fixture = await mountLoadedRulesAsync();
        const publish = publishButton(fixture);
        const text = category(fixture, 'TEXT_REVIEW', 'HARASSMENT');
        const image = category(fixture, 'IMAGE_REVIEW', 'VIOLENCE');
        expect(text.checked).toBe(true);
        expect(image.checked).toBe(true);

        text.click();
        fixture.detectChanges();
        expect(publish.disabled).toBe(true);
        category(fixture, 'TEXT_REVIEW', 'HATE').click();
        fixture.detectChanges();
        expect(publish.disabled).toBe(false);

        image.click();
        fixture.detectChanges();
        expect(publish.disabled).toBe(true);
        publish.click();
        http.expectNone({ method: 'PUT', url: POLICY_API });
    });
});

describe('Publishing and restoring Admin recipe moderation rules', () => {
    it('keeps the draft after a publish error and renders the acknowledged version after retry', async () => {
        const fixture = await mountLoadedRulesAsync();
        category(fixture, 'TEXT_REVIEW', 'HATE').click();
        changeSample(fixture, '0');
        const publish = publishButton(fixture);
        const rules = { textReviewCategories: ['harassment', 'hate'], imageReviewCategories: ['violence'], mistralSamplePercent: 0 };
        publish.click();
        expect(publish.disabled).toBe(false);
        publish.click();
        const failed = http.expectOne({ method: 'PUT', url: POLICY_API });
        expect(failed.request.body).toEqual(rules);
        expectSavePending(fixture);
        failed.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
        fixture.detectChanges();

        expect(host(fixture).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_MODERATION.ACTION_ERROR');
        expect(category(fixture, 'TEXT_REVIEW', 'HATE').checked).toBe(true);
        expect(sample(fixture).value).toBe('0');
        expect(publish.disabled).toBe(false);
        publish.click();
        const retried = http.expectOne({ method: 'PUT', url: POLICY_API });
        expect(retried.request.body).toEqual(rules);
        const acknowledgedRules = { ...rules, mistralSamplePercent: 10 };
        retried.flush(publishedPolicy(acknowledgedRules));
        fixture.detectChanges();
        expectPublished(fixture);
        expect(sample(fixture).value).toBe(String(acknowledgedRules.mistralSamplePercent));
        expect(host(fixture).textContent).not.toContain('ADMIN_RECIPE_MODERATION.MISTRAL_UNAVAILABLE');
        changeSample(fixture, '20');
        expect(host(fixture).querySelector('[role="status"]')).toBeNull();
    });

    it('keeps active rules after a missing restore target and can restore another version', async () => {
        const missingVersion = { ...HISTORICAL, id: MISSING_ID };
        const fixture = await mountLoadedRulesAsync({ ...POLICY, history: [ACTIVE_RULES, missingVersion, HISTORICAL, DEFAULT_RULES] });
        const unavailableRestore = restoreButton(fixture, MISSING_ID);
        const unavailableEndpoint = `${POLICY_API}/${MISSING_ID}/restore`;
        unavailableRestore.click();
        expect(unavailableRestore.disabled).toBe(false);
        unavailableRestore.click();
        const missing = http.expectOne({ method: 'POST', url: unavailableEndpoint });
        expectSavePending(fixture);
        expect(unavailableRestore.disabled).toBe(true);
        unavailableRestore.click();
        http.expectNone({ method: 'POST', url: unavailableEndpoint });
        missing.flush(null, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
        fixture.detectChanges();

        expect(host(fixture).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_MODERATION.ACTION_ERROR');
        expect(host(fixture).querySelector('section')?.textContent).toContain(ACTIVE_ID);
        expect(sample(fixture).value).toBe(String(INITIAL_SAMPLE));
        restoreButton(fixture, HISTORICAL_ID).click();
        http.expectOne({ method: 'POST', url: `${POLICY_API}/${HISTORICAL_ID}/restore` }).flush(publishedPolicy(HISTORICAL.rules));
        fixture.detectChanges();
        expectPublished(fixture);
        expect(category(fixture, 'TEXT_REVIEW', 'HATE').checked).toBe(true);
        expect(category(fixture, 'IMAGE_REVIEW', 'SEXUAL').checked).toBe(true);
        expect(sample(fixture).value).toBe(String(HISTORICAL.rules.mistralSamplePercent));
        http.expectNone({ method: 'PUT', url: POLICY_API });
    });

    it('cancels an in-flight publish when the mounted rules page is destroyed', async () => {
        const fixture = await mountLoadedRulesAsync();
        publishButton(fixture).click();
        const request = http.expectOne({ method: 'PUT', url: POLICY_API });
        expect(request.cancelled).toBe(false);
        fixture.destroy();
        expect(request.cancelled).toBe(true);
    });
});

function mountRules(): RulesFixture {
    const fixture = TestBed.createComponent(AdminModerationRulesComponent);
    fixture.detectChanges();
    return fixture;
}

async function mountLoadedRulesAsync(policy: RecipeModerationPolicyView = POLICY): Promise<RulesFixture> {
    const fixture = mountRules();
    http.expectOne({ method: 'GET', url: POLICY_API }).flush(policy);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
}

function host(fixture: RulesFixture): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function button(root: HTMLElement, key: string): HTMLButtonElement {
    const result = Array.from(root.querySelectorAll('button')).find(control => control.textContent.trim() === key);
    if (result === undefined) {
        throw new Error(`Expected a visible rules action for ${key}.`);
    }
    return result;
}

function publishButton(fixture: RulesFixture): HTMLButtonElement {
    return button(host(fixture), 'ADMIN_RECIPE_MODERATION.PUBLISH_RULES');
}

function restoreButton(fixture: RulesFixture, id: string): HTMLButtonElement {
    const row = Array.from(host(fixture).querySelectorAll<HTMLElement>('.history-row')).find(element => element.textContent.includes(id));
    if (row === undefined) {
        throw new Error(`Expected a history row for ${id}.`);
    }
    return button(row, 'ADMIN_RECIPE_MODERATION.RESTORE_RULES');
}

function sample(fixture: RulesFixture): HTMLInputElement {
    const control = host(fixture).querySelector<HTMLInputElement>('input[type="number"]');
    if (control === null) {
        throw new Error('Expected the Mistral sample field.');
    }
    return control;
}

function changeSample(fixture: RulesFixture, value: string): void {
    enterSample(fixture, value);
    fixture.detectChanges();
}

function enterSample(fixture: RulesFixture, value: string): void {
    const control = sample(fixture);
    control.value = value;
    control.dispatchEvent(new Event('input', { bubbles: true }));
}

function category(fixture: RulesFixture, legend: string, label: string): HTMLInputElement {
    const group = Array.from(host(fixture).querySelectorAll('fieldset')).find(
        fieldset => fieldset.querySelector('legend')?.textContent.trim() === `ADMIN_RECIPE_MODERATION.${legend}`,
    );
    const labels = group?.querySelectorAll('label') ?? [];
    const control = Array.from(labels).find(
        element => element.textContent.trim() === `ADMIN_RECIPE_MODERATION.CATEGORIES.${label}`,
    )?.control;
    if (!(control instanceof HTMLInputElement)) {
        throw new Error(`Expected ${label} in ${legend}.`);
    }
    return control;
}

function publishedPolicy(rules: typeof HISTORICAL.rules): RecipeModerationPolicyView {
    const active = { ...HISTORICAL, id: PUBLISHED_ID, publishedAt: '2026-09-01T12:00:00Z', rules };
    return { active, history: [active, ACTIVE_RULES, HISTORICAL, DEFAULT_RULES], mistralAvailable: true };
}

function expectSavePending(fixture: RulesFixture): void {
    fixture.detectChanges();
    expect(publishButton(fixture).disabled).toBe(true);
    expect(sample(fixture).disabled).toBe(true);
    expect(category(fixture, 'TEXT_REVIEW', 'HARASSMENT').disabled).toBe(true);
    expect(category(fixture, 'IMAGE_REVIEW', 'VIOLENCE').disabled).toBe(true);
}

function expectPublished(fixture: RulesFixture): void {
    expect(host(fixture).querySelector('[role="status"]')?.textContent).toContain('ADMIN_RECIPE_MODERATION.RULES_PUBLISHED');
    expect(host(fixture).querySelector('[role="alert"]')).toBeNull();
    expect(host(fixture).querySelector('section')?.textContent).toContain(PUBLISHED_ID);
    expect(sample(fixture).disabled).toBe(false);
}
