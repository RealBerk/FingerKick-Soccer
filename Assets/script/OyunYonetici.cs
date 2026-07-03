using System.Collections;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static bool oyunBasladi = false;
    public static bool santraVurusuYapildi = false; 

    public GameObject menuPanel;

    private int oyuncuSkoru = 0;
    private int rakipSkoru = 0;

    public TextMeshProUGUI oyuncuSkorText;
    public TextMeshProUGUI rakipSkorText;

    public Animator skorboardAnimator;

    public float oyunSuresi = 90f;
    private float kalanSure;

    public TextMeshProUGUI zamanText; 

    [Header("Top Hız Ayarları")]
    [Range(0f, 10f)]
    public float topYavaslikOrani = 1.0f;

    [Header("Oyun Objeleri (Reset İçin)")]
    public Transform oyuncuKarakter;
    public Transform rakipKarakter;
    public Transform topObjesi;

    private Vector3 oyuncuBaslangicPos;
    private Vector3 rakipBaslangicPos;
    private Vector3 topBaslangicPos;

    private Rigidbody2D oyuncuRb;
    private Rigidbody2D rakipRb;
    private Rigidbody2D topRb;

    private Collider2D oyuncuCollider;
    private Collider2D rakipCollider;
    private Collider2D topCollider;

    // 🎵 EKLENEN SES DEĞİŞKENLERİ
    [Header("Sesler")]
    public AudioSource lobbyMusic;   // Lobi müziği
    public AudioSource gameMusic;    // Maç müziği

    void Start()
    {
        oyunBasladi = false; 
        SkorlariGuncelle();
        kalanSure = oyunSuresi;

        if (oyuncuKarakter != null)
        {
            oyuncuBaslangicPos = oyuncuKarakter.position;
            oyuncuRb = oyuncuKarakter.GetComponent<Rigidbody2D>();
            oyuncuCollider = oyuncuKarakter.GetComponent<Collider2D>();
        }
        if (rakipKarakter != null)
        {
            rakipBaslangicPos = rakipKarakter.position;
            rakipRb = rakipKarakter.GetComponent<Rigidbody2D>();
            rakipCollider = rakipKarakter.GetComponent<Collider2D>();
        }
        if (topObjesi != null)
        {
            topBaslangicPos = topObjesi.position;
            topRb = topObjesi.GetComponent<Rigidbody2D>();
            topCollider = topObjesi.GetComponent<Collider2D>();
        }

        SahayiSifirla();
        TopYavasliginiGuncelle();

        // 🎵 Lobi müziğini başlat (EKLENEN SATIR)
        if (lobbyMusic != null)
            lobbyMusic.Play();
    }

    void Update()
    {
        if (oyunBasladi)
        {
            kalanSure -= Time.deltaTime;

            if (zamanText != null)
                zamanText.text = Mathf.Ceil(kalanSure).ToString();

            if (topRb != null && topRb.linearVelocity.magnitude > 0.05f)
            {
                topRb.linearVelocity = Vector2.Lerp(topRb.linearVelocity, Vector2.zero, Time.deltaTime * topYavaslikOrani);
            }

            if (kalanSure <= 0)
            {
                oyunBasladi = false;
                kalanSure = oyunSuresi;

                if (menuPanel != null)
                    menuPanel.SetActive(true);

                // 🎵 Maç müziğini durdur → Lobi müziğini aç (EKLENEN SATIRLAR)
                if (gameMusic != null && gameMusic.isPlaying)
                    gameMusic.Stop();
                if (lobbyMusic != null)
                    lobbyMusic.Play();

                SahayiSifirla();
            }
        }
    }

    void OnValidate()
    {
        TopYavasliginiGuncelle();
    }

    public void TopYavasliginiGuncelle()
    {
        if (topRb != null)
            topRb.linearDamping = topYavaslikOrani;
    }

    public void OyunuBaslat()
    {
        oyuncuSkoru = 0;
        rakipSkoru = 0;
        SkorlariGuncelle();

        FizikleriAktifEt(true);
        TopYavasliginiGuncelle();

        santraVurusuYapildi = true;
        Physics2D.IgnoreCollision(oyuncuCollider, topCollider, false);
        Physics2D.IgnoreCollision(rakipCollider, topCollider, false);

        oyunBasladi = true;
        kalanSure = oyunSuresi;

        if (menuPanel != null)
            menuPanel.SetActive(false);

        // 🎵 Lobi müziğini durdur → Maç müziğini başlat (EKLENEN SATIRLAR)
        if (lobbyMusic != null && lobbyMusic.isPlaying)
            lobbyMusic.Stop();
        if (gameMusic != null)
            gameMusic.Play();
    }

    public void OyuncuGolAtti()
    {
        oyuncuSkoru++;
        SkorlariGuncelle();
        if (skorboardAnimator != null)
            skorboardAnimator.SetTrigger("GolOldu");

        StartCoroutine(GolSonrasiYenidenBaslat(true));
    }

    public void RakipGolAtti()
    {
        rakipSkoru++;
        SkorlariGuncelle();
        if (skorboardAnimator != null)
            skorboardAnimator.SetTrigger("GolOldu");

        StartCoroutine(GolSonrasiYenidenBaslat(false));
    }

    IEnumerator GolSonrasiYenidenBaslat(bool golYeyenRakipMi)
    {
        oyunBasladi = false; 
        FizikleriAktifEt(false); 

        yield return new WaitForSeconds(2f); 

        oyuncuKarakter.position = oyuncuBaslangicPos;
        rakipKarakter.position = rakipBaslangicPos;

        if (golYeyenRakipMi)
            topObjesi.position = topBaslangicPos + new Vector3(1.5f, 0f, 0f);
        else
            topObjesi.position = topBaslangicPos + new Vector3(-1.5f, 0f, 0f);

        oyuncuRb.linearVelocity = Vector2.zero;
        rakipRb.linearVelocity = Vector2.zero;
        topRb.linearVelocity = Vector2.zero;

        santraVurusuYapildi = false;

        if (golYeyenRakipMi)
        {
            Physics2D.IgnoreCollision(oyuncuCollider, topCollider, true);
            Physics2D.IgnoreCollision(rakipCollider, topCollider, false);
        }
        else
        {
            Physics2D.IgnoreCollision(oyuncuCollider, topCollider, false);
            Physics2D.IgnoreCollision(rakipCollider, topCollider, true);
        }

        yield return new WaitForSeconds(1f); 

        FizikleriAktifEt(true);
        TopYavasliginiGuncelle();
        oyunBasladi = true;
    }

    void SahayiSifirla()
    {
        oyuncuKarakter.position = oyuncuBaslangicPos;
        rakipKarakter.position = rakipBaslangicPos;
        topObjesi.position = topBaslangicPos;

        oyuncuRb.linearVelocity = Vector2.zero;
        rakipRb.linearVelocity = Vector2.zero;
        topRb.linearVelocity = Vector2.zero;

        Physics2D.IgnoreCollision(oyuncuCollider, topCollider, false);
        Physics2D.IgnoreCollision(rakipCollider, topCollider, false);

        FizikleriAktifEt(false);
    }

    void FizikleriAktifEt(bool aktifMi)
    {
        oyuncuRb.isKinematic = !aktifMi;
        rakipRb.isKinematic = !aktifMi;
        topRb.isKinematic = !aktifMi;
    }

    void SkorlariGuncelle()
    {
        oyuncuSkorText.text = oyuncuSkoru.ToString();
        rakipSkorText.text = rakipSkoru.ToString();
    }

    public void SantraVurusunuKaldir()
    {
        santraVurusuYapildi = true;
        Physics2D.IgnoreCollision(oyuncuCollider, topCollider, false);
        Physics2D.IgnoreCollision(rakipCollider, topCollider, false);
    }
}
