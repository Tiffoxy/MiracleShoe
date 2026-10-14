using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
    public static class Cart
    {
        // Список позиций черновика.
        // Ключ — CommodityPositionID, значение — количество.
        public static Dictionary<int, int> Items { get; private set; } = new Dictionary<int, int>();

        // Есть ли что-то в корзине
        public static bool IsEmpty
        {
            get 
            { 
                return Items.Count == 0; 
            }
        }

        // Добавить одну штуку товарной позиции (или +1, если уже есть)
        public static void Add(int commodityPositionId, int qty)
        {
            var context = GostenovaMiracleShowEntities.GetContext();

            //узнаём, сколько есть на складе
            var cp = context.CommodityPosition
                .FirstOrDefault(c => c.CommodityPositionID == commodityPositionId);

            if (cp == null) return;

            int inStock = cp.QuantityInStock;

            int already = Items.ContainsKey(commodityPositionId) ? Items[commodityPositionId] : 0;
            int total = already + qty;

            //не даём взять больше, чем есть
            if (total > inStock)
                total = inStock;

            Items[commodityPositionId] = total;
        }

        // Изменить количество на delta (+1 или -1). Не выше остатка. Если 0 — удалить.
        public static void ChangeQuantity(int commodityPositionId, int delta)
        {
            if (!Items.ContainsKey(commodityPositionId)) return;

            var context = GostenovaMiracleShowEntities.GetContext();
            var cp = context.CommodityPosition
                .FirstOrDefault(c => c.CommodityPositionID == commodityPositionId);

            int inStock = cp != null ? cp.QuantityInStock : 0;
            int newQty = Items[commodityPositionId] + delta;

            //не выше остатка
            if (newQty > inStock) return;

            //ноль или меньше — убираем позицию
            if (newQty <= 0)
            {
                Items.Remove(commodityPositionId);
                return;
            }

            Items[commodityPositionId] = newQty;
        }

        // Удалить позицию полностью
        public static void Remove(int commodityPositionId)
        {
            if (Items.ContainsKey(commodityPositionId))
                Items.Remove(commodityPositionId);
        }

        // Очистить корзину (после оформления заказа)
        public static void Clear()
        {
            Items.Clear();
        }
    }
}