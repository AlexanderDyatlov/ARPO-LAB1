
---

# Лабораторная работа №3 — Контейнеризация веб-сервера в Docker и раздача сжатых файлов со стороны Nginx


Репозиторий расширен системой контейнеризации и автоматической публикации WebGL-билда игры с помощью **Docker**, веб-сервера **Nginx** (с поддержкой сжатия Gzip) и облачного реестра **GitHub Container Registry (GHCR)** в рамках CI/CD пайплайна.

---

## 1. Конфигурация веб-сервера Nginx (`nginx.conf`)

Для эффективной раздачи тяжелых статических файлов Unity WebGL (включая скомпилированный код WebAssembly) создан кастомный конфигурационный файл `nginx.conf`.

### Особенности конфигурации:

* **MIME-типы:** Корректная обработка `.wasm` файлов (`application/wasm`), что критично для запуска игрового движка в браузере.
* **Сжатие Gzip:** Включено обязательное сжатие трафика для уменьшения времени загрузки игровых архивов (`.wasm.gz`, `.js.gz`, `.data.gz`).
* **Кэширование:** Настроены заголовки кэширования для статических ассетов.

```nginx
server {
    listen 80;
    server_name localhost;

    root /usr/share/nginx/html;
    index index.html;

    # Поддержка gzip-сжатия для файлов Unity WebGL
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_proxied expired no-cache no-store private auth;
    gzip_types text/plain text/css text/xml text/javascript application/x-javascript application/xml application/wasm;

    location / {
        try_files $uri $uri/ =404;
    }

    # Корректные MIME-типы для WebAssembly
    types {
        application/wasm wasm;
    }
}

```

---

## 2. Создание Docker-образа (`Dockerfile`)

Для упаковки игры в изолированный легковесный контейнер используется официальный образ `nginx:alpine`.

### Содержимое `Dockerfile`:

```dockerfile
FROM nginx:alpine

# Удаляем стандартную страницу приветствия Nginx
RUN rm -rf /usr/share/nginx/html/*

# Копируем файлы веб-версии игры Unity в корневую папку Nginx
COPY Builds/WebGL/ /usr/share/nginx/html/

# Копируем кастомную конфигурацию Nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Открываем 80 порт для доступа к игре
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]

```

---

## 3. Интеграция в CI/CD Пайплайн 

В файл конфигурации `.github/workflows/main.yml` добавлена третья задача (`build_docker_image`), которая запускается автоматически после успешного прохождения проверок.

### Этапы задачи `build_docker_image`:

1. **Подготовка заглушек/билдов:** Создание структуры папок и плейсхолдеров для сжатых файлов на случай холодного запуска сборки.
2. **Авторизация в GHCR:** Безопасный вход в GitHub Container Registry с использованием встроенного `GITHUB_TOKEN`.
3. **Сборка и публикация:** С помощью `docker/build-push-action` образ компилируется и отправляется в облачный репозиторий пакетов GitHub с тегами `:latest` и по хэшу коммита (`:${{ github.sha }}`).

---

## 4. Локальное и облачное тестирование

### Локальная проверка:

Проведена проверка работоспособности собранного контейнера через локальный Docker CLI:

```powershell
docker build -t platformer-compressed:v1 .
docker run -d -p 8080:80 --name unity_compressed_container platformer-compressed:v1

```

* **Результат:** Контейнер успешно запущен, игра доступна по адресу `http://localhost:8080`, а во вкладке *Sources* браузера зафиксирована корректная отдача бинарного модуля `WebGL.wasm`.

### Публикация в облако:

<img width="581" height="229" alt="image" src="https://github.com/user-attachments/assets/9f43f619-5eb7-4920-9a0a-eec79d5bbc54" />


* **GitHub Actions:** Пайплайн успешно завершает все три задачи (`sanity_check`, `mirror_repo`, `build_docker_image`) с зелёным статусом.
* **GHCR Packages:** Готовый Docker-образ успешно опубликован и доступен в профиле репозитория GitHub Packages (`ghcr.io/alexanderdyatlov/platformer-game`).
* <img width="869" height="338" alt="image" src="https://github.com/user-attachments/assets/a49c6b6c-7b0b-46c1-96e7-8483adc0a5ca" />


---

## Структура репозитория 

```text
.
├── .github/
│   └── workflows/
│       └── main.yml        # CI/CD пайплайн (Sanity Check, Mirroring, Docker GHCR)
├── Assets/
│   ├── Editor/
│   │   └── BuildManager.cs # CLI-сборщик Unity
│   └── Scripts/            # Игровая логика
├── Builds/
│   └── WebGL/              # Сборка игры под браузер
├── Dockerfile              # Конфигурация сборки Docker-контейнера
├── nginx.conf              # Оптимизированный конфиг Nginx с Gzip
└── README.md               # Главный навигационный документ со ссылками

```
