FROM node:lts-alpine@sha256:d32cdf619f63fe0471182d08996dd516c6275bb5fd31ae06e55a570bd9e1ad43 AS build

WORKDIR /app

COPY package*.json ./

RUN npm ci

COPY . .

ARG BUILD_VERSION=dev
RUN sed -i "s/__BUILD_VERSION__/${BUILD_VERSION}/g" src/environments/environment.prod.ts src/environments/environment.staging.ts

RUN npx ng build fd-ui-kit && npm run build:prod && npm run build:admin && npm run build:storybook

FROM nginx:alpine@sha256:db35bfc6b2951e7f8a72db5db120288c127ffaeeb4a6d4b95a26fead017d5913

COPY --from=build /app/dist/browser/. /usr/share/nginx/html
COPY --from=build /app/dist-admin /usr/share/nginx/html-admin
COPY --from=build /app/dist-storybook /usr/share/nginx/html-storybook
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
