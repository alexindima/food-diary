FROM node:lts-alpine@sha256:50c8e8ca1d27439048670df5883f32d57cf81cff6233222c893fd0d9884cbd81 AS build

WORKDIR /app

COPY package*.json ./

RUN npm ci

COPY . .

ARG BUILD_VERSION=dev
RUN sed -i "s/__BUILD_VERSION__/${BUILD_VERSION}/g" src/environments/environment.prod.ts src/environments/environment.staging.ts

RUN npx ng build fd-ui-kit && npm run build:prod && npm run build:admin && npm run build:storybook

FROM nginx:alpine@sha256:72ba65eb42c10344912a84ff42408db7d34f2feb642204570ab8fc5ffd29f1d3

COPY --from=build /app/dist/browser/. /usr/share/nginx/html
COPY --from=build /app/dist-admin /usr/share/nginx/html-admin
COPY --from=build /app/dist-storybook /usr/share/nginx/html-storybook
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
