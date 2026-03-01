using UnityEngine;
using UnityEngine.InputSystem;

public class LinternaApagar : MonoBehaviour
{
    [SerializeField] private Light flashlight;

    void Awake()
    {
        // Si no lo arrastras en el inspector, lo busca en hijos
        if (flashlight == null) flashlight = GetComponentInChildren<Light>(true);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }
}