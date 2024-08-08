using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using Formatting = Newtonsoft.Json.Formatting;

namespace pingApp
{
    public class PingApp : INotifyPropertyChanged
    {

        // Champ privé pour stocker le poste sélectionné
        private Postes _post = null;
        // Propriété publique pour le poste sélectionné
        public Postes SelectedPost
        {
            // Retourne le poste actuellement sélectionné
            get { return this._post; }
            set
            {
                // Si la nouvelle valeur est null, crée un nouveau poste vide
                if (value == null)
                {
                    this._post = new Postes();
                }
                else
                {
                    this._post = value; // Sinon, assigne la nouvelle valeur
                }
                this.OnPropertyChanged("SelectedPost"); // Notifie que la propriété a changé
            }
        }

        // Propriété pour la recherche
        private string _search = null;
        public string Search
        {
            get { return _search; } // Retourne la chaîne de recherche actuelle
            set
            {
                // Assigne la nouvelle valeur de recherche
                this._search = value;
                // Notifie que le champ Search a changé
                this.OnPropertyChanged("Search");
                // Effectue une recherche avec la nouvelle valeur
                SearchPost();
            }
        }

        // Listes pour stocker les postes
        public List<Postes> PostesList { get; set; }
        public ObservableCollection<Postes> PostList_search { get; set; }

        // Stocke le statut de ping précédent
        public string PreviousPingStatus { get; set; }
        // Stocke l'ip précédante
        public string PreviousIpAddr { get; set; }

        // Constructeur de la classe PingApp
        public PingApp()
        {
            this.SelectedPost = new Postes(); // Initialise un nouveau poste sélectionné
            this.PostesList = new List<Postes>(); // Initialise la liste des postes
            this.PostList_search = new ObservableCollection<Postes>(this.PostesList); // Initialise la liste de recherche observable pour la recherche
            ReadJSON(); // Lit les données du fichier JSON
            CheckAllPostesPing(); // Vérifie le ping de tous les postes au démarrage de l'app
        }

        // Lire le fichier JSON avec toutes les données et les afficher
        public void ReadJSON()
        {
            string projectRootPath = GetProjectRootPath();
            string jsonFilePath = Path.Combine(projectRootPath, "data_json.json");
            if (File.Exists(jsonFilePath))
            {
                var postes = JsonConvert.DeserializeObject<List<Postes>>(File.ReadAllText(jsonFilePath));
                if (postes != null)
                {
                    foreach (var poste in postes)
                    {
                        PostesList.Add(poste);
                    }
                }
            }
            SearchPost();
        }

        //
        public void ClearFields()
        {
            this.SelectedPost = new Postes();
        }

        // Update the JSON file
        public void UpdateJSON()
        {
            string projectRootPath = GetProjectRootPath();
            string jsonFilePath = Path.Combine(projectRootPath, "data_json.json");
            File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(this.PostesList, Formatting.Indented));
        }

        // Recherche de postes
        public void SearchPost()
        {
            // Utiliser le Dispatcher pour s'assurer que la méthode est exécutée sur le thread UI
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Vide la liste de recherche
                PostList_search.Clear();

                // Si la chaîne de recherche est vide, ajoute tous les postes dans la liste de recherche
                if (string.IsNullOrEmpty(Search))
                {
                    foreach (Postes post in PostesList)
                    {
                        if (post != null)
                            PostList_search.Add(post);
                    }
                }
                else
                {
                    // Sinon, filtre les postes selon la chaîne de recherche
                    var postesListCopy = PostesList.ToList();
                    foreach (var post in postesListCopy.Where(post =>
                            (post.Post != null && post.Post.Contains(Search)) ||
                            (post.Ticket != null && post.Ticket.Contains(Search))).ToList())
                    {
                        PostList_search.Add(post);
                    }
                }
            });
        }

        // Check all the poste ping (e.g when the app start, buton refresh)
        public async void CheckAllPostesPing()
        {
            var tasks = PostesList.Select(poste => CheckPingNetworks(poste)).ToList();
            await Task.WhenAll(tasks);
            UpdateJSON();
        }

        // check all the adresse VPN, Wifi, LAN
        public async Task CheckPingNetworks(Postes poste, bool IsNewlyAdded = false)
        {
            string newPingStatus;
            string IpAddr;

            if (await CheckPing(poste.Post + ".intranet.chuv", 2000)) // Timeout de 2000 ms (2 secondes)
            {
                newPingStatus = "Lan";
                IpAddr = await(ResolveIPAddress(poste.Post + ".intranet.chuv"));
            }
            else if (await CheckPing(poste.Post + ".vpn.intranet.chuv", 2000))
            {
                newPingStatus = "VPN";
                IpAddr = await (ResolveIPAddress(poste.Post + ".vpn.intranet.chuv"));
            }
            else if (await CheckPing(poste.Post + ".wifi.intranet.chuv", 2000))
            {
                newPingStatus = "Wifi";
                IpAddr = await (ResolveIPAddress(poste.Post + ".wifi.intranet.chuv"));
            }
            else
            {
                newPingStatus = "False";
                IpAddr = null;
            }

            if (poste.IsPingSuccessful != newPingStatus)
            {
                if (IsNewlyAdded == false)
                {
                    // Notify if status has changed only if it is not a new post
                    NotifyPingStatusChange(poste, newPingStatus);
                }
                // Mettre à jour l'état post
                PreviousPingStatus = poste.IsPingSuccessful;
                poste.IsPingSuccessful = newPingStatus;
                
            }
            // Mettre à jour l'IP du post
            PreviousIpAddr = poste.IPAddress;
            poste.IPAddress = IpAddr;
            SearchPost();
        }

        // Make the ping
        private async Task<bool> CheckPing(string address, int timeout = 2000)
        {
            using (var ping = new Ping())
            {
                try
                {
                    var reply = await ping.SendPingAsync(address, timeout);
                    return reply.Status == IPStatus.Success;
                }
                catch {
                    return false;
                }
            }
        }

        // Get the Ip adresse
        private async Task<string> ResolveIPAddress(string hostName)
        {
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(hostName);
                // Filtrer pour obtenir uniquement les adresses IPv4
                var ipv4Address = addresses.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                return ipv4Address?.ToString();
            }
            catch
            {
                return null;
            }
        }


        // Notification for post changing state
        private void NotifyPingStatusChange(Postes poste, string newPingStatus)
        {
            if(newPingStatus == "False") // si pas connecté
            {
                new ToastContentBuilder()
                .AddArgument("action", "viewPost")
                .AddText($"{poste.Post} n'est pas connecté au réseau")
                .Show();
            }
            else // si poste connecté
            {
                new ToastContentBuilder()
                .AddArgument("action", "viewPost")
                .AddText($"{poste.Post} est connecté au {newPingStatus}")
                .AddText($"Adresse IP {PreviousIpAddr}")
                .Show();
            }     
        }


        // Enlever les espaces sur le champs post pour ne pas avoir de problème lors des pings
        public void TrimPost(Postes post)
        {
            if (post != null)
            {
                post.Post = post.Post?.Trim();
            }
        }

        public void DeletePoste(Postes poste)
        {
            PostesList.Remove(poste);
            SearchPost();
            UpdateJSON();
        }


        // Get the root path of the projet
        private string GetProjectRootPath()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            return Directory.GetParent(currentDirectory).Parent.FullName;
        }

        // Implémentation de l'interface INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        // Méthode pour notifier les changements de propriété
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
