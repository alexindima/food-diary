import { TestBed } from '@angular/core/testing';
import { FdUiToastService } from './fd-ui-toast.service';

describe('FdUiToastService', () => {
    let service: FdUiToastService;

    beforeEach(() => {
        vi.useFakeTimers();

        TestBed.configureTestingModule({
            providers: [FdUiToastService],
        });

        service = TestBed.inject(FdUiToastService);
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

    it('should apply configured appearance', () => {
        service.open('Error occurred', { appearance: 'negative' });

        expect(service.toasts()[0]?.appearance).toBe('negative');
    });

    it('should use default appearance when not provided', () => {
        service.open('Default message');

        expect(service.toasts()[0]?.appearance).toBe('default');
    });

    it('should use default duration of 5000', () => {
        service.open('Message');

        expect(service.toasts()[0]?.duration).toBe(5000);
    });

    it('should use custom duration', () => {
        service.open('Message', { duration: 8000 });

        expect(service.toasts()[0]?.duration).toBe(8000);
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

    it('should dismiss a toast after duration and exit animation', () => {
        service.open('Message', { duration: 1000 });

        vi.advanceTimersByTime(1000);
        expect(service.toasts()[0]?.leaving).toBe(false);

        vi.advanceTimersByTime(250);
        expect(service.toasts()[0]?.leaving).toBe(true);

        vi.advanceTimersByTime(200);
        expect(service.toasts()).toHaveLength(0);
    });

    it('should notify action and dismissal when action is triggered', () => {
        const ref = service.open('Message', { action: 'Undo', duration: 0 });
        const actionSpy = vi.fn();
        const dismissedSpy = vi.fn();

        ref.onAction().subscribe(actionSpy);
        ref.afterDismissed().subscribe(dismissedSpy);

        service.triggerAction(service.toasts()[0]!.id);
        vi.advanceTimersByTime(200);

        expect(actionSpy).toHaveBeenCalledTimes(1);
        expect(dismissedSpy).toHaveBeenCalledWith({ dismissedByAction: true });
        expect(service.toasts()).toHaveLength(0);
    });

    it('should deduplicate identical toasts', () => {
        const firstRef = service.open('Duplicate', { appearance: 'positive', duration: 5000 });
        const secondRef = service.open('Duplicate', { appearance: 'positive', duration: 5000 });

        expect(service.toasts()).toHaveLength(1);
        expect(secondRef).toBe(firstRef);
    });

    it('should create success toast with semantic defaults', () => {
        service.success('Saved');

        expect(service.toasts()[0]).toMatchObject({
            message: 'Saved',
            appearance: 'positive',
            duration: 3500,
            politeness: 'polite',
        });
    });

    it('should create error toast with semantic defaults', () => {
        service.error('Failed');

        expect(service.toasts()[0]).toMatchObject({
            message: 'Failed',
            appearance: 'negative',
            duration: 5000,
            politeness: 'assertive',
        });
    });

    it('should allow overriding semantic defaults', () => {
        service.info('Heads up', { duration: 1500, politeness: 'off' });

        expect(service.toasts()[0]).toMatchObject({
            message: 'Heads up',
            appearance: 'info',
            duration: 1500,
            politeness: 'off',
        });
    });
});
