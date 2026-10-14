# Build context: apps/web.
#   docker build -f apps/web/Dockerfile --build-arg APP=console -t smartrail-console apps/web
#   docker build -f apps/web/Dockerfile --build-arg APP=kiosk   -t smartrail-kiosk   apps/web
FROM node:24-alpine AS build
ARG APP=console
WORKDIR /repo

COPY package.json package-lock.json ./
COPY console/package.json ./console/package.json
COPY kiosk/package.json ./kiosk/package.json
RUN npm ci

COPY . .
RUN npm run build --workspace=${APP}

FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime
ARG APP=console
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /repo/${APP}/dist /usr/share/nginx/html

EXPOSE 8080
