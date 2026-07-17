import { computed, type Signal, signal } from '@angular/core';

export type RequestState<T, TError = string> =
    | { status: 'idle'; data: T | null; error: null }
    | { status: 'loading'; data: T | null; error: null }
    | { status: 'success'; data: T; error: null }
    | { status: 'error'; data: T | null; error: TError };

export type BeginRequestOptions = {
    showLoading?: boolean;
};

export class RequestStateController<T, TError = string> {
    private readonly stateValue = signal<RequestState<T, TError>>({ status: 'idle', data: null, error: null });
    private requestVersion = 0;

    public readonly state = this.stateValue.asReadonly();
    public readonly data: Signal<T | null> = computed(() => this.stateValue().data);
    public readonly error: Signal<TError | null> = computed(() => this.stateValue().error);
    public readonly isLoading = computed(() => this.stateValue().status === 'loading');
    public readonly hasData = computed(() => this.stateValue().data !== null);

    public begin(options: BeginRequestOptions = {}): number {
        const requestId = ++this.requestVersion;
        if (options.showLoading !== false) {
            this.stateValue.set({ status: 'loading', data: this.stateValue().data, error: null });
        }
        return requestId;
    }

    public succeed(requestId: number, data: T): boolean {
        if (!this.isCurrent(requestId)) {
            return false;
        }
        this.stateValue.set({ status: 'success', data, error: null });
        return true;
    }

    public fail(requestId: number, error: TError, options: { preserveData?: boolean } = {}): boolean {
        if (!this.isCurrent(requestId)) {
            return false;
        }
        const data = options.preserveData === false ? null : this.stateValue().data;
        this.stateValue.set({ status: 'error', data, error });
        return true;
    }

    public reset(): void {
        this.requestVersion += 1;
        this.stateValue.set({ status: 'idle', data: null, error: null });
    }

    public isCurrent(requestId: number): boolean {
        return requestId === this.requestVersion;
    }
}
