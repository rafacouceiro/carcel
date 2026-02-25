using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class ControlJugador : MonoBehaviour
{
    // Usar un Enum clarifica muchísimo en qué estado está el jugador para calcular velocidades y ruidos
    public enum PlayerState { Idle, Walking, Running }

    [Header("Movimiento")]
    public float velocidadAndar = 3.5f;
    public float velocidadCorrer = 6.5f;
    public float gravedad = -9.81f;

    [Header("Cámara / Ratón")]
    public float sensibilidad = 0.5f;
    public float maxPitch = 90f;

    // --- Variables de Estado Interno ---
    private CharacterController _controller;
    private Animator _animator;
    private Transform _camara;

    public PlayerState CurrentState { get; private set; }
    public float CurrentNoiseLevel { get; private set; } // Propiedad pública para que los guardias la lean

    // --- Variables de Input y Físicas ---
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _isSprintPressed;
    private float _rotacionX = 0f;
    private float _velocidadVertical = 0f;

    private void Start()
    {
        InicializarComponentes();
        BloquearCursor(true);
    }

    private void Update()
    {
        // 1. Leer las entradas del usuario (Teclado/Ratón)
        ProcesarInput();

        // 2. Controlar la vista (Rotación de cámara y personaje)
        ManejarCamara();

        // 3. Mover al personaje (Físicas y Gravedad)
        ManejarMovimiento();

        // 4. Actualizar el estado (Si anda, corre o está quieto)
        ActualizarEstado();

        // 5. Gestionar animaciones
        ManejarAnimaciones();

        // 6. Calcular el ruido (Para la IA)
        GenerarRuido();
    }

    // ==========================================
    // MÉTODOS MODULARES
    // ==========================================

    private void InicializarComponentes()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        Camera camChild = GetComponentInChildren<Camera>();
        if (camChild != null) _camara = camChild.transform;
        else if (Camera.main != null) _camara = Camera.main.transform;
        else Debug.LogWarning("[ControlJugador] No se encontró la cámara.");
    }

    private void ProcesarInput()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        // Leer movimiento
        float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        _moveInput = new Vector2(x, y);
        if (_moveInput.sqrMagnitude > 1f) _moveInput.Normalize();

        // Leer vista
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            _lookInput = mouse.delta.ReadValue() * sensibilidad;
        }
        else
        {
            _lookInput = Vector2.zero;
        }

        // Leer modificadores y acciones
        _isSprintPressed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

        // Gestión del cursor
        if (keyboard.escapeKey.wasPressedThisFrame) BloquearCursor(false);
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) BloquearCursor(true);
    }

    private void ManejarCamara()
    {
        if (_camara == null) return;

        // Rotación vertical (Cámara)
        _rotacionX -= _lookInput.y;
        _rotacionX = Mathf.Clamp(_rotacionX, -maxPitch, maxPitch);
        _camara.localRotation = Quaternion.Euler(_rotacionX, 0f, 0f);

        // Rotación horizontal (Cuerpo del jugador)
        transform.Rotate(Vector3.up * _lookInput.x);
    }

    private void ManejarMovimiento()
    {
        // Determinar velocidad objetivo
        float targetSpeed = _isSprintPressed ? velocidadCorrer : velocidadAndar;
        if (_moveInput.magnitude < 0.1f) targetSpeed = 0f;

        // Movimiento horizontal
        Vector3 moveHorizontal = (transform.right * _moveInput.x + transform.forward * _moveInput.y) * targetSpeed;

        // Aplicar gravedad
        if (_controller.isGrounded && _velocidadVertical < 0f)
        {
            _velocidadVertical = -2f; // Mantener al jugador pegado al suelo
        }
        _velocidadVertical += gravedad * Time.deltaTime;

        // Mover
        Vector3 move = new Vector3(moveHorizontal.x, _velocidadVertical, moveHorizontal.z);
        _controller.Move(move * Time.deltaTime);
    }

    private void ActualizarEstado()
    {
        if (_moveInput.magnitude < 0.1f) 
        {
            CurrentState = PlayerState.Idle;
        }
        else if (_isSprintPressed) 
        {
            CurrentState = PlayerState.Running;
        }
        else 
        {
            CurrentState = PlayerState.Walking;
        }
    }

    private void ManejarAnimaciones()
    {
        Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
        float velocidadNormalizada = horizontalVelocity.magnitude / velocidadCorrer;
        _animator.SetFloat("Speed", velocidadNormalizada, 0.1f, Time.deltaTime);
    }

    // ==========================================
    // SISTEMA DE RUIDO (INTERFAZ PARA LA IA)
    // ==========================================

    private void GenerarRuido()
    {
        // He eliminado los multiplicadores complejos. 
        // Ahora asignas el ruido explícitamente según el ESTADO, lo cual es predecible y fácil de balancear.
        switch (CurrentState)
        {
            case PlayerState.Idle:
                CurrentNoiseLevel = 0f;
                break;
            case PlayerState.Walking:
                CurrentNoiseLevel = 5f; // Alcance en metros del ruido al andar
                break;
            case PlayerState.Running:
                CurrentNoiseLevel = 15f; // Alcance en metros del ruido al correr
                break;
        }
    }

    private void BloquearCursor(bool bloquear)
    {
        Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !bloquear;
    }
}