import type { Provider } from '@angular/core';
import { provideTranslateService, type RootTranslateServiceConfig } from '@ngx-translate/core';

export function provideTranslateTesting(config?: RootTranslateServiceConfig): Provider[] {
    return provideTranslateService(config);
}
