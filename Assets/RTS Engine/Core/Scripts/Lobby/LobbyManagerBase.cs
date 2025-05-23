﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Lobby.Logging;
using RTSEngine.Lobby.Service;
using RTSEngine.Lobby.Utilities;
using RTSEngine.Lobby.UI;
using RTSEngine.Utilities;

namespace RTSEngine.Lobby
{
    public abstract class LobbyManagerBase<T> : MonoBehaviour, ILobbyManager<T>, IGameBuilder where T : ILobbyFactionSlot
    {
        #region Attributes
        [SerializeField, Tooltip("Code that uniquely identifies the game version/type that this lobby handles.")]
        private string gameCode = "2022.0.0";
        public string GameCode => gameCode;

        // Holds the active lobby faction slot instances
        private List<T> factionSlots = null;
        public IReadOnlyList<T> FactionSlots 
        {
            get
            {
                ClearEmptyFactionSlots();
                return factionSlots;
            }
        }
        public int FactionSlotCount
        {
            get
            {
                ClearEmptyFactionSlots();
                return factionSlots.Count;
            }
        }

        public T GetFactionSlot(int factionSlotID) => FactionSlots[factionSlotID];
        public int GetFactionSlotID(T slot)
        {
            ClearEmptyFactionSlots();
            return factionSlots.IndexOf(slot);
        }
        public T GetHostFactionSlot() => FactionSlots.FirstOrDefault(elem => elem.Role == FactionSlotRole.host);

        public T LocalFactionSlot { protected set; get; } = default;

        [Space, SerializeField, Tooltip("What scenes can be loaded through this lobby?")]
        private LobbyMapData[] maps = new LobbyMapData[0];
        public IReadOnlyList<LobbyMapData> Maps => maps;
        public LobbyMapData CurrentMap => maps[CurrentLobbyGameData.mapID];
        public LobbyMapData GetMap(int mapID) => mapID.IsValidIndex(maps) ? maps[mapID] : CurrentMap;

        [Space, SerializeField, Tooltip("Define colors that can be used for factions in the lobby.")]
        private ColorSelector factionColorSelector = new ColorSelector();
        public ColorSelector FactionColorSelector => factionColorSelector;

        [Space, SerializeField, Tooltip("Define the possible defeat conditions that the player can pick from for the game.")]
        private DefeatConditionDropdownSelector defeatConditionSelector = new DefeatConditionDropdownSelector();
        public DefeatConditionDropdownSelector DefeatConditionSelector => defeatConditionSelector;

        [Space, SerializeField, Tooltip("Define the possible speed modifiers that the player can pick from for the game.")]
        private TimeModifierDropdownSelector timeModifierSelector = new TimeModifierDropdownSelector();
        public TimeModifierDropdownSelector TimeModifierSelector => timeModifierSelector;

        [Space, SerializeField, Tooltip("Define the possible initial resourcess that the player can pick from for the game.")]
        private ResourceInputDropdownSelector initialResourcesSelector = new ResourceInputDropdownSelector();
        public ResourceInputDropdownSelector InitialResourcesSelector => initialResourcesSelector;

        public abstract bool IsStartingLobby { get; }

        public LobbyGameData CurrentLobbyGameData { private set; get; }

        // Lobby Services
        protected ILoggingService logger { private set; get; }
        protected ILobbyManagerUI lobbyUIMgr { private set; get; }
        protected ILobbyPlayerMessageUIHandler playerMessageUIHandler { private set; get; } 
        #endregion

        #region IGameBuilder
        public abstract bool IsMaster { get; }
        public abstract bool CanFreezeTimeOnPause { get; }

        public bool IsInputAdderReady => InputAdder.IsValid();
        public IInputAdder InputAdder { private set; get; }
        public event CustomEventHandler<IGameBuilder, EventArgs> InputAdderReady;

        public IGameManager ActiveGameMgr { private set; get; }

