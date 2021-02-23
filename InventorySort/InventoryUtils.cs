using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BepInEx;
using System.Reflection;
namespace InventorySort
{
    public static class InventoryUtils
    {
        static MethodInfo IsEquipmentSlot;
        static MethodInfo IsQuickSlot;

        public static bool HasPlugin(string guid)
        {
            var plugins = UnityEngine.Object.FindObjectsOfType<BaseUnityPlugin>();

            return plugins.Any(plugin => plugin.Info.Metadata.GUID == guid);
        }

        public static bool ShouldSortItem(Vector2i itemPos, Vector2i offset)
        {
            if (HasPlugin("randyknapp.mods.equipmentandquickslots"))
            {
                Plugin.instance.GetLogger().LogDebug("Found EquipmentAndQuickSlots plugin");
                if (IsEquipmentSlot == null && IsQuickSlot == null)
                {
                    var ass = Assembly.Load("EquipmentAndQuickSlots");
                    if (ass != null)
                    {
                        Plugin.instance.GetLogger().LogDebug("Found assembly");
                        var type = ass.GetTypes().First(a => a.IsClass && a.Name == "EquipmentAndQuickSlots");
                        var pubstatic = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                        IsEquipmentSlot = pubstatic.First(t => t.Name == "IsEquipmentSlot" && t.GetParameters().Length == 1);
                        Plugin.instance.GetLogger().LogDebug($"IsEquipmentSlot: {IsEquipmentSlot}");
                        IsQuickSlot = pubstatic.First(t => t.Name == "IsQuickSlot" && t.GetParameters().Length == 1);
                        Plugin.instance.GetLogger().LogDebug($"IsQuickSlot: {IsQuickSlot}");

                    }
                }

                if ((bool) IsEquipmentSlot?.Invoke(null, new object[] { itemPos }))
                    return false;
                if ((bool)IsQuickSlot?.Invoke(null, new object[] { itemPos }))
                    return false;
            }

            return itemPos.y > offset.y || (itemPos.y == offset.y && itemPos.x >= offset.x);
        }

        public static void Sort(Inventory inventory, int offset = 0)
        {
            inventory.GetAllItems().Sort((a, b) => a.m_shared.m_name.CompareTo(b.m_shared.m_name));
            var offsetv = new Vector2i(offset % inventory.GetWidth(), offset / inventory.GetWidth());

            var i = offset;
            foreach (var item in inventory.GetAllItems().Where(itm => ShouldSortItem(itm.m_gridPos, offsetv)))
            {
                var x = i % inventory.GetWidth();
                var y = i / inventory.GetWidth();
                item.m_gridPos = new Vector2i(x, y);
                i++;
            }

            //inventory.GetType().GetMethod("Changed").Invoke(inventory, new object[] { });
        }
    }

    public static class ContainerUtils
    {
        public static void Sort(Container container, int offset = 0)
        {
            InventoryUtils.Sort(container.GetInventory(), offset);
        }
    }
}
