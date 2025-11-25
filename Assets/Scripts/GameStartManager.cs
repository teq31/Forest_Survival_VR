using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartManager : MonoBehaviour
{
    [Header("UI & Player References")]
    [Tooltip("Panel-ul UI care conține butoanele Meniului de Start")]
    public GameObject startMenuPanel;
    
    [Tooltip("Noul Panel care conține textul de Tutorial")]
    public GameObject tutorialPanel; 
    
    [Tooltip("Rig-ul jucătorului (XR Origin) care trebuie activat la Start")]
    public GameObject playerRig;

    // Metodă apelată la pornirea scenei
    void Start()
    {
        // Forțează afișarea meniului principal la încărcarea scenei
        ShowMainMenu();
    }

    // Funcția care setează starea de Meniu principal
    public void ShowMainMenu()
    {
        // 1. UI: Activează panoul de start și asigură că tutorialul este ascuns
        if (startMenuPanel != null)
            startMenuPanel.SetActive(true);
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
        
        // 2. TIMP: Oprește timpul de joc
        Time.timeScale = 0f; 
        
        // 3. JUCĂTOR: Dezactivează controlul jucătorului (Rig-ul)
        if (playerRig != null)
            playerRig.SetActive(false);
            
        Debug.Log("Entering Main Menu state.");
    }

    // Funcție apelată de butonul "START GAME"
    public void StartGame()
    {
        // 1. UI: Ascunde meniul de start
        if (startMenuPanel != null)
            startMenuPanel.SetActive(false);
            
        // 2. TIMP: Porneste timpul
        Time.timeScale = 1f;
        
        // 3. JUCĂTOR: Activează controlul jucătorului
        if (playerRig != null)
            playerRig.SetActive(true);
            
        Debug.Log("Game Started! Player control activated.");
        // Aici poți adăuga DayNightCycle.StartCycle() sau alte logici de inițializare a jocului.
    }

    // Funcție apelată de butonul "TUTORIAL" din meniul principal
    public void OpenTutorial()
    {
        if (startMenuPanel != null)
            startMenuPanel.SetActive(false); // Ascunde meniul principal
        
        if (tutorialPanel != null)
            tutorialPanel.SetActive(true); // Afișează panoul de tutorial
        
        Debug.Log("Displaying Tutorial Panel.");
    }
    
    // Funcție apelată de butonul "BACK" din panoul de tutorial
    public void CloseTutorial()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false); // Ascunde panoul de tutorial
        
        // Revine la meniul principal (care are deja timeScale=0 și player dezactivat)
        ShowMainMenu(); 
        
        Debug.Log("Returning to Main Menu.");
    }

    // Funcție apelată de butonul "QUIT"
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quitting application...");
        // Pentru a testa în editor: UnityEditor.EditorApplication.isPlaying = false;
    }
}