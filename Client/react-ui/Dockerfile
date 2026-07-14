# 1. Aşama: React Uygulamasını Derleme
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build

# 2. Aşama: Nginx ile Statik Dosyaları Sunma
FROM nginx:alpine

# Derlenen React dosyalarını Nginx'in yayın klasörüne kopyala
COPY --from=build /app/dist /usr/share/nginx/html

# React Router yönlendirmelerinin (F5 yapınca 404 vermemesi için) çalışmasını sağlayan Nginx ayarı
RUN echo 'server { listen 80; location / { root /usr/share/nginx/html; index index.html index.htm; try_files $uri $uri/ /index.html; } }' > /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]