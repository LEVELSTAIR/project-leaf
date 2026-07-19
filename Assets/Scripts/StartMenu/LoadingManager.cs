using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton manager for loading scenes with a UI Toolkit loading screen.
/// </summary>
public class LoadingManager : MonoBehaviour
{
    // Singleton instance
    public static LoadingManager Instance { get; private set; }

    [Header("Loading Screen UIDocument")]
    [Tooltip("The UIDocument that displays the loading screen.")]
    [SerializeField] private UIDocument loadingUIDocument;

    [Header("Default Settings (can be overridden in LoadScene)")]
    [SerializeField] private float defaultMinimumTime = 0.5f;
    [SerializeField] private bool defaultUseAnyKey = true;
    [SerializeField] private Key defaultContinueKey = Key.Space;
    [SerializeField] private string defaultContinuePrompt = "Press any key to continue";

    // Cached UI elements
    private VisualElement progressFill;
    private Label progressText;
    private Label continuePromptLabel;

    // State
    private bool isLoading = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Cache UI elements from the UIDocument
        if (loadingUIDocument == null)
        {
            Debug.LogError("LoadingManager: UIDocument not assigned!");
            return;
        }

        var root = loadingUIDocument.rootVisualElement;
        progressFill = root.Q<VisualElement>("ProgressFill");
        progressText = root.Q<Label>("ProgressText");
        continuePromptLabel = root.Q<Label>("ContinuePrompt");

        if (continuePromptLabel != null)
            continuePromptLabel.text = defaultContinuePrompt;

        // Ensure loading screen is hidden at start
        HideLoadingUI();

        // If the GameObject was deactivated, we need to activate it to get the UIDocument to load,
        // but we keep it hidden via style.
        // We'll just ensure it's active in hierarchy (we'll deactivate after hiding?)
        // Actually, we want the GameObject active so UIDocument works, but we hide the root.
        // Let's just activate the GameObject if it's not, and hide the root.
        if (loadingUIDocument != null && loadingUIDocument.gameObject != null)
        {
            if (!loadingUIDocument.gameObject.activeSelf)
                loadingUIDocument.gameObject.SetActive(true);
            // Hide the root element
            if (loadingUIDocument.rootVisualElement != null)
                loadingUIDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------
    public void LoadScene(string sceneName, 
                         float minDisplayTime = -1f, 
                         bool? useAnyKey = null, 
                         Key? continueKey = null, 
                         string continuePrompt = null)
    {
        if (isLoading)
        {
            Debug.LogWarning("LoadingManager: Already loading a scene.");
            return;
        }

        // Use default values if not overridden
        float minTime = minDisplayTime >= 0 ? minDisplayTime : defaultMinimumTime;
        bool anyKey = useAnyKey ?? defaultUseAnyKey;
        Key key = continueKey ?? defaultContinueKey;
        string prompt = string.IsNullOrEmpty(continuePrompt) ? defaultContinuePrompt : continuePrompt;

        StartCoroutine(LoadSceneCoroutine(sceneName, minTime, anyKey, key, prompt));
    }

    // ------------------------------------------------------------
    // Core Loading Coroutine
    // ------------------------------------------------------------
    private IEnumerator LoadSceneCoroutine(string sceneName, float minTime, bool anyKey, Key key, string prompt)
    {
        isLoading = true;

        // Show loading UI
        ShowLoadingUI(prompt);

        // Wait one frame for UI to render
        yield return null;

        // Start async load
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float startTime = Time.time;

        // Update progress
        while (operation.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            UpdateProgress(progress);
            yield return null;
        }

        // Scene is ready - show 100% and continue prompt
        UpdateProgress(1f);
        ShowContinuePrompt(prompt);

        // Enforce minimum display time
        while (Time.time - startTime < minTime)
            yield return null;

        // Wait for key press
        yield return WaitForKeyPress(anyKey, key);

        // Allow scene activation
        operation.allowSceneActivation = true;
        while (!operation.isDone)
            yield return null;

        // Hide loading UI after scene is fully loaded
        HideLoadingUI();
        isLoading = false;
    }

    // ------------------------------------------------------------
    // UI Management
    // ------------------------------------------------------------
    private void ShowLoadingUI(string prompt)
    {
        if (loadingUIDocument == null) return;

        // Ensure GameObject is active
        loadingUIDocument.gameObject.SetActive(true);

        var root = loadingUIDocument.rootVisualElement;
        root.style.display = DisplayStyle.Flex;

        // Update prompt text
        if (continuePromptLabel != null)
        {
            continuePromptLabel.text = prompt;
            continuePromptLabel.style.display = DisplayStyle.None; // hidden initially
        }

        // Reset progress
        UpdateProgress(0f);
    }

    private void UpdateProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (progressFill != null)
            progressFill.style.width = Length.Percent(progress * 100f);
        if (progressText != null)
            progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
    }

    private void ShowContinuePrompt(string prompt)
    {
        if (continuePromptLabel != null)
        {
            continuePromptLabel.text = prompt;
            continuePromptLabel.style.display = DisplayStyle.Flex;
        }
    }

    private void HideLoadingUI()
    {
        if (loadingUIDocument != null)
        {
            var root = loadingUIDocument.rootVisualElement;
            if (root != null)
                root.style.display = DisplayStyle.None;
            // Optionally deactivate the GameObject to save performance
            // loadingUIDocument.gameObject.SetActive(false);
        }
    }

    // ------------------------------------------------------------
    // Key Press Wait (Input System)
    // ------------------------------------------------------------
    private IEnumerator WaitForKeyPress(bool useAnyKey, Key specificKey)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            Debug.LogWarning("No keyboard detected – continuing without key wait.");
            yield break;
        }

        while (true)
        {
            if (useAnyKey)
            {
                bool any = false;
                foreach (var k in keyboard.allKeys)
                {
                    if (k.wasPressedThisFrame)
                    {
                        any = true;
                        break;
                    }
                }
                if (any)
                    break;
            }
            else
            {
                if (keyboard[specificKey].wasPressedThisFrame)
                    break;
            }
            yield return null;
        }
    }
}