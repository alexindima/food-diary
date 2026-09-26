import { InjectionToken } from '@angular/core';
import type { TranslationObject } from '@ngx-translate/core';

export const SERVER_TRANSLATIONS = new InjectionToken<Readonly<Record<string, TranslationObject>>>('SERVER_TRANSLATIONS');
