using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventorySort
{
    public static class InventoryUtils
    {
        public static void Sort(Inventory inventory, int offset = 0)
        {
            inventory.GetAllItems().Sort((a, b) => a.m_shared.m_name.CompareTo(b.m_shared.m_name));
            var offsetv = new Vector2i(offset % inventory.GetWidth(), offset / inventory.GetWidth());

            var i = offset;
            foreach (var item in inventory.GetAllItems().Where(itm => itm.m_gridPos.y > offsetv.y || (itm.m_gridPos.y == offsetv.y && itm.m_gridPos.x >= offsetv.x)))
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
