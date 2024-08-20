using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Windows;
using System.Windows.Threading;
using System.Drawing;
using System.Windows.Input;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace pingApp
{
    public partial class MainWindow : Window
    {
        public PingApp PingApp { get; set; }
        private readonly DispatcherTimer pingTimer;

        // Construteur
        public MainWindow()
        {
            InitializeComponent();
            // Instancier
            this.PingApp = new PingApp();
            this.DataContext = this.PingApp;
            this.Title = "Ping App TIB";

            // Attacher un seul gestionnaire d'événements pour les raccourcis clavier
            this.KeyDown += Window_KeyDown;

            // Configuration du timer pour les pings périodiques
            pingTimer = new DispatcherTimer();
            pingTimer.Interval = TimeSpan.FromMinutes(3);
            pingTimer.Tick += (sender, args) => PingApp.CheckAllPostesPing();
            /*pingTimer.Tick += (s, e) =>
            {
                MessageBox.Show("Test");
            };*/
            pingTimer.Start(); // start the timer

            textbox_time.Text = "3"; // set the default value
        }

        // Action bouton ajouter
        private async void BtnAdd(object sender, RoutedEventArgs e)
        {
            // check if the SelectedPost.Post is null
            if (!string.IsNullOrWhiteSpace(this.PingApp.SelectedPost.Post))
            {
                // Enlever les espaces du nom du postes pour que le ping fonctionne parfaitement
                this.PingApp.TrimPost(this.PingApp.SelectedPost);

                this.PingApp.PostesList.Add(this.PingApp.SelectedPost); // Ajoute les éléments dans les champs dans la liste
                await Task.Run(() => PingApp.CheckPingNetworks(this.PingApp.SelectedPost, IsNewlyAdded: true)); // Check the poste if it ping directly
                this.PingApp.SelectedPost = new Postes(); // Créer un nouveau SelectedPost pour le prochain ajout
                this.PingApp.UpdateJSON(); // Update the JSON file with the value in the liste
                this.PingApp.SearchPost(); // Rafraîchit la liste après la vérification du ping
            }
        }

        // Shortcuts enter key
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.FocusedElement == textbox_hos))
            {
                BtnAdd(null, null);
                e.Handled = true;
            }
        }

        // Action du bouton Clear/Refresh
        private void BtnClear(object sender, RoutedEventArgs e)
        {
            this.PingApp.TrimPost(this.PingApp.SelectedPost);
            this.PingApp.ClearFields(); // Vide les champs
            this.PingApp.CheckAllPostesPing(); // Check ping
            this.PingApp.UpdateJSON(); // Mettre à jour la fichier JSON
            this.PingApp.SearchPost();
        }


        // Gestion de la fermeture de l'application
        private void Application_Exit(object sender,CancelEventArgs e)
        {
            this.PingApp.UpdateJSON(); // Mettre à jour le fichier en fonction de la liste
            StopPingTimer(); // Arrete le thread
        }

        // Arrete le thread pour ping
        private void StopPingTimer()
        {
            if (pingTimer != null && pingTimer.IsEnabled)
            {
                pingTimer.Stop(); // Stoper le thread si en cours
            }
        }

        // Bouton pour changer le timer
        private void BtnUpdateTimer(object sender, EventArgs e)
        {
            pingTimer.Interval = GetTimer(); // update timer 
            // comment for the dialog
            MessageTextBlock.Text = $"Le timer a été modifié.\n" +
                                    $"Ping tous les {GetTimer()}";
        }

        // Recup le timer (string) et convert en double
        private TimeSpan GetTimer()
        {
            TimeSpan defaultTimer = TimeSpan.FromMinutes(3); // Default timer 3 minutes
            // if we can convert to double we change the value of the timer
            if (double.TryParse(textbox_time.Text, out double minutes))
            {
                return TimeSpan.FromMinutes(minutes); // return new value
            }
            return defaultTimer; // return default value (3)
        }

        // Bouton refresh champs hidden
        private void BtnReset(object sender, EventArgs e)
        {
            this.PingApp.ClearFields(); // Vide les champs
            this.PingApp.UpdateJSON(); // Mettre à jour la fichier JSON
            this.PingApp.SearchPost();
        }

        // Bouton delete dans le datagrid
        private void DeletePoste_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Postes posteToDelete)
            {
                this.PingApp.DeletePoste(posteToDelete);
            }
        }
    }
}
