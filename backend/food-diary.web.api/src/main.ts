import { NestFactory } from '@nestjs/core';
import { AppModule } from './modules/app.module';
import { ValidationPipe } from '@nestjs/common';
import { ExceptionsFilter } from './filters/exceptions.filter';
import { DocumentBuilder, SwaggerModule } from '@nestjs/swagger';
import { GlobalLogger } from './common/logger.service';
import { config as loadEnv } from 'dotenv';
import { join } from 'path';

async function bootstrap() {
    const projectRoot = join(__dirname, '..', '..');
    const envFile = join(
        projectRoot,
        `.env.${process.env.NODE_ENV || 'development'}`,
    );

    loadEnv({ path: envFile });
    console.log(`Loaded environment variables from ${envFile}`);
    console.log('Environment Variables:', process.env);

    const app = await NestFactory.create(AppModule, {
        logger: new GlobalLogger(),
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
    SwaggerModule.setup('swagger', app, document);

    app.setGlobalPrefix('api');
    const port = process.env.PORT || 3000;
    await app.listen(port, () => {
        console.log(`Application is running on: http://localhost:${port}`);
    });
}
bootstrap();
