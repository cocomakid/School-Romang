using UnityEngine;

public class MilkItem : MonoBehaviour
{
    public int itemType = 1; // 1: 우유, 2: 제티

    public GameObject itemPrefab;

    private Rigidbody rb;
    private Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void DropToGround()
    {
        if (rb != null)
        {
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        if (col != null) col.isTrigger = false;
    }

    public bool CanPickUp() => true;
}