import { UserDto } from '../../user/dto/user.dto';

export class LoginResponseDto {
    constructor(
        public accessToken: string,
        public refreshToken: string,
        public user: UserDto,
    ) {}
}
