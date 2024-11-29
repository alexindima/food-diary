import { ErrorCode } from 'src/dto/api-response.dto';

export class FoodInUseException extends Error {
    errorCode: ErrorCode;

    constructor() {
        super('Food in use');
        this.errorCode = ErrorCode.FOOD_IN_USE;
    }
}
