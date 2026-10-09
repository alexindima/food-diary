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

@Service()
export class FoodRecognitionService {
    private readonly http = inject(HttpClient);
    private readonly auth = inject(AuthService);
    private readonly storage = inject(BrowserStorageService);
    private readonly baseUrl = `${environment.apiUrls.ai}/food/recognitions`;
    private readonly sdk = createSdkConnection(AiSdk, this.baseUrl, this.http);
    private readonly changes = new Subject<FoodRecognitionId | null>();
    private connection: HubConnection | null = null;
    private connecting: Promise<void> | null = null;
    private sessionUser: UserId | null = null;

    public constructor() {
        effect(() => {
            const user = this.currentUserId();
            if (user !== this.sessionUser) {
                this.sessionUser = user;
                const old = this.connection;
                this.connection = null;
                if (old !== null) {
                    void old.stop();
                }
                this.changes.next(null);
            }
        });
    }

    private currentUserId(): UserId | null {
        return optionalEntityId<'user'>(this.auth.getUserId());
    }

    public start(request: FoodVisionRequest): Observable<FoodVisionResponse> {
        return defer(() => {
            const user = this.currentUserId();
            if (user === null) {
                return throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Unauthorized }));
            }
            return from(this.requestKeyAsync(user, request)).pipe(switchMap(key => this.submit(key, user, request)));
        });
    }

    private submit(key: string, user: UserId, request: FoodVisionRequest): Observable<FoodVisionResponse> {
        const id = entityId<'food-recognition'>(this.storage.getItem('session', key) ?? crypto.randomUUID());
        // Persist before POST. A lost acceptance response retries the same durable task.
        this.storage.setItem('session', key, id);
        return this.sdk.client
            .postAiFoodRecognitions({ version: this.sdk.version, startFoodRecognitionHttpRequest: { id, ...request } })
            .pipe(
                map(recognitionJobFromSdk),
                retry({ count: 2, delay: (error: unknown) => (this.isTransient(error) ? timer(POST_RETRY_MS) : throwError(() => error)) }),
                switchMap(job => this.waitForResult(job.id, user)),
                catchError((error: unknown) => {
                    if (!this.isTransient(error)) {
                        this.storage.removeItem('session', key);
                    }
                    return throwError(() => error);
                }),
            );
    }

    public resume(id: FoodRecognitionId): Observable<FoodVisionResponse> {
        return defer(() => {
            const user = this.currentUserId();
            return user === null
                ? throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Unauthorized }))
                : this.waitForResult(id, user);
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
                    return throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Unauthorized }));
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
            return throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Unauthorized }));
        }
        if (job.vision === null) {
            const status =
                job.errorCode === 'Ai.QuotaExceeded'
                    ? HttpStatusCode.TooManyRequests
                    : job.errorCode === 'Ai.ConsentRequired'
                      ? HttpStatusCode.Forbidden
                      : HttpStatusCode.BadGateway;
            return throwError(() => new HttpErrorResponse({ status, error: { code: job.errorCode, terminal: true } }));
        }
        return of({
            ...job.vision,
            recognition: { id: job.id, nutrition: job.nutrition, errorCode: job.nutritionErrorCode ?? job.errorCode },
        });
    }

    private async connectAsync(user: UserId): Promise<void> {
        if (this.connection !== null) {
            return;
        }
        if (this.connecting !== null) {
            return this.connecting;
        }
        this.connecting = this.openConnectionAsync(user).finally(() => {
            this.connecting = null;
        });
        return this.connecting;
    }

    private async openConnectionAsync(user: UserId): Promise<void> {
        try {
            const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
            if (this.currentUserId() !== user) {
                return;
            }
            const connection = new HubConnectionBuilder()
                .withUrl(buildRealtimeHubUrl(environment.apiUrls.auth, '/hubs/food-recognition'), {
                    accessTokenFactory: () => this.auth.getToken() ?? '',
                })
                .withAutomaticReconnect()
                .configureLogging(LogLevel.None)
                .build();
            this.connection = connection;
            connection.on('RecognitionChanged', (id: string) => {
                this.changes.next(entityId<'food-recognition'>(id));
            });
            connection.onreconnected(() => {
                this.changes.next(null);
            });
            connection.onclose(() => {
                if (this.connection === connection) {
                    this.connection = null;
                }
            });
            await connection.start();
            if (this.currentUserId() !== user) {
                await connection.stop();
                return;
            }
            this.changes.next(null);
        } catch {
            this.connection = null;
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
