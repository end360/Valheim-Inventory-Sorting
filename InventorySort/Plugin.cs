using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySort
{
    [BepInPlugin("cf.end360.valheim.inventorysort", "Inventory Sort", "1.0.1")]
    [BepInProcess("valheim.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;

        #region Buttons
        internal GameObject sortButton;
        internal GameObject playerButton;
        internal GameObject containerButton;
        #endregion

        Harmony harmony;

        #region Config
        internal ConfigEntry<bool> ShouldAutoStack;
        internal ConfigEntry<string> GamepadJoystickSortKey;
        internal ConfigEntry<UnityEngine.KeyCode> SortKeyCode;
        #endregion

        int playerHeight = 0;
        int containerHeight = 0;

        const float BUTTON_WIDTH = 60f;
        const float BUTTON_HEIGHT = 20f;

        void Awake()
        {
            instance = this;
            harmony = Harmony.CreateAndPatchAll(typeof(InventoryGuiPatch));
            Logger.LogInfo("Patched InventoryGui");

            ShouldAutoStack = Config.Bind("General", "ShouldAutoStack", true, "Whether items should automatically be stacked together when sorting the inventory.");
            GamepadJoystickSortKey = Config.Bind("Controls", "GamepadJoystickSortKey", "JoyAttack", "What joystick input is used to sort the inventory. One of https://github.com/Valheim-Modding/Wiki/wiki/Key-Binding-Strings");
            SortKeyCode = Config.Bind("Controls", "SortKeyCode", KeyCode.R, "Which key sorts the inventory in the GUI. This is a UnityEngine.KeyCode.");
        }

        void OnDestroy()
        {
            harmony?.UnpatchSelf();
            Logger?.LogInfo("Unpatched InventoryGui");
            if(sortButton != null)
                Destroy(sortButton);
            if(playerButton != null)
                Destroy(playerButton);
            if(containerButton != null)
                Destroy(containerButton);
            instance = null;
        }

        void SortPlayerInventory()
        {
            InventoryUtils.Sort(Player.m_localPlayer.GetInventory(), 8);
            Logger.LogDebug("Sorted player inventory");
        }

        void SortContainer()
        {
            Inventory inventory = InventoryGui.instance.m_container.GetComponentInChildren<InventoryGrid>().GetInventory();
            InventoryUtils.Sort(inventory);
            Logger.LogDebug($"Sorted container inventory");
        }

        GameObject GetSortButton()
        {
            if (sortButton != null)
                return sortButton;

            GameObject obj = Instantiate(InventoryGui.instance.m_tabCraft.gameObject).gameObject;
            if (obj is null)
            {
                Logger.LogError($"SortButton couldn't be instantiated.");
                return null;
            }
            obj.name = "Sort";
            obj.GetComponentInChildren<Text>().text = "SORT";
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT);

            
            var btn = obj.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.interactable = true;
            obj.SetActive(false);

            var input = obj.GetComponent<UIGamePad>();
            input.m_keyCode = SortKeyCode.Value;
            input.m_zinputKey = GamepadJoystickSortKey.Value;
            // TODO: Find a way to get text for the hint.
            input.m_hint.SetActive(false);
            input.m_hint = null;
            return obj;
        }

        GameObject MakePlayerButton()
        {
            var bkg = InventoryGui.instance.m_player.Find("Bkg");

            GameObject obj = Instantiate(GetSortButton(), bkg);
            obj.transform.localPosition = new Vector3((obj.transform.parent as RectTransform).rect.width / 2 - BUTTON_WIDTH, (obj.transform.parent as RectTransform).rect.height / -2 - BUTTON_HEIGHT / 2, 0);

            var btn = obj.GetComponent<Button>();
            btn.onClick.AddListener(SortPlayerInventory);
            obj.SetActive(true);
            return obj;
        }

        GameObject MakeContainerButton()
        {
            var bkg = InventoryGui.instance.m_container.Find("Bkg");

            GameObject obj = Instantiate(GetSortButton(), bkg);
            obj.transform.localPosition = new Vector3((obj.transform.parent as RectTransform).rect.width / 2 - BUTTON_WIDTH, (obj.transform.parent as RectTransform).rect.height / -2 - BUTTON_HEIGHT / 2, 0);

            var btn = obj.GetComponent<Button>();
            btn.onClick.AddListener(SortContainer);
            obj.SetActive(true);
            return obj;
        }

        public void RebuildPlayerButton(InventoryGrid playerGrid, bool force = false)
        {
            if (playerGrid is null)
                throw new ArgumentNullException("playerGrid");

            var newSize = playerGrid?.GetInventory()?.GetHeight();
            if (newSize is null || playerHeight == newSize && !force)
                return;

            Logger.LogInfo(force ? "Player sort button being forcibly rebuilt." : $"Player inventory grid changed size from {playerHeight} to {newSize}, rebuilding.");

            if (playerButton != null)
                Destroy(playerButton);

            playerButton = MakePlayerButton();
            playerHeight = (int) newSize;

            var gui = InventoryGui.instance;
            if (gui is null)
                return;

            var bkg = gui.m_player.Find("Bkg");
            if (bkg is null)
                return;

            var scale = bkg.transform.localScale;

            if(scale != playerButton.transform.localScale)
                playerButton.transform.localScale = new Vector3(
                    playerButton.transform.localScale.x / scale.x,
                    playerButton.transform.localScale.y / scale.y,
                    0);
        }

        public void RebuildContainerButton(Container container, bool force = false)
        {
            if (container is null)
                return;

            

            var newSize = container.GetInventory().GetHeight();

            if (newSize == containerHeight && !force)
                return;

            Logger.LogInfo(force ? "Container sort button being forcibly rebuilt." : $"Player opened container of height {newSize}, was {containerHeight}, rebuilding sort button.");

            if (containerButton != null)
                Destroy(containerButton);

            containerButton = MakeContainerButton();
            
            newSize = containerHeight;
        }

        internal BepInEx.Logging.ManualLogSource GetLogger() => Logger;
    }

    [HarmonyPatch(typeof(InventoryGui))]
    public class InventoryGuiPatch
    {

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        internal static void PostfixUpdate(InventoryGrid ___m_playerGrid) => Plugin.instance?.RebuildPlayerButton(___m_playerGrid);

        [HarmonyPostfix]
        [HarmonyPatch("Show")]
        internal static void PostfixShow(Container container) => Plugin.instance?.RebuildContainerButton(container);
    }
}
