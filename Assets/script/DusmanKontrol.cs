using UnityEngine;

public class DusmanKontrol : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform hedefKale;          // Rakibin şut atacağı kale
    public Transform kendiKalem;         // (Opsiyonel) Savunma/pozisyon alma için kendi kalesi

    [Header("Hareket Ayarları")]
    [Tooltip("Maksimum koşu hızı")]
    public float maxHiz = 5f;
    [Tooltip("Küçük değer = daha keskin/ani hareket, büyük değer = daha ağır/yumuşak hareket. 0.2-0.3 arası 'sağlıklı yürüyüş' hissi verir")]
    public float ivmelenmeSuresi = 0.25f;
    [Tooltip("AI'nin hedef noktaya (topa/pozisyona) ne kadar hızlı yöneleceği. Düşük değer = akıcı, yüksek değer = ani dönüşler")]
    public float hedefTakipHizi = 3.5f;
    public float topKontrolMesafesi = 1.1f;

    [Header("Şut Ayarları")]
    public float sutMesafesi = 2.2f;
    public float sutGucuMin = 9f;
    public float sutGucuMax = 14f;
    [Tooltip("İnsansı hata payı - şutun kaleye tam ortalanmamasını sağlar (derece)")]
    public float sutSapmaAcisi = 8f;
    public float sutSonrasiBekleme = 0.5f;

    [Header("Zeka / Zorluk Ayarları")]
    [Tooltip("AI'nin topa/duruma tepki vermesi için karar güncelleme aralığı. Hareketin kendisi bundan bağımsız hep akıcıdır")]
    public float tepkiSuresi = 0.15f;
    [Tooltip("Topun hızına göre ne kadar ileri konum tahmini yapılacağı")]
    public float topTahminSuresi = 0.28f;

    [Header("Sıkışma Kontrolü")]
    public float sikismaKontrolSuresi = 0.6f;
    public float sikismaMesafeEsik = 0.15f;
    [Tooltip("Sıkışma kaçış ofsetinin ne kadar akıcı açılıp kapanacağı")]
    public float sikismaYumusatmaHizi = 3f;

    Rigidbody2D rb;
    Rigidbody2D topRb;
    Transform topTransform;
    GameManager gameManager;

    Vector2 smoothVelocityRef;   // rb.velocity için SmoothDamp referansı
    Vector2 hamHedefPozisyon;    // AI'nin "karar verdiği" ham hedef (aralıklarla güncellenir)
    Vector2 hedefPozisyon;       // Karaktere doğru akıcı şekilde yumuşatılmış gerçek hedef
    Vector2 sikismaOfset;
    Vector2 istenenSikismaOfset;

    Vector3 sonSikismaKontrolPozisyonu;
    float sikismaTimer;

    float tepkiTimer;
    float sutCooldownTimer;

    bool topBende = false;

    enum YZDurumu { TopaKosuyor, TopKontrolunde, SutBekliyor }
    YZDurumu durum = YZDurumu.TopaKosuyor;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        gameManager = FindObjectOfType<GameManager>();

        GameObject top = GameObject.FindGameObjectWithTag("Top");
        if (top != null)
        {
            topTransform = top.transform;
            topRb = top.GetComponent<Rigidbody2D>();
        }

        sonSikismaKontrolPozisyonu = transform.position;
        hamHedefPozisyon = transform.position;
        hedefPozisyon = transform.position;
    }

    void FixedUpdate()
    {
        if (!GameManager.oyunBasladi)
        {
            YumusakDur();
            return;
        }

        if (topTransform == null || hedefKale == null) return;

        if (!GameManager.santraVurusuYapildi && topTransform.position.x <= 0)
        {
            YumusakDur();
            return;
        }

        if (sutCooldownTimer > 0f) sutCooldownTimer -= Time.fixedDeltaTime;

        float mesafe = Vector2.Distance(transform.position, topTransform.position);

        // Histerezis: kontrolü kaybetme mesafesi, alma mesafesinden büyük olmalı
        // (aksi halde sınırda titreme/kararsızlık olur)
        if (mesafe > topKontrolMesafesi + 0.6f)
            topBende = false;

        // --- AI kararını (ham hedefi) belirli aralıklarla günceller: insansı gecikme ---
        tepkiTimer -= Time.fixedDeltaTime;
        if (tepkiTimer <= 0f)
        {
            tepkiTimer = tepkiSuresi;

            if (!topBende)
            {
                hamHedefPozisyon = HedefKonumHesapla();
            }
            else
            {
                Vector2 kaleYon = ((Vector2)hedefKale.position - (Vector2)transform.position).normalized;
                hamHedefPozisyon = (Vector2)topTransform.position - kaleYon * 0.3f;
            }
        }

        // --- Hedef nokta ASLA sıçramaz: her fizik karesinde ham hedefe doğru akıcı şekilde kayar ---
        // Bu, "kesik kesik" hissin asıl kaynağıydı - artık kararlar aralıklı olsa da hareket süreklidir.
        hedefPozisyon = Vector2.Lerp(hedefPozisyon, hamHedefPozisyon, hedefTakipHizi * Time.fixedDeltaTime);

        SikismaKontrolEt();

        // Sıkışma ofseti de anlık açılıp kapanmak yerine akıcı şekilde geçiş yapar
        sikismaOfset = Vector2.Lerp(sikismaOfset, istenenSikismaOfset, sikismaYumusatmaHizi * Time.fixedDeltaTime);

        HareketEt(hedefPozisyon + sikismaOfset);

        if (topBende)
        {
            durum = YZDurumu.TopKontrolunde;

            float kaleMesafe = Vector2.Distance(topTransform.position, hedefKale.position);

            if (kaleMesafe < sutMesafesi && sutCooldownTimer <= 0f)
            {
                SutCek(kaleMesafe);
            }
        }
        else
        {
            durum = YZDurumu.TopaKosuyor;
        }
    }

    // Topun mevcut hız vektörüne göre nereye gideceğini tahmin eder (interception)
    Vector2 HedefKonumHesapla()
    {
        if (topRb == null) return topTransform.position;

        Vector2 tahmin = (Vector2)topTransform.position + topRb.linearVelocity * topTahminSuresi;

        bool duvaraYakin =
            Mathf.Abs(topTransform.position.x) > 7f ||
            Mathf.Abs(topTransform.position.y) > 3.6f;

        if (duvaraYakin)
        {
            // Duvara/kenara yakınsa sekme tahmini yerine topun konumuna doğrudan yaklaş
            Vector2 aci = ((Vector2)transform.position - (Vector2)topTransform.position).normalized;
            tahmin = (Vector2)topTransform.position + aci * 1.2f;
        }

        return tahmin;
    }

    void SutCek(float kaleMesafe)
    {
        Vector2 sutYon = ((Vector2)hedefKale.position - (Vector2)topTransform.position).normalized;

        // İnsansı hata payı: yönü rastgele küçük bir açıyla saptır
        float sapma = Random.Range(-sutSapmaAcisi, sutSapmaAcisi);
        sutYon = AciyaGoreDondur(sutYon, sapma);

        // Kaleye yaklaştıkça biraz daha az güç, uzaktan biraz daha fazla güç
        float guc = Mathf.Lerp(sutGucuMax, sutGucuMin, 1f - (kaleMesafe / sutMesafesi));

        topRb.AddForce(sutYon * guc, ForceMode2D.Impulse);

        topBende = false;
        sutCooldownTimer = sutSonrasiBekleme;
        durum = YZDurumu.SutBekliyor;
    }

    Vector2 AciyaGoreDondur(Vector2 vektor, float derece)
    {
        float rad = derece * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            vektor.x * cos - vektor.y * sin,
            vektor.x * sin + vektor.y * cos
        );
    }

    void HareketEt(Vector2 hedef)
    {
        Vector2 yon = (hedef - rb.position);
        if (yon.sqrMagnitude < 0.0001f)
        {
            YumusakDur();
            return;
        }

        Vector2 istenenHiz = yon.normalized * maxHiz;

        // Ani AddForce + Clamp yerine SmoothDamp: hızlanma/yavaşlama eğrisi doğal ve yumuşak olur.
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, istenenHiz, ref smoothVelocityRef, ivmelenmeSuresi);
    }

    void YumusakDur()
    {
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, Vector2.zero, ref smoothVelocityRef, ivmelenmeSuresi);
    }

    // Rastgele "titretme" impulse'u yerine: gerçekten sıkışmışsa hedefe küçük ve
    // düzgün bir yanal ofset ekleyerek etrafından dolanmasını sağlar (Lerp ile akıcı geçiş yukarıda uygulanır).
    void SikismaKontrolEt()
    {
        sikismaTimer -= Time.fixedDeltaTime;
        if (sikismaTimer > 0f) return;

        sikismaTimer = sikismaKontrolSuresi;

        float katedilenMesafe = Vector2.Distance(transform.position, sonSikismaKontrolPozisyonu);
        sonSikismaKontrolPozisyonu = transform.position;

        bool hareketEtmeyeCalisiyor = rb.linearVelocity.sqrMagnitude > 0.01f || istenenSikismaOfset != Vector2.zero;

        if (katedilenMesafe < sikismaMesafeEsik && hareketEtmeyeCalisiyor)
        {
            Vector2 hedefYonu = (hedefPozisyon - (Vector2)transform.position).normalized;
            Vector2 dikYon = new Vector2(-hedefYonu.y, hedefYonu.x);
            istenenSikismaOfset = dikYon * 1f * (Random.value > 0.5f ? 1f : -1f);
        }
        else
        {
            istenenSikismaOfset = Vector2.zero;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Top"))
        {
            topBende = true;

            if (!GameManager.santraVurusuYapildi && gameManager != null)
                gameManager.SantraVurusunuKaldir();

            Vector2 kaleYon = ((Vector2)hedefKale.position - (Vector2)topTransform.position).normalized;
            topRb.AddForce(kaleYon * 2f, ForceMode2D.Impulse);
        }
    }
}
