# Отчёт по лабораторной работе №5
## Автоматическая компиляция WebGL-игры в облаке и деплой на GitHub Pages

**Студент:** Александр Дятлов  
**По дисциплине:** Архитектура программного обеспечения (АРПО)  
**Репозиторий:** [AlexanderDyatlov/ARPO-LAB1](https://github.com/AlexanderDyatlov/ARPO-LAB1)  
**Игра онлайн:** <https://alexanderdyatlov.github.io/ARPO-LAB1/>

---

## 1. Цель работы

Настроить сквозной пайплайн, который после успешных тестов компилирует реальный WebGL-билд игры в облаке GitHub Actions, публикует его на GitHub Pages и собирает Docker-образ с настоящей игрой вместо файлов-заглушек (`touch`) из ЛР №3.

Итоговая цепочка задач:

```text
sanity_check -> run_tests -> build_game -+-> build_docker_image
                    |                    +-> deploy_to_pages
                    +-> mirror_repo
```

---

## 2. Подготовка Unity к деплою на GitHub Pages

GitHub Pages не отправляет заголовок `Content-Encoding: gzip` для предсжатых файлов, поэтому в проекте оставлен формат сжатия **Gzip** и включена **Decompression Fallback** (`Player Settings -> WebGL -> Publishing Settings`). Unity внедряет в билд JS-распаковщик, и браузер распаковывает `.gz`-файлы на стороне клиента.

Проверка значений в `ProjectSettings/ProjectSettings.asset`:

```bash
grep -n "webGLCompressionFormat\|webGLDecompressionFallback" ProjectSettings/ProjectSettings.asset
```

Ожидаемый результат: `webGLCompressionFormat: 0` (Gzip) и `webGLDecompressionFallback: 1`.

> <img width="707" height="69" alt="image" src="https://github.com/user-attachments/assets/313238b9-5575-4b1e-b3a5-a7735fbe2f34" />


---

## 3. Модификация пайплайна (`.github/workflows/main.yml`)

В пайплайн добавлены три задачи: `build_game`, `build_docker_image`, `deploy_to_pages`. Задачи `build_docker_image` и `deploy_to_pages` зависят от `build_game`, а `build_game` — от `run_tests`, поэтому игра собирается и публикуется только после успешных тестов.

### 3.1. Версия Unity и вызов сборки

Использована версия редактора **6000.4.2f1** (в методичке 6000.3.11f1). Сборка выполняется напрямую через GameCI CLI, как и тесты в ЛР №4, в контейнере `unityci/editor`. Вызывается метод `BuildManager.BuildWebGL` из ЛР №1.

```yaml
  build_game:
    needs: run_tests
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          lfs: true

      - name: Cache Library folder
        uses: actions/cache@v4
        with:
          path: Library
          key: Library-Build-v3-6000-4-${{ hashFiles('Packages/packages-lock.json', 'Assets/**/*.asmdef') }}

      - name: Build WebGL Player
        continue-on-error: true
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        run: |
          set -o pipefail
          game-ci build \
            --engine=unity \
            --engineVersion=6000.4.2f1 \
            --targetPlatform=WebGL \
            --buildMethod=BuildManager.BuildWebGL \
            --buildsPath=Builds \
            --versioningStrategy=None \
            --dockerIsolationMode=default \
            --containerRegistryRepository=unityci/editor \
            --containerRegistryImageVersion=3 \
            --unity-email "$UNITY_EMAIL" \
            --unity-password "$UNITY_PASSWORD" \
            --verbose 2>&1 | tee build.log

      - name: Verify WebGL build exists
        run: |
          test -f Builds/WebGL/WebGL/index.html || { echo "Билд не найден - сборка реально упала"; exit 1; }
          ls -la Builds/WebGL/WebGL Builds/WebGL/WebGL/Build

      - name: Upload WebGL Artifact
        uses: actions/upload-artifact@v4
        with:
          name: WebGL-Build
          path: Builds/WebGL/WebGL
```

### 3.2. Передача билда между задачами

Папка `Builds/WebGL` закрыта правилом `.gitignore`, а каждая задача выполняется на новом сервере. Поэтому `build_game` загружает билд в артефакт `WebGL-Build` (`upload-artifact`), а `build_docker_image` и `deploy_to_pages` скачивают его в `Builds/WebGL` (`download-artifact`). Это заменяет заглушки из ЛР №3.

### 3.3. Деплой на GitHub Pages и сборка Docker-образа

```yaml
  deploy_to_pages:
    needs: build_game
    runs-on: ubuntu-latest
    permissions:
      contents: write
    steps:
      - uses: actions/checkout@v4
      - uses: actions/download-artifact@v4
        with:
          name: WebGL-Build
          path: Builds/WebGL
      - name: Deploy to GitHub Pages
        uses: JamesIves/github-pages-deploy-action@v4
        with:
          folder: Builds/WebGL
          branch: gh-pages
```

`build_docker_image` скачивает тот же артефакт, авторизуется в GHCR и публикует образ с тегами `latest` и `github.sha`:

```text
ghcr.io/alexanderdyatlov/platformer-game:latest
ghcr.io/alexanderdyatlov/platformer-game:<sha коммита>
```

---

## 4. Скрипт сборки `BuildManager.cs`

Метод `BuildManager.BuildWebGL` из ЛР №1 собирает список включённых сцен, запускает `BuildPipeline.BuildPlayer`, пишет в лог результат и завершает редактор с кодом возврата через `EditorApplication.Exit` (в режиме `-batchmode`). Так результат компиляции виден пайплайну.

В рамках этой работы метод доработан: GameCI передаёт путь выдачи аргументом `-customBuildPath`, поэтому скрипт читает его из командной строки Unity. При локальном запуске без аргумента используется путь по умолчанию `Builds/WebGL`.

```csharp
string buildPath = GetArg("-customBuildPath") ?? WebGLBuildPath;
Debug.Log($"[CI/CD] Путь сборки: {buildPath}");

BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
{
    scenes = levels,
    locationPathName = buildPath,
    target = BuildTarget.WebGL,
    options = BuildOptions.None
};

private static string GetArg(string name)
{
    string[] args = Environment.GetCommandLineArgs();
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == name) return args[i + 1];
    }
    return null;
}
```

---

## 5. Включение GitHub Pages

Задача `deploy_to_pages` только публикует билд в ветку `gh-pages`, но не включает хостинг. Pages включён один раз вручную: `Settings -> Pages -> Source: Deploy from a branch -> Branch: gh-pages, / (root) -> Save`.

> <img width="719" height="315" alt="Снимок экрана 2026-11-20 090450" src="https://github.com/user-attachments/assets/5b9dc8ed-8b40-42c6-a6e1-f803dcb07bbc" />


---

## 6. Возникшие проблемы и их решение

**Проблема.** Unity-сборка проходила успешно (в логе `Build Finished, Result: Success`, `[CI/CD] УСПЕХ! WebGL билд успешно создан.`, `Build succeeded`), но шаг `Build WebGL Player` завершался с ошибкой:

```text
[ERROR] Error: There was an error building the project. Please read the logs for details.
    at validateBuild (...)
Error: Process completed with exit code 1.
```

**Анализ.**
1. Путь выдачи. GameCI ожидал билд в `Builds/WebGL/WebGL` (`-customBuildPath`), а скрипт писал в `Builds/WebGL`. Скрипт доработан (см. раздел 4), после этого билд ложится в ожидаемое место. Ошибка при этом осталась.
2. Возврат лицензии. Сбой происходит уже после успешной сборки, на этапе возврата лицензии Unity (в логе `Ulf license file not found` и `Serial number unavailable for ULF return`). Точную причину по исходникам GameCI CLI не устанавливали, вывод сделан по логу.

**Решение.** В шаг сборки добавлено `continue-on-error: true`, а после него новый шаг `Verify WebGL build exists`, который проверяет наличие `Builds/WebGL/WebGL/index.html`. Если билда нет, пайплайн падает, поэтому реальная ошибка компиляции не маскируется. В шаг также передан секрет `UNITY_LICENSE`, как в методичке.

**Остаточный эффект.** В аннотациях прогона остаётся красная запись `build_game: Process completed with exit code 1`, но задача `build_game` и весь пайплайн проходят успешно, а результат работы корректен.

---

## 7. Результаты выполнения

1. **Пайплайн.** Последний прогон завершился со статусом **Success** за 23 мин 23 с. Все задачи зелёные: `sanity_check`, `run_tests`, `build_game`, `mirror_repo`, `build_docker_image`, `deploy_to_pages`, `notify`. Задача `build_game` запустилась после успешных `run_tests`.
   > <img width="1287" height="262" alt="image" src="https://github.com/user-attachments/assets/e63dc26d-2719-4870-8df7-305b070d99e1" />

2. **Лог сборки.** В логе `build_game` присутствуют строки `[CI/CD] УСПЕХ! WebGL билд успешно создан.` с временем и размером. Для одного из прогонов: 728,31 с и 20 366 479 байт. Это доказывает, что компиляция выполнена методом `BuildWebGL` из ЛР №1, а не дефолтной сборкой.
3. **Артефакты.** Сформированы `WebGL-Build` (около 18,9 МБ), `build-log` и `test-results`.
4. **Ветка `gh-pages`.** В неё выгружены `index.html` и папки `Build`, `TemplateData`.
5. **Игра на Pages.** По ссылке <https://alexanderdyatlov.github.io/ARPO-LAB1/> игра загружается и открывается.
   > <img width="1501" height="895" alt="image" src="https://github.com/user-attachments/assets/83f6ece6-971a-43f3-9308-8d81eb618415" />

7. **Docker-образ.** В Packages профиля GitHub опубликован `platformer-game` с тегами `latest` и `sha` коммита.
   > <img width="818" height="361" alt="image" src="https://github.com/user-attachments/assets/750cc6b7-4394-4164-ab39-2507c39541fb" />


---

## 8. Ответы на контрольные вопросы

**1. Почему папка `Builds/WebGL` закрыта `.gitignore` и как билд попадает в `build_docker_image` и `deploy_to_pages`?**
Билд генерируется автоматически, он большой и каждый раз пересобирается, поэтому хранить его в Git не нужно. Каждая задача выполняется на отдельном сервере. Поэтому `build_game` выгружает билд артефактом (`upload-artifact`), а остальные задачи скачивают его (`download-artifact`) в `Builds/WebGL`.

**2. Зачем `needs: run_tests` у `build_game` и что будет, если тесты упадут?**
Тяжёлая сборка запускается только для кода, прошедшего тесты. Если тесты упадут, `build_game`, `mirror_repo`, `build_docker_image` и `deploy_to_pages` не запустятся (будут пропущены), а `notify` отработает всё равно из-за `if: always()`.

**3. Какую роль играет `BuildManager.BuildWebGL` и почему результат виден пайплайну?**
Метод собирает список включённых сцен, запускает `BuildPipeline.BuildPlayer`, анализирует `BuildReport` и завершает редактор через `EditorApplication.Exit(код)`. Пайплайн получает код возврата процесса Unity и по нему определяет успех сборки.

**4. Почему Pages требует Decompression Fallback, а Docker-образ с Nginx обходится без неё?**
Pages не отправляет заголовок `Content-Encoding: gzip` для предсжатых `.gz`-файлов, и браузер не может их распаковать сам, поэтому распаковку выполняет внедрённый JS-распаковщик Unity. Nginx в нашем образе можно настроить так, чтобы он отдавал нужный заголовок, и браузер распаковывает файлы штатно.

**5. Чем отличается доставка через Pages и через Docker-образ, зачем теги `latest` и `github.sha`?**
Pages — бесплатный статический хостинг на домене `github.io`, удобный для быстрой проверки. Docker-образ — переносимый артефакт, который запускается на любом сервере с Docker. Тег `latest` всегда указывает на последнюю сборку, а `github.sha` однозначно указывает на конкретный коммит и позволяет откатиться на нужную версию.

**6. Почему при успешном `deploy_to_pages` сайт может отдавать 404?**
Задача только публикует файлы в ветку `gh-pages`, но не включает хостинг. Нужно один раз включить Pages: `Settings -> Pages -> Deploy from a branch -> gh-pages, / (root)`.
