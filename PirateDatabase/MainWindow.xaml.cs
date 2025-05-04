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
using System.Security.Policy;

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
            FillRankCombobox();
            FillComboboxes();
        }

        private async void btnCreatePirate_Click(object sender, RoutedEventArgs e)
        {
            //combo patten matching: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching
            try
            {
                if (txtbPirateName.Text.Length > 0 && cbRank.SelectedItem is Rank selectedRank )
                {
                    Pirate pirate = new Pirate { Name = txtbPirateName.Text, RankId=selectedRank.Id };

                    await _dbRepo.CreateNewPirate(pirate);
                    MessageBox.Show($"{pirate.Name} med rang {selectedRank.Name} är nu tillagd i databasen.");
                    FillComboboxes();


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

        //Insert syntax: https://github.com/systemvetenskap/gameCollection/blob/main/gameCollectionForelasning/MainWindow.xaml.cs
        private async void FillRankCombobox()
        {
            List<Rank> ranks = await _dbRepo.GetAllRanks();

            ranks.Insert(0, new Rank { Id = -1, Name = "**Välj rank**" });

            cbRank.ItemsSource = ranks;
            cbRank.DisplayMemberPath = "Name";
            cbRank.SelectedValuePath = "Id";
            cbRank.SelectedIndex = 0;


        }
        //Enkla sätt att fylla in Combo:https://github.com/systemvetenskap/gameCollection/blob/main/gameCollectionForelasning/MainWindow.xaml.cs
        private async void FillComboboxes()
        {
            List<Pirate> pirates = await _dbRepo.GetAllPirates();
            List<Ship> ships = await _dbRepo.GetAllShips();

            pirates.Insert(0, new Pirate { Id = -1, Name = "**Välj Pirat**" });
            ships.Insert(0, new Ship { Id = -1, Name = "**Välj Skepp**" });

            FillCombobox<Pirate>(cbSelectPirate, pirates);
            FillCombobox<Ship>(cbSelectShip, ships);
         
        }
        //Enkla sätt att fylla in Combo:https://github.com/systemvetenskap/gameCollection/blob/main/gameCollectionForelasning/MainWindow.xaml.cs
        private async void FillCombobox<T>(ComboBox cb, List<T> list)
        {
            cb.ItemsSource = list;
            cb.DisplayMemberPath = "Name";
            cb.SelectedValuePath = "Id";
            cb.SelectedIndex = 0;
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cbSelectPirate.SelectedItem is Pirate selectedPirate &&
                    cbSelectShip.SelectedItem is Ship selectedShip)
                {
                    await _dbRepo.OmboardPirateToShip(selectedPirate.Id, selectedShip.Id);
                    
                    MessageBox.Show($"{selectedPirate.Name} har bemannats på {selectedShip.Name}.");
                }
                else
                {
                    MessageBox.Show("Välj både en pirat och ett skepp");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tyvärr kunde piraten inte bemannas: {ex.Message}");
            }
        }
    }
}