import { HttpClient, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { effect, inject, Service } from '@angular/core';
import type { HubConnection } from '@microsoft/signalr';
import {
    catchError,
    defer,
    EMPTY,
    exhaustMap,
    filter,
    from,
    last,
    map,
    merge,
    type Observable,
    of,
    retry,
    Subject,
    switchMap,
    takeWhile,
    throwError,
    timer,
} from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
import { AI_TRIAL_QUOTA_EXCEEDED } from '../lib/ai-quota-error.utils';
import { buildRealtimeHubUrl } from '../lib/realtime-hub-url.utils';
import type { FoodVisionRequest, FoodVisionResponse } from '../models/ai.data';
import { type FoodRecognitionJob, RECOGNITION_PAGE_SIZE } from '../models/food-recognition.data';
import type { PageOf } from '../models/page-of.data';
import { entityId, type FoodRecognitionId, optionalEntityId, type UserId } from '../models/semantics/entity-id';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { recognitionJobFromSdk } from './sdk/ai-sdk.mapper';
import { AiSdk } from './sdk/generated/api/ai.service';
import { createSdkConnection } from './sdk/sdk-connection';
import { sdkPage } from './sdk/sdk-response';

const POLL_INTERVAL_MS = 5000;
const POST_RETRY_MS = 1000;
const HEX_RADIX = 16;

type RecognitionHubOwner = {
    readonly userId: UserId | null;
    connection: HubConnection | null;
    pending: Promise<void> | null;
};

function unauthorizedRecognition(): Observable<never> {
    return throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Unauthorized }));
}

@Service()
export class FoodRecognitionService {
    private readonly http = inject(HttpClient);
    private readonly auth = inject(AuthService);
    private readonly storage = inject(BrowserStorageService);
    private readonly baseUrl = `${environment.apiUrls.ai}/food/recognitions`;
    private readonly sdk = createSdkConnection(AiSdk, this.baseUrl, this.http);
    private readonly changes = new Subject<FoodRecognitionId | null>();
    private hubOwner: RecognitionHubOwner = { userId: null, connection: null, pending: null };

    public constructor() {
        effect(() => {
            this.synchronizeHubOwner();
        });
    }

    private currentUserId(): UserId | null {
        return optionalEntityId<'user'>(this.auth.getUserId());
    }

    public start(request: FoodVisionRequest): Observable<FoodVisionResponse> {
        return defer(() => {
            const user = this.currentUserId();
            if (user === null) {
                return unauthorizedRecognition();
            }
            return from(this.requestKeyAsync(user, request)).pipe(switchMap(key => this.submit(key, user, request)));
        });
    }

    private submit(key: string, user: UserId, request: FoodVisionRequest): Observable<FoodVisionResponse> {
        if (this.currentUserId() !== user) {
            return unauthorizedRecognition();
        }
        const id = entityId<'food-recognition'>(this.storage.getItem('session', key) ?? crypto.randomUUID());
        // Persist before POST. A lost acceptance response retries the same durable task.
        this.storage.setItem('session', key, id);
        return defer(() =>
            this.currentUserId() === user
                ? this.sdk.client.postAiFoodRecognitions({ version: this.sdk.version, startFoodRecognitionHttpRequest: { id, ...request } })
                : unauthorizedRecognition(),
        ).pipe(
            map(recognitionJobFromSdk),
            retry({ count: 2, delay: (error: unknown) => (this.isTransient(error) ? timer(POST_RETRY_MS) : throwError(() => error)) }),
            switchMap(job => this.waitForResult(job.id, user)),
            catchError((error: unknown) => {
                if (this.currentUserId() === user && !this.isTransient(error)) {
                    this.storage.removeItem('session', key);
                }
                return throwError(() => error);
            }),
        );
    }

    public resume(id: FoodRecognitionId): Observable<FoodVisionResponse> {
        return defer(() => {
            const user = this.currentUserId();
            return user === null ? unauthorizedRecognition() : this.waitForResult(id, user);
        });
    }

    public deleteRecognition(id: FoodRecognitionId): Observable<void> {
        return this.sdk.client.deleteAiFoodRecognitionsById({ version: this.sdk.version, id });
    }

    public list(page = 1, limit = RECOGNITION_PAGE_SIZE, isProductLabel = false): Observable<PageOf<FoodRecognitionJob>> {
        return this.sdk.client
            .getAiFoodRecognitionsPage({ version: this.sdk.version, page, limit, isProductLabel })
            .pipe(map(value => sdkPage(value, recognitionJobFromSdk)));
    }

    private waitForResult(id: FoodRecognitionId, user: UserId): Observable<FoodVisionResponse> {
        void this.connectAsync(user);
        return merge(timer(0, POLL_INTERVAL_MS), this.changes.pipe(filter(changed => changed === null || changed === id))).pipe(
            exhaustMap(() => {
                if (this.currentUserId() !== user) {
                    return unauthorizedRecognition();
                }
                return this.sdk.client.getAiFoodRecognitionsById({ version: this.sdk.version, id }).pipe(
                    map(recognitionJobFromSdk),
                    catchError((error: unknown) => (this.isTransient(error) ? EMPTY : throwError(() => error))),
                );
            }),
            takeWhile(job => job.status === 'Queued' || job.status === 'Running', true),
            last(),
            switchMap(job => from(this.releaseRequestAsync(user, job))),
            switchMap(job => this.toResult(job, user)),
        );
    }

