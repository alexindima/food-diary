import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { GamificationSdk } from '../../../shared/api/sdk/generated/api/gamification.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { GamificationData } from '../models/gamification.data';

@Service()
export class GamificationService {
    protected readonly baseUrl = environment.apiUrls.gamification;
    private readonly translateService = inject(TranslateService);
    private readonly sdk = createSdkConnection(GamificationSdk, this.baseUrl, inject(HttpClient));

    public getData(): Observable<GamificationData> {
        const headers = new HttpHeaders({ 'Accept-Language': resolveTranslateLanguage(this.translateService) });
        return this.sdk.client.getGamification({ version: this.sdk.version }, 'body', false, sdkRequestOptions(headers)).pipe(
            map(response => {
                const value = requireSdkFields(response, [
                    'currentStreak',
                    'longestStreak',
                    'totalMealsLogged',
                    'healthScore',
                    'weeklyAdherence',
                    'badges',
                ]);
                return {
                    ...value,
                    badges: value.badges.map(badgeResponse => {
                        const badge = requireSdkFields(badgeResponse, ['key', 'category', 'threshold', 'isEarned']);
                        return {
                            ...badge,
                            title: badge.title ?? undefined,
                            description: badge.description ?? undefined,
                            icon: badge.icon ?? undefined,
                        };
                    }),
                };
            }),
            catchError((error: unknown) => rethrowApiError('Get gamification error', error)),
        );
    }
}
