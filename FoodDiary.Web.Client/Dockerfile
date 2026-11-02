FROM node:lts-alpine@sha256:c6ad9e9c1d80f6eb607c26b4b0a132ff7a2344fad1c78ea3baa2a7d4c8de6d94 AS build

WORKDIR /app

COPY package*.json ./
COPY scripts/apply-dependency-patches.mjs ./scripts/

RUN npm ci

COPY . .

ARG BUILD_VERSION=dev
RUN sed -i "s/__BUILD_VERSION__/${BUILD_VERSION}/g" src/environments/environment.prod.ts src/environments/environment.staging.ts projects/fooddiary-admin/src/environments/environment.prod.ts

RUN npx ng build fd-ui-kit && npm run build:prod && npm run build:admin && npm run build:storybook

FROM nginx:alpine@sha256:785835ea65db4bba6f021acc0f3be91b0fef76a53fe41f8588c6698d2aed0606

# The same immutable client image serves static assets and the separate recipe SSR service.
RUN apk add --no-cache libstdc++ libatomic
COPY --from=build /usr/local/bin/node /usr/local/bin/node
# Fail the image build if the copied Node binary lacks a runtime dependency.
RUN /usr/local/bin/node --version
COPY --from=build /app/dist/server /app/server
COPY --from=build /app/dist/browser /app/browser

COPY --from=build /app/dist/browser/. /usr/share/nginx/html
COPY --from=build /app/dist-admin /usr/share/nginx/html-admin
COPY --from=build /app/dist-storybook /usr/share/nginx/html-storybook
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
