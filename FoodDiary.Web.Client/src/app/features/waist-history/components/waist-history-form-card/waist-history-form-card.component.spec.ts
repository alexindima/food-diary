import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { WaistHistoryFormCardComponent } from './waist-history-form-card.component';

describe('WaistHistoryFormCardComponent', () => {
    it('renders add mode by default', () => {
        const { fixture } = setupComponent(false);

        expect(getText(fixture)).toContain('WAIST_HISTORY.ADD');
    });

    it('renders edit mode and emits cancel', () => {
        const { component, fixture } = setupComponent(true);
        const cancelHandler = vi.fn();
        component.editCancel.subscribe(cancelHandler);

        component.editCancel.emit();

        expect(getText(fixture)).toContain('WAIST_HISTORY.UPDATE');
        expect(getText(fixture)).toContain('WAIST_HISTORY.CANCEL_EDIT');
        expect(cancelHandler).toHaveBeenCalledOnce();
    });
});

function setupComponent(isEditing: boolean): {
    component: WaistHistoryFormCardComponent;
    fixture: ComponentFixture<WaistHistoryFormCardComponent>;
} {
    TestBed.configureTestingModule({
        imports: [WaistHistoryFormCardComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(WaistHistoryFormCardComponent);
    fixture.componentRef.setInput(
        'form',
        new FormGroup({
            date: new FormControl('2026-05-15'),
            circumference: new FormControl('81.5'),
        }),
    );
    fixture.componentRef.setInput('isSaving', false);
    fixture.componentRef.setInput('isEditing', isEditing);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<WaistHistoryFormCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
