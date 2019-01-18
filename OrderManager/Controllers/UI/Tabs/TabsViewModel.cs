using Dragablz;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiBroker.Controllers
{
    public class TabsViewModel
    {
        private readonly ObservableCollection<TabContentViewModel> _items;
        public TabsViewModel()
        {
            _items = new ObservableCollection<TabContentViewModel>();
        }
        public TabsViewModel(params TabContentViewModel[] items)
        {
            _items = new ObservableCollection<TabContentViewModel>(items);
        }
        public ObservableCollection<TabContentViewModel> Items
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
