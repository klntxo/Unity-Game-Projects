using System.Collections;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    public float fuseTime = 3f;//time before grenade explodes
    public float throwForce = 15f; //force thrown
    public GameObject grenadePrefab;   // Throwable prefab reference
    public GameObject explosionPrefab; // prefab

    public baseWeapon weapon;          // Reference to baseWeapon script

    private SphereCollider col; //collider for explosion radius
    private bool exploded = false;

    private void Start()
    {
        if (weapon == null)
        {
            weapon = GetComponent<baseWeapon>(); //auto-assign weapon stats just in case
        }
    }

    public void FireGrenade(Transform shootingPoint)
    {
        if (weapon != null && weapon.throwSound != null)
        {
            AudioSource.PlayClipAtPoint(weapon.throwSound, shootingPoint.position, 1.0f);
        }

        if (grenadePrefab == null) return;

        // 1. Spawn the world grenade projectile
        GameObject grenadeObj = Instantiate(grenadePrefab, shootingPoint.position, shootingPoint.rotation);
        grenadeObj.SetActive(true);


        // Ensure the thrown projectile has solid collision so it hits the ground
        Collider grenadeCol = grenadeObj.GetComponent<Collider>();
        if (grenadeCol != null)
        {
            grenadeCol.isTrigger = false;
        }

        // Prevent thrown grenade from colliding with/stuck inside the player on release
        Collider playerCol = GetComponentInParent<Collider>();
        if (playerCol != null && grenadeCol != null)
        {
            Physics.IgnoreCollision(playerCol, grenadeCol);
        }

        // Activate physics movement on the thrown projectile
        Rigidbody rb = grenadeObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearDamping = 1.2f;  // Air and ground movement resistance (slows down till reach ground using gravity)
            rb.angularDamping = 4.0f; // Friction against rolling on the ground (slows down to a stop, rotating)
            rb.AddForce(Camera.main.transform.forward * throwForce, ForceMode.VelocityChange);
        }

        // Transfer weapon data and start fuse timer on the thrown instance
        Grenade spawnedGrenade = grenadeObj.GetComponent<Grenade>();
        if (spawnedGrenade != null)
        {
            spawnedGrenade.weapon = weapon;
            spawnedGrenade.fuseTime = fuseTime;
            spawnedGrenade.explosionPrefab = explosionPrefab;
            spawnedGrenade.StartCoroutine(spawnedGrenade.Fuse());
        }

        //weapon in player hand
        gameObject.SetActive(false);
    }

    private IEnumerator Fuse()
    {
        yield return new WaitForSeconds(fuseTime); //wait fuse time
        Explode();
    }

    private void Explode()
    {
        exploded = true;

        // Play sound
        if (weapon != null && weapon.explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(weapon.explosionSound, transform.position, 1.0f);
        }

        // Camera Shake
        if (CameraShake.Instance != null && Camera.main != null)
        {
            float distance = Vector3.Distance(transform.position, Camera.main.transform.position);
            float maxDistance = 20f;

            if (distance < maxDistance)
            {
                float intensity = Mathf.Lerp(0.6f, 0.05f, distance / maxDistance);
                CameraShake.Instance.Shake(0.35f, intensity);
            }
        }

        // Switch to trigger mode only during explosion to calculate damage (or else would have exploded when player collects it
        col = GetComponent<SphereCollider>();
        if (col != null && weapon != null)
        {
            col.isTrigger = true;
            col.radius = weapon.range;
        }

        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 1.5f);
        }

        Destroy(gameObject, 0.1f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!exploded) return; //only apply damage after explosion

        BasicEnemy enemy = other.GetComponent<BasicEnemy>();
        if (enemy != null && weapon != null)
        {
            enemy.TakeDamage(weapon.damage); //damage to enemy
        }
    }
}