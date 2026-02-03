using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject gameOverPanel;

    [Header("Game References")]
    public GameObject playerRig; 

    [Header("Game Over UI Elements")]
    public TMP_Text deathMessageText;
    public TMP_Text statsText;
    public Button restartButton;
    public Button mainMenuButton;

    private float survivalTime = 0f;
    private bool isGameActive = false;

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
    }

    private void Start()
    {
        isGameActive = true;
        Time.timeScale = 1f; 
        survivalTime = 0f;

        if (playerRig != null)
            playerRig.SetActive(true);
    }

    private void Update()
    {
        if (isGameActive)
        {
            survivalTime += Time.deltaTime;
        }
    }

    public void ShowGameOver(string reason, int sticks, int stones, int food, int water, int craftedItems)
    {
        if (!isGameActive) return;

        isGameActive = false;
        Time.timeScale = 0f; 

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        string message = GetDeathMessage(reason);
        if (deathMessageText != null) deathMessageText.text = message;

        int minutes = Mathf.FloorToInt(survivalTime / 60f);
        int seconds = Mathf.FloorToInt(survivalTime % 60f);

        string stats = $"Survived: {minutes:00}:{seconds:00}\n";
        stats += $"Resources: {sticks} sticks, {stones} stones, {food} food, {water} water\n";
        stats += $"Crafted Items: {craftedItems}";

        if (statsText != null) statsText.text = stats;
    }

    private string GetDeathMessage(string reason)
    {
        switch (reason)
        {
            case "hunger": return "You starved to death...";
            case "thirst": return "You died of dehydration...";
            case "panic": return "You succumbed to panic in the darkness...";
            default: return "You died...";
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
        SceneManager.LoadScene("MainMenu"); 
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}