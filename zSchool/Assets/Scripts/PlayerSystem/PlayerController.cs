using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
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
    private int maxHp = 4;

    [Header("Chalk System & UI (분필 시스템)")]
    public GameObject chalkPrefab;
    public Transform throwPoint;
    public float pickupRadius = 3.0f;
    public Transform[] spawnPoints; // 분필 전용 스폰 포인트
    [SerializeField] private Image[] chalkSlots = new Image[4];
    private int[] slotDamages = new int[4];
    private readonly Vector3 baseChalkScale = new Vector3(0.15f, 0.2f, 0.15f);

    [Header("Milk & Jety System (우유/제티 시스템)")]
    public GameObject milkPrefab;
    public GameObject jetyPrefab;
    public Transform[] milkSpawnPoints; // 우유 전용 스폰 포인트
    public Transform[] jetySpawnPoints; // 제티 전용 스폰 포인트

    [Header("Inventory UI & Icons (인벤토리 및 아이템 이미지)")]
    [SerializeField] private Image[] slotIcons = new Image[4];
    [SerializeField] private Sprite milkIconSprite;
    [SerializeField] private Sprite jetyIconSprite;
    [SerializeField] private Sprite chocoMilkIconSprite;
    private int[] slotItemTypes = new int[4]; // 0: 빈칸, 1: 우유, 2: 제티, 3: 초코우유

    [Header("Equipment Settings")]
    private int selectedSlotIndex = 0;
    public GameObject swordObject;
    public GameObject shieldObject;
    public GameObject milkInHandObject;
    public GameObject jetyInHandObject;
    public GameObject chocoMilkInHandObject;

    [Header("Camera Offset Settings")]
    public Vector3 cameraOffset = new Vector3(0.68f, 0f, -2.5f);

    [Header("Weapon & Shield Settings")]
    public Transform swordTransform;
    public float swordAttackRange = 2.5f;
    public int swordDamage = 3;
    public float attackCooldown = 1.0f;
    public float attackDelay = 1.0f;
    private float lastAttackTime;
    private bool isAttacking = false;

    [Header("Guard State")]
    public bool isBlocking = false;
    [Range(0f, 1f)]
    public float guardDamageReduction = 1.0f;

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

        currentHp = 4;
        hp = currentHp;
        UpdateHearts();

        for (int i = 0; i < slotDamages.Length; i++) slotDamages[i] = 0;
        for (int i = 0; i < slotItemTypes.Length; i++) slotItemTypes[i] = 0;

        UpdateChalkUI();
        UpdateInventoryUI();

        SpawnWorldChalks(2);
        SpawnInitialItems();

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
        if (Time.timeScale == 0f) return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) SelectSlot(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) SelectSlot(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) SelectSlot(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame) SelectSlot(3);
        }

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryCraftChocoMilk();
        }

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
                if (slotDamages[selectedSlotIndex] > 0)
                {
                    ThrowChalk(slotDamages[selectedSlotIndex]);
                    slotDamages[selectedSlotIndex] = 0;
                    UpdateChalkUI();
                    SelectSlot(selectedSlotIndex);
                }
                else
                {
                    UseSelectedItem();
                }
            }
        }

        if (Mouse.current != null)
        {
            isBlocking = (selectedSlotIndex == 0) && Mouse.current.rightButton.isPressed;
        }

        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame) TakeDamage(1);
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickupNearbyChalk();
            TryPickupItem();
        }

        CheckAndRefillChalks();

        transform.Rotate(0f, lookInput.x * mouseSensitivity, 0f);
        pitch = Mathf.Clamp(pitch - lookInput.y * mouseSensitivity, -20f, 60f);
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;
        if (Keyboard.current != null) isRunning = Keyboard.current.leftShiftKey.isPressed;

        float speed = isRunning ? moveSpeed * 2f : moveSpeed;
        if (isBlocking) speed *= 0.5f;
        if (isAttacking) speed *= 0.3f;

        move = move * speed;
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }

    // 🖍️ 분필 스폰 및 관리
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
                chalk.chalkPrefab = chalkPrefab; // 👈 정상 반영
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
            if (selectedSlotIndex == emptySlot) SelectSlot(selectedSlotIndex);
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
                if (i == 0) chalkSlots[i].enabled = true;
                else chalkSlots[i].enabled = (slotDamages[i] > 0);
            }
        }
    }

    private void ThrowChalk(int damage)
    {
        if (chalkPrefab == null) return;
        Vector3 spawnPos = (throwPoint != null) ? throwPoint.position : transform.position + transform.forward + Vector3.up * 1f;
        Quaternion spawnRot = (Camera.main != null) ? Camera.main.transform.rotation : transform.rotation;

        GameObject thrownChalkObj = Instantiate(chalkPrefab, spawnPos, spawnRot);
        Chalk chalkScript = thrownChalkObj.GetComponent<Chalk>();
        if (chalkScript != null)
        {
            chalkScript.damage = damage;
            chalkScript.chalkPrefab = chalkPrefab; // 👈 정상 반영
            chalkScript.isThrown = true;
        }
    }

    // 🥛 우유 & 제티 초기 스폰 함수
    private void SpawnInitialItems()
    {
        if (milkPrefab != null && milkSpawnPoints != null && milkSpawnPoints.Length > 0)
        {
            Instantiate(milkPrefab, milkSpawnPoints[0].position, Quaternion.identity).GetComponent<MilkItem>()?.DropToGround();
        }
        if (jetyPrefab != null && jetySpawnPoints != null && jetySpawnPoints.Length > 0)
        {
            Instantiate(jetyPrefab, jetySpawnPoints[0].position, Quaternion.identity).GetComponent<MilkItem>()?.DropToGround();
        }
    }

    // 🥛 우유/제티 아이템 사용 로직
    private void UseSelectedItem()
    {
        int currentItem = slotItemTypes[selectedSlotIndex];
        if (currentItem == 0) return;

        if (currentItem == 1) // 우유
        {
            maxHp += 1;
            hp = Mathf.Min(hp + 1, maxHp);
            UpdateHearts();
            Debug.Log("🥛 우유를 마셨다! 체력이 1 늘어났다.");
            RespawnItemAfterDelay(1, 12f);
        }
        else if (currentItem == 2) // 제티
        {
            Debug.Log("🍫 제티를 먹었다! 그냥 맛있었다.");
            RespawnItemAfterDelay(2, 12f);
        }
        else if (currentItem == 3) // 초코우유
        {
            hp = Mathf.Min(hp + 2, maxHp);
            UpdateHearts();
            Debug.Log("✨ 초코우유를 마셨다! 체력이 2 회복되었다.");
            RespawnItemAfterDelay(1, 12f);
            RespawnItemAfterDelay(2, 12f);
        }

        slotItemTypes[selectedSlotIndex] = 0;
        UpdateInventoryUI();
        SelectSlot(selectedSlotIndex);
    }

    private void TryCraftChocoMilk()
    {
        bool hasMilk = false;
        bool hasJety = false;
        int milkIndex = -1;
        int jetyIndex = -1;

        for (int i = 1; i < slotItemTypes.Length; i++)
        {
            if (slotItemTypes[i] == 1) { hasMilk = true; milkIndex = i; }
            if (slotItemTypes[i] == 2) { hasJety = true; jetyIndex = i; }
        }

        if (hasMilk && hasJety)
        {
            slotItemTypes[milkIndex] = 0;
            slotItemTypes[jetyIndex] = 3;

            RespawnItemAfterDelay(1, 12f);
            RespawnItemAfterDelay(2, 12f);

            UpdateInventoryUI();
            SelectSlot(selectedSlotIndex);
            Debug.Log("🎉 우유와 제티를 합쳐 '초코우유'를 만들었다!");
        }
        else
        {
            Debug.Log("⚠️ 인벤토리에 우유와 제티가 모두 있어야 조합할 수 있습니다.");
        }
    }

    public void RespawnItemAfterDelay(int itemType, float delay)
    {
        StartCoroutine(ItemRespawnRoutine(itemType, delay));
    }

    private System.Collections.IEnumerator ItemRespawnRoutine(int itemType, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        GameObject prefabToSpawn = null;
        Vector3 spawnPos = Vector3.zero;

        if (itemType == 1 && milkPrefab != null)
        {
            prefabToSpawn = milkPrefab;
            spawnPos = (milkSpawnPoints != null && milkSpawnPoints.Length > 0)
                ? milkSpawnPoints[Random.Range(0, milkSpawnPoints.Length)].position
                : transform.position + transform.forward * 3f + Vector3.up * 0.5f;
        }
        else if (itemType == 2 && jetyPrefab != null)
        {
            prefabToSpawn = jetyPrefab;
            spawnPos = (jetySpawnPoints != null && jetySpawnPoints.Length > 0)
                ? jetySpawnPoints[Random.Range(0, jetySpawnPoints.Length)].position
                : transform.position - transform.forward * 3f + Vector3.up * 0.5f;
        }

        if (prefabToSpawn != null)
        {
            GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            MilkItem milkScript = spawnedObj.GetComponent<MilkItem>();
            if (milkScript != null) milkScript.DropToGround();
            Debug.Log($"⏰ 12초가 지나 아이템(타입: {itemType})이 전용 스폰 위치에 리스폰되었습니다!");
        }
    }

    private void TryPickupItem()
    {
        int emptySlot = -1;
        for (int i = 1; i < slotItemTypes.Length; i++)
        {
            if (slotItemTypes[i] == 0) { emptySlot = i; break; }
        }
        if (emptySlot == -1) return;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRadius);
        foreach (var col in hitColliders)
        {
            MilkItem item = col.GetComponent<MilkItem>();
            if (item != null && item.CanPickUp())
            {
                slotItemTypes[emptySlot] = item.itemType;
                UpdateInventoryUI();
                if (selectedSlotIndex == emptySlot) SelectSlot(selectedSlotIndex);
                Destroy(col.gameObject);
                break;
            }
        }
    }

    private void SelectSlot(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
        bool isFirstSlot = (selectedSlotIndex == 0);
        int itemType = slotItemTypes[selectedSlotIndex];

        if (swordTransform != null) swordTransform.gameObject.SetActive(isFirstSlot);
        if (swordObject != null) swordObject.SetActive(isFirstSlot);
        if (shieldObject != null) shieldObject.SetActive(isFirstSlot);

        if (milkInHandObject) milkInHandObject.SetActive(!isFirstSlot && itemType == 1);
        if (jetyInHandObject) jetyInHandObject.SetActive(!isFirstSlot && itemType == 2);
        if (chocoMilkInHandObject) chocoMilkInHandObject.SetActive(!isFirstSlot && itemType == 3);

        if (!isFirstSlot) isBlocking = false;
    }

    private void UpdateInventoryUI()
    {
        if (slotIcons == null) return;
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotIcons[i] != null)
            {
                if (i == 0)
                {
                    slotIcons[i].enabled = true;
                }
                else
                {
                    int itemType = slotItemTypes[i];
                    if (itemType == 0)
                    {
                        slotIcons[i].enabled = false;
                        slotIcons[i].sprite = null;
                    }
                    else
                    {
                        slotIcons[i].enabled = true;
                        if (itemType == 1 && milkIconSprite != null) slotIcons[i].sprite = milkIconSprite;
                        else if (itemType == 2 && jetyIconSprite != null) slotIcons[i].sprite = jetyIconSprite;
                        else if (itemType == 3 && chocoMilkIconSprite != null) slotIcons[i].sprite = chocoMilkIconSprite;
                    }
                }
            }
        }
    }

    private System.Collections.IEnumerator SwordAttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        if (swordTransform != null) StartCoroutine(SwordSwingMotion());

        Vector3 attackOrigin = transform.position + Vector3.up * 1.0f;
        RaycastHit[] hits = Physics.SphereCastAll(attackOrigin, 0.8f, transform.forward, swordAttackRange);

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Monster"))
            {
                Monster monster = hit.collider.GetComponent<Monster>();
                if (monster != null) monster.TakeDamage(swordDamage);
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
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            swordTransform.localPosition = Vector3.Lerp(originalPos, targetPos, elapsed / 0.15f);
            swordTransform.localRotation = Quaternion.Slerp(originalRot, targetRot, elapsed / 0.15f);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.deltaTime;
            swordTransform.localPosition = Vector3.Lerp(targetPos, originalPos, elapsed / 0.25f);
            swordTransform.localRotation = Quaternion.Slerp(targetRot, originalRot, elapsed / 0.25f);
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
            if (blockedDamage <= 0) return;
            damage = blockedDamage;
        }
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