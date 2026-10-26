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
    [SerializeField] private Image[] chalkSlots = new Image[3];
    private int[] slotDamages = new int[3];
    private readonly Vector3 baseChalkScale = new Vector3(0.15f, 0.2f, 0.15f);

    [Header("Camera Offset Settings")]
    public Vector3 cameraOffset = new Vector3(0.68f, 0f, -2.5f);

    // ==========================================
    // ⚔️ [검 & 방패 전투 시스템 설정]
    // ==========================================
    [Header("Weapon & Shield Settings")]
    public Transform swordTransform;
    public Transform rightHandPoint;       // 오른손 검 위치
    public Transform leftHandPoint;        // 왼손 방패 위치
    public float swordAttackRange = 2.5f;   // 검 사거리
    public int swordDamage = 3;            // 검 데미지
    public float attackCooldown = 1.0f;    // 다음 공격까지의 쿨타임
    public float attackDelay = 1.0f;       // 검 휘두르기 딜레이 (1초)
    private float lastAttackTime;
    private bool isAttacking = false;      // 공격 중 여부

    [Header("Guard State")]
    public bool isBlocking = false;        // 현재 방어 상태 여부
    [Range(0f, 1f)]
    public float guardDamageReduction = 1.0f; // 방어 시 데미지 감쇄율 (1.0 = 100% 무효화)


    // 적과 피지컬 충돌 시 (선택사항)
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
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();
    public void OnJump(InputValue value)
    {
        if (value.isPressed && controller.isGrounded) verticalVelocity = jumpPower;
    }

    void Update()
    {
        // ⚔️ [마우스 좌클릭 : 검 휘두르기 공격 (방패 막기 중이거나 공격 중일 때는 불가)]
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!isAttacking && !isBlocking && Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(SwordAttackRoutine());   
            }
            else if (isBlocking)
            {
                Debug.Log("🛡️ 방패로 막는 중에는 공격할 수 없습니다!");
            }
            else if (isAttacking)
            {
                Debug.Log("⚔️ 검을 휘두르는 중입니다!");
            }
        }

        // 🛡️ [마우스 우클릭 : 방패 방어 (누르는 동안 가드)]
        if (Mouse.current != null)
        {
            isBlocking = Mouse.current.rightButton.isPressed;
        }

        // 기타 키보드 조작
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
        if (isBlocking) speed *= 0.5f;   // 방어 중 이동속도 50% 감속
        if (isAttacking) speed *= 0.3f;  // 공격 딜레이 중 이동속도 70% 감속

        move = move * speed;
        move.y = verticalVelocity;

        // 카메라 숄더뷰 및 벽 충돌 감지
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

    // ⚔️ [검 공격 코루틴 (딜레이 처리)]
    // ⚔️ [검 공격 코루틴]
    private System.Collections.IEnumerator SwordAttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // 1. 검을 앞으로 찔러/휘둘렀다 돌아오는 찌르기 모션 연출 시작
        if (swordTransform != null)
        {
            StartCoroutine(SwordSwingMotion());
        }

        // 2. 적 피격 판정
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

        // 3. 공격 후딜레이 대기
        yield return new WaitForSeconds(attackDelay);

        isAttacking = false;
    }

    // 🗡️ [간이 검 휘두르기/찌르기 모션 (앞으로 이동 및 회전 후 원위치)]
    private System.Collections.IEnumerator SwordSwingMotion()
    {
        Vector3 originalPos = swordTransform.localPosition;
        Quaternion originalRot = swordTransform.localRotation;

        // 앞으로 전진 및 살짝 찌르듯 회전할 목표 Transform
        Vector3 targetPos = originalPos + new Vector3(0.2f, -0.1f, 0.6f);
        Quaternion targetRot = originalRot * Quaternion.Euler(20f, -40f, -30f);

        float elapsed = 0f;
        float swingDuration = 0.15f; // 앞으로 나가는 시간 (0.15초)
        float returnDuration = 0.25f; // 다시 돌아오는 시간 (0.25초)

        // 1. 앞으로 휘두르며/찌르며 나아가기 (Fast Forward)
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swingDuration;
            swordTransform.localPosition = Vector3.Lerp(originalPos, targetPos, t);
            swordTransform.localRotation = Quaternion.Slerp(originalRot, targetRot, t);
            yield return null;
        }

        elapsed = 0f;

        // 2. 원래 위치와 회전으로 부드럽게 복귀 (Smooth Return)
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            swordTransform.localPosition = Vector3.Lerp(targetPos, originalPos, t);
            swordTransform.localRotation = Quaternion.Slerp(targetRot, originalRot, t);
            yield return null;
        }

        // 위치 오차 방지를 위해 최종 좌표 보정
        swordTransform.localPosition = originalPos;
        swordTransform.localRotation = originalRot;
    }

    // 🛡️ [데미지 피격 - 방어 시 무효화 적용]
    public void TakeDamage(int damage)
    {
        if (hp <= 0 || isInvincible) return;

        // 방패로 막는 중이면 데미지 차감 방지
        if (isBlocking)
        {
            int blockedDamage = Mathf.RoundToInt(damage * (1f - guardDamageReduction));
            Debug.Log($"🛡️ 방패로 막았습니다! 받은 데미지: {blockedDamage}");

            if (blockedDamage <= 0) return; // 완전 가드 시 리턴
            damage = blockedDamage;
        }

        hp -= damage;
        currentHp = hp;
        UpdateHearts();

        if (hp <= 0) GameOver();
    }

    // 분필 생성 및 줍기 시스템 유지
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
        int emptySlot = -1;
        for (int i = 0; i < slotDamages.Length; i++)
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
            Destroy(nearestChalk.gameObject);
        }
    }

    private void UpdateChalkUI()
    {
        if (chalkSlots == null) return;
        for (int i = 0; i < chalkSlots.Length; i++)
        {
            if (chalkSlots[i] != null) chalkSlots[i].enabled = (slotDamages[i] > 0);
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