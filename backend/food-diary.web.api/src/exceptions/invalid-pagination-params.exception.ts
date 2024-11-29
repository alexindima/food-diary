import { ErrorCode } from 'src/dto/api-response.dto';

export class InvalidPaginationParamsException extends Error {
    errorCode: ErrorCode;

    constructor() {
        super('Invalid pagination params');
        this.errorCode = ErrorCode.INVALID_PAGINATION_PARAMS;
    }
}
