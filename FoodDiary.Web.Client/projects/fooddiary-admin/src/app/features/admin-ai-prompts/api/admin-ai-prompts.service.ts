import { DOCUMENT } from '@angular/common';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { map, type Observable, switchMap } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { loadPagedCollection } from '../../../shared/api/load-paged-collection';
import { AdminAiPromptsSdk } from '../../../shared/api/sdk/generated/api/admin-ai-prompts.service';
import { AdminImagesSdk } from '../../../shared/api/sdk/generated/api/admin-images.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import type { AdminAiPrompt } from '../models/admin-ai-prompt';
import type { AdminAiPromptDraft, AdminAiPromptScenario } from '../models/admin-ai-prompt-scenario';
import { adminAiPromptFromSdk, adminAiPromptScenarioFromSdk } from './admin-ai-prompts-sdk.mapper';

@Service()
export class AdminAiPromptsService {
    private readonly http = inject(HttpClient);
    private readonly uploadHttp = new HttpClient(inject(HttpBackend));
    private readonly document = inject(DOCUMENT);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/ai-prompts`;
    private readonly sdk = createSdkConnection(AdminAiPromptsSdk, this.url, this.http);
    private readonly images = createSdkConnection(AdminImagesSdk, this.url, this.http);
    public getScenarios(): Observable<AdminAiPromptScenario[]> {
        return this.sdk.client
            .getAdminAiPromptsScenarios({ version: this.sdk.version })
            .pipe(map(items => items.map(adminAiPromptScenarioFromSdk)));
    }
    public preview(draft: AdminAiPromptDraft): Observable<{ text: string }> {
        return this.sdk.client
            .postAdminAiPromptsPreview({ version: this.sdk.version, adminAiPromptDraftHttpRequest: draft })
            .pipe(map(value => requireSdkFields(value, ['text'])));
    }
    public test(draft: AdminAiPromptDraft): Observable<{ text: string }> {
        const key = this.document.defaultView?.crypto.randomUUID();
        if (key === undefined) {
            throw new Error('A secure browser context is required.');
        }
        return this.sdk.client
            .postAdminAiPromptsTest({ version: this.sdk.version, idempotencyKey: key, adminAiPromptDraftHttpRequest: draft })
            .pipe(map(value => requireSdkFields(value, ['text'])));
    }
    public uploadImage(file: File): Observable<{ assetId: string }> {
        return this.images.client
            .postImagesUploadUrl({
                version: this.images.version,
                getImageUploadUrlHttpRequest: {
                    fileName: file.name,
                    contentType: file.type,
                    fileSizeBytes: file.size,
                },
            })
            .pipe(
                map(value => requireSdkFields(value, ['uploadUrl', 'assetId'])),
                switchMap(upload =>
                    this.uploadHttp
                        .put(upload.uploadUrl, file, {
                            headers: { 'Content-Type': file.type },
                            responseType: 'text',
                        })
                        .pipe(
                            switchMap(() =>
                                this.images.client
                                    .postImagesByAssetIdConfirm({ version: this.images.version, assetId: upload.assetId })
                                    .pipe(map(value => requireSdkFields(value, ['assetId']))),
                            ),
                        ),
                ),
            );
    }
    public getAll(): Observable<AdminAiPrompt[]> {
        return loadPagedCollection((page, limit) =>
            this.sdk.client
                .getAdminAiPrompts({ version: this.sdk.version, page, limit })
                .pipe(map(items => items.map(adminAiPromptFromSdk))),
        );
    }
    public save(key: string, locale: string, promptText: string, isActive: boolean): Observable<AdminAiPrompt> {
        return this.sdk.client
            .putAdminAiPromptsByKeyByLocale({
                version: this.sdk.version,
                key,
                locale,
                adminAiPromptUpsertHttpRequest: {
                    promptText,
                    isActive,
                },
            })
            .pipe(map(adminAiPromptFromSdk));
    }
}
