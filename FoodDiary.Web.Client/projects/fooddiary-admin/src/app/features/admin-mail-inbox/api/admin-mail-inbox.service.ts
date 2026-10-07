import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminMailInboxSdk } from '../../../shared/api/sdk/generated/api/admin-mail-inbox.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import type {
    AdminMailInboxFilters,
    AdminMailInboxMessageDetails,
    AdminMailInboxMessagePage,
    AdminMailInboxMessageSummary,
} from '../models/admin-mail-inbox.data';
import {
    adminMailInboxMessageDetailsFromSdk,
    adminMailInboxMessagePageFromSdk,
    adminMailInboxMessageSummaryFromSdk,
} from './admin-mail-inbox-sdk.mapper';

@Service()
export class AdminMailInboxService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/mail-inbox/messages`;
    private readonly sdk = createSdkConnection(AdminMailInboxSdk, this.baseUrl, this.http);

    public getMessagePage(page: number, limit: number, filters: AdminMailInboxFilters = {}): Observable<AdminMailInboxMessagePage> {
        const { recipient = '', category = '', unread } = filters;
        let params = new HttpParams().set('page', page).set('limit', limit);
        if (recipient.trim().length > 0) {
            params = params.set('recipient', recipient.trim());
        }
        if (category.length > 0) {
            params = params.set('category', category);
        }
        if (unread !== undefined) {
            params = params.set('unread', unread);
        }
        for (const key of ['fromUtc', 'toUtc', 'search', 'fromAddress', 'id'] as const) {
            const value = filters[key];
            if (value !== undefined && value.length > 0) {
                params = params.set(key, value);
            }
        }
        return this.sdk.client
            .getAdminMailInboxMessagesPage({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, params))
            .pipe(map(adminMailInboxMessagePageFromSdk));
    }

    public getMessages(limit: number, recipient = '', category = '', unread?: boolean): Observable<AdminMailInboxMessageSummary[]> {
        let params = new HttpParams().set('limit', limit);
        if (recipient.trim().length > 0) {
            params = params.set('recipient', recipient.trim());
        }
        if (category.length > 0) {
            params = params.set('category', category);
        }
        if (unread !== undefined) {
            params = params.set('unread', unread);
        }
        return this.sdk.client
            .getAdminMailInboxMessages({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, params))
            .pipe(map(items => items.map(adminMailInboxMessageSummaryFromSdk)));
    }

    public getMessage(id: string): Observable<AdminMailInboxMessageDetails> {
        return this.sdk.client
            .getAdminMailInboxMessagesById({ version: this.sdk.version, id })
            .pipe(map(adminMailInboxMessageDetailsFromSdk));
    }

    public markMessageRead(id: string): Observable<void> {
        return this.sdk.client.postAdminMailInboxMessagesByIdRead({ version: this.sdk.version, id });
    }
}
