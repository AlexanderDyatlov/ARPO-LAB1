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