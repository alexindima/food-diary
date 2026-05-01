import { AriaLivePoliteness } from '@angular/cdk/a11y';
import { computed, Injectable, signal } from '@angular/core';
import { Observable, Subject } from 'rxjs';

export type FdUiToastAppearance = 'default' | 'positive' | 'negative' | 'warning' | 'info';
export type FdUiToastHorizontalPosition = 'start' | 'center' | 'end';
export type FdUiToastVerticalPosition = 'top' | 'bottom';

export interface FdUiToastOptions {
    action?: string;
    appearance?: FdUiToastAppearance;
    duration?: number;
    horizontalPosition?: FdUiToastHorizontalPosition;
    verticalPosition?: FdUiToastVerticalPosition;
    politeness?: AriaLivePoliteness;
}

type FdUiToastSemanticKind = 'success' | 'error' | 'info';

export interface FdUiToastDismiss {
    dismissedByAction: boolean;
}

export interface FdUiToastInstance {
    id: string;
    message: string;
    action?: string;
    appearance: FdUiToastAppearance;
    duration: number;
    horizontalPosition: FdUiToastHorizontalPosition;
    verticalPosition: FdUiToastVerticalPosition;
    politeness: AriaLivePoliteness;
    leaving: boolean;
}

export class FdUiToastRef {
    private readonly afterDismissedSubject = new Subject<FdUiToastDismiss>();
    private readonly actionSubject = new Subject<void>();

    public constructor(private readonly dismissFn: () => void) {}

    public dismiss(): void {
        this.dismissFn();
    }

    public afterDismissed(): Observable<FdUiToastDismiss> {
        return this.afterDismissedSubject.asObservable();
    }

    public onAction(): Observable<void> {
        return this.actionSubject.asObservable();
    }

    public notifyAction(): void {
        if (!this.actionSubject.closed) {
            this.actionSubject.next();
            this.actionSubject.complete();
        }
    }

    public notifyDismiss(dismissedByAction: boolean): void {
        if (!this.actionSubject.closed) {
            this.actionSubject.complete();
        }

        if (!this.afterDismissedSubject.closed) {
            this.afterDismissedSubject.next({ dismissedByAction });
            this.afterDismissedSubject.complete();
        }
    }
}

@Injectable({
    providedIn: 'root',
})
export class FdUiToastService {
    private readonly enterAnimationMs = 250;
    private readonly exitAnimationMs = 200;
    private readonly toastState = signal<FdUiToastInstance[]>([]);
    private readonly refs = new Map<string, FdUiToastRef>();
    private readonly dismissTimers = new Map<string, ReturnType<typeof setTimeout>>();
    private readonly removeTimers = new Map<string, ReturnType<typeof setTimeout>>();

    public readonly toasts = computed(() => this.toastState());

    public open(message: string, options: FdUiToastOptions = {}): FdUiToastRef {
        const existingToast = this.findDuplicate(message, options);
        if (existingToast) {
            this.clearDismissTimer(existingToast.id);
            this.toastState.update(current =>
                current.map(item =>
                    item.id === existingToast.id
                        ? {
                              ...item,
                              action: options.action ?? item.action,
                              duration: options.duration ?? 5000,
                              politeness: options.politeness ?? item.politeness,
                          }
                        : item,
                ),
            );
            this.scheduleDismiss(existingToast.id, options.duration ?? 5000);
            return this.refs.get(existingToast.id)!;
        }

        const id = this.generateId();
        const toast: FdUiToastInstance = {
            id,
            message,
            action: options.action,
            appearance: options.appearance ?? 'default',
            duration: options.duration ?? 5000,
            horizontalPosition: options.horizontalPosition ?? 'center',
            verticalPosition: options.verticalPosition ?? 'bottom',
            politeness: options.politeness ?? 'polite',
            leaving: false,
        };

        const ref = new FdUiToastRef(() => this.dismiss(id));
        this.refs.set(id, ref);
        this.toastState.update(current => [...current, toast]);
        this.scheduleDismiss(id, toast.duration);
        return ref;
    }

    public success(message: string, options: FdUiToastOptions = {}): FdUiToastRef {
        return this.open(message, this.buildSemanticOptions('success', options));
    }

    public error(message: string, options: FdUiToastOptions = {}): FdUiToastRef {
        return this.open(message, this.buildSemanticOptions('error', options));
    }

    public info(message: string, options: FdUiToastOptions = {}): FdUiToastRef {
        return this.open(message, this.buildSemanticOptions('info', options));
    }

    public dismiss(id: string, dismissedByAction = false): void {
        const toast = this.toastState().find(item => item.id === id);
        if (!toast || toast.leaving) {
            return;
        }

        this.clearDismissTimer(id);
        this.toastState.update(current => current.map(item => (item.id === id ? { ...item, leaving: true } : item)));

        const removeTimer = setTimeout(() => {
            this.toastState.update(current => current.filter(item => item.id !== id));
            this.clearRemoveTimer(id);

            const ref = this.refs.get(id);
            ref?.notifyDismiss(dismissedByAction);
            this.refs.delete(id);
        }, this.exitAnimationMs);

        this.removeTimers.set(id, removeTimer);
    }

    public dismissAll(): void {
        this.toastState().forEach(toast => this.dismiss(toast.id));
    }

    public triggerAction(id: string): void {
        this.refs.get(id)?.notifyAction();
        this.dismiss(id, true);
    }

    private findDuplicate(message: string, options: FdUiToastOptions): FdUiToastInstance | undefined {
        const appearance = options.appearance ?? 'default';
        const horizontalPosition = options.horizontalPosition ?? 'center';
        const verticalPosition = options.verticalPosition ?? 'bottom';

        return this.toastState().find(
            toast =>
                !toast.leaving &&
                toast.message === message &&
                toast.appearance === appearance &&
                toast.action === options.action &&
                toast.horizontalPosition === horizontalPosition &&
                toast.verticalPosition === verticalPosition,
        );
    }

    private scheduleDismiss(id: string, duration: number): void {
        if (duration <= 0) {
            return;
        }

        const timer = setTimeout(() => this.dismiss(id), duration + this.enterAnimationMs);
        this.dismissTimers.set(id, timer);
    }

    private clearDismissTimer(id: string): void {
        const timer = this.dismissTimers.get(id);
        if (timer) {
            clearTimeout(timer);
            this.dismissTimers.delete(id);
        }
    }

    private clearRemoveTimer(id: string): void {
        const timer = this.removeTimers.get(id);
        if (timer) {
            clearTimeout(timer);
            this.removeTimers.delete(id);
        }
    }

    private generateId(): string {
        if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
            return crypto.randomUUID();
        }

        return `fd-ui-toast-${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
    }

    private buildSemanticOptions(kind: FdUiToastSemanticKind, options: FdUiToastOptions): FdUiToastOptions {
        switch (kind) {
            case 'success':
                return {
                    appearance: 'positive',
                    duration: 3500,
                    politeness: 'polite',
                    ...options,
                };
            case 'error':
                return {
                    appearance: 'negative',
                    duration: 5000,
                    politeness: 'assertive',
                    ...options,
                };
            case 'info':
                return {
                    appearance: 'info',
                    duration: 4000,
                    politeness: 'polite',
                    ...options,
                };
        }
    }
}
