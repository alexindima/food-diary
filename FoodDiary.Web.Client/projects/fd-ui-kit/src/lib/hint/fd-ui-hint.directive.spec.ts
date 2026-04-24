import '@angular/compiler';
import { Component } from '@angular/core';
import { OverlayContainer } from '@angular/cdk/overlay';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { FdUiHintDirective } from './fd-ui-hint.directive';

@Component({
    standalone: true,
    imports: [FdUiHintDirective],
    template: ' <button type="button" fdUiHint="Notifications" [fdUiHintShowDelay]="500">Open dialog</button> ',
})
class TestHostComponent {}

describe('FdUiHintDirective', () => {
    let fixture: ComponentFixture<TestHostComponent>;
    let overlayContainer: OverlayContainer;
    let overlayRoot: HTMLElement;
    let trigger: HTMLButtonElement;

    beforeEach(async () => {
        vi.useFakeTimers();

        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
            providers: [provideNoopAnimations()],
        }).compileComponents();

        overlayContainer = TestBed.inject(OverlayContainer);
        overlayRoot = overlayContainer.getContainerElement();

        fixture = TestBed.createComponent(TestHostComponent);
        fixture.detectChanges();

        trigger = fixture.nativeElement.querySelector('button');
    });

    afterEach(() => {
        vi.runOnlyPendingTimers();
        vi.useRealTimers();
    });

    it('cancels a pending tooltip show when the trigger is clicked', () => {
        trigger.dispatchEvent(new MouseEvent('mouseenter', { bubbles: true }));
        fixture.detectChanges();

        trigger.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        fixture.detectChanges();

        vi.advanceTimersByTime(500);
        fixture.detectChanges();

        expect(overlayRoot.querySelector('.fd-ui-hint')).toBeNull();
    });
});
