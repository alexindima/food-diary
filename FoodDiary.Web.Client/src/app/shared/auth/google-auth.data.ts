import type { UserId } from '../models/semantics/entity-id';

export type GoogleLoginRequest = {
    credential: string;
    rememberMe?: boolean;
    expectedUserId?: UserId;
};
