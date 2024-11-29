FROM node:lts

WORKDIR /app

COPY package*.json ./

RUN npm ci

COPY . .

RUN npm run prisma:generate

RUN npm run build

CMD ["npm", "run", "start:prod"]

EXPOSE 3000
