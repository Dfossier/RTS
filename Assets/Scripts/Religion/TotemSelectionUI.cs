using System;
using UnityEngine;
using UnityEngine.UI;
using RTSEngine.Game;
using RTSEngine.Upgrades;

public class TotemSelectionUI : MonoBehaviour, IPreRunGameService
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button heliosButton;
    [SerializeField] private Button aeolusButton;
    [SerializeField] private Button lykaiosButton;
    [SerializeField] private Button potamosButton;
    [SerializeField] private Button lithosButton;

    [Header("Totem Upgrades")]
    [SerializeField] private EntityComponentUpgrade heliosUpgrade;
    [SerializeField] private EntityComponentUpgrade aeolusUpgrade;
    [SerializeField] private EntityComponentUpgrade lykaiosUpgrade;
    [SerializeField] private EntityComponentUpgrade potamosUpgrade;
    [SerializeField] private EntityComponentUpgrade lithosUpgrade;

    private IGameManager gameMgr;

    public void Init(IGameManager gameMgr)
    {
        this.gameMgr = gameMgr;
        gameMgr.GameBuilt += OnGameBuilt;
    }

    private void OnGameBuilt(IGameManager gm, EventArgs args)
    {
        Time.timeScale = 0f;
        panel.SetActive(true);
    }

    private void ApplyTotem(EntityComponentUpgrade upgrade)
    {
        if (upgrade == null) { ClosePanel(); return; }

        upgrade.LaunchLocal(gameMgr, 0, gameMgr.LocalFactionSlotID);
        ClosePanel();
    }

    private void ClosePanel()
    {
        Time.timeScale = 1f;
        panel.SetActive(false);
    }

    public void OnHeliosSelected()  => ApplyTotem(heliosUpgrade);
    public void OnAeolusSelected()  => ApplyTotem(aeolusUpgrade);
    public void OnLykaiosSelected() => ApplyTotem(lykaiosUpgrade);
    public void OnPotamosSelected() => ApplyTotem(potamosUpgrade);
    public void OnLithosSelected()  => ApplyTotem(lithosUpgrade);
}
