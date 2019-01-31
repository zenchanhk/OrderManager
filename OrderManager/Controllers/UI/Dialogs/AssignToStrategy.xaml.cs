using AmiBroker.OrderManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AmiBroker.Controllers
{
    /// <summary>
    /// Interaction logic for AssignToStrategy.xaml
    /// </summary>
    public partial class AssignToStrategy : Window
    {
        public List<Strategy> Strategies { get; set; } = new List<Strategy>();
        public double AvailablePosition { get; set; }
        public Strategy SelectedItem { get; set; }
        public int AssignedPosition { get; set; }
        public AssignToStrategy()
        {
            InitializeComponent();
            Icon = (DrawingImage)this.FindResource("assignDrawingImage");
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
