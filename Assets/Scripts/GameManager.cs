using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject startMenuPanel; // NOU: Panoul Meniului de Start
    public GameObject gameOverPanel;
    
    [Header("Game References")]
    public GameObject playerRig; // Referința la XR Origin / XR Rig
    
    [Header("Game Over UI Elements")]
    public TMP_Text deathMessageText;
    public TMP_Text statsText;
    public Button restartButton;
    public Button mainMenuButton;

    // Variabile de stare
    private float survivalTime = 0f;
    private bool isGameActive = false; // Indica daca jocul ruleaza efectiv

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: pastreaza managerul intre scene

        // Asigura-te ca panourile sunt initial dezactivate
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Ataseaza Listeneri pentru butoanele de GameOver
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        
        // Ataseaza Listeneri pentru butoanele din Meniul de Start (Trebuie sa le faci in Inspector!)
    }

    private void Start()
    {
        // Daca nu exista meniu de start, porneste jocul direct
        if (startMenuPanel == null)
        {
            Debug.LogWarning("[GameManager] Start Menu Panel is NULL, starting game directly!");
            StartGame();
        }
        else
        {
            // La pornirea scenei, afiseaza meniul de start
            ShowStartMenu();
        }
    }

    private void Update()
    {
        if (isGameActive)
        {
            survivalTime += Time.deltaTime;
        }
    }
    
    // ----------------------------------------------------------------------
    //  START MENU LOGIC
    // ----------------------------------------------------------------------

    public void ShowStartMenu()
    {
        isGameActive = false;
        Time.timeScale = 0f;
        
        // Dezactiveaza rig-ul pana jocul incepe efectiv
        if (playerRig != null)
            playerRig.SetActive(false); 
        
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }
    
    // Apelat de butonul "START GAME"
    public void StartGame()
    {
        isGameActive = true;
        Time.timeScale = 1f;
        survivalTime = 0f; // Resetarea timpului la inceputul jocului
        
        if (playerRig != null)
            playerRig.SetActive(true); // Activeaza controlul jucatorului
        
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        // Aici poti adauga logica pentru pornirea DayNightCycle, etc.
    }
    
    // Apelat de butonul "TUTORIAL" (Functionalitate pe care trebuie sa o implementezi)
    public void OpenTutorial()
    {
        Debug.Log("Tutorial scene/panel requested.");
        // Aici poti sa incarci o scena Tutorial sau sa activezi un alt panou.
        // SceneManager.LoadScene("TutorialScene"); 
    }

    // ----------------------------------------------------------------------
    //  GAME OVER LOGIC
    // ----------------------------------------------------------------------

    public void ShowGameOver(string reason, int sticks, int stones, int food, int water, int craftedItems)
    {
        Debug.Log($"[GameManager] ShowGameOver called with reason: {reason}");
        
        if (!isGameActive) 
        {
            Debug.LogWarning("[GameManager] Game is not active, ignoring ShowGameOver call");
            return; 
        }

        isGameActive = false;
        Debug.Log("[GameManager] Setting Time.timeScale to 0");
        Time.timeScale = 0f;

        if (gameOverPanel != null) 
        {
            Debug.Log("[GameManager] Activating gameOverPanel");
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[GameManager] gameOverPanel is NULL! Connect it in Inspector!");
        }

        string message = GetDeathMessage(reason);
        if (deathMessageText != null) 
        {
            deathMessageText.text = message;
            Debug.Log($"[GameManager] Death message set: {message}");
        }
        else
        {
            Debug.LogWarning("[GameManager] deathMessageText is NULL!");
        }

        int minutes = Mathf.FloorToInt(survivalTime / 60f);
        int seconds = Mathf.FloorToInt(survivalTime % 60f);

        string stats = $"Survived: {minutes:00}:{seconds:00}\n";
        stats += $"Resources: {sticks} sticks, {stones} stones, {food} food, {water} water\n";
        stats += $"Crafted Items: {craftedItems}";

        if (statsText != null) 
        {
            statsText.text = stats;
            Debug.Log($"[GameManager] Stats set: {stats}");
        }
        else
        {
            Debug.LogWarning("[GameManager] statsText is NULL!");
        }

        Debug.Log($"[GameManager] Game Over complete! Reason: {reason}");
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

    // ----------------------------------------------------------------------
    //  SCENE MANAGEMENT
    // ----------------------------------------------------------------------

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        // Reincarca scena, care va duce la ShowStartMenu() in Start()
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quitting game...");
    }
}