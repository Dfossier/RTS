using UnityEngine;
using UnityEngine.UI;

public class DeitySelectionUI : MonoBehaviour
{
    public static DeitySelectionUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Button gorgonButton;
    [SerializeField] private Button dzuesButton;
    [SerializeField] private Button pseidonButton;
    [SerializeField] private Button aresButton;

    private AltarManager currentAltar;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(AltarManager.AltarPlacement placement, AltarManager altar)
    {
        currentAltar = altar;

        gorgonButton.gameObject.SetActive(false);
        dzuesButton.gameObject.SetActive(false);
        pseidonButton.gameObject.SetActive(false);
        aresButton.gameObject.SetActive(true);

        switch (placement)
        {
            case AltarManager.AltarPlacement.Plains:
                gorgonButton.gameObject.SetActive(true);
                break;
            case AltarManager.AltarPlacement.Hilltop:
                dzuesButton.gameObject.SetActive(true);
                break;
            case AltarManager.AltarPlacement.River:
                pseidonButton.gameObject.SetActive(true);
                break;
        }

        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);

    public void OnGorgonSelected()  => currentAltar?.SelectDeity("gorgon");
    public void OnDzuesSelected()   => currentAltar?.SelectDeity("dzues");
    public void OnPseidonSelected() => currentAltar?.SelectDeity("pseidon");
    public void OnAresSelected()    => currentAltar?.SelectDeity("ares");
}