        public void OnInputAdderReady(IInputAdder inputAdder)
        {
            this.InputAdder = inputAdder;

            var handler = InputAdderReady;
            handler?.Invoke(this, EventArgs.Empty);
        }

        public GameData Data => new GameData
        {
            defeatCondition = DefeatConditionSelector.CurrentValue,
            timeModifierOptions = new TimeModifierOptions 
            {
                values = TimeModifierSelector.Options.ToArray(),
                initialValueIndex = TimeModifierSelector.CurrentValueIndex
            } ,
            initialResources = InitialResourcesSelector.CurrentValue,

            factionSlotIndexSeed = CurrentLobbyGameData.factionSlotIndexSeed
        };

        public IEnumerable<FactionSlotData> FactionSlotDataSet => FactionSlots
            .Select(slot => slot.Data);

        public bool ClearDefaultEntities => false;

        public void OnGameBuilt (IGameManager gameMgr) 
        {
            // Hide the lobby UI elements
            lobbyUIMgr.Toggle(false);

            for(int factionID = 0; factionID < FactionSlotCount; factionID++)
                factionSlots[factionID].OnGameBuilt(gameFactionSlot: gameMgr.GetFactionSlot(factionID));

            ActiveGameMgr = gameMgr;

            OnGameBuiltComplete(gameMgr);
        }

        protected virtual void OnGameBuiltComplete (IGameManager gameMgr) { }

        public void OnGameLeave()
        {
            LeaveLobby();

            ActiveGameMgr = null;
        }
        #endregion

        #region Raising Events: Adding/Removing Faction Slots
        public event CustomEventHandler<T, EventArgs> FactionSlotAdded;
        public event CustomEventHandler<T, EventArgs> FactionSlotRemoved;

        private void RaiseFactionSlotAdded (T newSlot)
        {
            var handler = FactionSlotAdded;
            handler?.Invoke(newSlot, EventArgs.Empty);
        }
        private void RaiseFactionSlotRemoved (T newSlot)
        {
            var handler = FactionSlotRemoved;
            handler?.Invoke(newSlot, EventArgs.Empty);
        }
        #endregion

        #region Raising Events: Lobby Game Data
        public event CustomEventHandler<LobbyGameData, EventArgs> LobbyGameDataUpdated;

        private void RaiseLobbyGameDataUpdated (LobbyGameData prevLobbyGameData)
        {
            var handler = LobbyGameDataUpdated;
            handler?.Invoke(prevLobbyGameData, EventArgs.Empty);
        }
        #endregion

        #region Services
        private IReadOnlyDictionary<System.Type, ILobbyService> services = null;

        public V GetService<V>() where V : ILobbyService
        {
            if(!services.ContainsKey(typeof(V)))
                Debug.LogError ($"[{GetType().Name}] No service of type: '{typeof(V)}' has been registered!");

            if (services.TryGetValue(typeof(V), out ILobbyService value))
                return (V)value;

            return default;
        }

        private void RegisterServices()
        {
            // Only services that are attached to the same game object are recognized
            // Register the services when the game starts.
            services = GetComponents<ILobbyService>()
                .ToDictionary(service => service.GetType().GetSuperInterfaceType<ILobbyService>(), service => service);

            // Initialize services.
            foreach (ILobbyService service in services.Values)
                service.Init(this);
        }
        #endregion

