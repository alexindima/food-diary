import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { ExportService } from '../../../../shared/api/export.service';
import { CyclesService } from '../../api/cycles.service';
import { CycleTrackingFacade } from '../../lib/cycle-tracking.facade';
import { CycleSettingsDrawerComponent } from './cycle-settings-drawer';

const DURATION_FIELD_COUNT = 3;

let fixture: ComponentFixture<CycleSettingsDrawerComponent>;
let facade: CycleTrackingFacade;

beforeEach(() => {
    TestBed.configureTestingModule({
        imports: [CycleSettingsDrawerComponent],
        providers: [
            provideTranslateTesting(),
            CycleTrackingFacade,
            { provide: CyclesService, useValue: {} },
            { provide: ExportService, useValue: {} },
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
        const element = fixture.nativeElement as HTMLElement;
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
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('[role=alert]')?.textContent).toContain('CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
        expect(element.querySelectorAll('input[type=number]')).toHaveLength(DURATION_FIELD_COUNT);
        expect(element.querySelector('button[type=submit]')?.hasAttribute('disabled')).toBe(false);
    });
});
