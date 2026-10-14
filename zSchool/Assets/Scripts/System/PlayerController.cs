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

    [Header("Chalk Spawn Settings")]
    public Transform[] spawnPoints;

    [SerializeField] private Image[] chalkSlots = new Image[3];
    private int[] slotDamages = new int[3];

    // 분필 기본 스케일 (X:0.15, Y:0.2, Z:0.15)
    private readonly Vector3 baseChalkScale = new Vector3(0.15f, 0.2f, 0.15f);

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        hp = currentHp;
        UpdateHearts();

        // 시작 시 슬롯 비우기
        for (int i = 0; i < slotDamages.Length; i++)
        {
            slotDamages[i] = 0;
        }
        UpdateChalkUI();

        // 시작 시 맵에 분필 2개 스폰
        SpawnWorldChalks(2);
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();
    public void OnJump(InputValue value)
    {
        if (value.isPressed && controller.isGrounded) verticalVelocity = jumpPower;
    }

    // 💡 [카메라 설정 변수 추가]
    [Header("Camera Offset Settings")]
    public Vector3 cameraOffset = new Vector3(0.68f, 0f, -2.5f); // 숄더뷰 우측 오프셋 지정 (X: 0.68, Y: 0, Z: -2.5)

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            TakeDamage(1);
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ThrowChalk();
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickupNearbyChalk();
        }

        CheckAndRefillChalks();

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
        move = move * speed;
        move.y = verticalVelocity;

        // 💡 [숄더뷰 + 벽 충돌 로직]
        if (cameraPivot != null && cameraTranform != null)
        {
            // 숄더뷰 기본 위치 설정 (원하는 X축 오프셋 유지!)
            float defaultTargetZ = isRunning ? cameraOffset.z - 1.5f : cameraOffset.z;
            Vector3 targetLocalPos = new Vector3(cameraOffset.x, cameraOffset.y, defaultTargetZ);

            // cameraPivot을 기준으로 카메라의 목표 World 위치 계산
            Vector3 targetWorldPos = cameraPivot.TransformPoint(targetLocalPos);
            Vector3 pivotPos = cameraPivot.position;
            Vector3 dirToCam = (targetWorldPos - pivotPos).normalized;
            float maxDistance = Vector3.Distance(pivotPos, targetWorldPos);

            // Player 본인을 제외한 레이어 마스크
            int layerMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

            // SphereCast로 어깨 너비와 뒤쪽 벽을 동시에 감지
            if (Physics.SphereCast(pivotPos, 0.25f, dirToCam, out RaycastHit hit, maxDistance, layerMask))
            {
                // 벽에 걸리면 벽 앞으로 당겨진 좌표 계산
                Vector3 hitWorldPos = hit.point - dirToCam * 0.15f;
                targetLocalPos = cameraPivot.InverseTransformPoint(hitWorldPos);
            }

            // 카메라 위치 적용 (X축 숄더뷰 유지)
            Vector3 camPos = cameraTranform.localPosition;

            // 벽에 가깝게 당겨질 때는 즉시 반응, 원래대로 돌아갈 때는 부드럽게
            if (targetLocalPos.z > camPos.z)
            {
                camPos = targetLocalPos;
            }
            else
            {
                camPos = Vector3.Lerp(camPos, targetLocalPos, 20f * Time.deltaTime);
            }

            cameraTranform.localPosition = camPos;
        }

        controller.Move(move * Time.deltaTime);
    }

    public void SpawnWorldChalks(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int index = i % spawnPoints.Length;
                spawnPos = spawnPoints[index].position;
            }
            else
            {
                spawnPos = transform.position + transform.forward * (2f + i * 1.5f) + Vector3.up * 0.5f;
            }

            GameObject chalkObj = Instantiate(chalkPrefab, spawnPos, Quaternion.identity);
            Chalk chalk = chalkObj.GetComponent<Chalk>();

            if (chalk != null)
            {
                chalk.damage = 4;
                chalk.chalkPrefab = chalkPrefab;
                chalk.DropToGround();
            }

            chalkObj.transform.localScale = baseChalkScale;
        }
    }

    public void CheckAndRefillChalks()
    {
        for (int i = 0; i < slotDamages.Length; i++)
        {
            if (slotDamages[i] > 0) return;
        }

        Chalk[] activeChalks = FindObjectsByType<Chalk>(FindObjectsSortMode.None);

        if (activeChalks.Length == 0)
        {
            Debug.Log("모든 분필 소멸! 맵에 새 분필 2개를 스폰합니다.");
            SpawnWorldChalks(2);
        }
    }

    private void ThrowChalk()
    {
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
        slotDamages[targetSlot] = 0;
        UpdateChalkUI();

        Camera mainCam = Camera.main != null ? Camera.main : cameraPivot.GetComponentInChildren<Camera>();
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 50f) ? hit.point : ray.GetPoint(50f);

        // 💡 ThrowPoint가 있으면 사용하되, 플레이어 몸 앞쪽으로 안전 위치 잡기
        Vector3 spawnPos = throwPoint != null ? throwPoint.position : transform.position + transform.forward * 1.2f + transform.up * 1.2f;

        Vector3 targetDirection = (targetPoint - spawnPos).normalized;
        Quaternion spawnRot = Quaternion.LookRotation(targetDirection);

        GameObject chalkObj = Instantiate(chalkPrefab, spawnPos, spawnRot);
        Chalk chalk = chalkObj.GetComponent<Chalk>();

        if (chalk != null)
        {
            chalk.damage = chalkDmg;
            chalk.chalkPrefab = chalkPrefab;
            chalk.isThrown = true;
        }

        float scaleRatio = (float)chalkDmg / 4f;
        chalkObj.transform.localScale = baseChalkScale * scaleRatio;
    }

    private void TryPickupNearbyChalk()
    {
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

        if (nearestChalk != null)
        {
            slotDamages[emptySlot] = nearestChalk.damage;
            UpdateChalkUI();
            Destroy(nearestChalk.gameObject);
        }
    }

    private void UpdateChalkUI()
    {
        if (chalkSlots == null) return;

        for (int i = 0; i < chalkSlots.Length; i++)
        {
            if (chalkSlots[i] != null)
            {
                chalkSlots[i].enabled = (slotDamages[i] > 0);
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