import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { stubScrollIntoView } from '../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { MeasurementSystemService } from '../../shared/measurements/measurement-system.service';
import { type DayCalorieKey, DAYS_OF_WEEK, type GoalsResponse } from '../../shared/models/goals.data';
import { BrowserStorageService } from '../../shared/platform/browser-storage.service';
import { NutritionDataInvalidationService } from '../../shared/state/nutrition-data-invalidation.service';
import { GoalsService } from './api/goals.service';
import goalsRoutes from './goals.routes';

@Component({ template: '<h1>Other page</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class OtherPageComponent {}

const GOALS: GoalsResponse = {
    dailyCalorieTarget: 2000,
    proteinTarget: 110,
    fatTarget: 65,
    carbTarget: 210,
    fiberTarget: 25,
    waterGoal: 2000,
    desiredWeightKg: 72,
    desiredWaistCm: 82,
    calorieCyclingEnabled: false,
};
const UPDATED_CALORIES = 2400;
const CLASSIC_PROTEIN_GRAMS = 180;
const CLASSIC_FAT_GRAMS = 80;
const CLASSIC_CARB_GRAMS = 240;
const EDITED_PROTEIN_GRAMS = 175;
const IMPERIAL_WEIGHT = 154.32;
const IMPERIAL_WAIST = 31.5;
const CANONICAL_WEIGHT = 70;
const CANONICAL_WAIST = 80.01;
const IMPERIAL_LIMIT_EXCESS = 0.02;
const IMPERIAL_WEIGHT_INCREMENT = 0.01;

type GoalsApi = {
    [Method in 'getGoalsStrict' | 'updateGoals']: Mock<GoalsService[Method]>;
};

type GoalsRouteSetup = {
    api: GoalsApi;
    showSuccess: Mock<FdUiToastService['success']>;
    reportChange: Mock<NutritionDataInvalidationService['reportGoalMutation']>;
    startTour: Mock<FdTourService['start']>;
};

function configureGoals(): GoalsRouteSetup {
    const api: GoalsApi = {
        getGoalsStrict: vi.fn<GoalsService['getGoalsStrict']>().mockReturnValue(of(GOALS)),
        updateGoals: vi.fn<GoalsService['updateGoals']>().mockReturnValue(of(GOALS)),
    };
    const showSuccess = vi.fn<FdUiToastService['success']>();
    const reportChange = vi.fn<NutritionDataInvalidationService['reportGoalMutation']>();
    const startTour = vi.fn<FdTourService['start']>();
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                { path: 'goals', children: goalsRoutes },
                { path: 'other', component: OtherPageComponent },
            ]),
            provideTranslateTesting(),
            { provide: GoalsService, useValue: api },
            { provide: BrowserStorageService, useValue: { getItem: (): null => null, setItem: vi.fn() } },
            { provide: FdUiToastService, useValue: { success: showSuccess, error: vi.fn() } },
            { provide: NutritionDataInvalidationService, useValue: { reportGoalMutation: reportChange } },
            { provide: FdTourService, useValue: { start: startTour } },
        ],
    });
    return { api, showSuccess, reportChange, startTour };
}

function page(harness: RouterTestingHarness): HTMLElement {
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Expected a rendered goals route');
    }
    return host;
}

function requiredInput(host: HTMLElement, selector: string): HTMLInputElement {
    const input = host.querySelector(selector);
    if (!(input instanceof HTMLInputElement)) {
        throw new Error(`Expected a goals input matching ${selector}`);
    }
    return input;
}

function requiredButton(host: ParentNode, selector: string): HTMLButtonElement {
    const button = host.querySelector(selector);
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error(`Expected a goals button matching ${selector}`);
    }
    return button;
}

function editNumber(input: HTMLInputElement, value: number): void {
    input.value = String(value);
    input.dispatchEvent(new Event('input', { bubbles: true }));
}

function enterIncompleteNumber(input: HTMLInputElement): void {
    // jsdom cannot type a lone minus into number fields. Chrome proves the native keystroke.
    const badInput = vi.spyOn(input.validity, 'badInput', 'get').mockReturnValue(true);
    input.value = '';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    badInput.mockRestore();
}

function redraw(harness: RouterTestingHarness): void {
    TestBed.tick();
    harness.detectChanges();
}

async function confirmGoalsDepartureAsync(harness: RouterTestingHarness, selector: string): Promise<void> {
    await vi.waitFor(() => {
        redraw(harness);
        requiredButton(document, `fd-unsaved-changes-dialog ${selector}`).click();
    });
}

