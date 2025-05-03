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
using PirateDatabase.repositories;
using PirateDatabase.Models;
using System.Collections.Generic;

namespace PirateDatabase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DbRepository _dbRepo = new DbRepository();
        public MainWindow()
        {
            InitializeComponent();
            FillCombobox();
        }

        private async void btnCreatePirate_Click(object sender, RoutedEventArgs e)
        {
            //combo patten matching: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching
            try
            {
                if (txtbPirateName.Text.Length > 0 && cmbRank.SelectedItem is Rank selectedRank )
                {
                    Pirate pirate = new Pirate { Name = txtbPirateName.Text, RankId=selectedRank.Id };

                    await _dbRepo.CreateNewPirate(pirate);
                    MessageBox.Show($"{pirate.Name} med rang {selectedRank.Name} är nu tillagd i databasen.");
            
                }
                else
                {
                    MessageBox.Show("Du måste ange ett namn och välja en rang.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Det blev något fel: {ex.Message}");
            }
        }   

        private async void FillCombobox()
        {
            List<Rank> ranks = await _dbRepo.GetAllRanks();

            ranks.Insert(0, new Rank { Id = -1, Name = "**Välj rank" });

            cmbRank.ItemsSource = ranks;
            cmbRank.DisplayMemberPath = "Name";
            cmbRank.SelectedValuePath = "Id";
            cmbRank.SelectedIndex = 0;


        }
    }
}