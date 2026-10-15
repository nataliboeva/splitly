FROM node:24-alpine AS build
WORKDIR /app
RUN corepack enable

COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .
# The Docker setup always ships with demo data, so point people to the demo account.
ARG VITE_SHOW_DEMO_ACCOUNT=true
ENV VITE_SHOW_DEMO_ACCOUNT=$VITE_SHOW_DEMO_ACCOUNT
RUN pnpm build

FROM nginx:1.30-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
