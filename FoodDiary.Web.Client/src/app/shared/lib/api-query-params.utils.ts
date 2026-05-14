export type ApiQueryParams = Record<string, string | number>;

export function addOptionalStringParam(params: ApiQueryParams, key: string, value: string | undefined): void {
    if (value !== undefined && value.length > 0) {
        params[key] = value;
    }
}

export function addOptionalNumberParam(params: ApiQueryParams, key: string, value: number | undefined): void {
    if (value !== undefined) {
        params[key] = value;
    }
}
