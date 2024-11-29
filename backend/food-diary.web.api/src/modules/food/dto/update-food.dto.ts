import {
    IsEnum,
    IsNotEmpty,
    IsNumber,
    IsOptional,
    IsString,
} from 'class-validator';
import { Unit } from '@prisma/client';
import { ApiProperty } from '@nestjs/swagger';

export class UpdateFoodDto {
    @ApiProperty({ description: 'Name of the food', example: 'Banana' })
    @IsNotEmpty()
    name: string;

    @ApiProperty({
        description: 'Optional barcode of the food',
        example: '987654321098',
        required: false,
    })
    @IsOptional()
    @IsString()
    barcode?: string;

    @ApiProperty({
        description: 'Optional food category',
        example: 'Fruits',
        required: false,
    })
    @IsOptional()
    category?: string;

    @ApiProperty({ description: 'Calories per 100 grams', example: 96 })
    @IsNumber()
    caloriesPer100: number;

    @ApiProperty({ description: 'Proteins per 100 grams', example: 1.3 })
    @IsNumber()
    proteinsPer100: number;

    @ApiProperty({ description: 'Fats per 100 grams', example: 0.3 })
    @IsNumber()
    fatsPer100: number;

    @ApiProperty({ description: 'Carbs per 100 grams', example: 23 })
    @IsNumber()
    carbsPer100: number;

    @ApiProperty({ description: 'Default serving size', example: 1 })
    @IsNumber()
    defaultServing: number;

    @ApiProperty({
        description: 'Unit of default serving size',
        example: 'PIECE',
        enum: Unit,
    })
    @IsEnum(Unit)
    defaultServingUnit: Unit;
}
