# Production build (nginx-served static bundle). This is the file named plain "Dockerfile" on
# purpose — Railway defaults to building whatever's literally named that unless a service's build
# settings explicitly override it, and relying on an override (railway.json's dockerfilePath, or a
# dashboard field) turned out not to be reliable enough in practice: Railway built Dockerfile.dev
# (the Vite dev server) instead, which doesn't listen on the right port and isn't meant to serve
# real traffic — "Application failed to respond". Local dev (docker-compose) explicitly points at
# Dockerfile.dev instead, so it's unaffected by this swap.
#
# Vite bakes VITE_* variables into the JS bundle at BUILD time, not read at container runtime, so
# they must be passed as Docker build args (Railway does this automatically from the service's
# Variables — see RAILWAY_DEPLOY.md).
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

ARG VITE_API_URL
ARG VITE_META_PIXEL_ID
ARG VITE_TIKTOK_PIXEL_ID
ARG VITE_INSTAGRAM_URL
ARG VITE_TIKTOK_URL
ARG VITE_FACEBOOK_URL
ARG VITE_STORE_PHONE
ENV VITE_API_URL=$VITE_API_URL \
    VITE_META_PIXEL_ID=$VITE_META_PIXEL_ID \
    VITE_TIKTOK_PIXEL_ID=$VITE_TIKTOK_PIXEL_ID \
    VITE_INSTAGRAM_URL=$VITE_INSTAGRAM_URL \
    VITE_TIKTOK_URL=$VITE_TIKTOK_URL \
    VITE_FACEBOOK_URL=$VITE_FACEBOOK_URL \
    VITE_STORE_PHONE=$VITE_STORE_PHONE

RUN npm run build

FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.railway.conf /etc/nginx/conf.d/default.conf

EXPOSE 8080
