using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class SceneEventBus : MonoBehaviour
{
    public static event Action SceneChanged;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneChanged?.Invoke();
    }
}