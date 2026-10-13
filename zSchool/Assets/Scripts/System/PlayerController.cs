using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpPower = 5f;
    public float gravity = -20f;
    public float mouseSensitivity = 0.2f;
    public Transform cameraPivot;
    public Transform cameraTranform;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isRunning;
    private float pitch = 20f;
    private float verticalVelocity;
    private CharacterController controller;

    [Header("Hp")]
    [SerializeField] private UnityEngine.UI.Image[] hearts;
    private int hp = 4;
    public static int currentHp = 4;

    private bool isInvincible = false;

    private SpriteRenderer sr;

    /*public float rotateSpeed = 500f;
      private float mx = 0f;
      private float my = 0f;*/

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        hp = currentHp;
        UpdateHearts();
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && controller.isGrounded)
        {
            verticalVelocity = jumpPower;
        }
    }

    public void OnSprint(InputValue value)
    {
        isRunning = value.isPressed;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            TakeDamage(1);
        }

        transform.Rotate(0f, lookInput.x * mouseSensitivity, 0f);
        pitch = pitch - lookInput.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -20f, 60f);
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;

        if (Keyboard.current != null)
        {
            isRunning = Keyboard.current.leftShiftKey.isPressed;
        }
        float speed = moveSpeed;
        float targetZ = -3f;

        if (isRunning)
        {
            speed = moveSpeed * 2f;
            targetZ = -5f;
        }
        else
        {
            speed = moveSpeed;
            targetZ = -3f;
        }

        move = move * speed;
        move.y = verticalVelocity;

        Vector3 camPos = cameraTranform.localPosition;
        camPos.z = Mathf.Lerp(camPos.z, targetZ, 5f * Time.deltaTime);
        cameraTranform.localPosition = camPos;

        controller.Move(move * Time.deltaTime);

        /*float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        mx += mouseX * rotateSpeed * Time.deltaTime;
        my += mouseY * rotateSpeed * Time.deltaTime;

        my = Mathf.Clamp(my, -90f, 90f);

        transform.localEulerAngles = new Vector3(-my, mx, 0);*/
    }
    public void TakeDamage(int damage)
    {
        if (isInvincible || hp <= 0) return;

        hp -= damage;
        currentHp = hp;
        UpdateHearts();

        if (hp <= 0)
        {
            GameOver();
        }
    }

    private void UpdateHearts()
    {
        if (hearts == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < hp)
            {
                hearts[i].enabled = true; // 남은 HP 만큼 하트 활성화
            }
            else
            {
                hearts[i].enabled = false; // 깎인 HP 하트 비활성화
            }
        }
    }

    private void GameOver()
    {
        Debug.Log("게임 오버!");

        // 1. 시간 멈춤
        Time.timeScale = 0f;

        // 2. 마우스 커서 잠금 해제 및 화면에 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}