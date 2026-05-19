using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the start menu UI. Handles Start and Quit buttons.
/// Attach to any GameObject. Assign the UIDocument in the Inspector.
/// </summary>
public class StartMenuController : MonoBehaviour
{
    [Tooltip("Reference to the UIDocument that contains the start menu UI.")]
    [SerializeField] private UIDocument uiDocument;

    [Tooltip("Name of the main game scene (must be added to Build Settings).")]
    [SerializeField] private string mainGameSceneName = "MainGame";

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

        if (startButton != null)
            startButton.clicked += OnStartClicked;
        else
            Debug.LogError("StartMenuController: Button named 'Start' not found in UI!");

        if (quitButton != null)
            quitButton.clicked += OnQuitClicked;
        else
            Debug.LogError("StartMenuController: Button named 'Quit' not found in UI!");
    }

    private void OnDisable()
    {
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        Button startButton = root.Q<Button>("Start");
        Button quitButton = root.Q<Button>("Quit");

        if (startButton != null)
            startButton.clicked -= OnStartClicked;
        if (quitButton != null)
            quitButton.clicked -= OnQuitClicked;
    }

    private void OnStartClicked()
    {
        Debug.Log("Start button clicked. Loading main game scene...");
        SceneManager.LoadScene(mainGameSceneName);
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
}