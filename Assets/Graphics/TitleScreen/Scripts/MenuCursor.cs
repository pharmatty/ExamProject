using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuCursor : MonoBehaviour
{
    public RectTransform cursorRoot;        
    public Vector3 cursorOffset = new Vector3(-150f, 0f, 0f);
    public float moveSpeed = 10f;

    private GameObject currentSelected;
    private bool firstMove = true; 

    void Update()
    {
        var selected = EventSystem.current.currentSelectedGameObject;

        if (selected == null)
            return;

        if (selected != currentSelected)
        {
            currentSelected = selected;

            
            if (!firstMove)
            {
                UIAudio.Instance?.PlayMove();
            }
            firstMove = false;

            RectTransform target = selected.GetComponent<RectTransform>();
            StopAllCoroutines();
            StartCoroutine(MoveCursor(target));
        }
    }

    private System.Collections.IEnumerator MoveCursor(RectTransform target)
    {
        Vector3 start = cursorRoot.position;
        Vector3 end = target.position + cursorOffset;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            cursorRoot.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }
}