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
        // Создаем временный объект персонажа с компонентом Health
        GameObject playerObj = new GameObject("TestHealthObject");
        playerObj.SetActive(false);
        Health health = playerObj.AddComponent<Health>();
        health.maxHP = 3;
        playerObj.SetActive(true); // Awake() инициализирует currentHP = maxHP = 3

        // Получаем доступ к приватному полю currentHP через рефлексию
        FieldInfo hpField = typeof(Health).GetField("currentHP", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(hpField, "Ошибка: не удалось найти поле currentHP в классе Health!");

        int initialHP = (int)hpField.GetValue(health);

        // Действие: вызываем тестируемый метод Decrement по варианту №2
        health.Decrement();

        int finalHP = (int)hpField.GetValue(health);

        // Проверка: текущее здоровье должно уменьшиться ровно на 1
        Assert.AreEqual(initialHP - 5, finalHP, "Ошибка: метод Decrement() должен уменьшать HP ровно на 1!");

        // Очистка памяти
        Object.Destroy(playerObj);
        yield return null;
    }
}