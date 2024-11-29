import * as winston from 'winston';
import 'winston-daily-rotate-file'; // Импортируем ротацию файлов
import { Injectable, LoggerService } from '@nestjs/common';

@Injectable()
export class GlobalLogger implements LoggerService {
    private readonly logger: winston.Logger;

    public constructor() {
        const isProduction = process.env.NODE_ENV === 'production';

        this.logger = winston.createLogger({
            level: 'info',
            format: winston.format.combine(
                winston.format.timestamp(),
                winston.format.printf(({ timestamp, level, message }) => {
                    return `${timestamp} [${level.toUpperCase()}]: ${message}`;
                }),
            ),
            transports: [
                new winston.transports.DailyRotateFile({
                    filename: 'logs/app-%DATE%.log',
                    datePattern: 'YYYY-MM-DD',
                    zippedArchive: true,
                    maxSize: '20m',
                    maxFiles: '14d',
                }),
                ...(!isProduction
                    ? [
                          new winston.transports.Console({
                              format: winston.format.combine(
                                  winston.format.colorize(),
                                  winston.format.simple(),
                              ),
                          }),
                      ]
                    : []),
            ],
        });
    }

    public log(message: string): void {
        this.logger.info(message);
    }

    public error(message: string, trace?: string): void {
        this.logger.error(`${message}${trace ? ` - Trace: ${trace}` : ''}`);
    }

    public warn(message: string): void {
        this.logger.warn(message);
    }

    public debug(message: string): void {
        this.logger.debug(message);
    }

    public verbose(message: string): void {
        this.logger.verbose(message);
    }
}
