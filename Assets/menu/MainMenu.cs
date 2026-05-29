using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Настройки кнопки Играть")]
    [Tooltip("Имя сцены, которую нужно загрузить")]
    [SerializeField] private string sceneToLoad ="Game";

    [Tooltip("Время задержки перед загрузкой сцены (в секундах)")]
    [SerializeField] private float delayBeforeLoad = 2f;

    [Header("Компоненты для активации")]
    [Tooltip("Первый скрипт, который нужно включить")]
    [SerializeField] private MonoBehaviour firstScript;

    [Tooltip("Второй скрипт, который нужно включить")]
    [SerializeField] private MonoBehaviour secondScript;

    // Метод для кнопки "Играть"
    public void PlayGame()
    {
        // Активируем скрипты, если они привязаны в инспекторе
        if (firstScript != null) firstScript.enabled = true;
        if (secondScript != null) secondScript.enabled = true;

        // Запускаем таймер до загрузки сцены
        StartCoroutine(LoadSceneAfterDelay());
    }

    // Корутина для отсчета времени
    private IEnumerator LoadSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene(sceneToLoad);
    }

    // Метод для кнопки "Выход"
    public void QuitGame()
    {
        Debug.Log("Выход из игры..."); // Работает в редакторе Unity
        Application.Quit(); // Работает в скомпилированной игре
    }
}
