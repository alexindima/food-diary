import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { WeightHistoryFormCardComponent } from './weight-history-form-card';

describe('WeightHistoryFormCardComponent', () => {
    it('renders add mode by default', () => {
        const { fixture } = setupComponent(false);

        expect(getText(fixture)).toContain('WEIGHT_HISTORY.ADD');
    });

    it('renders edit mode and emits cancel', () => {
        const { component, fixture } = setupComponent(true);
        const cancelHandler = vi.fn();
        component['editCancel'].subscribe(cancelHandler);

        component['editCancel'].emit();

        expect(getText(fixture)).toContain('WEIGHT_HISTORY.UPDATE');
        expect(getText(fixture)).toContain('WEIGHT_HISTORY.CANCEL_EDIT');
        expect(cancelHandler).toHaveBeenCalledOnce();
    });
});

function setupComponent(isEditing: boolean): {
    component: WeightHistoryFormCardComponent;
    fixture: ComponentFixture<WeightHistoryFormCardComponent>;
} {
    TestBed.configureTestingModule({
        imports: [WeightHistoryFormCardComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(WeightHistoryFormCardComponent);
    const model = signal({ date: '2026-05-15', weight: '71.5' });
    fixture.componentRef.setInput(
        'form',
        TestBed.runInInjectionContext(() => form(model)),
    );
    fixture.componentRef.setInput('isSaving', false);
    fixture.componentRef.setInput('isEditing', isEditing);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<WeightHistoryFormCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
