import {
    ExecutionContext,
    Injectable,
    UnauthorizedException,
} from '@nestjs/common';
import { AuthGuard } from '@nestjs/passport';

@Injectable()
export class JwtAuthGuard extends AuthGuard('jwt') {
    handleRequest(err: any, user: any, info: any, _context: ExecutionContext) {
        if (info?.name === 'TokenExpiredError') {
            throw new UnauthorizedException('Token has expired');
        }

        if (info?.name === 'JsonWebTokenError') {
            throw new UnauthorizedException('Invalid token');
        }

        if (err || !user) {
            throw new UnauthorizedException(err?.message || 'Unauthorized');
        }

        return user;
    }
}
