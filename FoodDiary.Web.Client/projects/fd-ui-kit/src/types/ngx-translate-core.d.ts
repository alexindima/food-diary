import type { InterpolationParameters } from '@ngx-translate/core';

declare module '@ngx-translate/core' {
    interface TranslateService {
        instant(key: string, interpolateParams?: InterpolationParameters): string;
    }
}
