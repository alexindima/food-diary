import { computed, inject, Injectable, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { finalize, map } from 'rxjs';

import type { RefreshTokenSessionId } from '../../../shared/models/semantics/entity-id';
import { ActiveSessionsService } from '../api/active-sessions.service';
import type { ActiveSession, SessionRevocation } from '../models/active-session.model';

@Injectable()
export class ActiveSessionsFacade {
    private readonly api = inject(ActiveSessionsService);
    private readonly loadVersion = signal<number | undefined>(undefined);
    private readonly sessionList = signal<ActiveSession[]>([]);
    private readonly sessionsResource = rxResource({
        params: this.loadVersion,
        stream: () =>
            this.api.getAll().pipe(
                map(sessions => {
                    this.sessionList.set(sessions);
                }),
            ),
    });
    private readonly revokeFailed = signal(false);
    public readonly sessions = this.sessionList.asReadonly();
    public readonly isLoading = this.sessionsResource.isLoading;
    public readonly revocation = signal<SessionRevocation>({ kind: 'idle' });
    public readonly error = computed<'load' | 'revoke' | null>(() => {
        if (this.revokeFailed()) {
            return 'revoke';
        }
        return this.sessionsResource.error() === undefined ? null : 'load';
    });
    public readonly isBusy = computed(() => this.isLoading() || this.revocation().kind !== 'idle');
    public readonly isRevokingOthers = computed(() => this.revocation().kind === 'others');

    public isRevoking(sessionId: RefreshTokenSessionId): boolean {
        const state = this.revocation();
        return state.kind === 'single' && state.sessionId === sessionId;
    }

    public load(): void {
        if (this.isBusy()) {
            return;
        }
        this.revokeFailed.set(false);
        this.loadVersion.update(version => (version ?? 0) + 1);
    }

    public revoke(sessionId: RefreshTokenSessionId): void {
        if (this.isBusy()) {
            return;
        }
        this.startRevocation({ kind: 'single', sessionId });
        this.api
            .revoke(sessionId)
            .pipe(
                finalize(() => {
                    this.revocation.set({ kind: 'idle' });
                }),
            )
            .subscribe({
                next: () => {
                    this.sessionList.update(items => items.filter(item => item.id !== sessionId));
                },
                error: () => {
                    this.revokeFailed.set(true);
                },
            });
    }

    public revokeOthers(): void {
        if (this.isBusy()) {
            return;
        }
        this.startRevocation({ kind: 'others' });
        this.api
            .revokeOthers()
            .pipe(
                finalize(() => {
                    this.revocation.set({ kind: 'idle' });
                }),
            )
            .subscribe({
                next: () => {
                    this.sessionList.update(items => items.filter(item => item.isCurrent));
                },
                error: () => {
                    this.revokeFailed.set(true);
                },
            });
    }

    private startRevocation(revocation: Exclude<SessionRevocation, { kind: 'idle' }>): void {
        this.revokeFailed.set(false);
        if (this.sessionsResource.error() !== undefined) {
            this.sessionsResource.set(undefined);
        }
        this.revocation.set(revocation);
    }
}
