import type { AuthResponse } from '../../auth/auth.data';
import type { AuthenticationHttpResponse } from './generated/model/authentication-http-response';
import { requireSdkFields } from './sdk-response';
import { userFromSdk } from './user-sdk.mapper';

export function authResponseFromSdk(response: AuthenticationHttpResponse): AuthResponse {
    const value = requireSdkFields(response, ['user']);
    const accessToken = nonEmptyString(value.accessToken);
    const sessionId = nonEmptyString(value.sessionId);
    const impersonationContext = nonEmptyString(value.impersonationContext);
    if (
        accessToken === undefined &&
        (!Array.isArray(value.roles) || (sessionId === undefined && impersonationContext === undefined))
    ) {
        throw new Error('Missing browser session metadata');
    }
    return {
        accessToken,
        user: userFromSdk(value.user),
        roles: value.roles ?? [],
        sessionId,
        impersonationContext,
        impersonationReason: value.impersonationReason,
        rememberMe: value.rememberMe,
    };
}

function nonEmptyString(value: unknown): string | undefined {
    return typeof value === 'string' && value.length > 0 ? value : undefined;
}
