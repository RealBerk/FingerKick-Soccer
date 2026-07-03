using UnityEngine;

public class TopKontrol : MonoBehaviour
{
    private GameManager gameManager;
    private Rigidbody2D rb;

    [SerializeField] private float minHiz = 2f;   // Topun durmaması için minimum hız
    [SerializeField] private float maxHiz = 8f;   // Çok hızlanmasın diye maksimum hız

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        gameManager = Object.FindAnyObjectByType<GameManager>();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    void FixedUpdate()
    {
        // Oyun başlamadıysa topu tamamen durdur
        if (!GameManager.oyunBasladi)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        // Hız limitini uygula
        if (rb.linearVelocity.magnitude > maxHiz)
            rb.linearVelocity = rb.linearVelocity.normalized * maxHiz;
    }

    void OnCollisionEnter2D(Collision2D collision)
{
    // Eğer hız çok düşükse, biraz hız ekle ki top durmasın
    if (rb.linearVelocity.magnitude < 0.5f)
    {
        Vector2 normal = collision.contacts[0].normal;
        rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, normal) * 2f;
    }
}
void OnCollisionStay2D(Collision2D collision)
{
    // Eğer top kenara sıkıştıysa (çok yavaşsa), küçük bir rastgele kuvvet uygula
    if (rb.linearVelocity.magnitude < 0.3f)
    {
        Vector2 rastgeleYon = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        rb.AddForce(rastgeleYon * 2f, ForceMode2D.Impulse);
    }
}



    void OnTriggerEnter2D(Collider2D digerObje)
    {
        if (!GameManager.oyunBasladi) return;

        if (digerObje.CompareTag("OyuncuKalesi"))
        {
            gameManager.RakipGolAtti();
            Debug.Log("Rakip Gol Attı!");
        }
        else if (digerObje.CompareTag("RakipKalesi"))
        {
            gameManager.OyuncuGolAtti();
            Debug.Log("Oyuncu Gol Attı!");
        }
    }
}
