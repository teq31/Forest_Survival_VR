using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject gameOverPanel;
    public TMP_Text deathMessageText;
    public TMP_Text statsText;

    public Button restartButton;
    public Button mainMenuButton;
    public Button quitButton;

    private float survivalTime = 0f;
    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
    }

    private void Update()
    {
        if (!isGameOver)
        {
            survivalTime += Time.deltaTime;
        }
    }

    public void ShowGameOver(string reason, int sticks, int stones, int food, int water, int craftedItems)
    {
        if (isGameOver) return;
        
        isGameOver = true;
        Time.timeScale = 0f;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        string message = GetDeathMessage(reason);
        if (deathMessageText != null) deathMessageText.text = message;

        int minutes = Mathf.FloorToInt(survivalTime / 60f);
        int seconds = Mathf.FloorToInt(survivalTime % 60f);

        string stats = $"Survived: {minutes:00}:{seconds:00}\n";
        stats += $"Resources: {sticks} sticks, {stones} stones, {food} food, {water} water\n";
        stats += $"Crafted Items: {craftedItems}";

        if (statsText != null) statsText.text = stats;

        Debug.Log($"Game Over! Reason: {reason}");
    }

    private string GetDeathMessage(string reason)
    {
        switch (reason)
        {
            case "hunger":
                return "You starved to death...";
            case "thirst":
                return "You died of dehydration...";
            case "panic":
                return "You succumbed to panic in the darkness...";
            default:
                return "You died...";
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quitting game...");
    }
}
