using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class SizeSelectWindow : Window
    {
        public int SelectedCommodityPositionID { get; private set; }
        public int SelectedQuantity { get; private set; }
        private Product _product;
        private int _qty = 1;

        public SizeSelectWindow(Product product)
        {
            InitializeComponent();
            _product = product;

            ProductNameText.Text = product.ProductName;
            LoadSizes();
            UpdateQtyText();
        }

        //загружаем размеры этого товара с остатками
        private void LoadSizes()
        {
            var context = GostenovaMiracleShowEntities.GetContext();

            var list = context.CommodityPosition.ToList().Where(c => c.ProductID == _product.ProductID && c.QuantityInStock > 0)
                .Select(c => new SizeItem
                {
                    CommodityPositionID = c.CommodityPositionID,
                    SizeValue = c.Sizes != null ? c.Sizes.SizeValue : 0,
                    InStock = c.QuantityInStock
                })
                .ToList();

            SizeComboBox.ItemsSource = list;
            SizeComboBox.SelectedIndex = 0;
        }

        //обновить текст количества
        private void UpdateQtyText()
        {
            QtyText.Text = _qty.ToString();
        }

        //взять остаток у выбранного размера
        private int GetSelectedInStock()
        {
            var item = SizeComboBox.SelectedItem as SizeItem;
            return item != null ? item.InStock : 0;
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            //не даём больше, чем есть на складе
            if (_qty + 1 > GetSelectedInStock()) return;

            _qty++;
            UpdateQtyText();
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_qty <= 1) return;

            _qty--;
            UpdateQtyText();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var item = SizeComboBox.SelectedItem as SizeItem;
            if (item == null)
            {
                MessageBox.Show("Выберите размер.");
                return;
            }

            //запоминаем выбор — его прочитает вызывающее окно
            SelectedCommodityPositionID = item.CommodityPositionID;
            SelectedQuantity = _qty;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    //класс-обёртка для одного размера в ComboBox
    public class SizeItem
    {
        public int CommodityPositionID { get; set; }
        public decimal SizeValue { get; set; }
        public int InStock { get; set; }

        //что показываем в ComboBox
        public string DisplayText
        {
            get { return SizeValue + " (в наличии: " + InStock + " шт.)"; }
        }
    }
}