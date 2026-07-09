import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { TranslateLoader, type TranslationObject } from '@ngx-translate/core';
import { catchError, type Observable, of } from 'rxjs';

type TranslationDictionary = TranslationObject;

@Service()
export class AdminTranslationLoader extends TranslateLoader {
    private readonly http = inject(HttpClient);

    public getTranslation(lang: string): Observable<TranslationDictionary> {
        const normalizedLang = lang.toLowerCase().startsWith('ru') ? 'ru' : 'en';

        return this.http.get<TranslationDictionary>(`./assets/i18n/${normalizedLang}/core.json`).pipe(catchError(() => of({})));
    }
}
