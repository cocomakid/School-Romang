using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // 적 충돌 피해 및 무적 시간
    private bool isInvincible = false;
    public float invincibleDuration = 1f;
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
    [SerializeField] private Image[] chalkSlots = new Image[4];
    private int[] slotDamages = new int[4];
    private readonly Vector3 baseChalkScale = new Vector3(0.15f, 0.2f, 0.15f);

    // ==========================================
    // 🎒 [인벤토리 및 장착 시스템 설정]
    // ==========================================
    [Header("Inventory & Equipment Settings")]
    private int selectedSlotIndex = 0; // 0: 1번칸, 1: 2번칸, 2: 3번칸, 3: 4번칸
    public GameObject swordObject;     // 검 오브젝트
    public GameObject shieldObject;    // 방패 오브젝트
    public GameObject chalkInHandObject; // 🖐️ 손에 들리는 분필 오브젝트 (플레이어 손 자식으로 두고 평소엔 비활성화)

    [Header("Camera Offset Settings")]
    public Vector3 cameraOffset = new Vector3(0.68f, 0f, -2.5f);

    // ==========================================
    // ⚔️ [검 & 방패 전투 시스템 설정]
    // ==========================================
    [Header("Weapon & Shield Settings")]
    public Transform swordTransform;
    public Transform rightHandPoint;        // 오른손 검 위치
    public Transform leftHandPoint;         // 왼손 방패 위치
    public float swordAttackRange = 2.5f;   // 검 사거리
    public int swordDamage = 3;             // 검 데미지
    public float attackCooldown = 1.0f;     // 다음 공격까지의 쿨타임
    public float attackDelay = 1.0f;        // 검 휘두르기 딜레이 (1초)
    private float lastAttackTime;
    private bool isAttacking = false;       // 공격 중 여부

    [Header("Guard State")]
    public bool isBlocking = false;         // 현재 방어 상태 여부
    [Range(0f, 1f)]
    public float guardDamageReduction = 1.0f; // 방어 시 데미지 감쇄율 (1.0 = 100% 무효화)


    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Monster") && !isInvincible)
        {
            TakeDamage(1);
            StartCoroutine(InvincibilityCoroutine());
        }
    }

    private System.Collections.IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleDuration);
        isInvincible = false;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        hp = currentHp;
        UpdateHearts();

        for (int i = 0; i < slotDamages.Length; i++)
        {
            slotDamages[i] = 0;
        }
        UpdateChalkUI();
        SpawnWorldChalks(2);

        // 시작할 때 슬롯 1번 선택 상태 적용
        SelectSlot(0);
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();
    public void OnJump(InputValue value)
    {
        if (value.isPressed && controller.isGrounded) verticalVelocity = jumpPower;
    }

    void Update()
    {
        // 🛑 [핵심] 일시정지 상태(설정창 열림 등)일 때는 플레이어 조작 및 시점 회전을 완전히 차단
        if (Time.timeScale == 0f) return;

        // 🔢 [키보드 1, 2, 3, 4번 슬롯 선택 입력]
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) SelectSlot(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) SelectSlot(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) SelectSlot(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame) SelectSlot(3);
        }

        // ⚔️ [마우스 좌클릭 : 검 공격 (1번 슬롯이고 검이 장착되어 있을 때만)]
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (selectedSlotIndex == 0)
            {
                if (!isAttacking && !isBlocking && Time.time >= lastAttackTime + attackCooldown)
                {
                    StartCoroutine(SwordAttackRoutine());
                }
            }
            else
            {
                // 분필 슬롯일 때 마우스 좌클릭 시 분필 던지기 로직을 여기에 연동할 수 있습니다.
                if (slotDamages[selectedSlotIndex] > 0)
                {
                    ThrowChalk(slotDamages[selectedSlotIndex]); // 분필 던지기 함수 호출

                    // 인벤토리 슬롯 비우기
                    slotDamages[selectedSlotIndex] = 0;
                    UpdateChalkUI(); // UI 갱신
                    SelectSlot(selectedSlotIndex); // 손에 든 분필 숨기기 갱신
                }
                else
                {
                    Debug.Log("⚠️ 비어있는 슬롯입니다!");
                }
            }
        }

        // 🛡️ [마우스 우클릭 : 방어 (1번 슬롯일 때만)]
        if (Mouse.current != null)
        {
            isBlocking = (selectedSlotIndex == 0) && Mouse.current.rightButton.isPressed;
        }

        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            TakeDamage(1);
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

        // 이동 계산
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;

        if (Keyboard.current != null) isRunning = Keyboard.current.leftShiftKey.isPressed;

        float speed = isRunning ? moveSpeed * 2f : moveSpeed;
        if (isBlocking) speed *= 0.5f;
        if (isAttacking) speed *= 0.3f;

        move = move * speed;
        move.y = verticalVelocity;

        // 카메라 처리
        if (cameraPivot != null && cameraTranform != null)
        {
            float defaultTargetZ = isRunning ? cameraOffset.z - 1.5f : cameraOffset.z;
            Vector3 targetLocalPos = new Vector3(cameraOffset.x, cameraOffset.y, defaultTargetZ);

            Vector3 targetWorldPos = cameraPivot.TransformPoint(targetLocalPos);
            Vector3 pivotPos = cameraPivot.position;
            Vector3 dirToCam = (targetWorldPos - pivotPos).normalized;
            float maxDistance = Vector3.Distance(pivotPos, targetWorldPos);

            int layerMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

            if (Physics.SphereCast(pivotPos, 0.25f, dirToCam, out RaycastHit hit, maxDistance, layerMask))
            {
                Vector3 hitWorldPos = hit.point - dirToCam * 0.15f;
                targetLocalPos = cameraPivot.InverseTransformPoint(hitWorldPos);
            }

            Vector3 camPos = cameraTranform.localPosition;
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

    private void ThrowChalk(int damage)
    {
        if (chalkPrefab == null) return;

        // 1. 발사 위치(ThrowPoint) 설정 (없으면 플레이어 전방에 생성)
        Vector3 spawnPos = (throwPoint != null) ? throwPoint.position : transform.position + transform.forward + Vector3.up * 1f;

        // 카메라가 바라보는 방향으로 회전값 맞추기
        Quaternion spawnRot = (Camera.main != null) ? Camera.main.transform.rotation : transform.rotation;

        // 2. 분필 프리팹 생성 (Chalk 스크립트의 Start()에서 자동으로 isThrown = true 처리가 됨)
        GameObject thrownChalkObj = Instantiate(chalkPrefab, spawnPos, spawnRot);

        // 3. Chalk 컴포넌트에 데미지 설정
        Chalk chalkScript = thrownChalkObj.GetComponent<Chalk>();
        if (chalkScript != null)
        {
            chalkScript.damage = damage;
            chalkScript.chalkPrefab = chalkPrefab;
            chalkScript.isThrown = true; // 던져진 상태 명시
        }
    }

    // 🎒 [슬롯 선택 및 아이템 표시/숨김 처리]
    private void SelectSlot(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
        Debug.Log($"🎒 {slotIndex + 1}번 슬롯 선택됨");

        bool isFirstSlot = (selectedSlotIndex == 0);
        bool hasChalkInSlot = (slotDamages[selectedSlotIndex] > 0);

        // 1. 1번 슬롯일 때: 검과 방패 표시, 분필 숨김
        if (swordTransform != null) swordTransform.gameObject.SetActive(isFirstSlot);
        if (swordObject != null) swordObject.SetActive(isFirstSlot);
        if (shieldObject != null) shieldObject.SetActive(isFirstSlot);

        // 2. 2, 3, 4번 슬롯일 때: 검/방패 숨기고, 분필을 가지고 있다면 손에 분필 표시, 없으면 숨김
        if (chalkInHandObject != null)
        {
            // 1번 칸이 아니고, 해당 칸에 분필 데이터가 있으면 손에 분필 활성화
            chalkInHandObject.SetActive(!isFirstSlot && hasChalkInSlot);
        }

        if (!isFirstSlot)
        {
            isBlocking = false; // 다른 슬롯으로 바꾸면 방어 해제
        }
    }

    // ⚔️ [검 공격 코루틴]
    private System.Collections.IEnumerator SwordAttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (swordTransform != null)
        {
            StartCoroutine(SwordSwingMotion());
        }

        Vector3 attackOrigin = transform.position + Vector3.up * 1.0f;
        Vector3 attackDir = transform.forward;
        RaycastHit[] hits = Physics.SphereCastAll(attackOrigin, 0.8f, attackDir, swordAttackRange);

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Monster"))
            {
                Monster monster = hit.collider.GetComponent<Monster>();
                if (monster != null) monster.TakeDamage(swordDamage);

                RangedMonster rangedMonster = hit.collider.GetComponent<RangedMonster>();
                if (rangedMonster != null) rangedMonster.TakeDamage(swordDamage);
            }
        }

        yield return new WaitForSeconds(attackDelay);
        isAttacking = false;
    }

    private System.Collections.IEnumerator SwordSwingMotion()
    {
        Vector3 originalPos = swordTransform.localPosition;
        Quaternion originalRot = swordTransform.localRotation;

        Vector3 targetPos = originalPos + new Vector3(0.2f, -0.1f, 0.6f);
        Quaternion targetRot = originalRot * Quaternion.Euler(20f, -40f, -30f);

        float elapsed = 0f;
        float swingDuration = 0.15f;
        float returnDuration = .25f;

        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swingDuration;
            swordTransform.localPosition = Vector3.Lerp(originalPos, targetPos, t);
            swordTransform.localRotation = Quaternion.Slerp(originalRot, targetRot, t);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            swordTransform.localPosition = Vector3.Lerp(targetPos, originalPos, t);
            swordTransform.localRotation = Quaternion.Slerp(targetRot, originalRot, t);
            yield return null;
        }

        swordTransform.localPosition = originalPos;
        swordTransform.localRotation = originalRot;
    }

    public void TakeDamage(int damage)
    {
        if (hp <= 0 || isInvincible) return;

        if (isBlocking)
        {
            int blockedDamage = Mathf.RoundToInt(damage * (1f - guardDamageReduction));
            Debug.Log($"🛡️ 방패로 막았습니다! 받은 데미지: {blockedDamage}");

            if (blockedDamage <= 0) return;
            damage = blockedDamage;
        }

        hp -= damage;
        currentHp = hp;
        UpdateHearts();

        if (hp <= 0) GameOver();
    }

    public void SpawnWorldChalks(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = (spawnPoints != null && spawnPoints.Length > 0)
                ? spawnPoints[i % spawnPoints.Length].position
                : transform.position + transform.forward * (2f + i * 1.5f) + Vector3.up * 0.5f;

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
        for (int i = 0; i < slotDamages.Length; i++) if (slotDamages[i] > 0) return;

        Chalk[] activeChalks = FindObjectsByType<Chalk>(FindObjectsSortMode.None);
        if (activeChalks.Length == 0) SpawnWorldChalks(2);
    }

    private void TryPickupNearbyChalk()
    {
        // 2, 3, 4번 슬롯(인덱스 1, 2, 3) 중에서 빈 슬롯 탐색
        int emptySlot = -1;
        for (int i = 1; i < slotDamages.Length; i++)
        {
            if (slotDamages[i] == 0) { emptySlot = i; break; }
        }
        if (emptySlot == -1) return;

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

            // 만약 현재 방금 주워서 채운 슬롯을 보고 있었다면 손에 분필 즉시 반영
            if (selectedSlotIndex == emptySlot)
            {
                SelectSlot(selectedSlotIndex);
            }

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
                if (i == 0)
                {
                    chalkSlots[i].enabled = true; // 1번 칸은 항상 켜둠 (검&방패 아이콘)
                }
                else
                {
                    chalkSlots[i].enabled = (slotDamages[i] > 0); // 분필이 있을 때만 UI 켜기
                }
            }
        }
    }

    private void UpdateHearts()
    {
        if (hearts == null) return;
        for (int i = 0; i < hearts.Length; i++) hearts[i].enabled = (i < hp);
    }

    private void GameOver()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
    }
}