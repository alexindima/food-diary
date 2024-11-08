import { UserDto } from '../../users/dto/user.dto';

export interface AuthRequest extends Request {
    user: UserDto;
}
