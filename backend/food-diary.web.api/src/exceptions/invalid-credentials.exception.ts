import { ErrorCode } from '../dto/api-response.dto';

export class InvalidCredentialsException extends Error {
    errorCode: ErrorCode;

    constructor() {
        super('Invalid email or password');
        this.errorCode = ErrorCode.INVALID_CREDENTIALS;
    }
}