describe('Routed goal refresh deduplication', () => {
    it('opens only one retry while the selected request is pending, then permits a later retry', async () => {
        const { api } = configureGoals();
        const pending = new Subject<GoalsResponse>();
        const current = { ...GOALS, dailyCalorieTarget: UPDATED_CALORIES };
        api.getGoalsStrict
            .mockReturnValueOnce(throwError(() => new Error('Initial read unavailable')))
            .mockReturnValueOnce(pending)
            .mockReturnValueOnce(of(current));
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        const retry = requiredButton(host, 'fd-error-state button');
        retry.click();
        retry.click();
        redraw(harness);

        expect(api.getGoalsStrict).toHaveBeenCalledTimes(2);
        expect(pending.observed).toBe(true);
        expect(host.querySelector('.goals__skeleton')).not.toBeNull();
        pending.error(new Error('Retry unavailable'));
        redraw(harness);
        expect(host.querySelector('fd-error-state')).not.toBeNull();
        requiredButton(host, 'fd-error-state button').click();
        redraw(harness);

        const expectedReadCountAfterRecovery = 3;
        expect(api.getGoalsStrict).toHaveBeenCalledTimes(expectedReadCountAfterRecovery);
        expect(requiredInput(host, '#goals-editor-calories').value).toBe(String(UPDATED_CALORIES));
        expect(host.querySelector('.goals__skeleton')).toBeNull();
    });
});

describe('Routed goal save before departure', () => {
    beforeEach(() => {
        vi.stubGlobal(
            'matchMedia',
            vi.fn(() => ({ matches: false })),
        );
        onTestFinished(() => {
            vi.unstubAllGlobals();
        });
    });

    it('keeps a newer edit when the guard waits for a previously submitted save', async () => {
        const { api, reportChange } = configureGoals();
        const dialogs = TestBed.inject(FdUiDialogService);
        onTestFinished(() => {
            dialogs.closeAll();
        });
        const pending = new Subject<GoalsResponse | null>();
        const submittedWaterGoal = 2300;
        const newerWaterGoal = 2600;
        api.updateGoals.mockReturnValueOnce(pending).mockReturnValueOnce(of({ ...GOALS, waterGoal: newerWaterGoal }));
        const harness = await RouterTestingHarness.create('/goals');
        const router = TestBed.inject(Router);
        const host = page(harness);
        const water = requiredInput(host, '#goals-editor-water');
        editNumber(water, submittedWaterGoal);
        redraw(harness);
        requiredButton(host, 'fd-unsaved-changes-bar fd-ui-button:last-child button').click();
        redraw(harness);
        expect(pending.observed).toBe(true);
        editNumber(water, newerWaterGoal);
        redraw(harness);
        const firstDeparture = router.navigateByUrl('/other');
        await confirmGoalsDepartureAsync(harness, '.unsaved-changes-dialog__action--primary button');
        expect(api.updateGoals).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ waterGoal: submittedWaterGoal }));

        pending.next({ ...GOALS, waterGoal: submittedWaterGoal });
        pending.complete();
        await expect(firstDeparture).resolves.toBe(false);
        redraw(harness);
        expect(router.url).toBe('/goals');
        expect(water.value).toBe(String(newerWaterGoal));
        expect(host.querySelector('fd-unsaved-changes-bar')).not.toBeNull();

        const secondDeparture = router.navigateByUrl('/other');
        await confirmGoalsDepartureAsync(harness, '.unsaved-changes-dialog__action--primary button');
        await expect(secondDeparture).resolves.toBe(true);

        expect(api.updateGoals).toHaveBeenCalledTimes(2);
        expect(api.updateGoals).toHaveBeenLastCalledWith(expect.objectContaining({ waterGoal: newerWaterGoal }));
        expect(reportChange).toHaveBeenCalledTimes(2);
        expect(page(harness).textContent).toContain('Other page');
    });

    it('keeps invalid input for correction and permits deliberate discard without writing it', async () => {
        const { api } = configureGoals();
        const dialogs = TestBed.inject(FdUiDialogService);
        onTestFinished(() => {
            dialogs.closeAll();
        });
        const harness = await RouterTestingHarness.create('/goals');
        const router = TestBed.inject(Router);
        const water = requiredInput(page(harness), '#goals-editor-water');
        editNumber(water, -1);
        redraw(harness);
        const invalidSave = router.navigateByUrl('/other');
        await confirmGoalsDepartureAsync(harness, '.unsaved-changes-dialog__action--primary button');
        await expect(invalidSave).resolves.toBe(false);
        redraw(harness);

        expect(router.url).toBe('/goals');
        expect(water.value).toBe('-1');
        expect(water.getAttribute('aria-invalid')).toBe('true');
        expect(api.updateGoals).not.toHaveBeenCalled();
        const discard = router.navigateByUrl('/other');
        await confirmGoalsDepartureAsync(harness, 'fd-ui-button:nth-child(2) button');
        await expect(discard).resolves.toBe(true);
        expect(page(harness).textContent).toContain('Other page');
        expect(api.updateGoals).not.toHaveBeenCalled();
    });
});

