import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { CycleDayFormModel } from '../../lib/cycle-tracking.facade';
import { BLEEDING_TYPE_BLEEDING, BLEEDING_TYPE_SPOTTING, CYCLE_FLOW_LIGHT } from '../../models/cycle.data';
import { CycleDayEditorDrawerComponent } from './cycle-day-editor-drawer';

const INITIAL_DAY: CycleDayFormModel = {
    date: '2026-08-17',
    isBleeding: false,
    bleedingType: BLEEDING_TYPE_BLEEDING,
    flow: CYCLE_FLOW_LIGHT,
    pain: 0,
    mood: 0,
    energy: 0,
    sleepQuality: 0,
    bloating: 0,
    headache: 0,
    libido: 0,
    basalBodyTemperatureCelsius: null,
    ovulationTestResult: null,
    cervicalFluid: null,
    hadSex: false,
    notes: null,
};

describe('CycleDayEditorDrawerComponent', () => {
    let fixture: ComponentFixture<CycleDayEditorDrawerComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [CycleDayEditorDrawerComponent],
            providers: [provideTranslateTesting()],
        });
        fixture = TestBed.createComponent(CycleDayEditorDrawerComponent);
        const dayForm = TestBed.runInInjectionContext(() => form(signal({ ...INITIAL_DAY })));
        fixture.componentRef.setInput('dayForm', dayForm);
        fixture.componentRef.setInput('isSaving', false);
        fixture.componentRef.setInput('ovulationTestOptions', []);
        fixture.detectChanges();
    });

    it('updates the bleeding selection through the segmented control', () => {
        fixture.componentInstance['setBleeding'](BLEEDING_TYPE_SPOTTING);

        expect(fixture.componentInstance.dayForm().isBleeding().value()).toBe(true);
        expect(fixture.componentInstance.dayForm().bleedingType().value()).toBe(BLEEDING_TYPE_SPOTTING);

        fixture.componentInstance['setBleeding'](null);

        expect(fixture.componentInstance.dayForm().isBleeding().value()).toBe(false);
    });

    it('emits close from the drawer close action', () => {
        const closed = vi.fn();
        fixture.componentInstance.closed.subscribe(closed);

        fixture.componentInstance['close']();

        expect(closed).toHaveBeenCalledOnce();
    });
});
