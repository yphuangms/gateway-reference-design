using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace PublisherDesignerApp
{
    public class SessionStatusColorConverter: IValueConverter
    {
        private static Brush Status0_Color = new SolidColorBrush(Colors.Transparent);
        private static Brush Status1_Color = new SolidColorBrush(Colors.Green);
        private static Brush Status2_Color = new SolidColorBrush(Colors.Gold);

        public object Convert(object value,Type targetType,object parameter,string language)
        {
            if (value.ToString() == "0")
            {
                return Status0_Color;
            }
            else if (value.ToString() == "1")
            {
                return Status1_Color;
            }
            else if (value.ToString() == "2")
            {
                return Status2_Color;
            }
            return Status0_Color;
        }

        public object ConvertBack(object value,Type targetType,object parameter,string language)
        {
            return value;
        }
    }

    public class SessionStatusInfo : INotifyPropertyChanged
    {
        public string Name;
        public int Status;
        public string SourceUrl;
        public string Description;
        public bool IsActive { get; set; }  = false;
        public SessionInfo Session { get; set; } = null;

        public event PropertyChangedEventHandler PropertyChanged;

        public void UpdateSessionName(string sessionName)
        {
            Name = sessionName;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
        }

        public void UpdateSessionStatus(string sessionDetail)
        {
            SourceUrl = sessionDetail;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceUrl"));
        }

        public void UpdateServerStatus(int status, string serverStatusDetail)
        {
            Status =status;
            Description = serverStatusDetail;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Description"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Status"));
        }

        public void SetActive(bool isactive)
        {
            IsActive = isactive;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsActive"));
        }
    }
}
