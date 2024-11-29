export class ApiResponse<T> {
    public status: 'success' | 'error';
    public error?: ErrorCode;
    public data?: T;

    private constructor(status: 'success' | 'error', error?: ErrorCode, data?: T) {
        this.status = status;
        this.error = error;
        this.data = data;
    }

    public static success<T>(data: T): ApiResponse<T> {
        return new ApiResponse<T>('success', undefined, data);
    }

    public static error<T = null>(error: ErrorCode, errorData?: T): ApiResponse<T> {
        return new ApiResponse<T>('error', error, errorData);
    }
}
export enum ErrorCode {
    USER_EXISTS = 'USER_EXISTS',
    INVALID_CREDENTIALS = 'INVALID_CREDENTIALS',
    INTERNAL_SERVER_ERROR = 'INTERNAL_SERVER_ERROR',
    VALIDATION_ERROR = 'VALIDATION_ERROR',
    UNKNOWN_ERROR = 'UNKNOWN_ERROR',
}
