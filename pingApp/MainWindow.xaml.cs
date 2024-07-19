using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Windows;
using System.Windows.Threading;
using System.Drawing;
using System.Windows.Input;
using System.Threading.Tasks;

namespace pingApp
{
    public partial class MainWindow : Window
    {
        public PingApp pingApp { get; set; }
        private readonly DispatcherTimer pingTimer;

        // Construteur
        public MainWindow()
        {
            InitializeComponent();
            // Instancier
            this.pingApp = new PingApp();
            // Assigner
            this.DataContext = this.pingApp;
            this.Title = "Ping App";

            // Attacher un seul gestionnaire d'événements pour les raccourcis clavier
            this.KeyDown += Window_KeyDown;
            
            // Faire tourner les ping toutes les 2 minutes
            pingTimer = new DispatcherTimer();
            pingTimer.Interval = TimeSpan.FromMinutes(2);
            pingTimer.Tick += (sender, args) => pingApp.CheckAllPostesPing();
            pingTimer.Start();
        }

        // Action bouton delete
        private void BtnDelete(object sender, RoutedEventArgs e)
        {
            if (this.pingApp.SelectedPost != null) // check if a post is selected
            {
                this.pingApp.PostesList.Remove(this.pingApp.SelectedPost); // Remove the post from the list
                this.pingApp.SearchPost();
                this.pingApp.SelectedPost = new Postes();
                this.pingApp.SearchPost();
                this.pingApp.UpdateJSON();
            }
        }

        // Action bouton ajouter
        private async void BtnAdd(object sender, RoutedEventArgs e)
        {
            // check if the SelectedPost.Post is null
            if (!string.IsNullOrWhiteSpace(this.pingApp.SelectedPost.Post))
            {
                this.pingApp.PostesList.Add(this.pingApp.SelectedPost); // Ajoute les éléments dans les champs dans la liste
                await Task.Run(() => pingApp.CheckPingNetworks(this.pingApp.SelectedPost)); // Check the poste if it ping directly
                this.pingApp.SelectedPost = new Postes(); // Créer un nouveau SelectedPost pour le prochain ajout
                this.pingApp.UpdateJSON(); // Update the JSON file with the value in the liste
                this.pingApp.SearchPost(); // Rafraîchit la liste après la vérification du ping 
            }
        }

        // Shortcuts
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.FocusedElement == textbox_hos || Keyboard.FocusedElement == textbox_ticket))
            {
                BtnAdd(null, null);
                e.Handled = true;
            }
            else if (e.Key == Key.X && datagrid.SelectedItem != null)
            {
                BtnDelete(null, null);
                e.Handled = true;
            }
        }

        // Action bouton clear
        private void BtnClear(object sender, RoutedEventArgs e)
        {
            this.pingApp.ClearFields(); // Vide les champs
            pingApp.CheckAllPostesPing(); // Check ping
            this.pingApp.UpdateJSON(); // Mettre à jour la fichier JSON
            this.pingApp.SearchPost();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.pingApp.UpdateJSON(); // Update the JSON file with the value in the liste
        }
    }
}
