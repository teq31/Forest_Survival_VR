using UnityEngine;
using UnityEngine.SceneManagement; // Esențial pentru a schimba scenele

public class MenuManager : MonoBehaviour
{
    [Tooltip("MainScene")]
    public string gameSceneName = "MainScene";

    public void StartGame()
    {
        Debug.Log("Starting Game...");
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quitting Game...");
    }
}