using System.Windows;
using System.Windows.Media;
using FontAwesome.Sharp;

namespace AmiBroker.Controllers
{
    /// <summary>
    /// Interaction logic for AccountConfig.xaml
    /// </summary>
    public partial class AccountConfig : Window
    {
        public string AccName;
        public string Host;
        public int Port;
        public int ClientId;
        public bool IsActivate;
        public AccountConfig()
        {
            InitializeComponent();
            IconChar icon = IconChar.UserEdit;
            //this.Icon = ConvertBitmapToBitmapImage.Convert(icon.ToBitmap(16, System.Drawing.Color.DarkGray));
            //this.Icon = ImageHelper.ImageSourceForBitmap(icon.ToBitmap(16, System.Drawing.Color.DarkGray));   
            System.Windows.Media.Color indigo = Util.ConvertStringToColor("#FF4B0082");
            this.Icon = Util.mdIcons.ToImageSource<MaterialIcons>(MaterialIcons.Cogs, new SolidColorBrush(indigo), 18);
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            AccName = txtName.Text;
            Host = txtHost.Text;
            Port = int.Parse(txtPort.Text);
            ClientId = int.Parse(txtClientID.Text);
            IsActivate = (bool)chkIsEnabled.IsChecked;
            this.DialogResult = true;
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtName.Text = AccName;
            txtHost.Text = Host;
            txtPort.Text = Port > 0 ? Port.ToString() : "";
            txtClientID.Text = ClientId.ToString();
            chkIsEnabled.IsChecked = IsActivate;
        }
    }
    
}
