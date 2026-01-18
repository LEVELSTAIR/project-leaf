using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class EntrySceneController : MonoBehaviour
{
    [Header("References")]
    public CameraZoomController zoomController;
    public UIDocument loginDocument;
    public string gameSceneName = "Game_Garden_Solo";

    [Header("UI Names")]
    public string loginButtonName = "LoginButton";
    public string containerName = "LoginContainer";

    private VisualElement root;
    private Button loginButton;
    private VisualElement container;
    private bool isLoginInProgress = false;

    private void OnEnable()
    {
        if (loginDocument == null) loginDocument = GetComponent<UIDocument>();
        if (loginDocument != null)
        {
            root = loginDocument.rootVisualElement;
            loginButton = root.Q<Button>(loginButtonName);
            container = root.Q<VisualElement>(containerName);

            if (loginButton != null)
                loginButton.clicked += OnLoginClicked;
        }
    }

    private void OnDisable()
    {
        if (loginButton != null)
            loginButton.clicked -= OnLoginClicked;
    }

    private void Start()
    {
        // Preload the game scene additively but keep it hidden/inactive?
        // For now, simpler approach: Just load on login.
    }

    private void OnLoginClicked()
    {
        if (isLoginInProgress) return;
        isLoginInProgress = true;

        // Hide UI
        if (container != null) container.style.display = DisplayStyle.None;

        // Start Zoom
        if (zoomController != null)
        {
            zoomController.StartZoom(OnZoomComplete);
        }
        else
        {
            OnZoomComplete();
        }
    }

    private void OnZoomComplete()
    {
        // Load Game Scene
        SceneManager.LoadScene(gameSceneName);
        // Note: If using additive loading as per plan:
        // SceneManager.LoadScene(gameSceneName, LoadSceneMode.Additive);
        // And then unload this scene or disable the camera.
        // For simplicity, direct load is often safer unless seamless transition is strictly required. 
        // If seamless is required, we would wait for async load to finish before fading.
    }
}
