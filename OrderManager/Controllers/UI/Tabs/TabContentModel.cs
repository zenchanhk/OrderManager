using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AmiBroker.Controllers
{
    public class TabContentModel
    {
        public TabContentModel(string header)
        {
            Header = header;
            switch (header.ToLower())
            {
                case "pending orders":
                    Content = new PendingOrderTabView() { DataContext = MainViewModel.Instance };
                    break;
                case "account":
                    Content = new AccoutTabView() { DataContext = MainViewModel.Instance };
                    break;
                case "execution":
                    Content = new ExecutionTabView() { DataContext = MainViewModel.Instance };
                    break;
                case "message":
                    Content = new MessageTabView() { DataContext = MainViewModel.Instance };
                    break;
                case "script":
                    Content = new ScriptTabView() { DataContext = MainViewModel.Instance };
                    break;
                case "log":
                    Content = new LogTabView() { DataContext = MainViewModel.Instance };
                    break;
                case "portfolio":
                    Content = new PortfolioTabView() { DataContext = MainViewModel.Instance };
                    break;
            }
        }
        public string Header { get; }
        public UserControl Content { get; }
    }
}
