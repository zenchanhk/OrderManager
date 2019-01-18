using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dragablz;
using Dragablz.Dockablz;

using System.ComponentModel;

namespace AmiBroker.Controllers
{
    public class TabsViewModel1
    {        
        private readonly ObservableCollection<HeaderedItemViewModel> _items;
        public TabsViewModel1()
        {
            _items = new ObservableCollection<HeaderedItemViewModel>();
        }
        public TabsViewModel1(params HeaderedItemViewModel[] items)
        {
            _items = new ObservableCollection<HeaderedItemViewModel>(items);
        }
        public ObservableCollection<HeaderedItemViewModel> Items
        {
            get { return _items; }
        }

        // Property for dragablz
        private readonly IInterTabClient _interTabClient = new MainWindowTabClient();
        public IInterTabClient InterTabClient
        {
            get { return _interTabClient; }
        }

        public ItemActionCallback ClosingTabItemHandler
        {
            get { return ClosingTabItemHandlerImpl; }
        }
        private static void ClosingTabItemHandlerImpl(ItemActionCallbackArgs<TabablzControl> args)
        {
            //in here you can dispose stuff or cancel the close

            //here's your view model:
            var viewModel = args.DragablzItem.DataContext as HeaderedItemViewModel;

            //here's how you can cancel stuff:
            //args.Cancel();
        }
    }
}
