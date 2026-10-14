using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace WpfApp1
{
    public partial class AnalyticsPage : Page
    {
        private Client _currentUser;
        private int UserRoles;

        public AnalyticsPage(Client user, int userRoles)
        {
            InitializeComponent();
            _currentUser = user;
            UserRoles = userRoles;

            //по умолчанию — текущий месяц
            DateTime today = DateTime.Today;
            DateTime firstDay = new DateTime(today.Year, today.Month, 1);
            DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);

            StartDatePicker.SelectedDate = firstDay;
            EndDatePicker.SelectedDate = lastDay;

            //первый расчёт делаем сразу
            Recalculate();
        }

        //обработчик изменения даты — пересчитываем сразу
        private void DateChanged(object sender, SelectionChangedEventArgs e)
        {
            Recalculate();
        }

        private void Recalculate()
        {
            //пока окно грузится, SelectedDate может быть null — выходим тихо
            if (StartDatePicker == null || EndDatePicker == null) return;
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null) return;

            DateTime start = StartDatePicker.SelectedDate.Value.Date;
            DateTime end = EndDatePicker.SelectedDate.Value.Date;

            //начало не может быть позже конца
            if (start > end)
            {
                ErrorText.Text = "Дата начала не может быть позже даты окончания.";
                TotalOrdersText.Text = "Всего заказов: 0";
                TotalSumText.Text = "Общая сумма продаж: 0,00 ₽";
                TopProductsListView.ItemsSource = null;
                return;
            }

            ErrorText.Text = "";

            var context = GostenovaMiracleShowEntities.GetContext();

            //берём заказы за период включительно
            var orders = context.Order.ToList()
                .Where(o => o.OrderDate.Date >= start && o.OrderDate.Date <= end)
                .ToList();

            //1) количество заказов
            TotalOrdersText.Text = "Всего заказов: " + orders.Count;

            //2) сумма продаж
            decimal total = 0;
            foreach (var order in orders)
            {
                if (order.OrderItem == null) continue;
                foreach (var item in order.OrderItem)
                    total += item.Quantity * item.PriceAtOrderTime;
            }
            TotalSumText.Text = "Общая сумма продаж: " + total.ToString("N2") + " ₽";

            //3) топ-5 моделей по количеству проданных пар
            //собираем все OrderItem за период
            var items = new List<OrderItem>();
            foreach (var order in orders)
            {
                if (order.OrderItem == null) continue;
                items.AddRange(order.OrderItem);
            }

            //группируем по ProductID
            var top = items
                .Where(i => i.CommodityPosition != null && i.CommodityPosition.Product != null)
                .GroupBy(i => i.CommodityPosition.Product.ProductID)
                .Select(g => new
                {
                    ProductName = g.First().CommodityPosition.Product.ProductName,
                    Manufacture = g.First().CommodityPosition.Product.ManufactureTitle,
                    TotalPairs = g.Sum(i => i.Quantity)
                })
                .OrderByDescending(x => x.TotalPairs)
                .Take(5)
                .ToList();

            TopProductsListView.ItemsSource = top;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите период.");
                return;
            }

            DateTime start = StartDatePicker.SelectedDate.Value.Date;
            DateTime end = EndDatePicker.SelectedDate.Value.Date;

            if (start > end)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания.");
                return;
            }

            //куда сохранять — спрашиваем у пользователя
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "PDF файл (*.pdf)|*.pdf";
            dialog.FileName = "Отчёт_" + start.ToString("dd.MM.yyyy") + "_" + end.ToString("dd.MM.yyyy") + ".pdf";

            if (dialog.ShowDialog() != true) return;
            string path = dialog.FileName;

            var context = GostenovaMiracleShowEntities.GetContext();

            //те же данные, что на странице
            var orders = context.Order.ToList()
                .Where(o => o.OrderDate.Date >= start && o.OrderDate.Date <= end)
                .ToList();

            decimal total = 0;
            var allItems = new List<OrderItem>();
            foreach (var order in orders)
            {
                if (order.OrderItem == null) continue;
                foreach (var item in order.OrderItem)
                {
                    total += item.Quantity * item.PriceAtOrderTime;
                    allItems.Add(item);
                }
            }

            var top = allItems
                .Where(i => i.CommodityPosition != null && i.CommodityPosition.Product != null)
                .GroupBy(i => i.CommodityPosition.Product.ProductID)
                .Select(g => new
                {
                    ProductName = g.First().CommodityPosition.Product.ProductName,
                    Manufacture = g.First().CommodityPosition.Product.ManufactureTitle,
                    TotalPairs = g.Sum(i => i.Quantity)
                })
                .OrderByDescending(x => x.TotalPairs)
                .Take(5)
                .ToList();

            //шрифт с русскими буквами
            BaseFont baseFont = BaseFont.CreateFont(
                "C:\\Windows\\Fonts\\arial.ttf",
                BaseFont.IDENTITY_H,
                BaseFont.EMBEDDED);
            Font titleFont = new Font(baseFont, 18, Font.BOLD);
            Font boldFont = new Font(baseFont, 12, Font.BOLD);
            Font normalFont = new Font(baseFont, 12);

            //создаём PDF
            Document doc = new Document(PageSize.A4, 40, 40, 40, 40);
            PdfWriter.GetInstance(doc, new FileStream(path, FileMode.Create));
            doc.Open();

            //заголовок
            doc.Add(new Paragraph("Отчёт по продажам", titleFont));
            doc.Add(new Paragraph("Период: " + start.ToString("dd.MM.yyyy") + " — " + end.ToString("dd.MM.yyyy"), normalFont));
            doc.Add(new Paragraph(" ", normalFont));

            //показатели
            doc.Add(new Paragraph("Всего заказов: " + orders.Count, boldFont));
            doc.Add(new Paragraph("Общая сумма продаж: " + total.ToString("N2") + " ₽", boldFont));
            doc.Add(new Paragraph(" ", normalFont));

            //заголовок таблицы
            doc.Add(new Paragraph("Топ-5 моделей по проданным парам", boldFont));
            doc.Add(new Paragraph(" ", normalFont));

            //таблица
            PdfPTable table = new PdfPTable(3);
            table.WidthPercentage = 100;
            table.AddCell(new Phrase("Модель", boldFont));
            table.AddCell(new Phrase("Производитель", boldFont));
            table.AddCell(new Phrase("Пар продано", boldFont));

            foreach (var item in top)
            {
                table.AddCell(new Phrase(item.ProductName, normalFont));
                table.AddCell(new Phrase(item.Manufacture, normalFont));
                table.AddCell(new Phrase(item.TotalPairs.ToString(), normalFont));
            }

            doc.Add(table);
            doc.Close();

            MessageBox.Show("Отчёт сохранён: " + path);
        }
    }
}