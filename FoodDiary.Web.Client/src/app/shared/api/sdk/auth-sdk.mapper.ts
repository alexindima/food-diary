import type { AuthResponse } from '../../auth/auth.data';
import type { AuthenticationHttpResponse } from './generated/model/authentication-http-response';
import { requireSdkFields } from './sdk-response';
import { userFromSdk } from './user-sdk.mapper';

export function authResponseFromSdk(response: AuthenticationHttpResponse): AuthResponse {
    const value = requireSdkFields(response, ['accessToken', 'user']);
    return { accessToken: value.accessToken, user: userFromSdk(value.user) };
}
