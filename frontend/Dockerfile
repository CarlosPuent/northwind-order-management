# ---- Build stage ----
FROM node:22-alpine AS build
WORKDIR /app

# Copy everything first, then install and build
COPY . .
RUN npm ci
RUN npx quasar build

# ---- Serve stage ----
FROM nginx:alpine AS runtime

COPY --from=build /app/dist/spa /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]