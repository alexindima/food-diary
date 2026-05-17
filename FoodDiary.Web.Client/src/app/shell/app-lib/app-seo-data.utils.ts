import type { SeoData } from '../../services/seo.service';
import { isRecord } from '../../shared/lib/unknown-value.utils';

export function parseRouteSeoData(value: unknown): SeoData | null {
    if (!isRecord(value)) {
        return null;
    }

    return {
        ...(typeof value['titleKey'] === 'string' ? { titleKey: value['titleKey'] } : {}),
        ...(typeof value['descriptionKey'] === 'string' ? { descriptionKey: value['descriptionKey'] } : {}),
        ...(typeof value['path'] === 'string' ? { path: value['path'] } : {}),
        ...(typeof value['noIndex'] === 'boolean' ? { noIndex: value['noIndex'] } : {}),
        ...(typeof value['structuredDataBaseKey'] === 'string' ? { structuredDataBaseKey: value['structuredDataBaseKey'] } : {}),
        ...(Array.isArray(value['structuredDataFeatureKeys'])
            ? { structuredDataFeatureKeys: value['structuredDataFeatureKeys'].filter(item => typeof item === 'string') }
            : {}),
        ...(Array.isArray(value['structuredDataFaqKeys'])
            ? { structuredDataFaqKeys: value['structuredDataFaqKeys'].filter(item => typeof item === 'string') }
            : {}),
    };
}
