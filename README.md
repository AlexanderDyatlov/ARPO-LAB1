
---

```markdown
# Unity 2D Platformer — Build Automation & CI/CD Pipeline

Репозиторий содержит проект 2D-платформера на Unity с настроенной системой консольной автоматической сборки под WebGL и CI/CD пайплайном на GitHub Actions для верификации структуры и зеркалирования кода.

```

---

## 1. Автоматизация сборки (Unity CLI)

Для сборки проекта без запуска графического интерфейса Unity используется C#-скрипт `BuildManager.cs`, расположенный в служебной директории `Assets/Editor/`.

### Особенности реализации:

* **Сборка WebGL:** Автоматический запуск компиляции одной командой из PowerShell.
* **Автономность:** Флаги `-batchmode` и `-nographics` позволяют собирать проект на серверах без графической системы и GPU.
* **Контроль ошибок:** Коды завершения процесса (`0` — успех, `1` — ошибка) и логирование результатов в файл `build_webgl.log`.

### Команда локального запуска:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.2f1\Editor\Unity.exe" `
  -batchmode -nographics `
  -projectPath "B:\Works\ARPO\LAB1" `
  -executeMethod BuildManager.BuildWebGL `
  -quit -logFile build_webgl.log

```

### Результаты сборки:
<img width="900" height="364" alt="Снимок экрана 2026-10-06 141017" src="https://github.com/user-attachments/assets/39f74777-04ea-4974-81ac-884fc7174e5c" />

* **Запуск сборки через консоль и файл логов:**
* **Запущенная WebGL-игра на локальном сервере:**
<img width="1919" height="983" alt="Снимок экрана 2026-10-06 141525" src="https://github.com/user-attachments/assets/299d2b68-7199-4322-b2e2-e6a4d54860c5" />


## 2. CI/CD Пайплайн и Зеркалирование (GitHub Actions)

В проекте настроен автоматический рабочий процесс (`.github/workflows/main.yml`), который запускается при каждом коммите или слиянии (Merge) в ветку `main`.

### Структура пайплайна (Jobs):

1. **`sanity_check` (Диагностика):**
* Клонирует исходный код проекта.
* Проверяет наличие обязательных системных папок Unity (`ProjectSettings`, `Packages`).
* Выполняет поиск C#-скриптов игры в каталоге `Assets/`.


2. **`mirror_repo` (Автоматическое зеркалирование):**
* Запускается строго после успешного прохождения `sanity_check`.
* Использует зашифрованный токен `BACKUP_TOKEN` (Personal Access Token).
* Выкачивает полную историю коммитов (`fetch-depth: 0`).
* Автоматически дублирует весь код и историю в резервный репозиторий `ARPO-LAB1-backup` с помощью `git push --force`.



### Результаты работы CI/CD:


* **Успешное выполнение задач в GitHub Actions:**
  <img width="1582" height="283" alt="image" src="https://github.com/user-attachments/assets/93c197bb-ea77-4f9e-8d5f-a09896e31197" />
* **Синхронизированный резервный репозиторий:**
<img width="1123" height="773" alt="image" src="https://github.com/user-attachments/assets/8b97fae1-a3d6-49c6-8c55-c01d7e837aa9" />


## Структура репозитория

```text
.
├── .github/
│   └── workflows/
│       └── main.yml        # Конфигурация GitHub Actions (CI/CD)
├── Assets/
│   ├── Editor/
│   │   └── BuildManager.cs # C#-скрипт CLI-сборки WebGL
│   └── Scripts/            # Игровые C#-скрипты механики
├── Builds/
│   └── WebGL/              # Готовый билд игры для браузера
└── README.md

```

---



