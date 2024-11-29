import {
    IsArray,
    IsDateString,
    IsOptional,
    IsString,
    ValidateNested,
} from 'class-validator';
import { Type } from 'class-transformer';
import { CreateConsumptionItemDto } from './create-consumption-item.dto';
import { ApiProperty } from '@nestjs/swagger';

export class CreateConsumptionDto {
    @ApiProperty({
        description: 'Date of consumption',
        example: '2023-11-01T12:00:00Z',
    })
    @IsDateString()
    date: string;

    @ApiProperty({
        description: 'Optional comment',
        example: 'Lunch with family',
        required: false,
    })
    @IsOptional()
    @IsString()
    comment?: string;

    @ApiProperty({
        description: 'List of consumption items',
        type: [CreateConsumptionItemDto],
    })
    @IsArray()
    @ValidateNested({ each: true })
    @Type(() => CreateConsumptionItemDto)
    items: CreateConsumptionItemDto[];
}