describe('Routed body goal validation before departure', () => {
    it('blocks Save and Leave for a negative waist that would round to zero, then saves a corrected target', async () => {
        const { api } = configureGoals();
        const dialogs = TestBed.inject(FdUiDialogService);
        vi.stubGlobal(
            'matchMedia',
            vi.fn(() => ({ matches: false })),
        );
        onTestFinished(() => {
            dialogs.closeAll();
            vi.unstubAllGlobals();
        });
        TestBed.inject(MeasurementSystemService).setSystem('imperial');
        const harness = await RouterTestingHarness.create('/goals');
        const router = TestBed.inject(Router);
        const waist = requiredInput(page(harness), '#goals-editor-waist');
        const belowMinimum = -0.001;
        editNumber(waist, belowMinimum);
        redraw(harness);
        expect(waist.validity.rangeUnderflow).toBe(true);

        const invalidDeparture = router.navigateByUrl('/other');
        await confirmGoalsDepartureAsync(harness, '.unsaved-changes-dialog__action--primary button');
        await expect(invalidDeparture).resolves.toBe(false);
        redraw(harness);
        expect(router.url).toBe('/goals');
        expect(waist.value).toBe(String(belowMinimum));
        expect(waist.getAttribute('aria-describedby')).toBe('goals-editor-waist-error');
        expect(api.updateGoals).not.toHaveBeenCalled();

        api.updateGoals.mockReturnValueOnce(of({ ...GOALS, desiredWaistCm: CANONICAL_WAIST }));
        editNumber(waist, IMPERIAL_WAIST);
        redraw(harness);
        expect(waist.getAttribute('aria-describedby')).toBeNull();
        const correctedDeparture = router.navigateByUrl('/other');
        await confirmGoalsDepartureAsync(harness, '.unsaved-changes-dialog__action--primary button');
        await expect(correctedDeparture).resolves.toBe(true);
        expect(page(harness).textContent).toContain('Other page');
        expect(api.updateGoals).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ desiredWaistCm: CANONICAL_WAIST, desiredWeightKg: GOALS.desiredWeightKg }),
        );
    });
});

describe('Routed nutrition goal number edits', () => {
    it.each([
        { field: 'calories', target: 'dailyCalorieTarget', correction: 1950.5 },
        { field: 'protein', target: 'proteinTarget', correction: 123.5 },
        { field: 'water', target: 'waterGoal', correction: 2210.5 },
    ] as const)('blocks saving an unfinished $field value until it is corrected', async ({ field, target, correction }) => {
        const { api } = configureGoals();
        api.updateGoals.mockReturnValueOnce(of({ ...GOALS, [target]: correction }));
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        const input = requiredInput(host, `#goals-editor-${field}`);
        const saveSelector = 'fd-unsaved-changes-bar fd-ui-button:last-child button';
        expect(input.value).toBe(String(GOALS[target]));

        enterIncompleteNumber(input);
        redraw(harness);

        const save = requiredButton(host, saveSelector);
        expect(input.value).toBe('');
        expect(input.getAttribute('aria-invalid')).toBe('true');
        expect(save.disabled).toBe(true);
        save.click();
        expect(api.updateGoals).not.toHaveBeenCalled();

        editNumber(input, correction);
        redraw(harness);
        expect(input.getAttribute('aria-invalid')).toBe('false');
        requiredButton(host, saveSelector).click();
        await harness.fixture.whenStable();
        redraw(harness);

        expect(api.updateGoals).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ [target]: correction }));
        expect(host.querySelector('fd-unsaved-changes-bar')).toBeNull();
    });
});

