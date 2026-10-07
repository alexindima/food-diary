import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { HydrationSdk } from '../../../shared/api/sdk/generated/api/hydration.service';
import type { HydrationEntryHttpResponse } from '../../../shared/api/sdk/generated/model/hydration-entry-http-response';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { CreateHydrationEntryPayload, HydrationDaily, HydrationEntry } from '../../../shared/models/hydration.data';

@Service()
export class HydrationService {
    protected readonly baseUrl = environment.apiUrls.hydration;
    private readonly sdk = createSdkConnection(HydrationSdk, this.baseUrl, inject(HttpClient));

    public getDaily(dateUtc: Date): Observable<HydrationDaily> {
        const date = this.toCalendarDate(dateUtc);
        return this.sdk.client.getHydrationDaily({ version: this.sdk.version, dateUtc: date }).pipe(
            map(response => ({ ...requireSdkFields(response, ['dateUtc', 'totalMl']), goalMl: response.goalMl ?? null })),
            catchError((error: unknown) =>
                fallbackApiError('Hydration daily fetch error', error, {
                    dateUtc: date,
                    totalMl: 0,
                    goalMl: null,
                }),
            ),
        );
    }

    public getEntries(dateUtc: Date): Observable<HydrationEntry[]> {
        return this.sdk.client.getHydration({ version: this.sdk.version, dateUtc: this.toCalendarDate(dateUtc) }).pipe(
            map(entries => entries.map(hydrationEntryFromSdk)),
            catchError((error: unknown) => fallbackApiError('Hydration entries fetch error', error, [])),
        );
    }

    public addEntry(amountMl: number, timestampUtc: Date = new Date()): Observable<HydrationEntry> {
        const payload: CreateHydrationEntryPayload = {
            amountMl,
            timestampUtc: timestampUtc.toISOString(),
        };

        return this.sdk.client
            .postHydration({ version: this.sdk.version, idempotencyKey: crypto.randomUUID(), createHydrationEntryHttpRequest: payload })
            .pipe(
                map(hydrationEntryFromSdk),
                catchError((error: unknown) => rethrowApiError('Create hydration entry error', error)),
            );
    }

    private toCalendarDate(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}

function hydrationEntryFromSdk(response: HydrationEntryHttpResponse): HydrationEntry {
    return requireSdkFields(response, ['id', 'timestampUtc', 'amountMl']);
}
