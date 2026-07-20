using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace PolinasCafeWPF
{
    public class BillItem
    {
        public int ItemNo { get; set; }
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public string PriceFormatted => $"{Price:C2}";
    }

    public partial class MainWindow : Window
    {
        private const int MaxItems = 5;
        private const double GstRate = 0.05;

        private string[] itemDescriptions = new string[MaxItems];
        private double[] itemPrices = new double[MaxItems];
        private int itemCount = 0;

        private int tipMethod = 3;
        private double tipValue = 0.0;

        public MainWindow()
        {
            InitializeComponent();
            ClearAllLogic();
            UpdateUI();
        }

        #region UI Event Handlers

        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (itemCount >= MaxItems)
            {
                MessageBox.Show("Cannot add more items. The bill limit is 5 items.", "Limit Reached", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string desc = txtDescription.Text.Trim();
            if (desc.Length < 3 || desc.Length > 20)
            {
                MessageBox.Show("Invalid description! Length must be between 3 and 20 characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!double.TryParse(txtPrice.Text, out double price) || price <= 0)
            {
                MessageBox.Show("Invalid price! Price must be a positive number (> 0).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AddItemLogic(desc, price);
            txtDescription.Clear();
            txtPrice.Clear();
            UpdateUI();
        }

        private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (cmbRemoveIndex.SelectedIndex < 0)
            {
                MessageBox.Show("Please select an item number to remove.", "Selection Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int selectedIndex = cmbRemoveIndex.SelectedIndex;
            RemoveItemLogic(selectedIndex);
            UpdateUI();
        }

        private void TipOption_Changed(object sender, RoutedEventArgs e)
        {
            if (lblTipValue == null || txtTipValue == null) return;

            if (rbTipPercentage.IsChecked == true)
            {
                lblTipValue.Text = "Enter Tip Percentage (%):";
                txtTipValue.IsEnabled = true;
            }
            else if (rbTipAmount.IsChecked == true)
            {
                lblTipValue.Text = "Enter Tip Fixed Amount ($):";
                txtTipValue.IsEnabled = true;
            }
            else if (rbNoTip.IsChecked == true)
            {
                lblTipValue.Text = "No tip selected:";
                txtTipValue.Text = "0";
                txtTipValue.IsEnabled = false;
            }
        }

        private void BtnApplyTip_Click(object sender, RoutedEventArgs e)
        {
            if (itemCount == 0)
            {
                MessageBox.Show("There are no items in the bill to add tip for.", "Empty Bill", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int method = 3;
            double value = 0;

            if (rbTipPercentage.IsChecked == true)
            {
                method = 1;
                if (!double.TryParse(txtTipValue.Text, out value) || value < 0)
                {
                    MessageBox.Show("Invalid tip percentage! Enter a valid non-negative number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else if (rbTipAmount.IsChecked == true)
            {
                method = 2;
                if (!double.TryParse(txtTipValue.Text, out value) || value < 0)
                {
                    MessageBox.Show("Invalid tip amount! Enter a valid non-negative number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            SetTipLogic(method, value);
            UpdateUI();
            MessageBox.Show("Tip applied successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            ClearAllLogic();
            UpdateUI();
            MessageBox.Show("All items have been cleared.", "Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (itemCount == 0)
            {
                MessageBox.Show("Nothing to save. The bill is empty.", "Empty Bill", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string fileName = txtFileName.Text.Trim();
            if (fileName.Length < 1 || fileName.Length > 10)
            {
                MessageBox.Show("Invalid filename! Length must be between 1 and 10 characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".csv";
            }

            string projectPath = GetProjectPath();
            string fullPath = Path.Combine(projectPath, fileName);

            if (SaveDataLogic(fullPath))
            {
                MessageBox.Show($"Write to file '{fileName}' was successful.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Error writing to file.", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            string fileName = txtFileName.Text.Trim();
            if (fileName.Length < 1 || fileName.Length > 10)
            {
                MessageBox.Show("Invalid filename! Length must be between 1 and 10 characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".csv";
            }

            string projectPath = GetProjectPath();
            string fullPath = Path.Combine(projectPath, fileName);

            if (!File.Exists(fullPath))
            {
                MessageBox.Show($"Error: File '{fileName}' does not exist in project folder.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (LoadDataLogic(fullPath))
            {
                UpdateUI();
                MessageBox.Show($"Read from '{fileName}' was successful.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Error reading from file or data format is invalid.", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region UI Synchronization

        private void UpdateUI()
        {
            List<BillItem> displayList = new List<BillItem>();
            cmbRemoveIndex.Items.Clear();

            for (int i = 0; i < itemCount; i++)
            {
                displayList.Add(new BillItem
                {
                    ItemNo = i + 1,
                    Description = itemDescriptions[i],
                    Price = itemPrices[i]
                });

                cmbRemoveIndex.Items.Add($"{i + 1}. {itemDescriptions[i]} ({itemPrices[i]:C2})");
            }

            lstBillItems.ItemsSource = displayList;

            if (cmbRemoveIndex.Items.Count > 0)
            {
                cmbRemoveIndex.SelectedIndex = 0;
            }

            double netTotal = CalculateNetTotal();
            double tipAmount = CalculateTipAmount(netTotal);
            double gstAmount = netTotal * GstRate;
            double totalAmount = netTotal + tipAmount + gstAmount;

            lblNetTotal.Text = $"{netTotal:C2}";
            lblTipAmount.Text = $"{tipAmount:C2}";
            lblGstAmount.Text = $"{gstAmount:C2}";
            lblTotalAmount.Text = $"{totalAmount:C2}";
        }

        #endregion

        #region Pure Business Logic

        private bool AddItemLogic(string description, double price)
        {
            if (itemCount >= MaxItems) return false;

            itemDescriptions[itemCount] = description;
            itemPrices[itemCount] = price;
            itemCount++;
            return true;
        }

        private void RemoveItemLogic(int indexToRemove)
        {
            if (indexToRemove < 0 || indexToRemove >= itemCount) return;

            for (int i = indexToRemove; i < itemCount - 1; i++)
            {
                itemDescriptions[i] = itemDescriptions[i + 1];
                itemPrices[i] = itemPrices[i + 1];
            }
            itemDescriptions[itemCount - 1] = "";
            itemPrices[itemCount - 1] = 0;
            itemCount--;
        }

        private void SetTipLogic(int method, double value)
        {
            tipMethod = method;
            tipValue = value;
        }

        private void ClearAllLogic()
        {
            for (int i = 0; i < MaxItems; i++)
            {
                itemDescriptions[i] = "";
                itemPrices[i] = 0;
            }
            itemCount = 0;
            tipMethod = 3;
            tipValue = 0;
        }

        private double CalculateNetTotal()
        {
            double sum = 0;
            for (int i = 0; i < itemCount; i++)
            {
                sum += itemPrices[i];
            }
            return sum;
        }

        private double CalculateTipAmount(double netTotal)
        {
            if (tipMethod == 1) return Math.Round((netTotal * (tipValue / 100.0)), 2);
            if (tipMethod == 2) return tipValue;
            return 0.0;
        }

        private bool SaveDataLogic(string fullPath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(fullPath))
                {
                    for (int i = 0; i < itemCount; i++)
                    {
                        string safeDesc = itemDescriptions[i].Contains(",") ? $"\"{itemDescriptions[i]}\"" : itemDescriptions[i];
                        writer.WriteLine($"{safeDesc},{itemPrices[i]}");
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool LoadDataLogic(string fullPath)
        {
            try
            {
                string[] lines = File.ReadAllLines(fullPath);
                int loadedCount = 0;
                string[] tempDescriptions = new string[MaxItems];
                double[] tempPrices = new double[MaxItems];

                for (int i = 0; i < lines.Length && loadedCount < MaxItems; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;

                    string[] parts = lines[i].Split(',');
                    if (parts.Length >= 2)
                    {
                        string desc = parts[0].Trim('"');
                        if (double.TryParse(parts[1], out double price) && desc.Length >= 3 && desc.Length <= 20 && price > 0)
                        {
                            tempDescriptions[loadedCount] = desc;
                            tempPrices[loadedCount] = price;
                            loadedCount++;
                        }
                    }
                }

                ClearAllLogic();
                for (int i = 0; i < loadedCount; i++)
                {
                    itemDescriptions[i] = tempDescriptions[i];
                    itemPrices[i] = tempPrices[i];
                }
                itemCount = loadedCount;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetProjectPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo? d1 = Directory.GetParent(baseDir);
            DirectoryInfo? d2 = d1?.Parent;
            DirectoryInfo? d3 = d2?.Parent;
            return d3?.FullName ?? baseDir;
        }

        #endregion
    }
}