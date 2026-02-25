using UnityEngine;
using UnityEngine.InputSystem;
using AgenticPrison.Physical; // Necesario para acceder al NoiseManager

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class ControlJugador : MonoBehaviour
{
    public enum PlayerState { Idle, Walking, Running }

    [Header("Movimiento")]
    public float velocidadAndar = 3.5f;
    public float velocidadCorrer = 6.5f;
    public float gravedad = -9.81f;

    [Header("Cámara / Ratón")]
    public float sensibilidad = 0.5f;
    public float maxPitch = 90f;

    [Header("Ajustes de Sonido")]
    public float intervaloPasosAndar = 0.5f;
    public float intervaloPasosCorrer = 0.3f;

    // --- Variables de Estado Interno ---
    private CharacterController _controller;
    private Animator _animator;
    private Transform _camara;

    public PlayerState CurrentState { get; private set; }
    public float CurrentNoiseLevel { get; private set; }

    // --- Variables de Input y Físicas ---
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _isSprintPressed;
    private float _rotacionX = 0f;
    private float _velocidadVertical = 0f;
    private float _noiseTimer; // Temporizador para los pulsos de ruido

    private void Start()
    {
        InicializarComponentes();
        BloquearCursor(true);
    }

    private void Update()
    {
        ProcesarInput();
        ManejarCamara();
        ManejarMovimiento();
        ActualizarEstado();
        ManejarAnimaciones();
        
        // El sistema de ruido se actualiza cada frame pero emite por pulsos
        GenerarRuido();
    }

    private void InicializarComponentes()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        Camera camChild = GetComponentInChildren<Camera>();
        if (camChild != null) _camara = camChild.transform;
        else if (Camera.main != null) _camara = Camera.main.transform;
    }

    private void ProcesarInput()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        _moveInput = new Vector2(x, y);
        if (_moveInput.sqrMagnitude > 1f) _moveInput.Normalize();

        if (Cursor.lockState == CursorLockMode.Locked)
            _lookInput = mouse.delta.ReadValue() * sensibilidad;

        _isSprintPressed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

        if (keyboard.escapeKey.wasPressedThisFrame) BloquearCursor(false);
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) BloquearCursor(true);
    }

    private void ManejarCamara()
    {
        if (_camara == null) return;

        _rotacionX -= _lookInput.y;
        _rotacionX = Mathf.Clamp(_rotacionX, -maxPitch, maxPitch);
        _camara.localRotation = Quaternion.Euler(_rotacionX, 0f, 0f);

        transform.Rotate(Vector3.up * _lookInput.x);
    }

    private void ManejarMovimiento()
    {
        float targetSpeed = _isSprintPressed ? velocidadCorrer : velocidadAndar;
        if (_moveInput.magnitude < 0.1f) targetSpeed = 0f;

        Vector3 moveHorizontal = (transform.right * _moveInput.x + transform.forward * _moveInput.y) * targetSpeed;

        if (_controller.isGrounded && _velocidadVertical < 0f)
            _velocidadVertical = -2f;
        
        _velocidadVertical += gravedad * Time.deltaTime;

        Vector3 move = new Vector3(moveHorizontal.x, _velocidadVertical, moveHorizontal.z);
        _controller.Move(move * Time.deltaTime);
    }

    private void ActualizarEstado()
    {
        if (_moveInput.magnitude < 0.1f) 
            CurrentState = PlayerState.Idle;
        else if (_isSprintPressed) 
            CurrentState = PlayerState.Running;
        else 
            CurrentState = PlayerState.Walking;
    }

    private void ManejarAnimaciones()
    {
        Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
        float velocidadNormalizada = horizontalVelocity.magnitude / velocidadCorrer;
        _animator.SetFloat("Speed", velocidadNormalizada, 0.1f, Time.deltaTime);
    }

    private void GenerarRuido()
    {
        // 1. Definir el alcance del ruido según el estado actual
        switch (CurrentState)
        {
            case PlayerState.Idle:
                CurrentNoiseLevel = 0f;
                break;
            case PlayerState.Walking:
                CurrentNoiseLevel = 4f; // Alcance de 4 metros al andar
                break;
            case PlayerState.Running:
                CurrentNoiseLevel = 10f; // Alcance de 10 metros al correr
                break;
        }

        // 2. Lógica de pulsos: solo emitimos ruido si nos movemos
        if (CurrentNoiseLevel > 0f)
        {
            _noiseTimer -= Time.deltaTime;
            if (_noiseTimer <= 0f)
            {
                // Emitir el evento de ruido para que los guardias lo procesen
                NoiseManager.EmitNoise(new NoiseEvent(transform.position, CurrentNoiseLevel, "Fugitivo"));

                // Resetear el timer según la cadencia del paso
                _noiseTimer = (CurrentState == PlayerState.Running) ? intervaloPasosCorrer : intervaloPasosAndar;
            }
        }
        else
        {
            _noiseTimer = 0f; 
        }
    }

    private void BloquearCursor(bool bloquear)
    {
        Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !bloquear;
    }

    private void OnDrawGizmos()
    {
        if (CurrentState != PlayerState.Idle)
        {
            Gizmos.color = new Color(1, 1, 0, 0.3f); // Amarillo transparente
            Gizmos.DrawWireSphere(transform.position, CurrentNoiseLevel);
        }
    }
}