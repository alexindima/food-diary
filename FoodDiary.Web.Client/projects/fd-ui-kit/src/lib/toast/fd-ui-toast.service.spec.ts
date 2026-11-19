import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, onTestFinished, vi } from 'vitest';

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

    it('ignores actions on a dismissed toast during its exit animation', () => {
        const ref = service.open('Added 250 ml', { action: 'Undo', duration: 0 });
        const action = vi.fn();
        ref.onAction().subscribe(action);
        const id = service.toasts()[0].id;
        ref.dismiss();
        service.triggerAction(id);
        expect(action).not.toHaveBeenCalled();
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

describe('FdUiToastService feedback refresh and fallbacks', () => {
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

    it('keeps unrelated toasts and uses the default timer when refreshing one without a duration', () => {
        const status = service.open('Pending', { key: 'upload', duration: 0 });
        const other = service.open('Background task', { duration: 0 });
        const dismissed = vi.fn();
        const otherDismissed = vi.fn();
        status.afterDismissed().subscribe(dismissed);
        other.afterDismissed().subscribe(otherDismissed);
        const originalOther = service.toasts()[1];

        const updated = service.open('Completed', { key: 'upload' });
        expect(updated).toBe(status);
        expect(service.toasts()[0]?.message).toBe('Completed');
        expect(service.toasts()[1]).toBe(originalOther);
        vi.advanceTimersByTime(DEFAULT_DURATION_MS);
        expect(service.toasts()[0]?.leaving).toBe(false);
        vi.advanceTimersByTime(EXIT_START_DELAY_MS + EXIT_ANIMATION_MS);

        expect(dismissed).toHaveBeenCalledExactlyOnceWith({ dismissedByAction: false });
        expect(otherDismissed).not.toHaveBeenCalled();
        expect(service.toasts()).toEqual([originalOther]);
    });

    it('restarts the dismissal timer when a matching toast is refreshed just before its original deadline', () => {
        const original = service.open('Saving', { key: 'save', duration: SHORT_DURATION_MS });
        const dismissed = vi.fn();
        const complete = vi.fn();
        original.afterDismissed().subscribe({ next: dismissed, complete });
        vi.advanceTimersByTime(SHORT_DURATION_MS);

        expect(service.open('Saved', { key: 'save', duration: SHORT_DURATION_MS })).toBe(original);
        vi.advanceTimersByTime(EXIT_START_DELAY_MS + EXIT_ANIMATION_MS);
        expect(service.toasts()[0]).toMatchObject({ message: 'Saved', leaving: false });
        expect(dismissed).not.toHaveBeenCalled();
        expect(complete).not.toHaveBeenCalled();

        vi.advanceTimersByTime(SHORT_DURATION_MS);
        expect(service.toasts()).toEqual([]);
        expect(dismissed).toHaveBeenCalledExactlyOnceWith({ dismissedByAction: false });
        expect(complete).toHaveBeenCalledOnce();
    });

    it.each([undefined, {}])('keeps notifications dismissible without crypto.randomUUID (%j)', crypto => {
        vi.stubGlobal('crypto', crypto);
        onTestFinished(() => {
            vi.unstubAllGlobals();
        });
        const fallbackRandom = 0.5;
        const random = vi.spyOn(Math, 'random').mockReturnValue(fallbackRandom);
        onTestFinished(() => {
            random.mockRestore();
        });
        const ref = service.open('Ready', { duration: 0 });
        const dismissed = vi.fn();
        ref.afterDismissed().subscribe(dismissed);
        expect(service.toasts()[0]?.id).toMatch(/^fd-ui-toast-\d+-[a-z0-9]+$/);

        ref.dismiss();
        vi.advanceTimersByTime(EXIT_ANIMATION_MS);
        expect(service.toasts()).toEqual([]);
        expect(dismissed).toHaveBeenCalledExactlyOnceWith({ dismissedByAction: false });
    });
});

describe('FdUiToastService synchronous feedback observers', () => {
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

    it('leaves replacement feedback open when an action synchronously publishes another toast for the same key', () => {
        const key = 'delete-draft';
        const reference = service.open('Draft deleted', { key, duration: 0, action: 'Undo' });
        const dismissed = vi.fn();
        const callback = vi.fn(() => service.open('Draft restored', { key, duration: 0 }));
        reference.onAction().subscribe(callback);
        reference.afterDismissed().subscribe(dismissed);

        service.triggerAction(service.toasts()[0].id);
        expect(callback).toHaveBeenCalledOnce();
        expect(callback.mock.results[0].value).not.toBe(reference);
        expect(service.toasts()).toMatchObject([
            { message: 'Draft deleted', leaving: true },
            { message: 'Draft restored', leaving: false },
        ]);

        vi.advanceTimersByTime(EXIT_ANIMATION_MS);
        expect(service.toasts()).toMatchObject([{ message: 'Draft restored', leaving: false }]);
        expect(dismissed).toHaveBeenCalledExactlyOnceWith({ dismissedByAction: true });
    });

    it('lets the dismissal observer open new feedback after the old entry has been removed', () => {
        const key = 'upload';
        const original = service.open('Upload canceled', { key, duration: 0 });
        const reopened = vi.fn(() => service.open('Upload restarted', { key, duration: 0 }));
        original.afterDismissed().subscribe(reopened);

        original.dismiss();
        vi.advanceTimersByTime(EXIT_ANIMATION_MS);
        expect(reopened).toHaveBeenCalledExactlyOnceWith({ dismissedByAction: false });
        expect(service.toasts()).toMatchObject([{ message: 'Upload restarted', leaving: false }]);
        expect(service.open('Upload complete', { key, duration: 0 })).toBe(reopened.mock.results[0].value);
    });
});