    private toResult(job: FoodRecognitionJob, user: UserId): Observable<FoodVisionResponse> {
        if (this.currentUserId() !== user) {
            return unauthorizedRecognition();
        }
        if (job.vision === null) {
            const status =
                job.errorCode === 'Ai.QuotaExceeded' || job.errorCode === AI_TRIAL_QUOTA_EXCEEDED
                    ? HttpStatusCode.TooManyRequests
                    : job.errorCode === 'Ai.ConsentRequired' || job.errorCode === 'Ai.TrialEnded'
                      ? HttpStatusCode.Forbidden
                      : HttpStatusCode.BadGateway;
            return throwError(() => new HttpErrorResponse({ status, error: { code: job.errorCode, terminal: true } }));
        }
        return of({
            ...job.vision,
            recognition: { id: job.id, nutrition: job.nutrition, errorCode: job.nutritionErrorCode ?? job.errorCode },
        });
    }

    private synchronizeHubOwner(): RecognitionHubOwner {
        const userId = this.currentUserId();
        if (userId !== this.hubOwner.userId) {
            const old = this.hubOwner.connection;
            this.hubOwner = { userId, connection: null, pending: null };
            if (old !== null) {
                void old.stop();
            }
            this.changes.next(null);
        }
        return this.hubOwner;
    }

    private isCurrentHubOwner(owner: RecognitionHubOwner): boolean {
        return this.hubOwner === owner && this.currentUserId() === owner.userId;
    }

    private async connectAsync(user: UserId): Promise<void> {
        const owner = this.synchronizeHubOwner();
        if (owner.userId !== user || owner.connection !== null) {
            return;
        }
        owner.pending ??= this.openConnectionAsync(owner).finally(() => {
            owner.pending = null;
        });
        return owner.pending;
    }

    private async openConnectionAsync(owner: RecognitionHubOwner): Promise<void> {
        try {
            const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
            if (!this.isCurrentHubOwner(owner)) {
                return;
            }
            const connection = new HubConnectionBuilder()
                .withUrl(this.auth.sessionHubUrl(buildRealtimeHubUrl(environment.apiUrls.auth, '/hubs/food-recognition')), {
                    accessTokenFactory: () => this.auth.getToken() ?? '',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                })
                .withAutomaticReconnect()
                .configureLogging(LogLevel.None)
                .build();
            owner.connection = connection;
            connection.on('RecognitionChanged', (id: string) => {
                this.changes.next(entityId<'food-recognition'>(id));
            });
            connection.onreconnected(() => {
                this.changes.next(null);
            });
            connection.onclose(() => {
                owner.connection = null;
            });
            await connection.start();
            if (!this.isCurrentHubOwner(owner)) {
                await connection.stop();
                return;
            }
            this.changes.next(null);
        } catch {
            owner.connection = null;
            // HTTP polling remains active when SignalR cannot connect.
        }
    }

    private isTransient(error: unknown): boolean {
        if (error instanceof HttpErrorResponse) {
            const body: unknown = error.error;
            if (typeof body === 'object' && body !== null && 'terminal' in body && body.terminal === true) {
                return false;
            }
        }
        return (
            error instanceof HttpErrorResponse &&
            (error.status === 0 ||
                error.status === Number(HttpStatusCode.RequestTimeout) ||
                error.status >= Number(HttpStatusCode.InternalServerError))
        );
    }

    private async releaseRequestAsync(user: UserId, job: FoodRecognitionJob): Promise<FoodRecognitionJob> {
        const key = await this.requestKeyAsync(user, {
            imageAssetId: job.imageAssetId,
            description: job.description,
            isProductLabel: job.isProductLabel,
            additionalImageAssetIds: job.additionalImages?.map(image => image.imageAssetId),
        });
        if (this.storage.getItem('session', key) === job.id) {
            this.storage.removeItem('session', key);
        }
        return job;
    }

    private async requestKeyAsync(user: UserId, request: FoodVisionRequest): Promise<string> {
        const bytes = new TextEncoder().encode(
            JSON.stringify({
                imageAssetId: request.imageAssetId,
                description: request.description ?? null,
                ...(request.isProductLabel === true
                    ? { isProductLabel: true, additionalImageAssetIds: request.additionalImageAssetIds ?? [] }
                    : {}),
            }),
        );
        const hash = await crypto.subtle.digest('SHA-256', bytes);
        const fingerprint = Array.from(new Uint8Array(hash), value => value.toString(HEX_RADIX).padStart(2, '0')).join('');
        return `fd.recognition.${user}.${fingerprint}`;
    }
}
