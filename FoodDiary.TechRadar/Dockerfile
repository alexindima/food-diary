FROM node:22-alpine AS build
WORKDIR /repo
COPY . .
WORKDIR /repo/FoodDiary.TechRadar
RUN npm ci && npm run build

FROM nginx:1.29-alpine
COPY FoodDiary.TechRadar/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /repo/FoodDiary.TechRadar/dist /usr/share/nginx/html
EXPOSE 80
