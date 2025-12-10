using UnityEngine;

public class RotateUI : MonoBehaviour
{
    public float speed = 100f;

    void Update()
    {
        // Rotate clockwise on Z axis
        transform.Rotate(0, 0, -speed * Time.deltaTime, Space.Self);
    }
}