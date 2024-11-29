import { IsNumber } from 'class-validator';
import { ApiProperty } from '@nestjs/swagger';

export class CreateConsumptionItemDto {
    @ApiProperty({ description: 'Food item ID', example: 1 })
    @IsNumber()
    foodId: number;

    @ApiProperty({ description: 'Amount consumed', example: 2 })
    @IsNumber()
    amount: number;
}
