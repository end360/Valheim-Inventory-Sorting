using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

namespace InventorySort
{
    [BepInPlugin("cf.end360.valheim.inventorysort", "Inventory Sort", "1.0.1")]
    [BepInProcess("valheim.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;

        GameObject sortButton;
        GameObject playerButton;
        GameObject containerButton;

        public GameObject SortButton
        {
            get
            {
                if (sortButton != null)
                    return sortButton;
                sortButton = MakeSortButton();
                return sortButton;
            }
        }

        internal GameObject PlayerButton
        {
            get
            {
                if(playerButton != null)
                    return playerButton;
                playerButton = MakePlayerButton();
                return playerButton;
            }
        }
        internal GameObject ContainerButton
        {
            get
            {
                if (containerButton != null)
                    return containerButton;
                containerButton = MakeContainerButton();
                return containerButton;
            }
        }
        Harmony harmony;
        
        void Awake()
        {
            instance = this;
            harmony = Harmony.CreateAndPatchAll(typeof(InventoryGuiPatch));
            Logger.LogInfo("Patched InventoryGui");
            if(InventoryGui.instance != null)
            {
                InventoryGuiPatch.Postfix();
            }
        }

        void OnDestroy()
        {
            harmony.UnpatchSelf();
            Logger.LogInfo("Unpatched InventoryGui");
            Destroy(sortButton);
            Destroy(playerButton);
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

        GameObject MakeSortButton()
        {
            GameObject obj = Instantiate(InventoryGui.instance.m_tabCraft.gameObject).gameObject;
            obj.name = "Sort";
            obj.GetComponentInChildren<Text>().text = "SORT";
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 20);

            var btn = obj.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.interactable = true;
            obj.SetActive(false);
            return obj;
        }

        internal GameObject MakePlayerButton()
        {
            GameObject obj = Instantiate(SortButton, InventoryGui.instance.m_player);
            obj.transform.localPosition = new Vector3(530, -305, 0);

            var btn = obj.GetComponent<Button>();
            btn.onClick.AddListener(SortPlayerInventory);
            return obj;
        }

        internal GameObject MakeContainerButton()
        {
            GameObject obj = Instantiate(SortButton, InventoryGui.instance.m_container);
            obj.transform.localPosition = new Vector3(530, -189, 0);

            var btn = obj.GetComponent<Button>();
            btn.onClick.AddListener(SortContainer);
            obj.SetActive(true);

            return obj;
        }

        internal BepInEx.Logging.ManualLogSource GetLogger() => Logger;
    }

    [HarmonyPatch(typeof(InventoryGui))]
    public class InventoryGuiPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        internal static void Postfix()
        {
            if (Plugin.instance == null)
                return;

            Plugin.instance.PlayerButton.SetActive(true);
            Plugin.instance.GetLogger().LogDebug($"Created player button {Plugin.instance.PlayerButton}");

            Plugin.instance.ContainerButton.SetActive(true);
            Plugin.instance.GetLogger().LogDebug($"Created container button {Plugin.instance.ContainerButton}");
        }
    }
}
