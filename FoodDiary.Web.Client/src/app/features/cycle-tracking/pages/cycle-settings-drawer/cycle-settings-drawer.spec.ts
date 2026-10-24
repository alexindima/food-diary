import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { ExportService } from '../../../../shared/api/export.service';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../../../shared/development/development-tools';
import { CyclesService } from '../../api/cycles.service';
import { CycleDayFacade } from '../../lib/cycle-day.facade';
import { CycleEpisodeFacade } from '../../lib/cycle-episode.facade';
import { CycleExportFacade } from '../../lib/cycle-export.facade';
import { CycleFactorFacade } from '../../lib/cycle-factor.facade';
import { CycleSettingsFacade } from '../../lib/cycle-settings.facade';
import { CycleTrackingFacade } from '../../lib/cycle-tracking.facade';
import { CycleTrackingStateFacade } from '../../lib/cycle-tracking-state.facade';
import { CycleSettingsDrawerComponent } from './cycle-settings-drawer';

const DURATION_FIELD_COUNT = 3;

let fixture: ComponentFixture<CycleSettingsDrawerComponent>;
let facade: CycleTrackingFacade;
let publishedScreen: DevelopmentScreen;

beforeEach(() => {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    TestBed.configureTestingModule({
        imports: [CycleSettingsDrawerComponent],
        providers: [
            provideTranslateTesting(),
            CycleTrackingFacade,
            CycleTrackingStateFacade,
            CycleSettingsFacade,
            CycleDayFacade,
            CycleFactorFacade,
            CycleEpisodeFacade,
            CycleExportFacade,
            { provide: CyclesService, useValue: {} },
            { provide: ExportService, useValue: {} },
            {
                provide: DEVELOPMENT_SCREENS,
                useValue: {
                    attach: (screen: DevelopmentScreen) => {
                        publishedScreen = screen;
                        return (): void => {};
                    },
                },
            },
        ],
    });
    facade = TestBed.inject(CycleTrackingFacade);
    fixture = TestBed.createComponent(CycleSettingsDrawerComponent);
    fixture.componentRef.setInput('settingsForm', facade.settingsForm);
    fixture.componentRef.setInput('modeOptions', []);
    fixture.componentRef.setInput('goalOptions', []);
    fixture.componentRef.setInput('reproductiveStateOptions', []);
    fixture.componentRef.setInput('isSaving', false);
    fixture.componentRef.setInput('isDeleting', false);
    fixture.detectChanges();
});

describe('CycleSettingsDrawerComponent errors', () => {
    it('announces an invalid duration and associates it with the input', () => {
        facade.settingsModel.update(model => ({ ...model, averageCycleLength: 28.5 }));
        facade.settingsForm.averageCycleLength().markAsTouched();
        fixture.detectChanges();
        const element = document.body;
        const input = element.querySelector('input[type=number]');
        const alert = element.querySelector('[role=alert]');
        expect(alert?.textContent).toContain('CYCLE_TRACKING.AVG_LENGTH_ERROR');
        expect(input?.getAttribute('aria-invalid')).toBe('true');
        expect(input?.getAttribute('aria-describedby')).toContain(alert?.id);
        facade.settingsModel.update(model => ({ ...model, averageCycleLength: 28 }));
        fixture.detectChanges();
        expect(element.querySelector('[role=alert]')).toBeNull();
    });

    it('announces a failed save while preserving the settings controls', () => {
        fixture.componentRef.setInput('error', 'CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
        fixture.detectChanges();
        const element = document.body;
        expect(element.querySelector('[role=alert]')?.textContent).toContain('CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
        expect(element.querySelectorAll('input[type=number]')).toHaveLength(DURATION_FIELD_COUNT);
        expect(element.querySelector('button[type=submit]')?.hasAttribute('disabled')).toBe(false);
    });
});

describe('CycleSettingsDrawerComponent pending actions', () => {
    it.each(['isSaving', 'isDeleting'] as const)('blocks dismissal while %s', operation => {
        fixture.componentRef.setInput(operation, true);
        fixture.detectChanges();
        const closed = vi.fn();
        fixture.componentInstance.closed.subscribe(closed);
        const element = document.body;
        element.querySelector('.cycle-drawer')?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27, bubbles: true }));
        expect(closed).not.toHaveBeenCalled();
        const buttons = Array.from(element.querySelectorAll('button'));
        expect(buttons.find(button => button.textContent.includes('COMMON.CANCEL'))?.disabled).toBe(true);
        expect(element.querySelector<HTMLButtonElement>('.cycle-drawer__backdrop')?.disabled).toBe(true);
        expect(element.querySelector('form')?.getAttribute('aria-busy')).toBe('true');
        fixture.componentRef.setInput(operation, false);
        fixture.detectChanges();
        element.querySelector('.cycle-drawer')?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27, bubbles: true }));
        expect(closed).toHaveBeenCalledOnce();
    });
});

describe('Cycle settings development capability', () => {
    const draft = { averageCycleLength: 29, averagePeriodLength: 6, lutealLength: 13 };

    it('fills the actual numeric form while retaining consent values and exposing only allowlisted state', () => {
        const original = { ...facade.settingsModel() };
        publishedScreen.fillDraft?.(draft);
        fixture.detectChanges();
        expect(facade.settingsModel()).toEqual({ ...original, ...draft });
        expect(publishedScreen.isDirty?.()).toBe(true);
        expect(publishedScreen.read()).toMatchObject({
            values: draft,
            valid: true,
            fields: {
                averageCycleLength: { min: 18, max: 60, step: 1, unit: 'days' },
            },
        });
        expect(publishedScreen.read()).not.toHaveProperty('cycleTrackingConsentGranted');
        expect(JSON.stringify(publishedScreen.read())).not.toMatch(/notes|fertilitySignals|nutritionInsights/);
        publishedScreen.clearDraft?.();
        expect(facade.settingsModel()).toEqual(original);
        expect(publishedScreen.isDirty?.()).toBe(false);
    });

    it.each([
        { ...draft, averageCycleLength: 17 },
        { ...draft, averagePeriodLength: 0 },
        { ...draft, lutealLength: 13.5 },
        { ...draft, nutritionInsightsConsentGranted: true },
    ])('refuses invalid or extra draft fields atomically: %j', invalid => {
        const original = { ...facade.settingsModel() };
        expect(() => publishedScreen.fillDraft?.(invalid)).toThrow();
        expect(facade.settingsModel()).toEqual(original);
        expect(publishedScreen.isDirty?.()).toBe(false);
    });

    it('preserves other settings edited through the interface when discard is requested', () => {
        publishedScreen.fillDraft?.(draft);
        facade.settingsModel.update(value => ({ ...value, nutritionInsightsConsentGranted: true }));
        expect(() => publishedScreen.clearDraft?.()).toThrow('Other settings changed');
        expect(facade.settingsModel()).toMatchObject({ ...draft, nutritionInsightsConsentGranted: true });
    });

    it('reports actual validation errors and refuses tools while saving', () => {
        facade.settingsModel.update(value => ({ ...value, averageCycleLength: 61 }));
        expect(publishedScreen.read()).toMatchObject({
            valid: false,
            fields: {
                averageCycleLength: { errors: ['max'], validationMessage: 'CYCLE_TRACKING.AVG_LENGTH_ERROR' },
            },
        });
        fixture.componentRef.setInput('isSaving', true);
        fixture.detectChanges();
        expect(() => publishedScreen.fillDraft?.(draft)).toThrow('busy');
        expect(() => publishedScreen.clearDraft?.()).toThrow('busy');
    });
});

afterEach(() => vi.unstubAllGlobals());
