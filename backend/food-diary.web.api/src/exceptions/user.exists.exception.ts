import { ErrorCode } from '../dto/api-response.dto';

export class UserExistsException extends Error {
    errorCode: ErrorCode;

    constructor() {
        super('User already exists');
        this.errorCode = ErrorCode.USER_EXISTS;
    }
}
