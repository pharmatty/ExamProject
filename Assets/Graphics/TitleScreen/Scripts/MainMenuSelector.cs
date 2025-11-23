using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuSelector : MonoBehaviour
{
    public GameObject firstSelected;

    void Start()
    {
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }
}