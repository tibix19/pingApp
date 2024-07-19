using System;
using System.ComponentModel;

namespace pingApp
{
    public class Postes : INotifyPropertyChanged
    {
        private string _post;
        public string Post
        {
            get { return _post; }
            set
            {
                if (_post != value)
                {
                    _post = value;
                    OnPropertyChanged(nameof(Post));
                }
            }
        }

        private string _ticket;
        public string Ticket
        {
            get { return _ticket; }
            set
            {
                if (_ticket != value)
                {
                    _ticket = value;
                    OnPropertyChanged(nameof(Ticket));
                }
            }
        }

        private string _isPingSuccessful;
        public string IsPingSuccessful
        {
            get { return _isPingSuccessful; }
            set
            {
                if (_isPingSuccessful != value)
                {
                    _isPingSuccessful = value;
                    OnPropertyChanged(nameof(IsPingSuccessful));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
