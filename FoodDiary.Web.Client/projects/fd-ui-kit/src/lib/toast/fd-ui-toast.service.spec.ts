import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiToastService } from './fd-ui-toast.service';

const DEFAULT_DURATION_MS = 5000;
const CUSTOM_DURATION_MS = 8000;
const SHORT_DURATION_MS = 1000;
const EXIT_START_DELAY_MS = 250;
const EXIT_ANIMATION_MS = 200;
const SUCCESS_DURATION_MS = 3500;
const INFO_DURATION_MS = 1500;

function setupToastService(): FdUiToastService {
    TestBed.configureTestingModule({
        providers: [FdUiToastService],
    });

    return TestBed.inject(FdUiToastService);
}

describe('FdUiToastService creation', () => {
    let service: FdUiToastService;

    beforeEach(() => {
        vi.useFakeTimers();
        service = setupToastService();
    });

    afterEach(() => {
        service.dismissAll();
        vi.runOnlyPendingTimers();
        vi.useRealTimers();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should create toast with message', () => {
        service.open('Hello');

        expect(service.toasts()).toHaveLength(1);
        expect(service.toasts()[0]?.message).toBe('Hello');
    });
});

describe('FdUiToastService options', () => {
    let service: FdUiToastService;

    beforeEach(() => {
        vi.useFakeTimers();
        service = setupToastService();
    });

    afterEach(() => {
        service.dismissAll();
        vi.runOnlyPendingTimers();
        vi.useRealTimers();
    });

    it('should apply configured appearance', () => {
        service.open('Error occurred', { appearance: 'negative' });

        expect(service.toasts()[0]?.appearance).toBe('negative');
    });

    it('should use default appearance when not provided', () => {
        service.open('Default message');

        expect(service.toasts()[0]?.appearance).toBe('default');
    });

    it('should use default duration', () => {
        service.open('Message');

        expect(service.toasts()[0]?.duration).toBe(DEFAULT_DURATION_MS);
    });

    it('should use custom duration', () => {
        service.open('Message', { duration: CUSTOM_DURATION_MS });

        expect(service.toasts()[0]?.duration).toBe(CUSTOM_DURATION_MS);
    });

    it('should store action text', () => {
        service.open('Deleted', { action: 'Undo' });

        expect(service.toasts()[0]?.action).toBe('Undo');
    });

    it('should use bottom center position by default', () => {
        service.open('Message');

        expect(service.toasts()[0]?.horizontalPosition).toBe('center');
        expect(service.toasts()[0]?.verticalPosition).toBe('bottom');
    });
});

describe('FdUiToastService dismissal', () => {
    let service: FdUiToastService;

    beforeEach(() => {
        vi.useFakeTimers();
        service = setupToastService();
    });

    afterEach(() => {
        service.dismissAll();
        vi.runOnlyPendingTimers();
        vi.useRealTimers();
    });

    it('should dismiss a toast after duration and exit animation', () => {
        service.open('Message', { duration: SHORT_DURATION_MS });

        vi.advanceTimersByTime(SHORT_DURATION_MS);
        expect(service.toasts()[0]?.leaving).toBe(false);

        vi.advanceTimersByTime(EXIT_START_DELAY_MS);
        expect(service.toasts()[0]?.leaving).toBe(true);

        vi.advanceTimersByTime(EXIT_ANIMATION_MS);
        expect(service.toasts()).toHaveLength(0);
    });

    it('should notify action and dismissal when action is triggered', () => {
        const ref = service.open('Message', { action: 'Undo', duration: 0 });
        const actionSpy = vi.fn();
        const dismissedSpy = vi.fn();

        ref.onAction().subscribe(actionSpy);
        ref.afterDismissed().subscribe(dismissedSpy);

        service.triggerAction(service.toasts()[0].id);
        vi.advanceTimersByTime(EXIT_ANIMATION_MS);

        expect(actionSpy).toHaveBeenCalledTimes(1);
        expect(dismissedSpy).toHaveBeenCalledWith({ dismissedByAction: true });
        expect(service.toasts()).toHaveLength(0);
    });

    it('should deduplicate identical toasts', () => {
        const firstRef = service.open('Duplicate', { appearance: 'positive', duration: DEFAULT_DURATION_MS });
        const secondRef = service.open('Duplicate', { appearance: 'positive', duration: DEFAULT_DURATION_MS });

        expect(service.toasts()).toHaveLength(1);
        expect(secondRef).toBe(firstRef);
    });
});

describe('FdUiToastService semantic helpers', () => {
    let service: FdUiToastService;

    beforeEach(() => {
        vi.useFakeTimers();
        service = setupToastService();
    });

    afterEach(() => {
        service.dismissAll();
        vi.runOnlyPendingTimers();
        vi.useRealTimers();
    });

    it('should create success toast with semantic defaults', () => {
        service.success('Saved');

        expect(service.toasts()[0]).toMatchObject({
            message: 'Saved',
            appearance: 'positive',
            duration: SUCCESS_DURATION_MS,
            politeness: 'polite',
        });
    });

    it('should create error toast with semantic defaults', () => {
        service.error('Failed');

        expect(service.toasts()[0]).toMatchObject({
            message: 'Failed',
            appearance: 'negative',
            duration: DEFAULT_DURATION_MS,
            politeness: 'assertive',
        });
    });

    it('should allow overriding semantic defaults', () => {
        service.info('Heads up', { duration: INFO_DURATION_MS, politeness: 'off' });

        expect(service.toasts()[0]).toMatchObject({
            message: 'Heads up',
            appearance: 'info',
            duration: INFO_DURATION_MS,
            politeness: 'off',
        });
    });
});
