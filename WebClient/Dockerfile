# Stage 1 - build
FROM node:20-alpine AS build
WORKDIR /app
COPY . .
RUN npm install
RUN npm run build -- --configuration production

# Stage 2 - serve
FROM node:20-alpine
RUN npm install -g serve
WORKDIR /app
COPY --from=build /app/dist/WebClient/browser ./dist
EXPOSE 3000
CMD ["serve", "-s", "dist", "-l", "3000"]
