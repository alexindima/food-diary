import { ErrorCode } from 'src/dto/api-response.dto';

export class InvalidIdFormatException extends Error {
    errorCode: ErrorCode;

    constructor() {
        super('Invalid ID format');
        this.errorCode = ErrorCode.INVALID_ID_FORMAT;
    }
}
