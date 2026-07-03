using System.Collections;
using UnityEngine;
using UnityEngine.UI; // 🌟 Butonu karartıp açabilmek için UI kütüphanesini ekledik

public class OyuncuKontrol : MonoBehaviour
{
    private Rigidbody2D rb;
    private Camera anaKamera;

    public Joystick joystick;
    public float moveSpeed = 6f;

    public Rigidbody2D topRb;
    public float vurGucu = 15f; 

    [Header("Şut Ayarları")]
    public Button vurButonu; 
    public float maksimumMesafe = 1.5f; 
    public float cooldownSuresi = 10f; 
    private bool canShoot = true; 
    private bool topaTemasEdiyorMu = false; 

    private GameManager gameManager; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anaKamera = Camera.main;
        gameManager = FindObjectOfType<GameManager>(); 
    }

    void Update()
    {
        if (!GameManager.oyunBasladi)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero; // ✅ linearVelocity yerine velocity
            return;
        }

        // 🎮 Joystick inputunu al
        float moveX = joystick != null ? joystick.Horizontal : 0f;
        float moveY = joystick != null ? joystick.Vertical : 0f;
        Vector2 move = new Vector2(moveX, moveY);

        // Hareketi FixedUpdate’te uygulamak için kaydet
        hareketInput = move;
    }

    private Vector2 hareketInput;

    void FixedUpdate()
    {
        if (!GameManager.oyunBasladi)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        // ✅ Fizik tabanlı hareket (deltaTime gerekmez)
        rb.linearVelocity = hareketInput * moveSpeed;
    }

    // 🎯 Vur butonundan çağrılacak fonksiyon
    public void Vur()
    {
        if (!canShoot) return;

        if (topRb != null)
        {
            float mesafe = Vector2.Distance(transform.position, topRb.transform.position);

            if (topaTemasEdiyorMu || mesafe <= maksimumMesafe)
            {
                if (!GameManager.santraVurusuYapildi && gameManager != null)
                {
                    gameManager.SantraVurusunuKaldir();
                }

                Vector2 yon = (topRb.transform.position - transform.position).normalized;

                topRb.linearVelocity = Vector2.zero; // ✅ linearVelocity yerine velocity
                topRb.AddForce(yon * vurGucu, ForceMode2D.Impulse);

                StartCoroutine(CooldownRutini());
            }
            else
            {
                Debug.Log("Topa çok uzaksın! Şut çekilemedi.");
            }
        }
    }

    IEnumerator CooldownRutini()
    {
        canShoot = false; 
        if (vurButonu != null) vurButonu.interactable = false; 

        yield return new WaitForSeconds(cooldownSuresi); 

        canShoot = true; 
        if (vurButonu != null) vurButonu.interactable = true; 
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Top") || collision.gameObject.name.Contains("Top"))
        {
            topaTemasEdiyorMu = true; 

            if (!GameManager.santraVurusuYapildi && gameManager != null)
            {
                gameManager.SantraVurusunuKaldir();
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Top") || collision.gameObject.name.Contains("Top"))
        {
            topaTemasEdiyorMu = false; 
        }
    }
}
