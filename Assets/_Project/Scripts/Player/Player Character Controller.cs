using UnityEngine;

/// <summary>
///  Контроллер персонажа
/// </summary>
public class PlayerCharacterController : MonoBehaviour
{
    // !!! НА ДАННЫЙ МОМЕНТ НЕ ВСЕ ПАРАМЕТРЫ ИСПОЛЬЗУЮТСЯ, НО В ДАЛЬНЕЙШЕМ БУДУТ !!!
    [Header("Ссылки")]
    [Tooltip("Ссылка на основную камеру, используемую в контроллере")]
    public Camera PlayerCamera;

    [Header("Общие параметры")]
    [Tooltip("Сила тяжести для персонажа")]
    public float GravityDownForce = -9.81f;

    [Tooltip("Проверка слоя для заземления")]
    public LayerMask GroundCheckLayers = -1;

    [Tooltip("Расстояние от контроллера до земли")]
    public float GroundCheckDistance = 0.05f;

    [Header("Передвижение")]
    [Tooltip("Максимальная скорость ходьбы")]
    public float MaxSpeedOnGround = 10f;

    [Tooltip("Резкость движений")]
    public float MovementSharpnessOnGround = 15;

    [Tooltip("Множетель скорость передвижения в присяди")] [Range(0, 1)]
    public float CrouchSpeedModifier = 0.5f;

    [Tooltip("Множетель скорости бега")]
    public float SprintSpeedModifier = 2f;

    [Tooltip("Максимальная скорость передвижения в воздухе")]
    public float MaxSpeedInAir = 10f;

    [Tooltip("Ускорение в воздухе")]
    public float AccelerationSpeedInAir = 25f;

    [Header("Прыжок")]
    [Tooltip("Сила прыжка")]
    public float JumpForce = 9f;

    [Header("Вращение камеры")]
    [Tooltip("Скорость вращения камеры")]
    public float RotationSpeed = 200f;

    [Tooltip("Множетель для скорости вращения в прицеле")] [Range(0.1f, 1f)]
    public float AimingRotationMultiplier = 0.4f;

    [Tooltip("Ограничение вращения камеры по вертикали (в градусах)")]
    public float VerticalCameraClamp = 80f;

    [Header("Позиционирование")]
    [Tooltip("Высота на которой находится камера")]
    public float CameraHeightRatio = 0.9f;

    [Tooltip("Высота персонажа")]
    public float CapsuleHeightStanding = 1.8f;

    [Tooltip("Высота персонажа в присяди")]
    public float CapsuleHeightCrouching = 0.9f;

    [Tooltip("Скорость приседания")]
    public float CrouchingSharpness = 10f;

    [Header("Настройки")]
    [Tooltip("Чувствительность мыши")]
    public float mouseSensitivity = 100f;

    private Vector3 velocity;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isJumping;
    private bool isRunning;
    private bool isCrouching;
    private float initialCameraYOffset;

    private CharacterController characterController;
    private PlayerInput_Actions inputActions;

    /// <summary>
    /// Инициализация компонентов и подписка на события ввода
    /// </summary>
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputActions = new PlayerInput_Actions();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.Jump.performed += ctx => isJumping = true;
        inputActions.Player.Jump.canceled += ctx => isJumping = false;

        inputActions.Player.Sprint.performed += ctx => isRunning = true;
        inputActions.Player.Sprint.canceled += ctx => isRunning = false;

        inputActions.Player.Crouch.performed += ctx => ToggleCrouch();

        initialCameraYOffset = PlayerCamera.transform.localPosition.y;
    }

    /// <summary>
    /// Включение системы ввода
    /// </summary>
    private void OnEnable()
    {
        inputActions.Enable();
    }

    /// <summary>
    /// Отключение системы ввода
    /// </summary>
    private void OnDisable()
    {
        inputActions.Disable();
    }


    private void Update()
    {
        HandleMovement();
        HandleCrouch();
        HandleJump();
        ApplyGravity();
        HandleLook();
    }

    /// <summary>
    /// Метод движения персонажа
    /// </summary>
    private void HandleMovement()
    {
        float speedMultiplier = 1;
        if (isCrouching)
        {
            speedMultiplier = CrouchSpeedModifier;
        }
        else if (isRunning && characterController.isGrounded && !isJumping && !isCrouching)
        {
            speedMultiplier = SprintSpeedModifier;
        }

        Vector3 moveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y) * (MaxSpeedOnGround * speedMultiplier);
        characterController.Move(moveDirection * Time.deltaTime);
    }

    /// <summary>
    /// Метод вращения камеры
    /// </summary>
    private void HandleLook()
    {
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        float newXRotation = PlayerCamera.transform.localEulerAngles.x - mouseY;
        if (newXRotation > 180) newXRotation -= 360;
        newXRotation = Mathf.Clamp(newXRotation, -90f, 90f);

        PlayerCamera.transform.localEulerAngles = new Vector3(newXRotation, 0f, 0f);
    }

    /// <summary>
    /// Метод прыжка
    /// </summary>
    private void HandleJump()
    {
        if (isJumping && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(JumpForce * -2f * GravityDownForce);
            isRunning = false;
        }
    }

    /// <summary>
    /// Метод отвечающий за гравитацию
    /// </summary>
    private void ApplyGravity()
    {
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += GravityDownForce * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Метод переключения состояния приседания
    /// </summary>
    private void ToggleCrouch()
    {
        if (characterController.isGrounded)
        {
            isCrouching = !isCrouching;
            isRunning = false;
        }
    }

    /// <summary>
    /// Метод изменения высоты персонажа при приседании
    /// </summary>
    private void HandleCrouch()
    {
        float targetHeight = isCrouching ? CapsuleHeightCrouching : CapsuleHeightStanding;

        if (Mathf.Abs(characterController.height - targetHeight) > 0.01f)
        {
            float newHeight = Mathf.Lerp(characterController.height, targetHeight, CrouchingSharpness * Time.deltaTime);
            float deltaHeight = newHeight - characterController.height;

            characterController.height = newHeight;
            characterController.center = new Vector3(0, newHeight / 2, 0);

            Vector3 newPosition = transform.position + Vector3.up * deltaHeight / 2;
            transform.position = newPosition;

            UpdateCameraHeight(newHeight);
        }
    }

    /// <summary>
    /// Метод отвечающий за изменение высоты камеры при приседании
    /// </summary>
    /// <param name="newHeight"></param>
    private void UpdateCameraHeight(float newHeight)
    {
        float cameraYOffset = initialCameraYOffset * (newHeight / CapsuleHeightStanding);

        Vector3 newCameraPosition = PlayerCamera.transform.localPosition;
        newCameraPosition.y = Mathf.Lerp(PlayerCamera.transform.localPosition.y, cameraYOffset, CrouchingSharpness * Time.deltaTime);
        PlayerCamera.transform.localPosition = newCameraPosition;
    }
}
