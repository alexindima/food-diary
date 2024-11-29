import { UserDto } from '../../user/dto/user.dto';

export interface AuthRequest extends Request {
    user: UserDto;
}
