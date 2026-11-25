import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { UserService } from '../../shared/api/user.service';
import type { GoalsResponse } from '../../shared/models/goals.data';
import { entityId } from '../../shared/models/semantics/entity-id';
import type { User } from '../../shared/models/user.data';
import { BrowserStorageService } from '../../shared/platform/browser-storage.service';
import { NutritionDataInvalidationService } from '../../shared/state/nutrition-data-invalidation.service';
import goalsRoutes from './goals.routes';

@Component({ template: '<h1>Diary history</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class GoalsDepartureComponent {}

const GOALS_API = environment.apiUrls.goals;
const USER_INFO_API = `${environment.apiUrls.users}/info`;
const WEIGHT_GOAL_API = `${environment.apiUrls.users}/desired-weight`;
const WAIST_GOAL_API = `${environment.apiUrls.users}/desired-waist`;
const GOALS_URL = '/goals';
const DEPARTURE_URL = '/history';
const GOALS = {
    dailyCalorieTarget: 2000,
    waterGoal: 2000,
    desiredWeightKg: 72,
    desiredWaistCm: 82,
    calorieCyclingEnabled: false,
} satisfies GoalsResponse;
const USER = {
    id: entityId<'user'>('goals-http-user'),
    email: null,
    hasPassword: true,
    isActive: true,
    isEmailConfirmed: true,
    pushNotificationsEnabled: true,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: false,
    fastingCheckInReminderHours: 12,
    fastingCheckInFollowUpReminderHours: 20,
} satisfies User;
const SAVED_CALORIES = 2400;
const SAVED_WATER = 2300;
const UNSAVED_WATER = 2600;
const QUICK_WATER_TARGET = 2500;
const toastSuccess = vi.fn<FdUiToastService['success']>();
const toastError = vi.fn<FdUiToastService['error']>();
let http: HttpTestingController;

beforeEach(() => {
    toastSuccess.mockClear();
    toastError.mockClear();
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                { path: 'goals', children: goalsRoutes },
                { path: 'history', component: GoalsDepartureComponent },
            ]),
            provideTranslateTesting({ lang: 'en', fallbackLang: 'en' }),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: BrowserStorageService, useValue: { getItem: (): null => null, setItem: vi.fn() } },
            { provide: FdUiToastService, useValue: { success: toastSuccess, error: toastError } },
        ],
    });
    http = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    TestBed.inject(FdUiDialogService).closeAll();
    vi.unstubAllGlobals();
    http.verify();
});

describe('Ownership of a submitted goal save after leaving the editor', () => {
    it.each(['accepted', 'rejected'] as const)('leaves the next screen quiet when the earlier save is %s', async outcome => {
        const harness = await openGoalsAsync();
        const editor = page(harness);
        const water = input(editor, '#goals-editor-water');
        setNumber(water, SAVED_WATER);
        harness.detectChanges();
        button(editor, 'fd-unsaved-changes-bar fd-ui-button:last-child button').click();
        const pendingSave = goalWrite({ waterGoal: SAVED_WATER });

        setNumber(water, UNSAVED_WATER);
        const departure = TestBed.inject(Router).navigateByUrl(DEPARTURE_URL);
        await chooseDepartureAsync(harness, 'fd-ui-button:nth-child(2) button');
        await expect(departure).resolves.toBe(true);
        expect(page(harness).textContent).toContain('Diary history');
        expect(pendingSave.cancelled).toBe(false);

        if (outcome === 'accepted') {
            acceptSave(pendingSave, { ...GOALS, waterGoal: SAVED_WATER });
            expect(TestBed.inject(UserService).user()?.waterGoal).toBe(SAVED_WATER);
        } else {
            rejectSave(pendingSave);
            http.expectNone(USER_INFO_API);
        }
        await harness.fixture.whenStable();
        harness.detectChanges();

        const expectedMutations = Number(outcome === 'accepted');
        expect(TestBed.inject(NutritionDataInvalidationService).dashboardVersion()).toBe(expectedMutations);
        expect(TestBed.inject(NutritionDataInvalidationService).statisticsVersion()).toBe(expectedMutations);
        expect(page(harness).textContent).toContain('Diary history');
        expect(toastSuccess).not.toHaveBeenCalled();
        expect(toastError).not.toHaveBeenCalled();
    });
});

