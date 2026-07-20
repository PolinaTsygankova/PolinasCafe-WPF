using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PolinasCafeWPF
{
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
        }

        #region Чиста бізнес-логіка (Незалежна від UI)

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
            DirectoryInfo d1 = Directory.GetParent(baseDir);
            DirectoryInfo d2 = d1?.Parent;
            DirectoryInfo d3 = d2?.Parent;
            return d3?.FullName ?? baseDir;
        }

        #endregion
    }
}