describe('Routed goals load', () => {
    it('renders loading, lets the user retry an unsuccessful read and restores the editor', async () => {
        const { api } = configureGoals();
        const pending = new Subject<GoalsResponse>();
        api.getGoalsStrict.mockReturnValueOnce(pending);
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        expect(host.querySelector('.goals__status')?.textContent).toContain('GOALS_PAGE.STATUS_LOADING');
        expect(host.querySelector('.goals__skeleton')).not.toBeNull();
        expect(host.querySelector('fd-goals-editor')).toBeNull();

        pending.error(new Error('Goal history unavailable'));
        redraw(harness);
        expect(host.querySelector('fd-error-state [role="alert"]')?.textContent).toContain('ERRORS.LOAD_FAILED_TITLE');
        expect(host.querySelector('fd-goals-editor')).toBeNull();
        requiredButton(host, 'fd-error-state button').click();
        redraw(harness);

        expect(api.getGoalsStrict).toHaveBeenCalledTimes(2);
        expect(requiredInput(host, '#goals-editor-calories').value).toBe(String(GOALS.dailyCalorieTarget));
        expect(host.querySelector('fd-error-state')).toBeNull();
        expect(api.updateGoals).not.toHaveBeenCalled();
    });

    it('releases a pending goals read when the routed editor is left', async () => {
        const { api } = configureGoals();
        const pending = new Subject<GoalsResponse>();
        api.getGoalsStrict.mockReturnValueOnce(pending);
        const harness = await RouterTestingHarness.create('/goals');
        expect(pending.observed).toBe(true);

        await harness.navigateByUrl('/other');

        expect(pending.observed).toBe(false);
        pending.next(GOALS);
        pending.complete();
        expect(page(harness).textContent).toContain('Other page');
        expect(api.updateGoals).not.toHaveBeenCalled();
    });
});

describe('Routed goal preset catalog', () => {
    it('lists every macro preset and preserves the selected amounts when switching back to custom', async () => {
        stubScrollIntoView();
        const { api } = configureGoals();
        const options = [
            { label: 'CUSTOM', grams: ['110', '65', '210'] },
            { label: 'BALANCED_GYM', grams: ['200', '89', '100'] },
            { label: 'CLASSIC', grams: ['150', '67', '200'] },
            { label: 'LOW_CARB', grams: ['250', '67', '100'] },
            { label: 'HIGH_CARB', grams: ['125', '56', '250'] },
            { label: 'ENDURANCE', grams: ['100', '67', '250'] },
        ];
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        const control = requiredButton(host, '.goals-editor-nutrition__preset button[aria-haspopup="listbox"]');
        const labels = options.map(option => `GOALS_PAGE.MACRO_PRESET_${option.label}`);
        const macroInputs = ['protein', 'fats', 'carbs'].map(macro => requiredInput(host, `#goals-editor-${macro}`));
        const readMacros = (): string[] => macroInputs.map(input => input.value);
        const select = (index: number): void => {
            control.click();
            redraw(harness);
            const menu = [...document.querySelectorAll<HTMLButtonElement>('[role="listbox"] [role="option"]')];
            expect(menu.map(option => option.textContent.trim())).toEqual(labels);
            menu[index].click();
            redraw(harness);
            expect(control.textContent).toContain(labels[index]);
        };

        for (const [index, option] of options.entries()) {
            select(index);
            expect(readMacros()).toEqual(option.grams);
            expect(requiredInput(host, '#goals-editor-fiber').value).toBe(String(GOALS.fiberTarget));
        }

        const enduranceMacros = readMacros();
        select(0);
        editNumber(requiredInput(host, '#goals-editor-calories'), UPDATED_CALORIES);
        redraw(harness);
        expect(readMacros()).toEqual(enduranceMacros);
        expect(api.updateGoals).not.toHaveBeenCalled();
    });
});

