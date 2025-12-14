using UnityEngine;

//==========================================
//           Projectile Controller
//==========================================
// It's a projectile... that's all it does....
// no major notes here

public class ProjectileController : MonoBehaviour
{
    public float lifeTime = 5f;
    public int damage = 10;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        // Example: damage player
        if (other.collider.CompareTag("Player"))
        {
            other.collider.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            Destroy(gameObject);
        }

        
    }
}
