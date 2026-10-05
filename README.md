# Министерство образования Республики Беларусь
### Учреждение образования «Полоцкий государственный университет имени Евфросинии Полоцкой»
**Факультет Информационных Технологий**  
**Кафедра Вычислительных систем и сетей**  


---

**Лабораторная работа №1**

По дисциплине: «Автоматизация разработки и проектирования ПО»

На тему: «Автоматизация сборки 2D игры через командную строку (CLI)»

* **Выполнил:** Студент группы 23-СТ Дятлов А.В.
* **Проверил:** Бровко Н.В.

*Полоцк, 2026 г.*

---

## Цель работы

Освоение процесса автоматизированной компиляции и сборки проектов Unity в режиме командной строки (CLI) под платформу WebGL без запуска графического интерфейса редактора.

---

## Этап 1. Подготовка проекта и создание C#-скрипта сборщика

1. В редакторе Unity был загружен проект на базе шаблона **2D Platformer Microgame**.
2. Для обеспечения совместимости билда с локальными веб-серверами в настройках проекта (`Edit → Project Settings → Player → WebGL → Publishing Settings`) параметр **Compression Format** был переключен со значения *Gzip* на *Disabled*.
3. В директории `Assets/Editor/` создан C#-скрипт `BuildManager.cs`. Скрипт содержит статический метод `BuildWebGL()`, который считывает активные сцены из `Build Settings`, конфигурирует параметры `BuildPlayerOptions` и вызывает метод `BuildPipeline.BuildPlayer`.

**Листинг 1 — Исходный код скрипта автоматизированной сборки (`Assets/Editor/BuildManager.cs`)**

using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildManager
{
    private static readonly string WebGLBuildPath = "Builds/WebGL";

    public static void BuildWebGL()
    {
        Debug.Log("[CI/CD] Запущен автоматический процесс сборки WebGL...");

        string[] levels = GetScenes();
        if (levels.Length == 0)
        {
            Debug.LogError("[CI/CD] Ошибка: В Настройках Сборки (Build Settings) не найдено ни одной активной сцены!");
            ExitWithCode(1);
            return;
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = levels,
            locationPathName = WebGLBuildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[CI/CD] УСПЕХ! WebGL билд успешно создан.");
            Debug.Log($"[CI/CD] Время сборки: {summary.totalTime.TotalSeconds:F2} сек. Размер: {summary.totalSize} байт.");
            ExitWithCode(0);
        }
        else
        {
            Debug.LogError($"[CI/CD] ОШИБКА СБОРКИ! Количество ошибок: {summary.totalErrors}");
            ExitWithCode(1);
        }
    }

    private static string[] GetScenes()
    {
        var editorScenes = EditorBuildSettings.scenes;
        int activeCount = 0;
        foreach (var scene in editorScenes)
        {
            if (scene.enabled) activeCount++;
        }

        string[] scenePaths = new string[activeCount];
        int index = 0;
        foreach (var scene in editorScenes)
        {
            if (scene.enabled)
            {
                scenePaths[index] = scene.path;
                index++;
            }
        }
        return scenePaths;
    }

    private static void ExitWithCode(int code)
    {
        if (Environment.CommandLine.Contains("-batchmode"))
        {
            EditorApplication.Exit(code);
        }
    }
}

---

## Этап 2. Выполнение сборки через интерфейс командной строки (CLI)

1. Редактор Unity был полностью закрыт.
2. Запуск процесса сборки осуществлен из консоли PowerShell с использованием аргументов пакетного режима:
* `-batchmode` — запуск Unity в фоновом режиме без GUI;
* `-nographics` — отключение инициализации графического процессора;
* `-executeMethod BuildManager.BuildWebGL` — автоматический вызов C#-метода;
* `-quit` — завершение процесса по окончании работы;
* `-logFile build_webgl.log` — перенаправление вывода в лог-файл.



**Команда запуска в PowerShell:**

& "C:\Program Files\Unity\Hub\Editor\6000.4.2f1\Editor\Unity.exe" -batchmode -nographics -projectPath "B:\Works\ARPO\LAB1" -executeMethod BuildManager.BuildWebGL -quit -logFile build_webgl.log


3. В ходе анализа файла `build_webgl.log` зафиксированы итоговые показатели сборки:
* **Время сборки:** 633.20 сек.
* **Размер сборки:** 64 790 731 байт (~61.8 МБ).
* **Итоговый статус:** `[CI/CD] УСПЕХ! WebGL билд успешно создан.`



---

## Этап 3. Локальное тестирование и публикация проекта в Git

1. Для предотвращения ошибок политики безопасности браузера (CORS) готовый билд из директории `Builds/WebGL` был развернут с помощью локального HTTP-сервера.
2. В ходе тестирования подтверждена полная работоспособность WebGL-версии: игра загружается, графика и управление персонажем функционируют корректно.
3. В корне проекта сформирован файл `.gitignore`, исключающий служебные каталоги (`Library/`, `Temp/`, `Builds/`) и лог-файлы.
4. Инициализирован Git-репозиторий, созданы ветки `main` и `LR1`, сформирован Pull Request на GitHub и отправлен на рецензирование (Peer Review).

---

## Ответы на контрольные вопросы

1. **Зачем в команде запуска CLI использовать флаг `-nographics` и какую роль он сыграет при переносе пайплайна на удаленный сервер в облаке?**
Флаг `-nographics` отключает инициализацию графического движка и видеокарты. На удаленных CI/CD серверах (например, GitHub Actions, GitLab CI) виртуальные машины работают в headless-режиме без графической оболочки и физической дискретной видеокарты. Флаг `-nographics` позволяет выполнять компиляцию проекта и сборку билда на таких серверах без ошибок инициализации графического API.
2. **Что произойдет, если запустить консольную сборку проекта, в настройках Build Settings которого не выбрана ни одна сцена игры? Какая строка нашего кода обрабатывает эту ситуацию?**
В этом случае метод `GetScenes()` вернет пустой массив строк (`levels.Length == 0`). Программа выполнит проверку `if (levels.Length == 0)`, выведет сообщение об ошибке `Debug.LogError("[CI/CD] Ошибка: В Настройках Сборки...")` и принудительно завершит процесс Unity с кодом ошибки `1` через вызов `ExitWithCode(1)`.
3. **Почему класс `BuildManager` и его методы обязательно должны быть объявлены как `public static`?**
Флаг `-executeMethod` вызывает метод из командной строки напрямую, когда редактор Unity еще не загрузил какую-либо сцену и не создал экземпляры объектов. Метод должен быть `static`, чтобы Unity могла вызвать его без создания объекта класса, и `public`, чтобы он был доступен для вызова из внешней среды редактора.

---

## Вывод

В ходе лабораторной работы освоены навыки автоматической сборки проектов Unity под платформу WebGL с использованием интерфейса командной строки (CLI). Написан скрипт `BuildManager.cs`, выполняющий компиляцию проекта в пакетном режиме, проанализированы логи сборки и успешно выполнено локальное тестирование готового WebGL-приложения. Полученные навыки необходимы для построения процессов непрерывной интеграции и развертывания (CI/CD).