describe('Routed goal preset', () => {
    it('recalculates a selected preset for changed calories, then saves a manually customized macro', async () => {
        stubScrollIntoView();
        const { api, showSuccess, reportChange } = configureGoals();
        api.updateGoals.mockReturnValueOnce(
            of({
                ...GOALS,
                dailyCalorieTarget: UPDATED_CALORIES,
                proteinTarget: EDITED_PROTEIN_GRAMS,
                fatTarget: CLASSIC_FAT_GRAMS,
                carbTarget: CLASSIC_CARB_GRAMS,
            }),
        );
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        const presetControl = requiredButton(host, '.goals-editor-nutrition__preset button[aria-haspopup="listbox"]');
        presetControl.click();
        redraw(harness);
        const preset = Array.from(document.querySelectorAll<HTMLButtonElement>('[role="listbox"] [role="option"]')).find(
            option => option.textContent.trim() === 'GOALS_PAGE.MACRO_PRESET_CLASSIC',
        );
        if (preset === undefined) {
            throw new Error('Classic macro preset is missing');
        }
        preset.click();
        redraw(harness);

        editNumber(requiredInput(host, '#goals-editor-calories'), UPDATED_CALORIES);
        redraw(harness);
        const protein = requiredInput(host, '#goals-editor-protein');
        expect(protein.value).toBe(String(CLASSIC_PROTEIN_GRAMS));
        expect(requiredInput(host, '#goals-editor-fats').value).toBe(String(CLASSIC_FAT_GRAMS));
        expect(requiredInput(host, '#goals-editor-carbs').value).toBe(String(CLASSIC_CARB_GRAMS));
        expect(requiredInput(host, '#goals-editor-fiber').value).toBe(String(GOALS.fiberTarget));
        expect(api.updateGoals).not.toHaveBeenCalled();

        vi.useFakeTimers();
        try {
            editNumber(protein, EDITED_PROTEIN_GRAMS);
            redraw(harness);
            const draftObservationMs = 1000;
            await vi.advanceTimersByTimeAsync(draftObservationMs);
            expect(api.updateGoals).not.toHaveBeenCalled();
        } finally {
            vi.useRealTimers();
        }
        expect(presetControl.textContent).toContain('GOALS_PAGE.MACRO_PRESET_CUSTOM');
        requiredButton(host, 'fd-unsaved-changes-bar fd-ui-button:last-child button').click();
        await harness.fixture.whenStable();
        redraw(harness);

        expect(api.updateGoals).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                dailyCalorieTarget: UPDATED_CALORIES,
                proteinTarget: EDITED_PROTEIN_GRAMS,
                fatTarget: CLASSIC_FAT_GRAMS,
            }),
        );
        expect(reportChange).toHaveBeenCalledOnce();
        expect(showSuccess).toHaveBeenCalledExactlyOnceWith('GOALS_PAGE.SAVED_TOAST');
        expect(host.querySelector('fd-unsaved-changes-bar')).toBeNull();
        expect(protein.value).toBe(String(EDITED_PROTEIN_GRAMS));
    });

    it('updates an already mounted preset label on a language change and starts help only from its button', async () => {
        const { api, startTour } = configureGoals();
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { GOALS_PAGE: { MACRO_PRESET_CUSTOM: 'Custom' } });
        translate.setTranslation('ru', { GOALS_PAGE: { MACRO_PRESET_CUSTOM: 'Свой набор' } });
        await firstValueFrom(translate.use('en'));
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        const select = requiredButton(host, '.goals-editor-nutrition__preset button[aria-haspopup="listbox"]');
        expect(select.textContent).toContain('Custom');
        expect(startTour).not.toHaveBeenCalled();

        await firstValueFrom(translate.use('ru'));
        redraw(harness);

        expect(select.textContent).toContain('Свой набор');
        expect(api.getGoalsStrict).toHaveBeenCalledOnce();
        requiredButton(host, '[data-tour-id="goals-tour-help"] button').click();
        expect(startTour).toHaveBeenCalledExactlyOnceWith(expect.any(Object), { force: true });
    });
});

describe('Routed body goals in imperial units', () => {
    it('blocks saving a weight one hundredth of a pound above the rendered field maximum', async () => {
        const { api } = configureGoals();
        TestBed.inject(MeasurementSystemService).setSystem('imperial');
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        const weight = requiredInput(host, '#goals-editor-weight');
        const maximum = Number(weight.max);
        editNumber(weight, maximum);
        redraw(harness);
        expect(weight.validity.rangeOverflow).toBe(false);

        const aboveMaximum = maximum + IMPERIAL_WEIGHT_INCREMENT;
        editNumber(weight, aboveMaximum);
        redraw(harness);
        expect(weight.validity.rangeOverflow).toBe(true);
        const save = requiredButton(host, 'fd-unsaved-changes-bar fd-ui-button:last-child button');
        save.click();
        await harness.fixture.whenStable();

        expect(api.updateGoals).not.toHaveBeenCalled();
        expect(weight.getAttribute('aria-invalid')).toBe('true');
        expect(weight.valueAsNumber).toBe(aboveMaximum);
        expect(save.disabled).toBe(true);

        const canonicalMaximum = 500;
        api.updateGoals.mockReturnValueOnce(of({ ...GOALS, desiredWeightKg: canonicalMaximum }));
        editNumber(weight, maximum);
        redraw(harness);
        expect(weight.validity.valid).toBe(true);
        expect(save.disabled).toBe(false);
        save.click();
        await harness.fixture.whenStable();
        redraw(harness);
        expect(api.updateGoals).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ desiredWeightKg: canonicalMaximum }));
        expect(weight.valueAsNumber).toBe(maximum);
        expect(weight.getAttribute('aria-invalid')).toBe('false');
    });
});

