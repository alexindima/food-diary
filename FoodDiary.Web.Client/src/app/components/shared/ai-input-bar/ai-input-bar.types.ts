import { FoodVisionResponse } from '../../../shared/models/ai.data';

export type AiInputBarTextResult = {
    text: string;
    result: FoodVisionResponse;
};
