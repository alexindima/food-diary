import { NestFactory } from '@nestjs/core';
import { AppModule } from './modules/app.module';
import { ValidationPipe } from '@nestjs/common';
import { ExceptionsFilter } from './filters/exceptions.filter';
import { DocumentBuilder, SwaggerModule } from '@nestjs/swagger';
import { GlobalLogger } from './common/logger.service';
import { config as loadEnv } from 'dotenv';
import { join } from 'path';
import * as fs from 'fs';

async function bootstrap() {
    const logger = new GlobalLogger();
    const projectRoot = join(__dirname, '..', '..');
    const envFile = join(
        projectRoot,
        `.env.${process.env.NODE_ENV || 'development'}`,
    );

    loadEnv({ path: envFile });
    logger.log(`Loaded environment variables from ${envFile}`);
    logger.log(`Environment Variables: ${process.env}`);

    let httpsOptions = null;
    if (process.env.SSL_KEY_PATH && process.env.SSL_CERT_PATH) {
        try {
            httpsOptions = {
                key: fs.readFileSync(process.env.SSL_KEY_PATH),
                cert: fs.readFileSync(process.env.SSL_CERT_PATH),
            };
            logger.log('HTTPS options successfully loaded.');
        } catch (error) {
            logger.error('Error loading HTTPS options:', error.message);
        }
    } else {
        logger.log('SSL environment variables not set. Starting in HTTP mode.');
    }

    const app = await NestFactory.create(AppModule, {
        logger: new GlobalLogger(),
        ...(httpsOptions ? { httpsOptions } : {}),
    });

    app.enableCors({
        origin: '*',
        methods: '*',
        credentials: true,
    });

    app.useGlobalPipes(new ValidationPipe({ transform: true }));
    app.useGlobalFilters(new ExceptionsFilter());

    const config = new DocumentBuilder()
        .setTitle('Food diary API')
        .setVersion('1.0')
        .build();

    const document = SwaggerModule.createDocument(app, config);
    SwaggerModule.setup('api/swagger', app, document);

    app.setGlobalPrefix('api');
    const port = process.env.PORT || 3000;
    await app.listen(port, () => {
        logger.log(
            `Application is running on: ${httpsOptions ? 'https' : 'http'}://localhost:${port}`,
        );
    });
}
bootstrap();
