import { ApiProperty } from '@nestjs/swagger';
import { IsArray, IsString, ValidateNested } from 'class-validator';
import { CreateRecipeIngredientDto } from './create-recipe-ingredient.dto';
import { Type } from 'class-transformer';

export class CreateRecipeStepDto {
    @ApiProperty({
        description: 'Step description',
        example: 'Mix all ingredients together',
    })
    @IsString()
    description: string;

    @ApiProperty({
        description: 'List of ingredients for this step',
        type: [CreateRecipeIngredientDto],
    })
    @IsArray()
    @ValidateNested({ each: true })
    @Type(() => CreateRecipeIngredientDto)
    ingredients: CreateRecipeIngredientDto[];
}
