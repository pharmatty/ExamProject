using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeIn : MonoBehaviour
{
    public float duration = 2f;
    private Image img;

    void Start()
    {
        img = GetComponent<Image>();

        
        Color c = img.color;
        c.a = 0f;
        img.color = c;

        StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;

            Color c = img.color;
            c.a = t / duration;   
            img.color = c;

            yield return null;
        }
    }
}