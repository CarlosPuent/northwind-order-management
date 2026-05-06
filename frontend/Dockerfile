# ---- Build stage ----
FROM node:22-alpine AS build
WORKDIR /app

# Accept the Maps key at build time so Vite can embed it in the bundle
ARG VITE_GOOGLE_MAPS_API_KEY
ENV VITE_GOOGLE_MAPS_API_KEY=$VITE_GOOGLE_MAPS_API_KEY

COPY . .
RUN npm ci
RUN npx quasar build

# ---- Serve stage ----
FROM nginx:alpine AS runtime

COPY --from=build /app/dist/spa /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]