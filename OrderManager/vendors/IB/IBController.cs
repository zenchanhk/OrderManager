using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IB.CSharpApiClient;
using IB.CSharpApiClient.Events;
using IBApi;

namespace AmiBroker.Controllers
{
    class IBController : ApiClient, IController, INotifyPropertyChanged
    {
        public Type Type { get { return this.GetType(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<AccountInfo> Accounts { get; }

        private ConnectionParam _pConnParam;
        public ConnectionParam ConnParam
        {
            get { return _pConnParam; }
            set
            {
                if (_pConnParam != value)
                {
                    _pConnParam = value;
                    DisplayName = "IB(" + value.AccName + ")";
                    OnPropertyChanged("ConnParam");
                }
            }
        }

        private bool _pIsConnected = false;
        public bool IsConnected
        {
            get { return ClientSocket.IsConnected(); }
            set
            {
                if (_pIsConnected != value)
                {
                    _pIsConnected = value;
                    OnPropertyChanged("IsConnected");
                }
            }
        }

        private string _pName;
        public string DisplayName
        {
            get { return _pName; }
            set
            {
                if (_pName != value)
                {
                    _pName = value;
                    OnPropertyChanged("DisplayName");
                }
            }
        }

        private string _pConnectionStatus = "Disconnected";
        public string ConnectionStatus
        {
            get { return _pConnectionStatus; }
            set
            {
                if (_pConnectionStatus != value)
                {
                    _pConnectionStatus = value;
                    OnPropertyChanged("ConnectionStatus");
                }
            }
        }

        private AccountInfo _pSelectedAccount;
        public AccountInfo SelectedAccount
        {
            get { return _pSelectedAccount; }
            set
            {
                if (_pSelectedAccount != value)
                {
                    _pSelectedAccount = value;
                    OnPropertyChanged("SelectedAccount");
                }
            }
        }
        
        private MainWindow mainWin;
        public IBController(MainWindow mw)
        {
            mainWin = mw;
            setHandler();
            Accounts = new ObservableCollection<AccountInfo>();
        }

        private void setHandler()
        {
            EventDispatcher.ConnectionStatus += eh_ConnectionStatus;
            EventDispatcher.ConnectionClosed += eh_ConnectionClosed;
            EventDispatcher.Error += eh_Error;
            EventDispatcher.OrderStatus += eh_OrderStatus;
            EventDispatcher.OpenOrder += eh_OpenOrder;
            EventDispatcher.ManagedAccounts += eh_ManagedAccounts;
            EventDispatcher.Position += eh_Position;
            EventDispatcher.UpdatePortfolio += eh_UpdatePortfolio;
            EventDispatcher.AccountSummary += eh_AccountSummary;
            EventDispatcher.AccountValue += EventDispatcher_AccountValue;
        }

        private void eh_Position(object sender, PositionEventArgs e)
        {
            
        }

        private void Request()
        { 
            ClientSocket.reqManagedAccts();            
            ClientSocket.reqAllOpenOrders();
            ClientSocket.reqAutoOpenOrders(true);
            ClientSocket.reqPositions();    // notify to swich reqAccountUpdates() to update portfolio
            ClientSocket.reqAccountSummary(1, "All", AccountSummaryTags.GetAllTags());
            //ClientSocket.reqAccountUpdates();            
        }
        public async void Connect()
        {
            try
            {
                await ConnectAsync(ConnParam.Host, ConnParam.Port, ConnParam.ClientId);
                Request();
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(TaskCanceledException))
                {
                    mainWin.MessageList.Insert(0, new Message()
                    {
                        Time = DateTime.Now,
                        Source = DisplayName,
                        Text = "Cannot connect due to time out"
                    });
                }
                if (!IsConnected)
                    ConnectionStatus = "Disconnected";
            }
        }
        private void eh_AccountSummary(object sender, AccountSummaryEventArgs e)
        {
            AccountInfo acc = Accounts.FirstOrDefault<AccountInfo>(x => x.Name == e.Account);
            System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                if (acc == null)
                {
                    Accounts.Add(new AccountInfo() { Name = e.Account });
                    mainWin.LogList.Add(new Log()
                    {
                        Source = DisplayName,
                        Time = DateTime.Now,
                        Text = "A new account has been added"
                    });
                }
                AccountTag tag = acc.Properties.FirstOrDefault<AccountTag>(x => x.Tag == e.Tag);
                if (tag == null)
                    acc.Properties.Add(new AccountTag() { Tag = e.Tag, Currency = e.Currency, Value = e.Value });
                else
                    tag.Value = e.Value;
            });
        }
        private void eh_ManagedAccounts(object sender, ManagedAccountsEventArgs e)
        {
            List<string> accounts = e.ManagedAccounts as List<string>;
            System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                foreach (var acc in accounts)
                {
                    AccountInfo acc1 = Accounts.FirstOrDefault<AccountInfo>(x => x.Name == acc);
                    if (acc1 == null)
                        Accounts.Add(new AccountInfo() { Name = acc });
                }
                if (SelectedAccount == null && Accounts.Count > 0)
                    SelectedAccount = Accounts[0];
            });
        }
        private void eh_OpenOrder(object sender, OpenOrderEventArgs e)
        {
            System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                DisplayedOrder dOrder = mainWin.Orders.FirstOrDefault<DisplayedOrder>(x => x.OrderId == e.OrderId);
                if (dOrder == null)
                {
                    dOrder = new DisplayedOrder()
                    {
                        OrderId = e.Order.OrderId,
                        Action = e.Order.Action,
                        Type = e.Order.OrderType,
                        Symbol = e.Contract.Symbol,
                        Currency = e.Contract.Currency,
                        Status = e.OrderState.Status,
                        Account = e.Order.Account,
                        Tif = e.Order.Tif,
                        GTD = e.Order.GoodTillDate,
                        GAT = e.Order.GoodAfterTime,
                        StopPrice = e.Order.TrailStopPrice,
                        LmtPrice = e.Order.LmtPrice,
                        Quantity = e.Order.TotalQuantity,
                        Exchange = e.Contract.Exchange,
                        ParentId = e.Order.ParentId,
                        OcaGroup = e.Order.OcaGroup,
                        OcaType = e.Order.OcaType,
                        Source = DisplayName,
                        Time = DateTime.Now
                    };
                    mainWin.Orders.Insert(0, dOrder);
                }                
            });

        }
        private void eh_OrderStatus(object sender, OrderStatusEventArgs e)
        {
            DisplayedOrder dOrder = mainWin.Orders.FirstOrDefault<DisplayedOrder>(x => x.OrderId == e.OrderId);
            if (dOrder == null)
            {
                mainWin.LogList.Insert(0, new Log() { Text = String.Format("Order Id: %d cannot be found", e.OrderId), Time = DateTime.Now });
            }
            else
            {
                // make a copy if every step needs keeping
                if (mainWin.UserPreference.KeepTradeSteps)
                {
                    dOrder = dOrder.ShallowCopy();
                    dOrder.Time = DateTime.Now;
                    System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                    {
                        mainWin.Orders.Insert(0, dOrder);
                    });
                }
                System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                {
                    dOrder.Status = e.Status;
                    dOrder.Filled = e.Filled;
                    dOrder.Remaining = e.Remaining;
                    dOrder.AvgPrice = e.AvgFillPrice;
                });
            }
        }
        private void eh_UpdatePortfolio(object sender, UpdatePortfolioEventArgs e)
        {
            bool isInPortfolio = true;
            string symbol_name = e.Contract.Symbol;
            SymbolInMkt symbol = mainWin.Portfolio.FirstOrDefault<SymbolInMkt>(x => x.Symbol == symbol_name);
            if (symbol == null)
            {
                symbol = new SymbolInMkt();
                isInPortfolio = false;
            }
            System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                if (isInPortfolio)
                {
                    symbol.AvgCost = e.AverageCost;
                    symbol.MktPrice = e.MarketPrice;
                    symbol.Position = e.Position;
                    symbol.RealizedPNL = e.RealizedPNL;
                    symbol.UnrealizedPNL = e.UnrealizedPNL;
                }
                else
                {
                    symbol.Symbol = e.Contract.Symbol;
                    symbol.Currency = e.Contract.Currency;
                    symbol.Account = e.AccountName;
                    symbol.AvgCost = e.AverageCost;
                    symbol.MktPrice = e.MarketPrice;
                    symbol.Position = e.Position;
                    symbol.RealizedPNL = e.RealizedPNL;
                    symbol.UnrealizedPNL = e.UnrealizedPNL;
                    symbol.Source = DisplayName;
                    mainWin.Portfolio.Insert(0, symbol);
                }
            });
        }
        private void eh_Error(object sender, IB.CSharpApiClient.Events.ErrorEventArgs e)
        {
            string msg = e.Exception != null ? e.Exception.Message : e.Message;
            if (e.Message != null && (e.Message.Contains("Connectivity between IB and Trader Workstation has been lost")
                || e.Message.Contains("Connectivity between Trader Workstation and server is broken")))
            {
                ConnectionStatus = "Error";
            }
            if (e.Message != null && e.Message.Contains("Connectivity between IB and Trader Workstation has been restored"))
            {
                ConnectionStatus = "Connected";
            }
            if (e.Message != null && e.Message.Contains("Unable to read beyond the end of stream"))
            {
                ConnectionStatus = "Disconnected";
            }
            if (e.Exception != null)
            {
                ConnectionStatus = "Disconnected";
            }
            System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                mainWin.MessageList.Insert(0, new Message()
                {
                    Time = DateTime.Now,
                    Code = e.ErrorCode,
                    Text = msg,
                    Source = DisplayName
                });
            });

            if (!ClientSocket.IsConnected() && ConnectionStatus == "Connected")
            {
                ConnectionStatus = "Disconnected";
                return;
            }
            if (e.Message != null && e.Message.Contains("disconnect") && ConnectionStatus == "Connected")
            {
                ConnectionStatus = "Error";
            }
        }
        private void eh_ConnectionClosed(object sender, EventArgs e)
        {
            ConnectionStatus = "Disconnected";
        }

        private void eh_ConnectionStatus(object sender, IB.CSharpApiClient.Events.ConnectionStatusEventArgs e)
        {
            if (e.IsConnected)
            {
                ConnectionStatus = "Connected";
                System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                {
                    mainWin.MessageList.Insert(0, new Message()
                    {
                        Source = DisplayName,
                        Time = DateTime.Now,
                        Text = "Connected -- NextValidOrderID: " + e.NextValidOrderId
                    });
                });
            }                
            else
                ConnectionStatus = "Disconnected";
        }
        private void EventDispatcher_AccountValue(object sender, AccountValueEventArgs e)
        {
            throw new NotImplementedException();
        }
        
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
