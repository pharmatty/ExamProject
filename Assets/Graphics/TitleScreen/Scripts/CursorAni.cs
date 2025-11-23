using UnityEngine;

public class CursorBounce : MonoBehaviour
{
    public float amplitude = 6f;      
    public float frequency = 4f;      

    private RectTransform rect;
    private Vector3 startPos;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        startPos = rect.localPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * frequency) * amplitude;
        rect.localPosition = startPos + new Vector3(offset, 0f, 0f);
    }
}