        #region Initializing/Terminating
        private void Awake()
        {
            // We need this component to pass to the map scene
            this.gameObject.DontDestroyOnLoad();

            this.logger = GetComponent<ILobbyLoggingService>();

            RTSHelper.Init(this);

            // Register the lobby related services.
            RegisterServices();

            lobbyUIMgr = GetService<ILobbyManagerUI>();
            playerMessageUIHandler = GetService<ILobbyPlayerMessageUIHandler>();

            if (!logger.RequireValid(lobbyUIMgr,
              $"[{GetType().Name}] A component that extends the interface '{typeof(ILobbyManagerUI).Name}' must be attached to the same game object to handle UI."))
                return; 

            factionSlots = new List<T>();

            // Validating maps
            if (!logger.RequireTrue(maps.Length > 0,
              $"[{GetType().Name}] At least one map must be assigned!"))
                return; 
            foreach(LobbyMapData map in maps)
                map.Init(this);

            FactionColorSelector.Init(this);

            // Initialize options drop down menus
            defeatConditionSelector.Init(this); 
            timeModifierSelector.Init(this);
            initialResourcesSelector.Init(this);

            CurrentLobbyGameData = new LobbyGameData
            {
                mapID = 0,

                defeatConditionID = 0,
                initialResourcesID = 0,
                timeModifierID = 0,

                factionSlotIndexSeed = RTSHelper.GenerateRandomIndexList(CurrentMap.factionsAmount.max)
            };

            OnInit();
        }

        protected virtual void OnInit() { }

        private void OnDestroy()
        {
            OnDestroyed();
        }

        protected virtual void OnDestroyed() { }
        #endregion

        #region Updating Lobby Game Data
        public abstract bool IsLobbyGameDataMaster();

        public void UpdateLobbyGameData (LobbyGameData newLobbyGameData)
        {
            LobbyGameData prevLobbyGameData = CurrentLobbyGameData;
            CurrentLobbyGameData = newLobbyGameData;

            // Only change the drop down menu values of the lobby game data in case this is not the lobby game data master
            // Because the lobby game data master needs to change these values through the UI interface in order for this method to be called on their local end
            // Resetting the drop down menu values for the lobby game data master means that the drop down value change event will be triggered
            // When that event is triggered again, an update to the lobby game data will be called and an endless loop will occur
            if (!IsLobbyGameDataMaster())
            {
                DefeatConditionSelector.SetOption(newLobbyGameData.defeatConditionID);
                TimeModifierSelector.SetOption(newLobbyGameData.timeModifierID);
                InitialResourcesSelector.SetOption(newLobbyGameData.initialResourcesID);
            }

            RaiseLobbyGameDataUpdated(prevLobbyGameData);
            OnLobbyGameDataUpdated(prevLobbyGameData);
        }

        protected virtual void OnLobbyGameDataUpdated(LobbyGameData prevLobbyGameData) { }
        #endregion

        #region Adding/Removing Factions Slots
        private void ClearEmptyFactionSlots()
        {
            factionSlots = factionSlots.Where(slot => slot.IsValid()).ToList();
        }

        public void AddFactionSlot (T newSlot)
        {
            if (IsStartingLobby)
                return;

            factionSlots.Add(newSlot);

            newSlot.FactionRoleUpdated += HandleFactionSlotRoleUpdated;

            RaiseFactionSlotAdded(newSlot);
        }

        public abstract bool CanRemoveFactionSlot(T slot);

        protected void RemoveFactionSlot (T slot)
        {
            if (!CanRemoveFactionSlot(slot))
                return;

            factionSlots.Remove(slot);

            InterruptStartLobby();

            slot.FactionRoleUpdated -= HandleFactionSlotRoleUpdated;

            RaiseFactionSlotRemoved(slot);
            OnFactionSlotRemoved(slot);
        }

        protected virtual void OnFactionSlotRemoved(T slot) { }

        protected virtual void HandleFactionSlotRoleUpdated(ILobbyFactionSlot slot, EventArgs args) { }
        #endregion

        #region Starting/Leaving Lobby
        public void LeaveLobby()
        {
            if (InterruptStartLobby())
                return;

            OnPreLobbyLeave();
            
            DontDestroyOnLoadManager.Destroy(gameObject);
        }

        protected abstract void OnPreLobbyLeave();

        public void StartLobby()
        {
            if (IsStartingLobby)
                return;

            OnStartLobby();
        }

        protected virtual void OnStartLobby() { }

        public bool InterruptStartLobby()
        {
            if (!IsStartingLobby)
                return false;

            OnStartLobbyInterrupt();

            return true;
        }

        protected virtual void OnStartLobbyInterrupt() { }
        #endregion
    }
}
