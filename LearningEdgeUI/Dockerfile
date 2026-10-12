# Stage 1: Build the Angular bundle
FROM node:24-alpine AS build
WORKDIR /src

# Copy manifests first so the dependency layer survives source-only changes.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration docker

# Stage 2: Serve the static bundle and proxy /api to the api container
FROM nginx:1.29-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/tms-ui/browser /usr/share/nginx/html
EXPOSE 80
