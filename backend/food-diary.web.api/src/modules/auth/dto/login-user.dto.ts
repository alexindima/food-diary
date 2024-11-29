import { IsEmail, IsNotEmpty, IsString, MinLength } from 'class-validator';
import { ApiProperty } from '@nestjs/swagger';

export class LoginUserDto {
    @ApiProperty({ description: 'User email', example: 'user@example.com' })
    @IsEmail()
    email: string;

    @ApiProperty({ description: 'User password', example: 'securePassword123' })
    @IsString()
    @IsNotEmpty()
    @MinLength(6)
    password: string;
}
