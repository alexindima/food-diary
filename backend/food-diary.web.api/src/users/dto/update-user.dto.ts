import {
    IsBoolean,
    IsDate,
    IsNumber,
    IsOptional,
    IsString,
    MinLength,
} from 'class-validator';

export class UpdateUserDto {
    @IsString()
    @IsOptional()
    username?: string;

    @IsString()
    @MinLength(6)
    @IsOptional()
    password?: string;

    @IsString()
    @IsOptional()
    firstName?: string;

    @IsString()
    @IsOptional()
    lastName?: string;

    @IsDate()
    @IsOptional()
    birthDate?: Date;

    @IsString()
    @IsOptional()
    gender?: string;

    @IsNumber()
    @IsOptional()
    weight?: number;

    @IsNumber()
    @IsOptional()
    height?: number;

    @IsString()
    @IsOptional()
    goal?: string;

    @IsString()
    @IsOptional()
    profileImage?: string;

    @IsBoolean()
    @IsOptional()
    isActive?: boolean;
}
