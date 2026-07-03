using UnityEngine;

public class ExitMarket : MonoBehaviour
{
    public GameObject marketPanel;

    public void CloseMarket()
    {
        marketPanel.SetActive(false);
    }
}
