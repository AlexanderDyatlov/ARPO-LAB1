# Отчёты по лабораторным работам (АРПО)

**Студент:** Александр Дятлов  
**Учебный курс:** Архитектура программного обеспечения (АРПО)  
**Проект:** Unity 2D Platformer — Автоматизация сборки, CI/CD, Docker, Unit-тестирование, деплой и уведомления

---

## Навигация по отчётам

| Лабораторная работа | Тема | Ссылка на отчёт |
| :--- | :--- | :--- |
| **ЛР №1** | Сборка Unity через CLI и структура проекта | [Открыть LR1.md](LR1.md) |
| **ЛР №2** | Интеграция CI/CD в GitHub Actions и зеркалирование | [Открыть LR2.md](LR2.md) |
| **ЛР №3** | Контейнеризация и публикация приложения (Docker, Nginx, GHCR) | [Открыть LR3.md](LR3.md) |
| **ЛР №4** | Автоматизация тестирования (Unity Test Runner) в CI/CD | [Открыть LR4.md](LR4.md) |
| **ЛР №5** | Облачная сборка WebGL и деплой на GitHub Pages | [Открыть LR5.md](LR5.md) |
| **ЛР №6** | Мониторинг пайплайна, Webhooks и Telegram-оповещения | [Открыть LR6.md](LR6.md) |

---

## Ссылки на результат

| Что | Ссылка |
| :--- | :--- |
| Игра онлайн (GitHub Pages) | <https://alexanderdyatlov.github.io/ARPO-LAB1/> |
| Docker-образ (GHCR) | `ghcr.io/alexanderdyatlov/platformer-game:latest` |
| Запуск образа локально | `docker run -d -p 8080:80 ghcr.io/alexanderdyatlov/platformer-game:latest` |
| Пайплайн (GitHub Actions) | [Actions](https://github.com/AlexanderDyatlov/ARPO-LAB1/actions) |

---

## Как устроен пайплайн (ЛР №2–6)

Пайплайн описан в `.github/workflows/main.yml` и запускается при каждом `push` в ветку `main`.

```text
sanity_check -> run_tests -> build_game -+-> build_docker_image
                    |                    +-> deploy_to_pages
                    +-> mirror_repo
                                         все задачи --> notify (Telegram)
```

| Задача | Что делает | ЛР |
| :--- | :--- | :--- |
| `sanity_check` | Проверяет структуру Unity-проекта | №2 |
| `run_tests` | Запускает EditMode и PlayMode тесты в контейнере Unity | №4 |
| `mirror_repo` | Зеркалирует репозиторий в резервный | №2 |
| `build_game` | Собирает WebGL-билд методом `BuildManager.BuildWebGL` и выгружает артефакт | №5 |
| `build_docker_image` | Собирает Docker-образ с настоящей игрой и публикует в GHCR | №3, №5 |
| `deploy_to_pages` | Публикует билд в ветку `gh-pages` (GitHub Pages) | №5 |
| `notify` | Отправляет результат прогона в Telegram при любом исходе | №6 |

Игра собирается только после успешных тестов, а Docker-образ и публикация на Pages выполняются только после успешной сборки.

---

## ЛР №5: что сделано

1. Включена **Decompression Fallback** в настройках WebGL, чтобы игра работала на GitHub Pages (он не отдаёт `Content-Encoding: gzip`).
2. Добавлена задача `build_game`: сборка Unity 6 (`6000.4.2f1`) в контейнере через GameCI CLI с вызовом `BuildManager.BuildWebGL`.
3. Билд передаётся между задачами через артефакт `WebGL-Build`, вместо заглушек из ЛР №3.
4. Добавлены `deploy_to_pages` (ветка `gh-pages`) и `build_docker_image` (теги `latest` и `sha` коммита).
5. Вручную включён GitHub Pages: `Settings -> Pages -> Branch: gh-pages`.

> Скриншоты: успешный прогон в Actions, работающая игра в браузере, пакет в GHCR.

## ЛР №6: что сделано

1. Создан Telegram-бот через `@BotFather`, получены токен и `chat_id`.
2. Токен и `chat_id` добавлены в секреты репозитория `TELEGRAM_BOT_TOKEN` и `TELEGRAM_CHAT_ID`.
3. Добавлена задача `notify` с `needs` на все задачи и `if: always()`: при успехе бот присылает ссылку на игру, автора коммита и ссылку на прогон, при падении указывает упавшую задачу.

> Скриншоты: сообщение в Telegram об успешной сборке и о падении.