describe('Routed out-of-range body goal editing', () => {
    it('restores the loaded value on discard after an invalid edit that left the canonical target untouched', async () => {
        const { api } = configureGoals();
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        const weight = requiredInput(host, '#goals-editor-weight');
        const loaded = weight.value;
        expect(loaded).toBe(String(GOALS.desiredWeightKg));

        editNumber(weight, Number(weight.max) + IMPERIAL_WEIGHT_INCREMENT);
        redraw(harness);
        expect(weight.validity.rangeOverflow).toBe(true);
        expect(weight.getAttribute('aria-invalid')).toBe('true');

        requiredButton(host, 'fd-unsaved-changes-bar fd-ui-button:first-child button').click();
        redraw(harness);
        expect(weight.value).toBe(loaded);
        expect(weight.validity.valid).toBe(true);
        expect(weight.getAttribute('aria-invalid')).toBe('false');
        expect(host.querySelector('fd-unsaved-changes-bar')).toBeNull();
        expect(api.updateGoals).not.toHaveBeenCalled();
    });

    it('shows out-of-range errors and saves corrected body goals in kilograms and centimeters', async () => {
        const { api } = configureGoals();
        TestBed.inject(MeasurementSystemService).setSystem('imperial');
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        const weight = requiredInput(host, '#goals-editor-weight');
        const waist = requiredInput(host, '#goals-editor-waist');
        expect(weight.parentElement?.textContent).toContain('GENERAL.UNITS.LB');
        expect(waist.parentElement?.textContent).toContain('GENERAL.UNITS.IN');
        editNumber(weight, Number(weight.max) + IMPERIAL_LIMIT_EXCESS);
        editNumber(waist, Number(waist.max) + IMPERIAL_LIMIT_EXCESS);
        redraw(harness);

        expect(weight.getAttribute('aria-invalid')).toBe('true');
        expect(waist.getAttribute('aria-invalid')).toBe('true');
        expect(host.querySelector('#goals-editor-weight-error')?.textContent).toContain('GOALS_PAGE.EDITOR.BODY_RANGE');
        expect(host.querySelector('#goals-editor-waist-error')?.textContent).toContain('GOALS_PAGE.EDITOR.BODY_RANGE');
        const save = requiredButton(host, 'fd-unsaved-changes-bar fd-ui-button:last-child button');
        expect(save.disabled).toBe(true);
        expect(api.updateGoals).not.toHaveBeenCalled();

        editNumber(weight, IMPERIAL_WEIGHT);
        editNumber(waist, IMPERIAL_WAIST);
        redraw(harness);
        expect(weight.getAttribute('aria-invalid')).toBe('false');
        expect(waist.getAttribute('aria-invalid')).toBe('false');
        expect(weight.validity.stepMismatch).toBe(false);
        expect(waist.validity.stepMismatch).toBe(false);
        save.click();
        await harness.fixture.whenStable();

        expect(api.updateGoals).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ desiredWeightKg: CANONICAL_WEIGHT, desiredWaistCm: CANONICAL_WAIST }),
        );
    });
});

