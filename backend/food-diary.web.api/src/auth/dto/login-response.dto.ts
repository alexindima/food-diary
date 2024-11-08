import { UserDto } from '../../users/dto/user.dto';

export class LoginResponseDto {
    access_token: string;
    user: UserDto;
}
