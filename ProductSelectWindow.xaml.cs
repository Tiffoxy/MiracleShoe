using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace WpfApp1
{
    public partial class ProductSelectWindow : Window
    {
        //выбранный товар — его прочитает OrderWindow
        public Product SelectedProduct { get; private set; }

        public ProductSelectWindow()
        {
            InitializeComponent();

            var context = GostenovaMiracleShowEntities.GetContext();
            ProductListView.ItemsSource = context.Product.ToList();
        }

        private void ProductListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectCurrent();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectCurrent();
        }

        private void SelectCurrent()
        {
            var product = ProductListView.SelectedItem as Product;
            if (product == null)
            {
                MessageBox.Show("Выберите товар.");
                return;
            }

            SelectedProduct = product;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}