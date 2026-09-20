import { inject, type Signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { map } from 'rxjs';

import { resolveTranslateLanguage } from './translate-language.utils';

export function injectCurrentLanguage(): Signal<string> {
    const translate = inject(TranslateService);
    return toSignal(translate.onLangChange.pipe(map(event => event.lang)), {
        initialValue: resolveTranslateLanguage(translate),
    });
}
