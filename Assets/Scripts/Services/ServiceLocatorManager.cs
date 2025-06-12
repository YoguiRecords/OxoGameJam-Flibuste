using UnityEngine;

[DefaultExecutionOrder(-200)]
public class ServiceLocatorManager : MonoBehaviour
{
    private void Awake()
    {
        if (FindObjectsByType<ServiceLocatorManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
                              UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        GameServiceLocator.CleanupSceneScopedServices();
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
