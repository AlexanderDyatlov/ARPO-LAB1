
```markdown
# Отчёт по лабораторной работе №4 — Автоматизация тестирования (Unity Test Runner) в CI/CD

**Студент:** Александр Дятлов  
**Курс:** Архитектура программного обеспечения (АРПО)  
**Индивидуальный вариант:** Вариант №2 (Механика: `Health`, тип теста: **PlayMode**, проверка корректности уменьшения HP методом `Decrement()`).

---

## Цель работы
Настроить автоматическое модульное и интеграционное тестирование Unity-проекта (в режимах EditMode и PlayMode) и интегрировать его в облачный CI/CD пайплайн GitHub Actions в качестве обязательного фильтра (гейта) качества, блокирующего сборку и публикацию нерабочего кода.

---

## 1. Архитектура модулей (Assembly Definitions)
Для изоляции кода и предотвращения попадания тестовых зависимостей в релизную сборку созданы модули через файлы `.asmdef`:
- **`GameAssembly.asmdef`** (`Assets/Scripts/`) — объединяет игровую логику и имеет ссылки на внешние пакеты проекта (`Unity.InputSystem`, `Unity.Cinemachine`).
- **`Tests.asmdef`** (`Assets/Tests/EditMode/`) — модуль для быстрых проверок сцены (с флагом `includePlatforms: ["Editor"]`).
- **`PlayModeTests.asmdef`** (`Assets/Tests/PlayMode/`) — модуль для интеграционных тестов игровой симуляции.

---

## 2. Разработанные тесты (Листинги и описание)

### 2.1. EditMode-тест (`PlayerTests.cs`)
Проверяет корректность настройки сцены `SampleScene`: наличие объекта игрока `Player`, его тега и обязательных компонентов `PlayerController` и `Health` без запуска симуляции.

```csharp
using NUnit.Framework;
using Platformer.Mechanics;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PlayerTests
{
    private const string MainScenePath = "Assets/Scenes/SampleScene.unity";

    [Test]
    public void EditMode_PlayerTag_IsCorrect()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), "Ошибка: не удалось открыть сцену SampleScene!");

        GameObject player = GameObject.Find("Player");
        Assert.IsNotNull(player, "Ошибка: на сцене не найден объект игрока 'Player'!");

        Assert.AreEqual("Player", player.tag, "Ошибка: У объекта игрока сброшен тег 'Player'!");
        Assert.IsNotNull(player.GetComponent<PlayerController>(), "Ошибка: на объекте игрока отсутствует скрипт PlayerController!");
        Assert.IsNotNull(player.GetComponent<Health>(), "Ошибка: на объекте игрока отсутствует скрипт Health!");
    }
}

```

### 2.2. PlayMode-тест по Варианту №2 (`HealthDecrementTest.cs`)

Проверяет индивидуальное требование: метод `Decrement()` класса `Health` должен уменьшать здоровье персонажа ровно на 1. Для доступа к инкапсулированному приватному полю `currentHP` используется механизм рефлексии (`Reflection`).

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
        // Подготовка: создаем изолированный объект с компонентом Health
        GameObject playerObj = new GameObject("TestHealthObject");
        playerObj.SetActive(false);
        Health health = playerObj.AddComponent<Health>();
        health.maxHP = 3;
        playerObj.SetActive(true); // Активация инициализирует currentHP = 3

        // Получаем доступ к приватному полю currentHP через рефлексию
        FieldInfo hpField = typeof(Health).GetField("currentHP", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(hpField, "Ошибка: не удалось найти поле currentHP в классе Health!");

        int initialHP = (int)hpField.GetValue(health);

        // Действие: вызываем метод Decrement по варианту №2
        health.Decrement();

        int finalHP = (int)hpField.GetValue(health);

        // Проверка: здоровье должно уменьшиться ровно на 1
        Assert.AreEqual(initialHP - 1, finalHP, "Ошибка: метод Decrement() должен уменьшать HP ровно на 1!");

        Object.Destroy(playerObj);
        yield return null;
    }
}

```

---

## 3. Интеграция в CI/CD Пайплайн (GitHub Actions)

В файл конфигурации `.github/workflows/main.yml` внедрён шаг **`run_tests`**, запускающий GameCI CLI в контейнере `unityci/editor` с флагом `--coverageEnabled=false` для предотвращения крашей Unity 6 в headless-режиме (SIGSEGV).

Задачи **`mirror_repo`** и **`build_docker_image`** зависят от успешного завершения тестов (`needs: run_tests`). Это гарантирует, что ни один дефектный коммит не попадёт в облачный реестр контейнеров GHCR.

---

## 4. Результаты

1. Разработанные тесты успешно проходят в окне *Test Runner* в Unity (зелёные галочки).
2. Облачный пайплайн GitHub Actions успешно выполняет прогон тестов и сохраняет артефакты `test-results`.

```

---

### 2. Содержимое файла `README.md`
Создай или замени содержимое файла **`README.md`** в корне репозитория этим текстом:

```markdown
# Отчёты по лабораторным работам (АРПО)

**Студент:** Александр Дятлов  
**Учебный курс:** Архитектура программного обеспечения (АРПО)  
**Проект:** Unity 2D Platformer — Автоматизация сборки, CI/CD, Docker и Unit-тестирование

---

## Навигация по отчётам

| Лабораторная работа | Тема | Ссылка на отчёт |
| :--- | :--- | :--- |
| **ЛР №1** | Сборка Unity через CLI и структура проекта | [Открыть LR1.md](LR1.md) |
| **ЛР №2** | Интеграция CI/CD в GitHub Actions и зеркалирование | [Открыть LR2.md](LR2.md) |
| **ЛР №3** | Контейнеризация и публикация приложения (Docker, Nginx, GHCR) | [Открыть LR3.md](LR3.md) |
| **ЛР №4** | Автоматизация тестирования (Unity Test Runner) в CI/CD | [Открыть LR4.md](LR4.md) |

```

---

