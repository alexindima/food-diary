import {
    ArgumentsHost,
    Catch,
    ExceptionFilter,
    HttpException,
    HttpStatus,
} from '@nestjs/common';
import { Response } from 'express';
import { ApiResponseDto, ErrorCode } from '../dto/api-response.dto';
import { UserExistsException } from '../exceptions/user.exists.exception';
import { InvalidCredentialsException } from '../exceptions/invalid-credentials.exception';
import { InvalidIdFormatException } from '../exceptions/invalid-id-format.exception';
import { InvalidPaginationParamsException } from '../exceptions/invalid-pagination-params.exception';
import { FoodInUseException } from '../exceptions/food-in-use.exception';

@Catch()
export class ExceptionsFilter implements ExceptionFilter {
    catch(exception: unknown, host: ArgumentsHost) {
        const ctx = host.switchToHttp();
        const response = ctx.getResponse<Response>();

        let statusCode = HttpStatus.INTERNAL_SERVER_ERROR;
        let error: ErrorCode = ErrorCode.INTERNAL_SERVER_ERROR;
        let data: Record<string, any> | null = null;

        if (exception instanceof HttpException) {
            statusCode = exception.getStatus();
            const responseData = exception.getResponse();
            if (typeof responseData === 'string') {
                data = { message: responseData };
            } else if (typeof responseData === 'object') {
                data = responseData as Record<string, any>;
            }
        } else if (exception instanceof UserExistsException) {
            statusCode = HttpStatus.CONFLICT;
            error = exception.errorCode;
        } else if (exception instanceof InvalidCredentialsException) {
            statusCode = HttpStatus.UNAUTHORIZED;
            error = exception.errorCode;
        } else if (exception instanceof InvalidIdFormatException) {
            statusCode = HttpStatus.BAD_REQUEST;
            error = exception.errorCode;
            data = { message: exception.message };
        } else if (exception instanceof InvalidPaginationParamsException) {
            statusCode = HttpStatus.BAD_REQUEST;
            error = exception.errorCode;
            data = { message: exception.message };
        } else if (exception instanceof FoodInUseException) {
            statusCode = HttpStatus.BAD_REQUEST;
            error = exception.errorCode;
            data = { message: exception.message };
        } else if (exception instanceof Error) {
            data = { message: exception.message };
        } else {
            data = { message: 'An unexpected error occurred' };
        }

        response.status(statusCode).json(ApiResponseDto.error(error, data));
    }
}
