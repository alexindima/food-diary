import '@angular/compiler';

import { OverlayContainer } from '@angular/cdk/overlay';
import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiHintDirective } from './fd-ui-hint.directive';

const HINT_SHOW_DELAY_MS = 500;

@Component({
    standalone: true,
    imports: [FdUiHintDirective],
    template: ' <button type="button" fdUiHint="Notifications" [fdUiHintShowDelay]="hintShowDelayMs">Open dialog</button> ',
})
class TestHostComponent {
    protected readonly hintShowDelayMs = HINT_SHOW_DELAY_MS;
}

describe('FdUiHintDirective', () => {
    let fixture: ComponentFixture<TestHostComponent>;
    let overlayContainer: OverlayContainer;
    let overlayRoot: HTMLElement;
    let trigger: HTMLButtonElement;

    beforeEach(async () => {
        vi.useFakeTimers();

        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
            providers: [],
        }).compileComponents();

        overlayContainer = TestBed.inject(OverlayContainer);
        overlayRoot = overlayContainer.getContainerElement();

        fixture = TestBed.createComponent(TestHostComponent);
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const button = host.querySelector<HTMLButtonElement>('button');
        if (button === null) {
            throw new Error('Expected hint trigger to exist.');
        }

        trigger = button;
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

        vi.advanceTimersByTime(HINT_SHOW_DELAY_MS);
        fixture.detectChanges();

        expect(overlayRoot.querySelector('.fd-ui-hint')).toBeNull();
    });
});
