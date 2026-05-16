import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { FastingMessageViewModel } from '../../fasting-page-lib/fasting-page.types';
import { FastingAlertsSectionComponent } from './fasting-alerts-section.component';

describe('FastingAlertsSectionComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [FastingAlertsSectionComponent, TranslateModule.forRoot()],
        });
    });

    it('renders nothing without alerts', () => {
        const fixture = createComponent([]);

        expect(getElement(fixture).textContent.trim()).toBe('');
    });

    it('renders alerts and emits prompt actions', () => {
        const fixture = createComponent([createAlert()]);
        const promptSnooze = vi.fn();
        const promptDismiss = vi.fn();
        fixture.componentInstance.promptSnooze.subscribe(promptSnooze);
        fixture.componentInstance.promptDismiss.subscribe(promptDismiss);
        const element = getElement(fixture);
        const alert = fixture.debugElement.query(By.css('fd-ui-inline-alert'));

        expect(element.textContent).toContain('Hydrate');
        expect(element.textContent).toContain('Drink water');
        alert.triggerEventHandler('primaryAction', undefined);
        alert.triggerEventHandler('secondaryAction', undefined);

        expect(promptSnooze).toHaveBeenCalledWith('alert-1');
        expect(promptDismiss).toHaveBeenCalledWith('alert-1');
    });
});

function createComponent(alerts: FastingMessageViewModel[]): ComponentFixture<FastingAlertsSectionComponent> {
    const fixture = TestBed.createComponent(FastingAlertsSectionComponent);
    fixture.componentRef.setInput('alerts', alerts);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<FastingAlertsSectionComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createAlert(): FastingMessageViewModel {
    return {
        message: {
            id: 'alert-1',
            titleKey: 'FASTING.ALERT',
            bodyKey: 'FASTING.ALERT_BODY',
            tone: 'warning',
            bodyParams: null,
        },
        severity: 'warning',
        title: 'Hydrate',
        body: 'Drink water',
    };
}
