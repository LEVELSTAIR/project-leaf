using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the start menu UI. Handles Start, Quit, and (Steam builds) Play Online buttons.
/// Attach to any GameObject. Assign the UIDocument in the Inspector.
/// </summary>
public class StartMenuController : MonoBehaviour
{
    [Tooltip("Reference to the UIDocument that contains the start menu UI.")]
    [SerializeField] private UIDocument uiDocument;

    [Tooltip("Name of the single-player garden scene (must be in Build Settings).")]
    [SerializeField] private string soloSceneName = "Game_Garden_Solo";

    [Tooltip("Name of the shared multiplayer scene (must be in Build Settings for Steam builds).")]
    [SerializeField] private string multiplayerSceneName = "Eden_Shared";

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("StartMenuController: UIDocument is not assigned in the Inspector!");
            return;
        }

        var root = uiDocument.rootVisualElement;

        Button startButton = root.Q<Button>("Start");
        Button quitButton = root.Q<Button>("Quit");
#if STEAM_BUILD
        Button multiplayerButton = root.Q<Button>("Multiplayer");
#endif

        if (startButton != null)
            startButton.clicked += OnStartClicked;
        else
            Debug.LogError("StartMenuController: Button named 'Start' not found in UI!");

        if (quitButton != null)
            quitButton.clicked += OnQuitClicked;
        else
            Debug.LogError("StartMenuController: Button named 'Quit' not found in UI!");

#if STEAM_BUILD
        if (multiplayerButton != null)
        {
            multiplayerButton.clicked += OnMultiplayerClicked;
            multiplayerButton.style.display = DisplayStyle.Flex;
        }
        else
        {
            Debug.LogWarning("StartMenuController: No button named 'Multiplayer' in UI — add it to StartMenu.uxml for Steam builds.");
        }
#endif
    }

    private void OnDisable()
    {
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        root.Q<Button>("Start")?.UnregisterValueChangedCallback(null);

        Button startButton = root.Q<Button>("Start");
        Button quitButton = root.Q<Button>("Quit");
#if STEAM_BUILD
        Button multiplayerButton = root.Q<Button>("Multiplayer");
        if (multiplayerButton != null) multiplayerButton.clicked -= OnMultiplayerClicked;
#endif
        if (startButton != null) startButton.clicked -= OnStartClicked;
        if (quitButton != null) quitButton.clicked -= OnQuitClicked;
    }

    private void OnStartClicked()
    {
        Debug.Log("Start button clicked. Loading solo scene...");
        SceneManager.LoadScene(soloSceneName);
    }

    private void OnQuitClicked()
    {
        Debug.Log("Quit button clicked. Exiting application...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

#if STEAM_BUILD
    private void OnMultiplayerClicked()
    {
        Debug.Log("Multiplayer button clicked. Loading multiplayer scene...");
        SceneManager.LoadScene(multiplayerSceneName);
    }
#endif
}