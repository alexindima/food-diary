import { InjectionToken } from '@angular/core';
import type { TranslationObject } from '@ngx-translate/core';

export type TranslationBundle = 'core' | 'landing' | 'seo' | 'privacy' | 'app';
type ServerTranslationBundles = Readonly<Record<string, Readonly<Partial<Record<TranslationBundle, TranslationObject>>>>>;

export const SERVER_TRANSLATIONS = new InjectionToken<ServerTranslationBundles>('SERVER_TRANSLATIONS');
