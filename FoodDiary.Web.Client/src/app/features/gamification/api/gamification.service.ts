import { HttpHeaders } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import { createDefaultGamificationData } from '../lib/gamification.constants';
import type { GamificationData } from '../models/gamification.data';

@Service()
export class GamificationService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.gamification;
    private readonly translateService = inject(TranslateService);

    public getData(): Observable<GamificationData> {
        const headers = new HttpHeaders({ 'Accept-Language': resolveTranslateLanguage(this.translateService) });
        return super
            .get<GamificationData>('', undefined, headers)
            .pipe(catchError((error: unknown) => fallbackApiError('Get gamification error', error, createDefaultGamificationData())));
    }
}
