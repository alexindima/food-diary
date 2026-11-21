export const PrivacyConsentPurpose = {
    HealthTracking: 1,
    AiProcessing: 2,
    MarketingAttribution: 3,
} as const;

export type PrivacyConsentPurposeValue = (typeof PrivacyConsentPurpose)[keyof typeof PrivacyConsentPurpose];
