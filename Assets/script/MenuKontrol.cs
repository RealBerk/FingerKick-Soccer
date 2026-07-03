using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject mainMenuUI; // Canvas veya ana menü objesi
    public GameObject gameUI;     // Oyun sahnesi objeleri (isteğe bağlı)

    public void PlayGame()
    {
        // Ana menü sahnesini gizle
        mainMenuUI.SetActive(false);

        // Oyun sahnesini aktif et (eğer aynı sahnede başlıyorsa)
        if (gameUI != null)
            gameUI.SetActive(true);
    }
}