describe('Routed optional body goals', () => {
    it('clears empty weight and waist targets while preserving the calorie and water goals', async () => {
        const { api, reportChange } = configureGoals();
        const persisted = { ...GOALS, desiredWeightKg: null, desiredWaistCm: null };
        api.updateGoals.mockReturnValueOnce(of(persisted));
        TestBed.inject(MeasurementSystemService).setSystem('metric');
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        const weight = requiredInput(host, '#goals-editor-weight');
        const waist = requiredInput(host, '#goals-editor-waist');
        expect(weight.value).toBe(String(GOALS.desiredWeightKg));
        expect(waist.value).toBe(String(GOALS.desiredWaistCm));
        for (const input of [weight, waist]) {
            input.value = '';
            input.dispatchEvent(new Event('input', { bubbles: true }));
        }
        redraw(harness);

        const save = requiredButton(host, 'fd-unsaved-changes-bar fd-ui-button:last-child button');
        expect(save.disabled).toBe(false);
        save.click();
        await harness.fixture.whenStable();
        redraw(harness);

        expect(api.updateGoals).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                desiredWeightKg: null,
                desiredWaistCm: null,
                dailyCalorieTarget: GOALS.dailyCalorieTarget,
                waterGoal: GOALS.waterGoal,
            }),
        );
        expect(reportChange).toHaveBeenCalledOnce();
        expect(host.querySelector('fd-unsaved-changes-bar')).toBeNull();
        expect(weight.getAttribute('aria-invalid')).toBe('false');
        expect(waist.getAttribute('aria-invalid')).toBe('false');
    });
});

const WEEKLY_TARGETS: Record<DayCalorieKey, number> = {
    mondayCalories: 2200,
    tuesdayCalories: 2300,
    wednesdayCalories: 1900,
    thursdayCalories: 2100,
    fridayCalories: 2450,
    saturdayCalories: 2600,
    sundayCalories: 0,
};

describe('Routed weekly calorie cycling validation', () => {
    it('keeps an incomplete Monday out of saves and weekday copying until Reset replaces it', async () => {
        const { api } = configureGoals();
        api.getGoalsStrict.mockReturnValueOnce(of({ ...GOALS, ...WEEKLY_TARGETS, calorieCyclingEnabled: true }));
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        const monday = requiredInput(host, '#goals-editor-cycling-mondayCalories');
        const repeat = weeklyAction(host, 'GOALS_PAGE.EDITOR.REPEAT_WEEKDAYS');

        enterIncompleteNumber(monday);
        redraw(harness);
        const save = requiredButton(host, 'fd-unsaved-changes-bar fd-ui-button:last-child button');
        expect(monday.value).toBe('');
        expect(monday.getAttribute('aria-describedby')).toBe('goals-editor-cycling-error');
        expect(host.querySelector('#goals-editor-cycling-error')?.textContent).toContain('GOALS_PAGE.EDITOR.NUMBER_RANGE');
        expect(save.disabled).toBe(true);
        expect(repeat.disabled).toBe(true);
        expect(repeat.getAttribute('title')).toBe('GOALS_PAGE.EDITOR.NUMBER_RANGE');
        save.click();
        repeat.click();
        expect(requiredInput(host, '#goals-editor-cycling-tuesdayCalories').value).toBe(String(WEEKLY_TARGETS.tuesdayCalories));
        expect(api.updateGoals).not.toHaveBeenCalled();

        weeklyAction(host, 'GOALS_PAGE.EDITOR.RESET_DAYS').click();
        redraw(harness);
        expect(visibleWeeklyTargets(host)).toEqual(DAYS_OF_WEEK.map(() => GOALS.dailyCalorieTarget));
        expect(monday.getAttribute('aria-invalid')).toBe('false');
        expect(host.querySelector('#goals-editor-cycling-error')).toBeNull();
        expect(save.disabled).toBe(false);
        expect(repeat.disabled).toBe(false);
        expect(repeat.getAttribute('title')).toBeNull();
    });

    it('discards hidden incomplete weekday edits when cycling is toggled off and keeps unrelated validation', async () => {
        const { api } = configureGoals();
        api.getGoalsStrict.mockReturnValueOnce(of({ ...GOALS, ...WEEKLY_TARGETS, calorieCyclingEnabled: true }));
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        const water = requiredInput(host, '#goals-editor-water');
        const toggle = requiredButton(host, 'fd-goals-cycling-row button[role="switch"]');
        enterIncompleteNumber(requiredInput(host, '#goals-editor-cycling-mondayCalories'));
        enterIncompleteNumber(water);
        redraw(harness);

        toggle.click();
        redraw(harness);
        const save = requiredButton(host, 'fd-unsaved-changes-bar fd-ui-button:last-child button');
        expect(host.querySelector('fd-goals-cycling-day')).toBeNull();
        expect(water.getAttribute('aria-invalid')).toBe('true');
        expect(save.disabled).toBe(true);
        toggle.click();
        redraw(harness);
        expectWeeklyTargets(host, WEEKLY_TARGETS);
        expect(host.querySelector('#goals-editor-cycling-error')).toBeNull();
        expect(save.disabled).toBe(true);

        editNumber(water, GOALS.waterGoal ?? 0);
        redraw(harness);
        expect(save.disabled).toBe(false);
        expect(api.updateGoals).not.toHaveBeenCalled();
    });
});

