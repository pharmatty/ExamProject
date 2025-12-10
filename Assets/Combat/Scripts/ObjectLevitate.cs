using UnityEngine;

public class Levitate : MonoBehaviour
{
    [Header("Movement Settings")]
    public float amplitude = 0.5f; // How high it moves
    public float frequency = 2f;   // Speed of levitation

    private Vector3 startPos;

    void Start()
    {
        // Save the starting position
        startPos = transform.localPosition;
    }

    void Update()
    {
        // Calculate a vertical offset
        float offsetY = Mathf.Sin(Time.time * frequency) * amplitude;

        // Apply the offset
        transform.localPosition = new Vector3(
            startPos.x,
            startPos.y + offsetY,
            startPos.z
        );
    }
}