using System.Collections;
using UnityEngine;

public class SpawnAnimation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float AnimationTime = 2f;

    void Start()
    {
        transform.localScale = Vector3.zero;
        StartCoroutine(DoAnimation());
    }

    IEnumerator DoAnimation()
    {
        float timer = 0f;
        float duration = AnimationTime * .75f;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;

            transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.25f, timer / duration);
        }

        timer = 0f;
        duration = AnimationTime * .25f;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;

            transform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, timer / duration);
        }
    }
}