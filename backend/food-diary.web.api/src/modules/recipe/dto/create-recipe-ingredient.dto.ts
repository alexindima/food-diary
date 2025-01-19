import { ApiProperty } from '@nestjs/swagger';
import { IsNumber, IsOptional, IsString } from 'class-validator';

export class CreateRecipeIngredientDto {
    @ApiProperty({ description: 'Food item ID', example: 1 })
    @IsNumber()
    foodId: number;

    @ApiProperty({ description: 'Amount of ingredient', example: 200 })
    @IsNumber()
    amount: number;

    @ApiProperty({
        description: 'Unit of measurement',
        example: 'grams',
        required: false,
    })
    @IsOptional()
    @IsString()
    unit?: string;
}