it('keeps the native goal draft when Save and Leave fails and leaves only after a successful HTTP retry', async () => {
    const harness = await openGoalsAsync();
    const editor = page(harness);
    const calories = input(editor, '#goals-editor-calories');
    setNumber(calories, SAVED_CALORIES);
    const router = TestBed.inject(Router);

    const failedDeparture = router.navigateByUrl(DEPARTURE_URL);
    await chooseDepartureAsync(harness, '.unsaved-changes-dialog__action--primary button');
    rejectSave(goalWrite({ dailyCalorieTarget: SAVED_CALORIES }));
    await expect(failedDeparture).resolves.toBe(false);
    harness.detectChanges();

    expect(router.url).toBe(GOALS_URL);
    expect(calories.value).toBe(String(SAVED_CALORIES));
    expect(editor.querySelector('fd-unsaved-changes-bar')).not.toBeNull();
    expect(toastError).toHaveBeenCalledExactlyOnceWith('GOALS_PAGE.STATUS_ERROR');
    http.expectNone(USER_INFO_API);

    const retryDeparture = router.navigateByUrl(DEPARTURE_URL);
    await chooseDepartureAsync(harness, '.unsaved-changes-dialog__action--primary button');
    acceptSave(goalWrite({ dailyCalorieTarget: SAVED_CALORIES }), { ...GOALS, dailyCalorieTarget: SAVED_CALORIES });
    await expect(retryDeparture).resolves.toBe(true);
    harness.detectChanges();

    expect(TestBed.inject(UserService).user()?.dailyCalorieTarget).toBe(SAVED_CALORIES);
    expect(TestBed.inject(NutritionDataInvalidationService).dashboardVersion()).toBe(1);
    expect(toastSuccess).toHaveBeenCalledExactlyOnceWith('GOALS_PAGE.SAVED_TOAST');
    expect(page(harness).textContent).toContain('Diary history');
});

it('keeps the clear draft after a partial save and retries only the remaining body target', async () => {
    const harness = await openGoalsAsync();
    const editor = page(harness);
    const weight = input(editor, '#goals-editor-weight');
    const waist = input(editor, '#goals-editor-waist');
    setNumber(weight, '');
    setNumber(waist, '');
    harness.detectChanges();

    const cleared = { desiredWeightKg: null, desiredWaistCm: null };
    const save = button(editor, 'fd-unsaved-changes-bar fd-ui-button:last-child button');
    save.click();
    const firstWrite = goalWrite({ ...cleared, dailyCalorieTarget: GOALS.dailyCalorieTarget, waterGoal: GOALS.waterGoal });
    http.expectNone(WEIGHT_GOAL_API);
    firstWrite.flush(GOALS);
    const weightClear = http.expectOne({ method: 'PUT', url: WEIGHT_GOAL_API });
    expect(weightClear.request.body).toEqual({ desiredWeightKg: null });
    http.expectNone(WAIST_GOAL_API);
    weightClear.flush({ desiredWeightKg: null });
    const failedWaistClear = http.expectOne({ method: 'PUT', url: WAIST_GOAL_API });
    expect(failedWaistClear.request.body).toEqual({ desiredWaistCm: null });
    rejectSave(failedWaistClear);
    await harness.fixture.whenStable();
    harness.detectChanges();

    http.expectNone(USER_INFO_API);
    expect(toastSuccess).not.toHaveBeenCalled();
    expect(toastError).toHaveBeenCalledExactlyOnceWith('GOALS_PAGE.STATUS_ERROR');
    expect(weight.valueAsNumber).toBe(0);
    expect(waist.valueAsNumber).toBe(0);
    expect(save.disabled).toBe(false);

    save.click();
    goalWrite(cleared).flush({ ...GOALS, desiredWeightKg: null });
    http.expectNone(WEIGHT_GOAL_API);
    const remainingWaistClear = http.expectOne({ method: 'PUT', url: WAIST_GOAL_API });
    expect(remainingWaistClear.request.body).toEqual({ desiredWaistCm: null });
    remainingWaistClear.flush({ desiredWaistCm: null });
    acceptUserRefresh({ ...GOALS, ...cleared });
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(toastSuccess).toHaveBeenCalledExactlyOnceWith('GOALS_PAGE.SAVED_TOAST');
    expect(editor.querySelector('fd-unsaved-changes-bar')).toBeNull();
    await harness.navigateByUrl(DEPARTURE_URL, GoalsDepartureComponent);
    await harness.navigateByUrl(GOALS_URL);
    http.expectOne({ method: 'GET', url: GOALS_API }).flush({ ...GOALS, ...cleared });
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(input(page(harness), '#goals-editor-weight').valueAsNumber).toBe(0);
    expect(input(page(harness), '#goals-editor-waist').valueAsNumber).toBe(0);
});

