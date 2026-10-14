using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfApp1
{
    public partial class OrderHistoryPage : Page
    {
        private Client _currentUser;
        private int UserRoles;

        public OrderHistoryPage()
        {
            InitializeComponent();
            LoadOrders();
        }

        public OrderHistoryPage(Client user, int userRoles)
        {
            InitializeComponent();
            _currentUser = user;
            UserRoles = userRoles;
            LoadOrders();
        }

        private void LoadOrders()
        {
            var context = GostenovaMiracleShowEntities.GetContext();
            // Загружаем заказы + клиента + позиции
            var orders = context.Order.Include("Client").Include("OrderItem").ToList();
            // Превращаем в список OrderView для отображения
            var view = orders.Select(o => new OrderView
            {
                OrderID = o.OrderID,
                OrderDate = o.OrderDate,
                ClientFIO = o.Client != null
                    ? $"{o.Client.LastName} {o.Client.FirstName} {o.Client.Patrynomic}".Trim()
                    : "(клиент неизвестен)",
                TotalSum = o.OrderItem != null
                    ? o.OrderItem.Sum(oi => oi.Quantity * oi.PriceAtOrderTime)
                    : 0,
                SourceOrder = o
            }).ToList();
            OrderListView.ItemsSource = view;
            //кнопка "Создать заказ" — только для админа и менеджера
            if (UserRoles == 1 || UserRoles == 2)
                CreateOrderBtn.Visibility = Visibility.Visible;
            else
                CreateOrderBtn.Visibility = Visibility.Collapsed;

            if (UserRoles == 1 || UserRoles == 2)
                DeleteOrderBtn.Visibility = Visibility.Visible;
            else
                DeleteOrderBtn.Visibility = Visibility.Collapsed;
        }
        private void DeleteOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            var selected = OrderListView.SelectedItem as OrderView;
            if (selected == null)
            {
                MessageBox.Show("Выберите заказ.");
                return;
            }
            var result = MessageBox.Show("Удалить заказ №" + selected.OrderID + "?","Подтверждение",MessageBoxButton.YesNo,MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            var context = GostenovaMiracleShowEntities.GetContext();
            //сначала удаляем все позиции заказа
            var items = context.OrderItem.ToList().Where(oi => oi.OrderID == selected.OrderID).ToList();
            foreach (var item in items)
                context.OrderItem.Remove(item);
            //потом сам заказ
            var order = context.Order.FirstOrDefault(o => o.OrderID == selected.OrderID);
            if (order != null)
                context.Order.Remove(order);
            context.SaveChanges();
            LoadOrders();
        }
        private void CreateOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null)
            {
                MessageBox.Show("Вы не авторизованы.");
                return;
            }

            var context = GostenovaMiracleShowEntities.GetContext();

            var newOrder = new Order
            {
                ClientID = _currentUser.ClientID,
                OrderDate = DateTime.Now
            };
            context.Order.Add(newOrder);
            context.SaveChanges();

            LoadOrders();
            MessageBox.Show("Заказ создан. Номер: " + newOrder.OrderID);
        }

        private void OrderListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selected = OrderListView.SelectedItem as OrderView;
            if (selected == null) return;

            // Открываем окно заказа и передаём туда заказ
            var window = new OrderWindow(selected.SourceOrder, _currentUser, UserRoles);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
            LoadOrders();
        }
        
    }
}