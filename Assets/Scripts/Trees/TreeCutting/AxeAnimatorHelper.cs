using UnityEngine;
using System.Collections;

public class AxeAnimatorHelper : MonoBehaviour
{
    public static AxeAnimatorHelper Instance; // Singleton for easy access

    private Animator animator;

    private void Awake()
    {
        // Singleton setup – only one axe in the scene
        if (Instance == null)
            Instance = this;
        else
            Debug.LogWarning("Multiple AxeAnimatorHelper found – destroying duplicate", gameObject);
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogError("AxeAnimatorHelper needs an Animator component on the same GameObject!", gameObject);
    }

    /// <summary>
    /// Triggers the chop animation for the given duration.
    /// </summary>
    /// <param name="boolName">Name of the bool parameter (e.g., "IsChopping")</param>
    /// <param name="duration">How long to keep the bool true</param>
    public void PlayChop(string boolName, float duration)
    {
        if (animator == null) return;

        animator.SetBool(boolName, true);
        StartCoroutine(ResetBoolAfterDelay(boolName, duration));
    }

    private IEnumerator ResetBoolAfterDelay(string boolName, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null)
            animator.SetBool(boolName, false);
    }
}