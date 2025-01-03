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

    @ApiProperty({ description: 'Calories per base amount', example: 96 })
    @IsNumber()
    caloriesPerBase: number;

    @ApiProperty({ description: 'Proteins per base amount', example: 1.3 })
    @IsNumber()
    proteinsPerBase: number;

    @ApiProperty({ description: 'Fats per base amount', example: 0.3 })
    @IsNumber()
    fatsPerBase: number;

    @ApiProperty({ description: 'Carbs per base amount', example: 23 })
    @IsNumber()
    carbsPerBase: number;

    @ApiProperty({ description: 'Default serving size', example: 1 })
    @IsNumber()
    baseAmount: number;

    @ApiProperty({
        description: 'Unit of default serving size',
        example: 'PIECE',
        enum: Unit,
    })
    @IsEnum(Unit)
    baseUnit: Unit;
}
