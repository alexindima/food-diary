import { type HttpContext, HttpHeaders } from '@angular/common/http';
import type { TranslateService } from '@ngx-translate/core';

import { sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';

export function productLanguageRequestOptions(translate: TranslateService, context?: HttpContext): { context: HttpContext } {
    return sdkRequestOptions(new HttpHeaders({ 'Accept-Language': resolveTranslateLanguage(translate) }), context);
}
