using UnityEngine;

// Bu scripti TOP objesinin üzerine ekle.
// Sahadaki kenar/duvar objelerinin tag'i "Duvar" olmalı.
[RequireComponent(typeof(Rigidbody2D))]
public class TopSekme : MonoBehaviour
{
    [Header("Sekme Ayarları")]
    [Tooltip("1 = enerji kaybetmeden tam sekme, 0.8 = %20 enerji kaybederek sekme")]
    [Range(0f, 1.5f)]
    public float sekmeCarpani = 0.9f;

    [Tooltip("Sekme sonrası minimum hız (çok yavaş/duraksayan sekmeleri önler)")]
    public float minimumSekmeHizi = 2f;

    [Tooltip("Duvar tag'i")]
    public string duvarTagi = "Duvar";

    Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Hızlı hareket eden topun duvarı "atlamasını" (tunneling) önlemek için şart
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag(duvarTagi)) return;
        if (collision.contactCount == 0) return;

        Vector2 carpmaNormali = collision.GetContact(0).normal;
        Vector2 gelenHiz = rb.linearVelocity;

        // Yansıma formülü: v' = v - 2*(v·n)*n
        Vector2 yansiyanHiz = Vector2.Reflect(gelenHiz, carpmaNormali) * sekmeCarpani;

        // Sekme sonrası çok düşük hız kalırsa topu biraz güçlendir (takılıp kalmasın)
        if (yansiyanHiz.magnitude < minimumSekmeHizi)
        {
            yansiyanHiz = yansiyanHiz.normalized * minimumSekmeHizi;
        }

        rb.linearVelocity = yansiyanHiz;
    }
}