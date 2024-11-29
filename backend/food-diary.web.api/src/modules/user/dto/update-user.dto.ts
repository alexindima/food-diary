import {
    IsBoolean,
    IsDateString,
    IsNumber,
    IsOptional,
    IsString,
    MinLength,
} from 'class-validator';
import { ApiProperty } from '@nestjs/swagger';

export class UpdateUserDto {
    @ApiProperty({
        description: 'Username',
        example: 'user123',
        required: false,
    })
    @IsString()
    @IsOptional()
    username?: string;

    @ApiProperty({
        description: 'User password',
        example: 'newPassword123',
        required: false,
    })
    @IsString()
    @MinLength(6)
    @IsOptional()
    password?: string;

    @ApiProperty({
        description: 'First name',
        example: 'John',
        required: false,
    })
    @IsString()
    @IsOptional()
    firstName?: string;

    @ApiProperty({ description: 'Last name', example: 'Doe', required: false })
    @IsString()
    @IsOptional()
    lastName?: string;

    @ApiProperty({
        description: 'Birth date',
        example: '1990-01-01',
        required: false,
    })
    @IsDateString()
    @IsOptional()
    birthDate?: string;

    @ApiProperty({ description: 'Gender', example: 'Male', required: false })
    @IsString()
    @IsOptional()
    gender?: string;

    @ApiProperty({
        description: 'Weight in kilograms',
        example: 70,
        required: false,
    })
    @IsNumber()
    @IsOptional()
    weight?: number;

    @ApiProperty({
        description: 'Height in centimeters',
        example: 175,
        required: false,
    })
    @IsNumber()
    @IsOptional()
    height?: number;

    @ApiProperty({
        description: 'Profile image URL',
        example: 'http://example.com/image.jpg',
        required: false,
    })
    @IsString()
    @IsOptional()
    profileImage?: string;

    @ApiProperty({
        description: 'User activity status',
        example: true,
        required: false,
    })
    @IsBoolean()
    @IsOptional()
    isActive?: boolean;
}
