import { ApiProperty } from '@nestjs/swagger';
import {
    IsArray,
    IsNumber,
    IsOptional,
    IsString,
    ValidateNested,
} from 'class-validator';
import { CreateRecipeStepDto } from './create-recipe-step.dto';
import { Type } from 'class-transformer';

export class UpdateRecipeDto {
    @ApiProperty({
        description: 'Recipe name',
        example: 'Updated Chocolate Cake',
    })
    @IsString()
    name: string;

    @ApiProperty({
        description: 'Optional recipe description',
        example: 'Updated description',
        required: false,
    })
    @IsOptional()
    @IsString()
    description?: string;

    @ApiProperty({ description: 'Preparation time in minutes', example: 30 })
    @IsNumber()
    prepTime: number;

    @ApiProperty({ description: 'Cooking time in minutes', example: 60 })
    @IsNumber()
    cookTime: number;

    @ApiProperty({ description: 'Number of servings', example: 6 })
    @IsNumber()
    servings: number;

    @ApiProperty({
        description: 'List of steps for preparing the recipe',
        type: [CreateRecipeStepDto],
    })
    @IsArray()
    @ValidateNested({ each: true })
    @Type(() => CreateRecipeStepDto)
    steps: CreateRecipeStepDto[];
}
