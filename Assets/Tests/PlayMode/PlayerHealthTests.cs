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
        GameObject playerObj = new GameObject("TestPlayer");
        playerObj.SetActive(false);
        Health health = playerObj.AddComponent<Health>();
        health.maxHP = 3;
        playerObj.SetActive(true);

        Assert.IsTrue(health.IsAlive, "Ошибка: персонаж должен быть жив на старте!");

        health.Decrement();
        Assert.IsTrue(health.IsAlive, "Ошибка: персонаж не должен погибнуть после одного урона!");

        health.Decrement();
        health.Decrement();
        Assert.IsFalse(health.IsAlive, "Ошибка: логика нанесения урона работает некорректно!");

        Object.Destroy(playerObj);
        yield return null;
    }
}