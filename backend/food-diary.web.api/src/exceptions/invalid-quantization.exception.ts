import { ErrorCode } from '../dto/api-response.dto';

export class InvalidQuantizationException extends Error {
    errorCode: ErrorCode;

    constructor() {
        super('Quantization days must be greater than 0');
        this.errorCode = ErrorCode.QUANTIZATION_ERROR;
    }
}
