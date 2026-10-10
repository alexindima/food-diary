export class OpenApiFixtures {
    constructor(document?: unknown);
    operation(method: string, url: string): { operationId: string; path: string };
    assertSchema<T>(schemaName: string, body: T): T;
    assertResponse(method: string, url: string, status: number, body: unknown): void;
}
export function fixtureUuid(label: string): string;
export function jsonFixture(
    contract: OpenApiFixtures,
    method: string,
    url: string,
    body: unknown,
    status?: number,
): { status: number; contentType: string; body: string };
export function invalidJsonFixture(body: unknown, reason: string, status?: number): { status: number; contentType: string; body: string };
