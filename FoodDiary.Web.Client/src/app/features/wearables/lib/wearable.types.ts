import type { WearableConnection, WearableProvider } from '../models/wearable.data';

export type WearableProviderConfig = {
    id: WearableProvider;
    name: string;
    icon: string;
};

export type WearableProviderRow = WearableProviderConfig & {
    connection: WearableConnection | undefined;
};

export type WearableMetric = {
    key: string;
    labelKey: string;
    icon: string;
    value: number;
    unit: string;
    numberFormat: string;
};
