export const isRecord = (value: unknown): value is Record<string, unknown> =>
    typeof value === 'object' && value !== null && !Array.isArray(value);

export const getRecordProperty = (value: unknown, property: string): unknown => (isRecord(value) ? value[property] : undefined);

export const getStringProperty = (value: unknown, property: string): string | undefined => {
    const propertyValue = getRecordProperty(value, property);
    return typeof propertyValue === 'string' ? propertyValue : undefined;
};

export const getNumberProperty = (value: unknown, property: string): number | undefined => {
    const propertyValue = getRecordProperty(value, property);
    return typeof propertyValue === 'number' ? propertyValue : undefined;
};
