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
        static Dictionary<string, bool> cache = new Dictionary<string, bool>();

        public static bool HasPlugin(string guid)
        {
            if (cache.ContainsKey(guid))
                return cache[guid];
            var plugins = UnityEngine.Object.FindObjectsOfType<BaseUnityPlugin>();

            cache[guid] = plugins.Any(plugin => plugin.Info.Metadata.GUID == guid);
            return cache[guid];
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

        public static void Sort(Inventory inventory, int offset = 0, bool autoStack = false)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var offsetv = new Vector2i(offset % inventory.GetWidth(), offset / inventory.GetWidth());
            var toSort = inventory.GetAllItems()
                .Where(itm => ShouldSortItem(itm.m_gridPos, offsetv))
                .OrderBy((itm) => itm.m_shared.m_name);

            if (Plugin.instance.ShouldAutoStack.Value) {
                var grouped = toSort.Where(itm => itm.m_stack < itm.m_shared.m_maxStackSize).GroupBy(itm => itm.m_shared.m_name).Where(itm => itm.Count() > 1).Select(grouping => grouping.ToList());
                Plugin.instance.GetLogger().LogInfo($"There are {grouped.Count()} groups of stackable items");
                foreach (var nonFullStacks in grouped)
                {
                    var maxStack = nonFullStacks.First().m_shared.m_maxStackSize;

                    var numTimes = 0;
                    var curStack = nonFullStacks[0];
                    nonFullStacks.RemoveAt(0);

                    var enumerator = nonFullStacks.GetEnumerator();
                    while (nonFullStacks.Count >= 1)
                    {
                        numTimes += 1;
                        enumerator.MoveNext();
                        var stack = enumerator.Current;
                        if(stack == null)
                            break;

                        if (curStack.m_stack >= maxStack)
                        {
                            curStack = stack;
                            nonFullStacks.Remove(stack);
                            enumerator = nonFullStacks.GetEnumerator();
                            continue;
                        }

                        var toStack = Math.Min(maxStack - curStack.m_stack, stack.m_stack);
                        if (toStack > 0)
                        {
                            curStack.m_stack += toStack;
                            stack.m_stack -= toStack;

                            if (stack.m_stack <= 0)
                            {
                                inventory.RemoveItem(stack);
                            }
                        }
                    }
                    Plugin.instance.GetLogger().LogDebug($"Auto-Stacked in {numTimes} iterations");
                }
            }

            foreach (var item in toSort)
            {
                var x = offset % inventory.GetWidth();
                var y = offset / inventory.GetWidth();
                item.m_gridPos = new Vector2i(x, y);
                offset++;
            }
            sw.Stop();
            Plugin.instance.GetLogger().LogDebug($"Sorting inventory took {sw.Elapsed}");

            // Clear the cache in case anyone is using something that loads plugins at run-time.
            cache.Clear();
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
