using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class OrderWindow : Window
    {
        // Заказ, который нам передали. Через него будем работать дальше.
        private Order _order;
        private System.Collections.Generic.List<OrderItemView> _itemsView;

        private Client _currentUser;
        private int UserRoles;

        public OrderWindow(Order order, Client user, int userRoles)
        {
            InitializeComponent();
            _order = order;
            _currentUser = user;
            UserRoles = userRoles;

            // На всякий случай — если заказ не передали.
            if (_order == null)
            {
                MessageBox.Show("Заказ не передан.");
                Close();
                return;
            }
            Title = "Заказ №" + _order.OrderID;
            OrderNumberText.Text = _order.OrderID.ToString();
            OrderDateText.Text = _order.OrderDate.ToString("dd.MM.yyyy");

            if (_order.Client != null)
            {
                ClientNameText.Text = (_order.Client.LastName + " " +
                                       _order.Client.FirstName + " " +
                                       _order.Client.Patrynomic).Trim();
            }
            else
            {
                ClientNameText.Text = "(клиент не указан)";
            }
            LoadItems();
            if (UserRoles != 1)
            {
                SaveButton.Visibility = Visibility.Collapsed;
                AddItemButton.Visibility = Visibility.Collapsed;
            } 
        }

        private void LoadItems()
        {
            var items = new System.Collections.Generic.List<OrderItemView>();

            if (_order.OrderItem != null)
            {
                foreach (var oi in _order.OrderItem)
                {
                    var view = new OrderItemView
                    {
                        ProductName = oi.CommodityPosition != null && oi.CommodityPosition.Product != null
                            ? oi.CommodityPosition.Product.ProductName
                            : "(товар неизвестен)",

                        SizeText = oi.CommodityPosition != null && oi.CommodityPosition.Sizes != null
                            ? oi.CommodityPosition.Sizes.SizeValue.ToString()
                            : "—",

                        Quantity = oi.Quantity,
                        Price = oi.PriceAtOrderTime,
                        LineSum = oi.Quantity * oi.PriceAtOrderTime,
                        SourceItem = oi
                    };

                    items.Add(view);
                }
            }

            _itemsView = items;
            OrderItemsListView.ItemsSource = _itemsView;
        }
        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserRoles != 1) return;

            var item = (sender as Button)?.DataContext as OrderItemView;
            if (item == null || item.SourceItem == null) return;

            var context = GostenovaMiracleShowEntities.GetContext();
            var cp = context.CommodityPosition.FirstOrDefault(c => c.CommodityPositionID == item.SourceItem.CommodityPositionID);

            if (cp == null) return;

            //нельзя больше, чем есть на складе
            if (cp.QuantityInStock <= 0)
            {
                MessageBox.Show("Вы патаетесь добавить товара больше чем есть на складе.");
                return;
            }

            //списываем одну со склада
            cp.QuantityInStock--;
            item.SourceItem.Quantity++;
            item.Quantity = item.SourceItem.Quantity;
            item.LineSum = item.Quantity * item.Price;
            OrderItemsListView.Items.Refresh();
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserRoles != 1)
                return;

            var item = (sender as Button)?.DataContext as OrderItemView;

            if (item == null || item.SourceItem == null) 
                return;

            if (item.SourceItem.Quantity <= 1) 
                return;

            var context = GostenovaMiracleShowEntities.GetContext();
            var cp = context.CommodityPosition.FirstOrDefault(c => c.CommodityPositionID == item.SourceItem.CommodityPositionID);
            if (cp == null) 
                return;
            //возвращаем одну на склад
            cp.QuantityInStock++;

            item.SourceItem.Quantity--;
            item.Quantity = item.SourceItem.Quantity;
            item.LineSum = item.Quantity * item.Price;

            OrderItemsListView.Items.Refresh();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserRoles != 1) return;

            var item = (sender as Button)?.DataContext as OrderItemView;
            if (item == null || item.SourceItem == null) return;

            var result = MessageBox.Show("Удалить позицию?", "Подтверждение",MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            var context = GostenovaMiracleShowEntities.GetContext();

            //возвращаем всё на склад
            var cp = context.CommodityPosition.FirstOrDefault(c => c.CommodityPositionID == item.SourceItem.CommodityPositionID);
            if (cp != null)
                cp.QuantityInStock += item.SourceItem.Quantity;

            //удаляем саму позицию
            var real = context.OrderItem.FirstOrDefault(x => x.OrderItemID == item.SourceItem.OrderItemID);
            if (real != null) context.OrderItem.Remove(real);

            context.SaveChanges();

            _order.OrderItem.Remove(item.SourceItem);
            LoadItems();
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserRoles != 1)
            {
                MessageBox.Show("Только администратор может сохранять изменения.");
                return;
            }

            GostenovaMiracleShowEntities.GetContext().SaveChanges();
            MessageBox.Show("Изменения сохранены.");
        }
        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserRoles != 1)
            {
                MessageBox.Show("Только администратор может добавлять позиции.");
                return;
            }

            //выбор товара
            var productWindow = new ProductSelectWindow();
            productWindow.Owner = this;
            if (productWindow.ShowDialog() != true) return;

            var product = productWindow.SelectedProduct;
            if (product == null) return;

            //выбор размера и количества
            var sizeWindow = new SizeSelectWindow(product);
            sizeWindow.Owner = this;
            if (sizeWindow.ShowDialog() != true) return;

            int cpId = sizeWindow.SelectedCommodityPositionID;
            int qty = sizeWindow.SelectedQuantity;

            var context = GostenovaMiracleShowEntities.GetContext();

            //находим товарную позицию
            var cp = context.CommodityPosition
                .FirstOrDefault(c => c.CommodityPositionID == cpId);
            if (cp == null) return;

            //проверяем остаток
            if (cp.QuantityInStock < qty)
            {
                MessageBox.Show("На складе недостаточно товара.");
                return;
            }

            //создаём новую позицию в этом заказе
            var newItem = new OrderItem
            {
                OrderID = _order.OrderID,
                CommodityPositionID = cpId,
                Quantity = qty,
                PriceAtOrderTime = cp.Product != null ? cp.Product.Price : 0
            };
            context.OrderItem.Add(newItem);

            //списываем со склада
            cp.QuantityInStock -= qty;

            context.SaveChanges();

            //перечитываем заказ и обновляем список
            _order = context.Order.FirstOrDefault(o => o.OrderID == _order.OrderID);
            LoadItems();

            MessageBox.Show("Позиция добавлена.");
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}