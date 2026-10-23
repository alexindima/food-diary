import { Service } from '@angular/core';
import { Subject } from 'rxjs';

@Service()
export class SessionEventsService {
    private readonly authenticatedSubject = new Subject<void>();
    private readonly sessionEndedSubject = new Subject<void>();
    private readonly localSessionInvalidatingSubject = new Subject<void>();
    private readonly sessionEndCleanups = new Set<() => Promise<void>>();
    private localInvalidation = false;
    private sessionEndNavigations = 0;

    public get isLocalSessionInvalidated(): boolean {
        return this.localInvalidation;
    }

    public get isSessionEndNavigation(): boolean {
        return this.sessionEndNavigations > 0;
    }

    public async runSessionEndNavigationAsync(navigate: () => Promise<unknown>): Promise<void> {
        this.sessionEndNavigations++;
        try {
            await navigate();
        } finally {
            this.sessionEndNavigations--;
        }
    }

    public readonly authenticated$ = this.authenticatedSubject.asObservable();
    public readonly sessionEnded$ = this.sessionEndedSubject.asObservable();
    public readonly localSessionInvalidating$ = this.localSessionInvalidatingSubject.asObservable();

    public notifyAuthenticated(): void {
        this.localInvalidation = false;
        this.authenticatedSubject.next();
    }

    public notifySessionEnded(): void {
        this.localInvalidation = false;
        this.sessionEndedSubject.next();
    }

    public notifyLocalSessionInvalidated(): void {
        this.localInvalidation = true;
        this.sessionEndedSubject.next();
    }

    public notifyLocalSessionInvalidating(): void {
        this.localInvalidation = true;
        this.localSessionInvalidatingSubject.next();
    }

    public registerSessionEndCleanup(cleanup: () => Promise<void>): () => void {
        this.sessionEndCleanups.add(cleanup);
        return () => {
            this.sessionEndCleanups.delete(cleanup);
        };
    }

    // eslint-disable-next-line local/async-function-suffix -- The nullable result preserves synchronous logout when no cleanup is registered.
    public prepareSessionEnd(): Promise<void> | null {
        if (this.sessionEndCleanups.size === 0) {
            return null;
        }

        const pending = [...this.sessionEndCleanups].map(async cleanup => {
            await cleanup();
        });
        return Promise.allSettled(pending).then(() => {});
    }
}
