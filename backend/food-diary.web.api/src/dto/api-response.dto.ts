export class ApiResponseDto<T> {
    status: ResponseType;
    error?: ErrorCode;
    data?: T;

    private constructor(status: ResponseType, error: ErrorCode, data?: T) {
        this.status = status;
        this.error = error;
        this.data = data;
    }

    static success<T>(data: T): ApiResponseDto<T> {
        return new ApiResponseDto<T>('success', undefined, data);
    }

    static error<T = null>(error: ErrorCode, errorData?: T): ApiResponseDto<T> {
        return new ApiResponseDto<T>(
            'error',
            error,
            errorData ? errorData : undefined,
        );
    }
}

type ResponseType = 'success' | 'error';

export enum ErrorCode {
    USER_EXISTS = 'USER_EXISTS',
    INVALID_CREDENTIALS = 'INVALID_CREDENTIALS',
    INTERNAL_SERVER_ERROR = 'INTERNAL_SERVER_ERROR',
    VALIDATION_ERROR = 'VALIDATION_ERROR',
    INVALID_ID_FORMAT = 'INVALID_ID_FORMAT',
    INVALID_PAGINATION_PARAMS = 'INVALID_PAGINATION_PARAMS',
    FOOD_IN_USE = 'FOOD_IN_USE',
    QUANTIZATION_ERROR = 'QUANTIZATION_ERROR',
}
