using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class CartWindow : Window
    {
        private Client _currentUser;

        public CartWindow(Client user)
        {
            InitializeComponent();
            _currentUser = user;
            LoadCartItems();
        }

        private void LoadCartItems()
        {
            var context = GostenovaMiracleShowEntities.GetContext();
            var items = new List<OrderItemView>();

            // Проходим по всем позициям корзины
            foreach (var pair in Cart.Items)
            {
                int commodityPositionId = pair.Key;
                int quantity = pair.Value;

                // Ищем товарную позицию в БД
                var cp = context.CommodityPosition
                    .FirstOrDefault(c => c.CommodityPositionID == commodityPositionId);

                if (cp == null) continue;

                var view = new OrderItemView
                {
                    ProductName = cp.Product != null ? cp.Product.ProductName : "(товар неизвестен)",
                    SizeText = cp.Sizes != null ? cp.Sizes.SizeValue.ToString() : "—",
                    Quantity = quantity,
                    Price = cp.Product != null ? cp.Product.Price : 0,
                    LineSum = (cp.Product != null ? cp.Product.Price : 0) * quantity,
                    SourceItem = null   // в корзине настоящего OrderItem ещё нет
                };

                items.Add(view);
            }

            CartItemsListView.ItemsSource = items;
            UpdateTotal(items);
        }

        private void UpdateTotal(List<OrderItemView> items)
        {
            decimal total = items.Sum(i => i.LineSum);
            TotalText.Text = "Итого: " + total.ToString("N2");
        }

        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            //корзина пустая — нечего оформлять
            if (Cart.IsEmpty)
            {
                MessageBox.Show("Корзина пуста.");
                return;
            }

            //нет пользователя — некуда записывать
            if (_currentUser == null)
            {
                MessageBox.Show("Вы не авторизованы.");
                return;
            }

            var context = GostenovaMiracleShowEntities.GetContext();

            //загружаем все товарные позиции из корзины
            var cpIds = Cart.Items.Keys.ToList();
            var positions = context.CommodityPosition.ToList()
                .Where(c => cpIds.Contains(c.CommodityPositionID))
                .ToList();

            //проверяем остаток: не хватает — стоп
            foreach (var pair in Cart.Items)
            {
                var cp = positions.FirstOrDefault(c => c.CommodityPositionID == pair.Key);
                if (cp == null)
                {
                    MessageBox.Show("Товарная позиция не найдена.");
                    return;
                }
                if (cp.QuantityInStock < pair.Value)
                {
                    string name = cp.Product != null ? cp.Product.ProductName : "(товар)";
                    string size = cp.Sizes != null ? cp.Sizes.SizeValue.ToString() : "?";
                    MessageBox.Show("Недостаточно товара на складе: " + name + ", размер " + size +
                                    ". В наличии: " + cp.QuantityInStock + " шт.");
                    return;
                }
            }

            //создаём заказ
            var newOrder = new Order
            {
                ClientID = _currentUser.ClientID,
                OrderDate = System.DateTime.Now
            };
            context.Order.Add(newOrder);
            context.SaveChanges();

            //добавляем позиции и списываем со склада
            foreach (var pair in Cart.Items)
            {
                var cp = positions.First(c => c.CommodityPositionID == pair.Key);

                var item = new OrderItem
                {
                    OrderID = newOrder.OrderID,
                    CommodityPositionID = cp.CommodityPositionID,
                    Quantity = pair.Value,
                    PriceAtOrderTime = cp.Product != null ? cp.Product.Price : 0
                };
                context.OrderItem.Add(item);

                //списываем со склада
                cp.QuantityInStock -= pair.Value;
            }
            context.SaveChanges();

            //очищаем корзину
            Cart.Clear();

            MessageBox.Show("Заказ оформлен. Номер: " + newOrder.OrderID);
            Close();
        }
        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.DataContext as OrderItemView;
            if (item == null) return;

            // Находим ID товарной позиции по имени+размеру
            int cpId = FindCommodityPositionId(item);
            if (cpId == 0) return;

            Cart.ChangeQuantity(cpId, +1);
            LoadCartItems();
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.DataContext as OrderItemView;
            if (item == null) return;

            int cpId = FindCommodityPositionId(item);
            if (cpId == 0) return;

            Cart.ChangeQuantity(cpId, -1);
            LoadCartItems();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.DataContext as OrderItemView;
            if (item == null) return;

            int cpId = FindCommodityPositionId(item);
            if (cpId == 0) return;

            Cart.Remove(cpId);
            LoadCartItems();
        }

        // Вспомогательный метод: по позиции в UI находим её CommodityPositionID
        private int FindCommodityPositionId(OrderItemView item)
        {
            var context = GostenovaMiracleShowEntities.GetContext();

            // Ищем CommodityPosition по имени товара и размеру
            var cp = context.CommodityPosition.ToList()
                .FirstOrDefault(c =>
                    c.Product != null &&
                    c.Product.ProductName == item.ProductName &&
                    c.Sizes != null &&
                    c.Sizes.SizeValue.ToString() == item.SizeText);

            return cp != null ? cp.CommodityPositionID : 0;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}