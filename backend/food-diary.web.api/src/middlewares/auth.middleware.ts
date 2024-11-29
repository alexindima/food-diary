import {
    Injectable,
    NestMiddleware,
    UnauthorizedException,
} from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { NextFunction } from 'express';
import { ConfigService } from '@nestjs/config';

@Injectable()
export class AuthMiddleware implements NestMiddleware {
    constructor(
        private readonly jwtService: JwtService,
        private readonly configService: ConfigService,
    ) {}

    use(req: Request, res: Response, next: NextFunction) {
        const authHeader = req.headers['authorization'];
        if (!authHeader || !authHeader.startsWith('Bearer ')) {
            throw new UnauthorizedException('Missing or invalid token');
        }

        const token = authHeader.split(' ')[1];
        try {
            const decoded = this.validateToken(token);

            req['userId'] = decoded.sub;

            next();
        } catch (error) {
            throw new UnauthorizedException(error.message || 'Invalid token');
        }
    }

    private validateToken(token: string): any {
        try {
            const secret = this.configService.get<string>('JWT_SECRET');
            return this.jwtService.verify(token, { secret });
        } catch (error) {
            if (error.name === 'TokenExpiredError') {
                throw new UnauthorizedException('Token has expired');
            } else if (error.name === 'JsonWebTokenError') {
                throw new UnauthorizedException('Invalid token signature');
            }
            throw new UnauthorizedException('Invalid token');
        }
    }
}