describe('Routed weekly calorie cycling', () => {
    it('copies the entered Monday target to weekdays, keeps both weekend targets and saves the displayed week', async () => {
        const { api, reportChange } = configureGoals();
        const original = { ...GOALS, ...WEEKLY_TARGETS, calorieCyclingEnabled: true };
        api.getGoalsStrict.mockReturnValueOnce(of(original));
        const nextWeekday = 2550;
        const nextTargets = {
            ...WEEKLY_TARGETS,
            mondayCalories: nextWeekday,
            tuesdayCalories: nextWeekday,
            wednesdayCalories: nextWeekday,
            thursdayCalories: nextWeekday,
            fridayCalories: nextWeekday,
        };
        api.updateGoals.mockReturnValueOnce(of({ ...original, ...nextTargets }));
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        expectWeeklyTargets(host, WEEKLY_TARGETS);

        editNumber(requiredInput(host, '#goals-editor-cycling-mondayCalories'), nextWeekday);
        redraw(harness);
        weeklyAction(host, 'GOALS_PAGE.EDITOR.REPEAT_WEEKDAYS').click();
        redraw(harness);
        expectWeeklyTargets(host, nextTargets);
        expect(api.updateGoals).not.toHaveBeenCalled();

        requiredButton(host, 'fd-unsaved-changes-bar fd-ui-button:last-child button').click();
        await harness.fixture.whenStable();
        redraw(harness);
        expect(api.updateGoals).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ ...nextTargets, calorieCyclingEnabled: true, dailyCalorieTarget: GOALS.dailyCalorieTarget }),
        );
        expect(reportChange).toHaveBeenCalledOnce();
        expect(host.querySelector('fd-unsaved-changes-bar')).toBeNull();
        expectWeeklyTargets(host, nextTargets);
    });

    it('resets the weekly values to the current calorie draft and restores all loaded targets on discard', async () => {
        const { api } = configureGoals();
        api.getGoalsStrict.mockReturnValueOnce(of({ ...GOALS, ...WEEKLY_TARGETS, calorieCyclingEnabled: true }));
        const harness = await RouterTestingHarness.create('/goals');
        const host = page(harness);
        const daily = requiredInput(host, '#goals-editor-calories');
        const toggle = requiredButton(host, 'fd-goals-cycling-row button[role="switch"]');
        expect(toggle.getAttribute('aria-checked')).toBe('true');

        editNumber(daily, UPDATED_CALORIES);
        redraw(harness);
        weeklyAction(host, 'GOALS_PAGE.EDITOR.RESET_DAYS').click();
        redraw(harness);
        expect(visibleWeeklyTargets(host)).toEqual(DAYS_OF_WEEK.map(() => UPDATED_CALORIES));
        toggle.click();
        redraw(harness);
        expect(host.querySelector('fd-goals-cycling-day')).toBeNull();
        toggle.click();
        redraw(harness);
        expect(visibleWeeklyTargets(host)).toEqual(DAYS_OF_WEEK.map(() => UPDATED_CALORIES));

        requiredButton(host, 'fd-unsaved-changes-bar fd-ui-button:first-child button').click();
        redraw(harness);
        expect(daily.value).toBe(String(GOALS.dailyCalorieTarget));
        expectWeeklyTargets(host, WEEKLY_TARGETS);
        expect(host.querySelector('fd-unsaved-changes-bar')).toBeNull();
        expect(api.updateGoals).not.toHaveBeenCalled();
    });
});

function weeklyAction(host: HTMLElement, label: string): HTMLButtonElement {
    const button = Array.from(host.querySelectorAll<HTMLButtonElement>('.goals-editor-cycling__actions button')).find(action =>
        action.textContent.includes(label),
    );
    if (button === undefined) {
        throw new Error(`Expected a weekly goal action ${label}`);
    }
    return button;
}

function expectWeeklyTargets(host: HTMLElement, targets: Record<DayCalorieKey, number>): void {
    expect(visibleWeeklyTargets(host)).toEqual(DAYS_OF_WEEK.map(day => targets[day.key]));
}

function visibleWeeklyTargets(host: HTMLElement): number[] {
    return DAYS_OF_WEEK.map(day => requiredInput(host, `#goals-editor-cycling-${day.key}`).valueAsNumber);
}
