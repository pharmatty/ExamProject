using UnityEngine;
using TMPro;

public class VictoryUI : MonoBehaviour
{
    public GameObject victoryImage;
    public TextMeshProUGUI expText;

    [Header("Fade Settings")]
    public float fadeDuration = 0.25f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        
        if (victoryImage == null)
            victoryImage = gameObject;

        
        canvasGroup = victoryImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = victoryImage.AddComponent<CanvasGroup>();

        victoryImage.SetActive(false);
        canvasGroup.alpha = 0f;
    }

    public void Show(int exp)
    {
      
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        if (victoryImage == null)
        {
            Debug.LogError("[VictoryUI] victoryImage is NULL. Cannot show Victory UI.");
            return;
        }

        if (expText != null)
            expText.text = $"{exp}";

        
        if (canvasGroup == null)
        {
            canvasGroup = victoryImage.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = victoryImage.AddComponent<CanvasGroup>();
        }

        victoryImage.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float t = 0f;
        canvasGroup.alpha = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}