using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для ProductPage.xaml
    /// </summary>
    public partial class ProductPage : Page
    {
        private int PageSize = 10;
        private List<Product> CurrentPageLIst = new List<Product>();
        private int currentPage = 1;
        private List<Product> _filtredProducts;
        private Client _currentUser;//для сохранения текущего юзера
        private int UserRoles;
        public ProductPage(Client user)
        {
            InitializeComponent();
            _currentUser = user;
            UserRoles = user?.RoleID ?? 0; 
            var currentProduct = GostenovaMiracleShowEntities.GetContext().Product.ToList();
            ProductListView.ItemsSource = currentProduct;
            if(user != null)
            {
                FIOTB.Text = "Вы авторизованы как " + user.FirstName + " " +
                    user.LastName + " " + user.Patrynomic;
               switch(user.RoleID)
                {
                    case  1:
                        RoleTB.Text = "Роль: Администратор";break;
                    case 2:
                        RoleTB.Text = "Роль: Менеджер"; break;
                    case 3:
                        RoleTB.Text = "Роль: Авторизованный клиент"; break;
                }
            }
            else
            {
                FIOTB.Text = "Вы вошли гость";
                RoleTB.Text = "Роль: Гость";
            }
            ComboCateghory.SelectedIndex = 0;
            ComboBoh.SelectedIndex = 0;
            UpdateProduct();
            ApplyRolePermissions();
        }
        private void ChangePage()
        {
            PageListBox.Items.Clear();
            int totalPage = (_filtredProducts.Count + PageSize - 1) / PageSize;
            if (totalPage == 0) totalPage = 1;
            
            for(int i = 0; i <= totalPage; i++)
            {
                PageListBox.Items.Add(i);
            }
            PageListBox.SelectedItem = currentPage;

            var productPage = _filtredProducts
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
            ProductListView.ItemsSource = productPage;
        }

        private void UpdateProduct()
        {
            var currentProduct = GostenovaMiracleShowEntities.GetContext().Product.ToList();

            if (!string.IsNullOrEmpty(TBSearch.Text))
            {
                string searchText = TBSearch.Text.ToLower();
                searchText = searchText.Replace("Ё", "Е").Replace("ё", "е");
                currentProduct = currentProduct.Where(p =>
                (p.ProductName != null && p.ProductName.ToLower().Contains(searchText)) ||
                (p.Category != null && p.CategoryTitle.ToLower().Contains(searchText)) ||
                (p.Manufacture != null && p.ManufactureTitle.ToLower().Contains(searchText)) ||
                (p.SubCategory != null && p.SubCategoryTitle.ToLower().Contains(searchText))
                ).ToList();

            }

            if (ComboBoh.SelectedIndex > 0)
            {
                string sortType = (ComboBoh.SelectedItem as TextBlock).Text;

                switch(sortType)
                { 
                case "по нименованию от А до я":
                    currentProduct = currentProduct.OrderBy(p => p.ProductName).ToList();
                    break;
                case "по производителю по убыванию":
                    currentProduct = currentProduct.OrderBy(p => p.Manufacture).ToList();
                    break;
                case "По возрастнаию цены":
                    currentProduct = currentProduct.OrderBy(p => p.Price).ToList();
                    break;
                case "По убыванию цены":
                    currentProduct = currentProduct.OrderByDescending(p => p.Price).ToList();
                    break;

                }
            }

            if (ComboCateghory.SelectedIndex == 1)
            {
                currentProduct = currentProduct.Where(p => p.CategoryTitle == "Детская обувь").ToList();
            }
            if (ComboCateghory.SelectedIndex == 2)
            {
                currentProduct = currentProduct.Where(p => p.CategoryTitle == "Женская обувь").ToList();
            }
            if (ComboCateghory.SelectedIndex == 3)
            {
                currentProduct = currentProduct.Where(p => p.CategoryTitle == "Мужская обувь").ToList();
            }

            _filtredProducts = currentProduct;
            currentPage = 1;
            ChangePage();
        }

        private void ApplyRolePermissions()
        {
            // Скрываем систему поиска для неавторизованного пользователя
            if (UserRoles == 1 || UserRoles == 2 || UserRoles == 3)

            {
                TSearch.Visibility = Visibility.Visible;
                TCateghory.Visibility = Visibility.Visible;
                TBoh.Visibility = Visibility.Visible;
                TBSearch.Visibility = Visibility.Visible;
                ComboCateghory.Visibility = Visibility.Visible;
                ComboBoh.Visibility = Visibility.Visible;
            }
            else 
            {
                TSearch.Visibility = Visibility.Collapsed;
                TCateghory.Visibility = Visibility.Collapsed;
                TBoh.Visibility = Visibility.Collapsed;
                TBSearch.Visibility = Visibility.Collapsed;
                ComboCateghory.Visibility = Visibility.Collapsed;
                ComboBoh.Visibility = Visibility.Collapsed;
            }

            // Добавлять к заказу может любой авторизованный (1, 2, 3)
            if (UserRoles == 0)
                AddToOrderMenuItem.Visibility = Visibility.Collapsed;
            else
                AddToOrderMenuItem.Visibility = Visibility.Visible;
            //видимость заказов
            if (UserRoles == 1 || UserRoles == 2)
                OrdersBtn.Visibility = Visibility.Visible;
            else
                OrdersBtn.Visibility = Visibility.Collapsed;

            //видимость корзины
            if (UserRoles == 0 || Cart.IsEmpty)
                CartBtn.Visibility = Visibility.Collapsed;
            else
                CartBtn.Visibility = Visibility.Visible;

            //видимость аналитики — только для админа
            if (UserRoles == 1)
                AnalyticsBtn.Visibility = Visibility.Visible;
            else
                AnalyticsBtn.Visibility = Visibility.Collapsed;


            UpdateCartButton();
        }

        private void UpdateCartButton()
        {
            // Гостю корзину не показываем вообще
            if (UserRoles == 0)
            {
                CartBtn.Visibility = Visibility.Collapsed;
                return;
            }

            // Если корзина пустая — прячем, иначе показываем
            CartBtn.Visibility = Cart.IsEmpty
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
        private void TBSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateProduct();
        }

        private void ComboCateghory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProduct();
        }

        private void ComboBoh_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProduct();
        }

        private void LeftDirBtn_Click(object sender, RoutedEventArgs e)
        {
            int totalPage = (_filtredProducts.Count - 1) / PageSize;
            if(currentPage > 1)
            {
                currentPage--;
                ChangePage();
            }
        }

        private void RightDirBtn_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (_filtredProducts.Count + PageSize - 1) / PageSize;
            if (currentPage < totalPages)
            {
                currentPage++;
                ChangePage();
            }
        }

        private void PageListBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (PageListBox.SelectedItem != null)
            {
                int page = (int)PageListBox.SelectedItem;
                if (page != currentPage)
                {
                    currentPage = page;
                    ChangePage();
                }
            }
        }

        private void MenuItemAddToOrderClick(object sender, RoutedEventArgs e)
        {
            if (UserRoles == 0)
            {
                MessageBox.Show("Войдите, чтобы добавить товар к заказу.");
                return;
            }

            var product = ProductListView.SelectedItem as Product;
            if (product == null)
            {
                MessageBox.Show("Не выбран товар.");
                return;
            }

            //открываем окно выбора размера
            var window = new SizeSelectWindow(product);
            window.Owner = Application.Current.MainWindow;

            //если пользователь подтвердил — кладём в корзину
            if (window.ShowDialog() == true)
            {
                Cart.Add(window.SelectedCommodityPositionID, window.SelectedQuantity);
            }

            //обновляем видимость кнопки корзины
            UpdateCartButton();
        }



        private void OrdersBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new OrderHistoryPage(_currentUser, UserRoles));
        }

        private void AnalyticsBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AnalyticsPage(_currentUser, UserRoles));
        }
        private void CartBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new CartWindow(_currentUser);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
            UpdateCartButton();
        }
        //123
    }
}
