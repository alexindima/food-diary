import { User } from '@prisma/client';
import { ApiProperty } from '@nestjs/swagger';

export class UserDto {
    @ApiProperty({ description: 'User email', example: 'user@example.com' })
    email: string;

    @ApiProperty({ description: 'Username', example: 'user123' })
    username: string;

    @ApiProperty({
        description: 'First name',
        example: 'John',
        required: false,
    })
    firstName?: string;

    @ApiProperty({ description: 'Last name', example: 'Doe', required: false })
    lastName?: string;

    @ApiProperty({
        description: 'Birth date',
        example: '1990-01-01T00:00:00.000Z',
        required: false,
    })
    birthDate?: Date;

    @ApiProperty({ description: 'Gender', example: 'Male', required: false })
    gender?: string;

    @ApiProperty({
        description: 'Weight in kilograms',
        example: 70,
        required: false,
    })
    weight?: number;

    @ApiProperty({
        description: 'Height in centimeters',
        example: 175,
        required: false,
    })
    height?: number;

    @ApiProperty({
        description: 'Profile image URL',
        example: 'http://example.com/image.jpg',
        required: false,
    })
    profileImage?: string;

    @ApiProperty({ description: 'User activity status', example: true })
    isActive: boolean;

    constructor(user: User) {
        this.email = user.email;
        this.username = user.username;
        this.firstName = user.firstName;
        this.lastName = user.lastName;
        this.birthDate = user.birthDate;
        this.gender = user.gender;
        this.weight = user.weight;
        this.height = user.height;
        this.profileImage = user.profileImage;
        this.isActive = user.isActive;
    }
}
