using UnityEngine;
using System.Collections;

public class PickaxeAnimatorHelper : MonoBehaviour
{
    public static PickaxeAnimatorHelper Instance;

    private Animator animator;

    private void Awake() => Instance = this;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogError("PickaxeAnimatorHelper needs an Animator", gameObject);
    }

    public void PlaySwing(string boolName, float duration)
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