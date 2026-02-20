using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class ControlJugador : MonoBehaviour
{
    [Header("Ruido")]
    public float radioRuidoMaximo = 10f;   // alcance máximo del ruido
    public float multiplicadorRuido = 1.2f; // escala intensidad
    private float nivelRuido; // valor actual de ruido

    [Header("Movimiento")]
    public float velocidadAndar = 3.5f;
    public float velocidadCorrer = 6.5f;
    public float gravedad = -9.81f;

    [Header("Cámara / Ratón")]
    public float sensibilidad = 0.5f;
    public float maxPitch = 90f;

    private CharacterController controller;
    private Animator animator;
    private Transform camara;

    private float rotacionX = 0f;
    private float velocidadVertical = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // Cámara: busca una cámara hija, si no usa la MainCamera
        Camera camChild = GetComponentInChildren<Camera>();
        if (camChild != null) camara = camChild.transform;
        else if (Camera.main != null) camara = Camera.main.transform;
        else Debug.LogWarning("No se encontró Camera. Mete una cámara dentro del jugador o marca una como MainCamera.");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        // --- 1) Mirar (ratón/trackpad) ---
        if (camara != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 look = mouse.delta.ReadValue();
            float mouseX = look.x * sensibilidad;
            float mouseY = look.y * sensibilidad;

            rotacionX -= mouseY;
            rotacionX = Mathf.Clamp(rotacionX, -maxPitch, maxPitch);

            camara.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        // --- 2) Input movimiento (WASD) ---
        float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);

        Vector2 input = new Vector2(x, y);
        if (input.sqrMagnitude > 1f) input.Normalize(); // diagonales

        // Umbral para evitar que se quede "andando" por ruido
        bool moving = input.magnitude > 0.1f;

        bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        bool running = shift && moving;

        float speed = running ? velocidadCorrer : velocidadAndar;

        Vector3 moveHorizontal = (transform.right * input.x + transform.forward * input.y) * speed;

        // --- 3) Gravedad ---
        if (controller.isGrounded && velocidadVertical < 0f)
            velocidadVertical = -2f; // pegado al suelo

        velocidadVertical += gravedad * Time.deltaTime;

        Vector3 move = new Vector3(moveHorizontal.x, velocidadVertical, moveHorizontal.z);
        controller.Move(move * Time.deltaTime);

        // --- 4) Animaciones (basadas en velocidad real) ---

        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        float velocidadActual = horizontalVelocity.magnitude;

        // --- Ruido ---
        if (velocidadActual < 0.1f)
        {
            nivelRuido = 0f;
        }
        else
        {
            nivelRuido = Mathf.Clamp(velocidadActual * multiplicadorRuido, 0f, radioRuidoMaximo);
        }

        float velocidadNormalizada = velocidadActual / velocidadCorrer;

        animator.SetFloat("Speed", velocidadNormalizada, 0.1f, Time.deltaTime);

        // --- 5) Escape libera ratón ---
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Click para volver a bloquear
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public float ObtenerNivelRuido()
    {
        return nivelRuido;
    }

}