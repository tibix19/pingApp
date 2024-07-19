using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using Formatting = Newtonsoft.Json.Formatting;

namespace pingApp
{
    public class PingApp : INotifyPropertyChanged
    {

        // Binding 
        private Postes _post = null;
        public Postes SelectedPost
        {
            get { return this._post; }
            set
            {
                if (value == null)
                {
                    this._post = new Postes();
                }
                else
                {
                    this._post = value;
                }
                this.OnPropertyChanged("SelectedPost");
            }
        }

        // Binding sur l'event de recherche
        private string _search = null;
        public string Search
        {
            get
            {
                return _search;
            }
            set
            {
                this._search = value;
                this.OnPropertyChanged("Search");
                SearchPost();
            }
        }

        // Listes
        public List<Postes> PostesList { get; set; }
        public ObservableCollection<Postes> PostList_search { get; set; }
        public string PreviousPingStatus { get; set; }

        // Constructeur
        public PingApp()
        {
            this.SelectedPost = new Postes();
            this.PostesList = new List<Postes>();
            this.PostList_search = new ObservableCollection<Postes>(this.PostesList);
            ReadJSON();
            CheckAllPostesPing();
        }

        // Lire le fichier JSON avec toutes les informations et les afficher
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

        public void SearchPost()
        {
            PostList_search.Clear();

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
                var postesListCopy = PostesList.ToList();
                foreach (var post in postesListCopy.Where(post =>
                        (post.Post != null && post.Post.Contains(Search)) ||
                        (post.Ticket != null && post.Ticket.Contains(Search))).ToList())
                {
                    PostList_search.Add(post);
                }
            }
        }

        // Check all the poste ping (e.g when the app start, buton refresh)
        public async void CheckAllPostesPing()
        {
            var tasks = PostesList.Select(poste => CheckPingNetworks(poste)).ToList();
            await Task.WhenAll(tasks);
            UpdateJSON();
        }

        // check all the adresse VPN, Wifi, LAN
        public async Task CheckPingNetworks(Postes poste)
        {
            string newPingStatus;

            if (await CheckPing(poste.Post + ".intranet.chuv", 2000)) // Timeout de 2000 ms (2 secondes)
            {
                newPingStatus = "Lan";
            }
            else if (await CheckPing(poste.Post + ".vpn.intranet.chuv", 2000))
            {
                newPingStatus = "VPN";
            }
            else if (await CheckPing(poste.Post + ".wifi.intranet.chuv", 2000))
            {
                newPingStatus = "Wifi";
            }
            else
            {
                newPingStatus = "False";
            }

            if (poste.IsPingSuccessful != newPingStatus)
            {
                // Notify if status has changed
                NotifyPingStatusChange(poste, newPingStatus);
                PreviousPingStatus = poste.IsPingSuccessful;
                poste.IsPingSuccessful = newPingStatus;
            }
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
                catch
                {
                    return false;
                }
            }
        }

        // Notification for post changing state
        private void NotifyPingStatusChange(Postes poste, string newPingStatus)
        {
            if(newPingStatus == "False") // si pas connecté ce message
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
                .Show();
            }     
        }


        // Get the root path of the projet
        private string GetProjectRootPath()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            return Directory.GetParent(currentDirectory).Parent.FullName;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
