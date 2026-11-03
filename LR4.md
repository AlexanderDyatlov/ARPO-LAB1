
# Отчёт по лабораторной работе №4
## Автоматизация тестирования (Unity Test Runner) в CI/CD

**Студент:** Александр Дятлов  
**Учебная дисциплина:** Архитектура программного обеспечения (АРПО)  
**Вариант:** №2  
- **Тестируемая механика:** Компонент здоровья `Health` (`Assets/Scripts/Mechanics/Health.cs`).  
- **Тип теста:** Интеграционный тест (`PlayMode`).  
- **Цель варианта:** Проверить, что метод `Decrement()` уменьшает значение текущего здоровья персонажа (`currentHP`) ровно на 1 единицу.

---

## 1. Цель работы и теоретическая вводная

**Цель:** Настроить автоматический запуск Unity-тестов (EditMode и PlayMode) в облачном пайплайне так, чтобы облачная компиляция игры и сборка Docker-образа выполнялись только при успешном прохождении всех тестов.


---

## 2. Разделение кодовой базы и настройка Assembly Definitions

Для корректной работы тестов в проекте создана структура сборок:

```text
Assets/
├── Scripts/
│   └── GameAssembly.asmdef       # Основная игровая логика
└── Tests/
    ├── EditMode/
    │   ├── Tests.asmdef          # Сборка EditMode-тестов
    │   └── PlayerTests.cs
    └── PlayMode/
        ├── PlayModeTests.asmdef  # Сборка PlayMode-тестов
        ├── PlayerHealthTests.cs
        └── HealthDecrementTest.cs

```

### 2.1. Игровая сборка `GameAssembly.asmdef`

Файл расположен в `Assets/Scripts/` и объединяет все скрипты механик игры.

```json
{
    "name": "GameAssembly",
    "references": [
        "Unity.InputSystem",
        "Unity.Cinemachine"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}

```

* **Разбор конфигурации:** В массив `"references"` явно добавлены библиотеки `Unity.InputSystem` и `Unity.Cinemachine`. Без них при выносе кода в отдельную сборку скрипты теряют видимость внешних пакетов Unity Package Manager.

### 2.2. Сборка EditMode-тестов `Tests.asmdef`

Файл расположен в `Assets/Tests/EditMode/`.

```json
{
    "name": "Tests",
    "references": [
        "GameAssembly"
    ],
    "includePlatforms": [
        "Editor"
    ]
}

```

* **Разбор конфигурации:** Параметр `"includePlatforms": ["Editor"]` указывает Unity, что данная сборка должна компилироваться только внутри редактора. Это гарантирует запуск тестов строго в режиме **EditMode**. Ссылка на `GameAssembly` дает тестам доступ к классам `PlayerController` и `Health`.

### 2.3. Сборка PlayMode-тестов `PlayModeTests.asmdef`

Файл расположен в `Assets/Tests/PlayMode/`.

```json
{
    "name": "PlayModeTests",
    "rootNamespace": "",
    "references": [
        "GameAssembly",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}

```

* **Разбор конфигурации:** Отсутствие ограничения `includePlatforms` позволяет запускать симуляцию игрового процесса. Для поддержки атрибута `[UnityTest]` и фреймворка `NUnit` подключены `UnityEngine.TestRunner` и `nunit.framework.dll`.

---

## 3. Подробный разбор программного кода тестов

### 3.1. EditMode-тест проверки структуры сцены (`PlayerTests.cs`)

Данный тест проверяет целостность стартовой сцены и наличие ключевых компонентов игрока до запуска симуляции.

```csharp
using NUnit.Framework;
using Platformer.Mechanics;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PlayerTests
{
    // Относительный путь к главной игровой сцене
    private const string MainScenePath = "Assets/Scenes/SampleScene.unity";

    [Test]
    public void EditMode_PlayerTag_IsCorrect()
    {
        // 1. Открываем сцену в Editor-режиме
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), "Ошибка: не удалось открыть сцену SampleScene!");

        // 2. Находим игровой объект персонажа
        GameObject player = GameObject.Find("Player");
        Assert.IsNotNull(player, "Ошибка: на сцене не найден объект игрока 'Player'!");

        // 3. Проверяем тег и наличие жизненно важных скриптов
        Assert.AreEqual("Player", player.tag, "Ошибка: У объекта игрока сброшен тег 'Player'!");
        Assert.IsNotNull(player.GetComponent<PlayerController>(), "Ошибка: на объекте игрока отсутствует скрипт PlayerController!");
        Assert.IsNotNull(player.GetComponent<Health>(), "Ошибка: на объекте игрока отсутствует скрипт Health!");
    }
}

```