it('blocks an invalid water target until the user selects a valid visible preset', async () => {
    const harness = await openGoalsAsync();
    const editor = page(harness);
    const water = input(editor, '#goals-editor-water');
    setNumber(water, -1);
    harness.detectChanges();
    const save = button(editor, 'fd-unsaved-changes-bar fd-ui-button:last-child button');

    expect(water.getAttribute('aria-invalid')).toBe('true');
    expect(editor.querySelector('#goals-editor-water-error')?.textContent).toContain('GOALS_PAGE.EDITOR.NUMBER_RANGE');
    expect(save.disabled).toBe(true);
    save.click();
    http.expectNone({ method: 'PATCH', url: GOALS_API });

    const preset = button(editor, '.goals-editor-quick-values button:nth-child(2)');
    expect(preset.textContent).toContain('2,500');
    preset.click();
    harness.detectChanges();
    expect(water.valueAsNumber).toBe(QUICK_WATER_TARGET);
    expect(water.getAttribute('aria-invalid')).toBe('false');
    expect(editor.querySelector('#goals-editor-water-error')).toBeNull();
    expect(save.disabled).toBe(false);

    save.click();
    acceptSave(goalWrite({ waterGoal: QUICK_WATER_TARGET }), { ...GOALS, waterGoal: QUICK_WATER_TARGET });
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(toastSuccess).toHaveBeenCalledExactlyOnceWith('GOALS_PAGE.SAVED_TOAST');
    expect(editor.querySelector('fd-unsaved-changes-bar')).toBeNull();
});

async function openGoalsAsync(): Promise<RouterTestingHarness> {
    const harness = await RouterTestingHarness.create(GOALS_URL);
    http.expectOne({ method: 'GET', url: GOALS_API }).flush(GOALS);
    await harness.fixture.whenStable();
    harness.detectChanges();
    return harness;
}

function goalWrite(expected: Partial<GoalsResponse>): TestRequest {
    const request = http.expectOne({ method: 'PATCH', url: GOALS_API });
    expect(request.request.body).toEqual(
        expect.objectContaining({ desiredWeightKg: GOALS.desiredWeightKg, desiredWaistCm: GOALS.desiredWaistCm, ...expected }),
    );
    return request;
}

function acceptSave(request: TestRequest, response: GoalsResponse): void {
    request.flush(response);
    acceptUserRefresh(response);
}

function acceptUserRefresh(response: GoalsResponse): void {
    http.expectOne({ method: 'GET', url: USER_INFO_API }).flush({
        ...USER,
        dailyCalorieTarget: response.dailyCalorieTarget,
        calories: response.dailyCalorieTarget,
        waterGoal: response.waterGoal,
        desiredWeightKg: response.desiredWeightKg,
        desiredWaistCm: response.desiredWaistCm,
    });
}

function rejectSave(request: TestRequest): void {
    request.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
}

function page(harness: RouterTestingHarness): HTMLElement {
    const root = harness.routeNativeElement;
    if (root === null) {
        throw new Error('Expected a rendered goals route');
    }
    return root;
}

function button(host: ParentNode, selector: string): HTMLButtonElement {
    const control = host.querySelector(selector);
    if (!(control instanceof HTMLButtonElement)) {
        throw new Error(`Expected the goals button ${selector}`);
    }
    return control;
}

function input(host: ParentNode, selector: string): HTMLInputElement {
    const control = host.querySelector(selector);
    if (!(control instanceof HTMLInputElement)) {
        throw new Error(`Expected the goals input ${selector}`);
    }
    return control;
}

function setNumber(control: HTMLInputElement, value: number | ''): void {
    control.value = String(value);
    control.dispatchEvent(new Event('input', { bubbles: true }));
}

async function chooseDepartureAsync(harness: RouterTestingHarness, selector: string): Promise<void> {
    await vi.waitFor(() => {
        harness.detectChanges();
        button(document, `fd-unsaved-changes-dialog ${selector}`).click();
    });
}
