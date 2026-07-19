using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class StartMenuController : MonoBehaviour
{
    [Header("Start Menu UI")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Scene Loading")]
    [SerializeField] private string mainGameSceneName = "MainGame";

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument not assigned!");
            return;
        }

        var root = uiDocument.rootVisualElement;
        Button continueButton = root.Q<Button>("Continue");
        Button newGameButton = root.Q<Button>("NewGame");
        Button quitButton = root.Q<Button>("Quit");

        if (continueButton != null)
            continueButton.clicked += OnContinueClicked;
        else
            Debug.LogError("Button 'Continue' not found!");

        if (newGameButton != null)
            newGameButton.clicked += OnNewGameClicked;
        else
            Debug.LogError("Button 'NewGame' not found!");

        if (quitButton != null)
            quitButton.clicked += OnQuitClicked;
        else
            Debug.LogError("Button 'Quit' not found!");

        // Optionally hide the continue button if no save exists
        if (continueButton != null && SaveManager.Instance != null)
        {
            continueButton.style.display = SaveManager.Instance.HasSave() 
                ? DisplayStyle.Flex 
                : DisplayStyle.None;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        var root = uiDocument?.rootVisualElement;
        if (root == null) return;

        Button continueButton = root.Q<Button>("Continue");
        Button newGameButton = root.Q<Button>("NewGame");
        Button quitButton = root.Q<Button>("Quit");

        if (continueButton != null) continueButton.clicked -= OnContinueClicked;
        if (newGameButton != null) newGameButton.clicked -= OnNewGameClicked;
        if (quitButton != null) quitButton.clicked -= OnQuitClicked;
    }

    // -------- Click Handlers --------

    private void OnContinueClicked()
    {
        // Load the scene – the PlayerSpawner will automatically apply the save
        // because SaveManager.HasSave() is true.
        StartGame();
    }

    private void OnNewGameClicked()
    {
        // Delete the save file so PlayerSpawner won't load it
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSave();
            Debug.Log("Save deleted. Starting fresh.");
        }

        StartGame();
    }

    private void OnQuitClicked()
    {
        Debug.Log("Quitting...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void StartGame()
    {
        // Hide buttons (optional)
        var root = uiDocument.rootVisualElement;
        root.Q<Button>("Continue").style.display = DisplayStyle.None;
        root.Q<Button>("NewGame").style.display = DisplayStyle.None;
        root.Q<Button>("Quit").style.display = DisplayStyle.None;

        // Load via your LoadingManager
        LoadingManager.Instance?.LoadScene(
            sceneName: mainGameSceneName,
            minDisplayTime: 1.0f,
            useAnyKey: true,
            continueKey: Key.Space,
            continuePrompt: "Press SPACE to continue"
        );
    }
}