#### Детальное описание работы скрипта:

1. `EditorSceneManager.OpenScene`: Имитирует открытие сцены разработчиком в редакторе. Это позволяет программно проинспектировать Иерархию (Hierarchy).
2. `GameObject.Find("Player")`: Выполняет поиск объекта по имени. Использование поиска по тегу на данном этапе избегается, чтобы зафиксировать случай, если тег был случайно сброшен.
3. `Assert.AreEqual` и `Assert.IsNotNull`: Проверки фреймворка NUnit. Если хотя бы один элемент отсутствует, тест завершается с ошибкой и выводит указанный текст в консоль и отчет.

---

### 3.2. PlayMode-тест базвого цикла жизни (`PlayerHealthTests.cs`)

Интеграционный тест, проверяющий поведение скрипта `Health` при получении нескольких единиц урона вплоть до гибели объекта.

```csharp
using System.Collections;
using NUnit.Framework;
using Platformer.Mechanics;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerHealthTests
{
    [UnityTest]
    public IEnumerator PlayMode_PlayerTakesDamage_HealthDecreases()
    {
        // 1. Создаем пустой GameObject и отключаем его для безопасной настройки
        GameObject playerObj = new GameObject("TestPlayer");
        playerObj.SetActive(false);

        // 2. Добавляем компонент Health и устанавливаем максимальное HP
        Health health = playerObj.AddComponent<Health>();
        health.maxHP = 3;

        // 3. Активируем объект — при этом срабатывает метод Awake(), задающий currentHP = maxHP (3)
        playerObj.SetActive(true);

        Assert.IsTrue(health.IsAlive, "Ошибка: персонаж должен быть жив на старте!");

        // 4. Наносим 1 единицу урона
        health.Decrement();
        Assert.IsTrue(health.IsAlive, "Ошибка: персонаж не должен погибнуть после одного урона!");

        // 5. Наносим оставшийся урон
        health.Decrement();
        health.Decrement();
        Assert.IsFalse(health.IsAlive, "Ошибка: логика нанесения урона работает некорректно!");

        // 6. Очищаем память и пропускаем кадр для завершения уничтожения
        Object.Destroy(playerObj);
        yield return null;
    }
}

```

#### Детальное описание работы скрипта:

1. `playerObj.SetActive(false)` / `SetActive(true)`: Важный паттерн тестирования в Unity. Добавление компонента через `AddComponent` на активный объект сразу вызывает `Awake()`. Отключение объекта позволяет задать `maxHP = 3` *до* того, как `Awake()` присвоит `currentHP = maxHP`.
2. `[UnityTest]` и `IEnumerator`: Атрибут Unity Test Framework, позволяющий тесту выполняться в течение нескольких кадров.
3. `yield return null`: Приостанавливает выполнение теста на 1 кадр, давая возможность Unity применить изменения и отработать методу `Object.Destroy()`.

---

### 3.3. PlayMode-тест по Варианту №2 (`HealthDecrementTest.cs`)

Тест проверяет точное соответствие индивидуальному заданию: метод `Decrement()` должен уменьшать текущее здоровье персонажа (`currentHP`) **ровно на 1 единицу**.

```csharp
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Platformer.Mechanics;
using UnityEngine;
using UnityEngine.TestTools;

public class HealthDecrementTest
{
    [UnityTest]
    public IEnumerator PlayMode_Health_Decrement_DecreasesByExactlyOne()
    {
        // 1. Создание и инициализация тестового объекта
        GameObject playerObj = new GameObject("TestHealthObject");
        playerObj.SetActive(false);
        Health health = playerObj.AddComponent<Health>();
        health.maxHP = 3;
        playerObj.SetActive(true);

        // 2. Извлечение доступа к приватному полю currentHP с помощью Reflection (Рефлексии)
        FieldInfo hpField = typeof(Health).GetField("currentHP", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(hpField, "Ошибка: не удалось найти приватное поле currentHP в классе Health!");

        // 3. Считываем начальное значение HP
        int initialHP = (int)hpField.GetValue(health);

        // 4. Выполняем тестируемое действие
        health.Decrement();

        // 5. Считываем конечное значение HP
        int finalHP = (int)hpField.GetValue(health);

        // 6. Проверяем дельту изменения
        Assert.AreEqual(initialHP - 1, finalHP, "Ошибка: метод Decrement() должен уменьшать HP ровно на 1!");

        // 7. Освобождаем ресурсы
        Object.Destroy(playerObj);
        yield return null;
    }
}

```

#### Детальное описание использования Рефлексии (Reflection):

Поле `currentHP` в классе `Health` объявлено с модификатором `private int currentHP;`. Чтобы проверить его значение в юнит-тесте, не нарушая инкапсуляцию и не меняя исходный код игры, используется C# Reflection API:

* `typeof(Health).GetField(...)`: Запрашивает метаданные класса `Health`.
* `BindingFlags.NonPublic | BindingFlags.Instance`: Флаги поиска, указывающие, что нужно найти скрытое (не публичное) поле экземпляра класса.
* `hpField.GetValue(health)`: Извлекает фактическое значение поля из конкретного объекта `health`.
* `Assert.AreEqual(initialHP - 1, finalHP)`: Строгая математическая проверка разницы значений до и после вызова `Decrement()`.

---

## 4. Конфигурация CI/CD пайплайна (`.github/workflows/main.yml`)

Для автоматического прогона тестов в облаке GitHub Actions добавлены задачи `run_tests` и перенастроена цепочка зависимостей:

```yaml
name: Unity 2D Platformer CI/CD

on:
  push:
    branches: [ "main" ]

jobs:
  sanity_check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Check Unity Directories
        run: |
          if [ -d "ProjectSettings" ]; then echo "OK"; else exit 1; fi

  # тестирование
  run_tests:
    needs: sanity_check
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          lfs: true

      # Кэширование папки Library для ускорения импорта ассетов
      - name: Cache Library folder
        uses: actions/cache@v4
        with:
          path: Library
          key: Library-${{ hashFiles('Packages/packages-lock.json', 'Assets/**/*.asmdef') }}

      # Установка официального CLI 
      - name: Install GameCI CLI
        run: |
          curl -SL -o game-ci.tar.gz [https://github.com/game-ci/cli/releases/download/v0.1.72/game-ci-linux-x64.tar.gz](https://github.com/game-ci/cli/releases/download/v0.1.72/game-ci-linux-x64.tar.gz)
          tar xzf game-ci.tar.gz
          ./game-ci
          echo "$PWD" >> $GITHUB_PATH

      # запуск тестов
      - name: Run Unity Tests
        run: |
          game-ci test --docker --dockerShmSize=1025m \
            --engineTestPlatforms='playmode;editmode;COMBINE RESULTS' \
            --engineVersion=6000.3.11f1 --coverageEnabled=false \
            --artifactsPath=artifacts \
            --dockerIsolationMode=default \
            --containerRegistryRepository=unityci/editor \
            --containerRegistryImageVersion=3
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}

      # Сохранение логов и отчетов тестов
      - name: Upload test artifacts
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: artifacts/

  # зеркало
  mirror_repo:
    needs: run_tests
    if: github.repository == 'AlexanderDyatlov/ARPO-LAB1'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - name: Push to Backup
        env:
          BACKUP_TOKEN: ${{ secrets.BACKUP_TOKEN }}
        run: |
          git push --force https://x-access-token:${BACKUP_TOKEN}@[github.com/AlexanderDyatlov/ARPO-LAB1-backup.git](https://github.com/AlexanderDyatlov/ARPO-LAB1-backup.git) main

  # докер
  build_docker_image:
    needs: run_tests
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    steps:
      - uses: actions/checkout@v4
      - name: Log in to GHCR
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - name: Build and Push Docker Image
        uses: docker/build-push-action@v5
        with:
          context: .
          file: ./Dockerfile
          push: true
          tags: |
            ghcr.io/alexanderdyatlov/platformer-game:latest
            ghcr.io/alexanderdyatlov/platformer-game:${{ github.sha }}

```

### Ключевые особенности настройки пайплайна:

1. **Зависимость `needs: run_tests`:** Гарантирует, что этапы `mirror_repo` и `build_docker_image` даже не начнут выполняться, если хотя бы один тест завершился неудачей.
2. **Флаг `--coverageEnabled=false`:** Отключает инструментирование покрытия кода, которое вызвало бы сбой редактора Unity 6 в Linux-контейнере (ошибка `SIGSEGV, exit code 139`).
3. **Объединение результатов (`COMBINE RESULTS`):** Прогоняет EditMode и PlayMode тесты за один запуск контейнера и генерирует единый XML-отчёт в папку `artifacts/`.

---

## 5. Результаты выполнения

1. **Локальная проверка:** В окне *Test Runner* в Unity все 3 теста (`EditMode_PlayerTag_IsCorrect`, `PlayMode_PlayerTakesDamage_HealthDecreases` и `PlayMode_Health_Decrement_DecreasesByExactlyOne`) успешно выполняются и отмечаются зелёными индикаторами.
2. **Облачная проверка:** В разделе GitHub Actions зафиксирован успешный проход всего пайплайна. Артефакт `test-results` с подробным отчетом прогона тестов сформирован и доступен для скачивания.

---

