using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

    [Header("Hp UI")]
    [SerializeField] private Image[] hearts;
    private int hp = 4;
    public static int currentHp = 4;

    [Header("Chalk System & UI")]
    public GameObject chalkPrefab;
    public Transform throwPoint;
    public float pickupRadius = 3.0f;

    // 💡 화면의 네모박스 Image 3개를 인스펙터 창에서 연결
    [SerializeField] private Image[] chalkSlots = new Image[3];

    // 슬롯 3개에 들어있는 분필의 데미지 저장 (0: 비어있음, 4, 2, 1)
    private int[] slotDamages = new int[3];

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        hp = currentHp;
        UpdateHearts();

        // 시작 시 기본적으로 4짜리 분필 2개 지급 예시
        slotDamages[0] = 4;
        slotDamages[1] = 4;
        slotDamages[2] = 0; // 3번째 슬롯은 비어있음

        UpdateChalkUI();
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();
    public void OnJump(InputValue value)
    {
        if (value.isPressed && controller.isGrounded) verticalVelocity = jumpPower;
    }

    void Update()
    {
        // K키 데미지 테스트
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            TakeDamage(1);
        }

        // 분필 던지기 (좌클릭) & 줍기 (E키)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ThrowChalk();
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickupNearbyChalk();
        }

        // 시선 회전
        transform.Rotate(0f, lookInput.x * mouseSensitivity, 0f);
        pitch = pitch - lookInput.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -20f, 60f);
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);

        // 이동
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;

        if (Keyboard.current != null) isRunning = Keyboard.current.leftShiftKey.isPressed;

        float speed = isRunning ? moveSpeed * 2f : moveSpeed;
        float targetZ = isRunning ? -5f : -3f;

        move = move * speed;
        move.y = verticalVelocity;

        Vector3 camPos = cameraTranform.localPosition;
        camPos.z = Mathf.Lerp(camPos.z, targetZ, 5f * Time.deltaTime);
        cameraTranform.localPosition = camPos;

        controller.Move(move * Time.deltaTime);
    }

    // ----------------------------------------------------
    // 분필 던지기 (마지막 슬롯부터 꺼내서 발사)
    // ----------------------------------------------------
    private void ThrowChalk()
    {
        // 꽉 차있는 마지막 슬롯 인덱스 찾기
        int targetSlot = -1;
        for (int i = slotDamages.Length - 1; i >= 0; i--)
        {
            if (slotDamages[i] > 0)
            {
                targetSlot = i;
                break;
            }
        }

        if (targetSlot == -1)
        {
            Debug.Log("슬롯에 분필이 없습니다!");
            return;
        }

        int chalkDmg = slotDamages[targetSlot];
        slotDamages[targetSlot] = 0; // 해당 슬롯 비우기
        UpdateChalkUI();             // UI 갱신

        // 분필 생성
        Vector3 spawnPos = throwPoint != null ? throwPoint.position : cameraPivot.position + cameraPivot.forward;
        Quaternion spawnRot = cameraPivot.rotation;

        GameObject chalkObj = Instantiate(chalkPrefab, spawnPos, spawnRot);
        Chalk chalk = chalkObj.GetComponent<Chalk>();

        chalk.damage = chalkDmg;
        chalk.chalkPrefab = chalkPrefab;

        float scaleRatio = (float)chalkDmg / 4f;
        chalkObj.transform.localScale = Vector3.one * Mathf.Max(scaleRatio, 0.4f);
    }

    // ----------------------------------------------------
    // 주변 분필 줍기 (비어있는 첫 번째 슬롯으로 들어옴)
    // ----------------------------------------------------
    private void TryPickupNearbyChalk()
    {
        // 비어있는 슬롯 인덱스 찾기
        int emptySlot = -1;
        for (int i = 0; i < slotDamages.Length; i++)
        {
            if (slotDamages[i] == 0)
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot == -1)
        {
            Debug.Log("슬롯 3개가 모두 가득 찼습니다!");
            return;
        }

        // 주변 분필 탐색
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRadius);
        Chalk nearestChalk = null;
        float nearestDistance = float.MaxValue;

        foreach (var col in hitColliders)
        {
            Chalk chalk = col.GetComponent<Chalk>();
            if (chalk != null && chalk.CanPickUp())
            {
                float dist = Vector3.Distance(transform.position, chalk.transform.position);
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearestChalk = chalk;
                }
            }
        }

        // 줍기 성공 시
        if (nearestChalk != null)
        {
            slotDamages[emptySlot] = nearestChalk.damage; // 비어있는 슬롯에 저장
            UpdateChalkUI();                               // UI 갱신
            Destroy(nearestChalk.gameObject);
        }
    }

    // UI 이미지 3개 갱신 함수
    private void UpdateChalkUI()
    {
        if (chalkSlots == null) return;

        for (int i = 0; i < chalkSlots.Length; i++)
        {
            if (chalkSlots[i] != null)
            {
                // 슬롯에 데미지 값이 남아있으면 UI 아이콘 켜고, 0이면 끔
                bool hasChalk = slotDamages[i] > 0;
                chalkSlots[i].enabled = hasChalk;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (hp <= 0) return;
        hp -= damage;
        currentHp = hp;
        UpdateHearts();
        if (hp <= 0) GameOver();
    }

    private void UpdateHearts()
    {
        if (hearts == null) return;
        for (int i = 0; i < hearts.Length; i++) hearts[i].enabled = (i < hp);
    }

    private void GameOver()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}