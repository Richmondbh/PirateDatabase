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
using System.Diagnostics.Eventing.Reader;
using System;
using System.Numerics;

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
            FillComboboxes();

        }

        private async void btnCreatePirate_Click(object sender, RoutedEventArgs e)
        {
            //combo patten matching: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching
            try
            {
                if (txtbPirateName.Text.Length > 0 && cbRank.SelectedItem is PirateRank selectedRank)
                {
                    Pirate pirate = new Pirate { Name = txtbPirateName.Text, RankId = selectedRank.Id };

                    await _dbRepo.CreateNewPirate(pirate);
                    MessageBox.Show($"({pirate.Name}) med rang ({selectedRank.Name}) är nu tillagd i databasen.");
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



        //Enkla sätt att fylla in Combo:https://github.com/systemvetenskap/gameCollection/blob/main/gameCollectionForelasning/MainWindow.xaml.cs
        private async void FillComboboxes()
        {
            List<Pirate> pirates = await _dbRepo.GetAllPirates();
            List<Ship> ships = await _dbRepo.GetAllShips();
            List<PirateRank> ranks = await _dbRepo.GetAllRanks();
            List<ShipType> shipTypes = await _dbRepo.GetAllShipTypes();


            pirates.Insert(0, new Pirate { Id = -1, Name = "**Välj Pirat**" });
            ships.Insert(0, new Ship { Id = -1, Name = "**Välj Skepp**" });
            ranks.Insert(0, new PirateRank { Id = -1, Name = "**Välj Rank**" });
            shipTypes.Insert(0, new ShipType { Id = -1, Name = "**Välj Skepp Typ**" });

            FillCombobox<Pirate>(cbSelectPirate, pirates);
            FillCombobox<Ship>(cbSelectShip, ships);
            FillCombobox<Ship>(cbSelectShipToSink, ships);
            FillCombobox<Ship>(cbSelectShipToChangeCrew, ships);
            FillCombobox<PirateRank>(cbRank, ranks);
            FillCombobox<ShipType>(cbShipTypes, shipTypes);
            FillCombobox<Ship>(cbShipChange, ships);

        }

        //Enkla sätt att fylla in Combo:https://github.com/systemvetenskap/gameCollection/blob/main/gameCollectionForelasning/MainWindow.xaml.cs
        private async void FillCombobox<T>(ComboBox cb, List<T> list)
        {
            cb.ItemsSource = list;
            cb.DisplayMemberPath = "Name";
            cb.SelectedValuePath = "Id";
            cb.SelectedIndex = 0;
        }

        private async void OmboardShip_click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cbSelectPirate.SelectedItem is Pirate selectedPirate &&
                   cbSelectShip.SelectedItem is Ship selectedShip)
                {
                    await _dbRepo.OmboardPirateToShip(selectedPirate.Id, selectedShip.Id);

                    MessageBox.Show($"({selectedPirate.Name}) har bemannats på ({selectedShip.Name}).");
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

        private async void btnpirateSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string nameSearch = txtPirateSearch.Text;
                if (txtPirateSearch.Text.Length < 0)
                {
                    MessageBox.Show("Ange ett namn för att söka.");
                    return;
                }

                List<Pirate> pirateSearch = await _dbRepo.SearchFörPirateOrParrot(txtPirateSearch.Text);

                if (pirateSearch == null)
                {
                    MessageBox.Show("Ingen pirat eller papegoja hittades.");
                }

                lbPirateSearch.ItemsSource = pirateSearch;
                lbPirateSearch.DisplayMemberPath = "Name";

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Något gick fel : {ex.Message}");

            }
        }


        private async void btnSinkShip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cbSelectShipToSink.SelectedItem is Ship selectedShip)
                {
                    Ship results = await _dbRepo.SinkShip(selectedShip.Id);

                    MessageBox.Show($"Skeppet ({selectedShip.Name}) har sjunkit! Av ({results.TotalPirateCount}) pirater,  ({results.DeadCount}) har drunknat medan ({results.SurvivorsCount}) som  överlevt har simmat till Tortuga igen.");
                }

                else
                {
                    MessageBox.Show("Välj ett skepp för att sänka");
                }

            }

            catch (Exception ex)
            {
                MessageBox.Show($"Tyvärr lyckades skeppet inte sjunkas: {ex.Message}");

            }
        }

        private async void btnUpdateCrewNumber_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (cbSelectShipToChangeCrew.SelectedItem is Ship selectedShip && txtstateMaxCrewnumber.Text.Length > 0)
                {
                    int maxCrewnumber = int.Parse(txtstateMaxCrewnumber.Text);
                    string shipTypeName = await _dbRepo.UpdateCrewNumber(selectedShip.Id, maxCrewnumber);
                    MessageBox.Show($"Max antal pirater för ({selectedShip.Name}) av typ ({shipTypeName}) uppdaterat till [{maxCrewnumber}].");
                }
                else
                {
                    MessageBox.Show("Välj ett skepp och ange ett giltigt maxantal.");
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Fel vid uppdatering: {ex.Message}");
            }
        }

        private void lbPirateSearch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbPirateSearch.SelectedItem is Pirate selectedPirate)
            {
                MessageBox.Show($"{selectedPirate.Name} har Rang ({selectedPirate.RankName}), är på Skeppet ({selectedPirate.ShipName}) och " +
                                $"det finns ({selectedPirate.CrewNumber}) pirater knutna till detta skepp");
            }
        }

        private async  void btnChangeShipType_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cbShipChange.SelectedItem is Ship selectedShip && cbShipTypes.SelectedItem is ShipType selectedType)
                {
                    await _dbRepo.UpdateShipType(selectedShip.Id, selectedType.Id);
                    MessageBox.Show($"Skeppet ({selectedShip.Name}) har nu ändrats till typ ({selectedType.Name}).");
                }
                else
                {
                    MessageBox.Show("Välj både ett skepp och en skeppstyp.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fel vid uppdatering: {ex.Message}");
            }
        }
    }
    
}