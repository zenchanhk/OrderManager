﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using IB.CSharpApiClient;
using IB.CSharpApiClient.Events;
using IBApi;
using Sdl.MultiSelectComboBox.API;
using Easy.MessageHub;
using AmiBroker.OrderManager;
using System.Reflection;

namespace AmiBroker.Controllers
{    
    public class IBContract
    {
        public Contract Contract { get; set; }
        public double MinTick { get; set; }
        public double LotSize { get; set; }
    }
    public class IBController : ApiClient, IController, INotifyPropertyChanged
    {
        MessageHub _hub = MessageHub.Instance;
        public Type Type { get { return this.GetType(); } }
        public EClientSocket Client { get => ClientSocket; }
        public IApiEvent IBEventDispatcher { get => EventDispatcher; }
        public static Dictionary<string, IBContract> Contracts { get; } = new Dictionary<string, IBContract>();

        private int orderId = 0;

        private bool _pDummy;
        public bool Dummy
        {
            get { return _pDummy; }
            set
            {
                if (_pDummy != value)
                {
                    _pDummy = value;
                    OnPropertyChanged("Dummy");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<AccountInfo> Accounts { get; }        
        public string Vendor { get; } = "IB";
        public static string VendorFullName { get; } = "Interactive Broker";

        private ConnectionParam _pConnParam;        
        public ConnectionParam ConnParam
        {
            get { return _pConnParam; }
            set
            {
                if (_pConnParam != value)
                {
                    _pConnParam = value;
                    DisplayName = Vendor + "(" + value.AccName + ")";
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
            private set
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


        public BitmapImage Image { get; }
        public Size ImageSize { get; }
        public IItemGroup Group { get; set; }
        public bool IsEnabled { get; set; } = true; // indicate if seletable in multiselect combox

        private string last_req_account;
        private MainViewModel mainVM;
        public IBController(MainViewModel vm)
        {
            mainVM = vm; //it's neccessary since constructor being called during MainViewModel constructing
            setHandler();
            Accounts = new ObservableCollection<AccountInfo>();
            Uri uri = new Uri("pack://application:,,,/OrderManager;component/Controllers/images/ib.png");
            Image = new BitmapImage(uri);
            ImageSize = new Size(16, 16);
            Group = DefaultGroupService.GetItemGroup("IB");
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
            EventDispatcher.ContractDetails += EventDispatcher_ContractDetails;
        }

        private void EventDispatcher_ContractDetails(object sender, ContractDetailsEventArgs e)
        {
            //e.ContractDetails.Summary.ConId
            int i = 0;
        }

        public async Task<ContractDetailsEventArgs> test()
        {
            /*
            List<Contract> contracts = new List<Contract>();
            //EventDispatcher.ContractDetails += EventDispatcher_ContractDetails;
            contracts.Add(new Contract { LocalSymbol = "EUR.AUD", Exchange="IDEALPRO", SecType="CASH" });
            contracts.Add(new Contract { LocalSymbol = "MHIG9", Exchange = "HKFE", SecType = "FUT" });
            //contracts.Add(new Contract { LocalSymbol = "QQQ", Exchange = "SMART", SecType = "STK" });
            contracts.Add(new Contract { LocalSymbol = "5", Exchange = "SEHK", SecType = "STK" });
            //ClientSocket.reqContractDetails(0, contract);
            //return null;*/
            List<string> contracts = new List<string>();
            contracts.Add("EUR.AUD-IDEALPRO-CASH");
            try
            {
                foreach (var contract in contracts)
                {
                    var c = await reqContractDetailsAsync(contract);
                }                
            }
            catch (Exception ex)
            {
                int i = 0; 
                
            }
            
            return null;
        }
        public async Task<IBContract> reqContractDetailsAsync(string symbolName)
        {
            Contract contract = new Contract();
            string[] parts = symbolName.Split(new char[] { '-' });
            switch (parts.Length)
            {
                case 1:
                    contract.LocalSymbol = parts[0];
                    contract.Exchange = "SMART";
                    contract.SecType = "STK";
                    break;
                case 2:
                    contract.LocalSymbol = parts[0];
                    contract.Exchange = parts[1];
                    contract.SecType = "STK";
                    break;
                case 3:
                    contract.LocalSymbol = parts[0];
                    contract.Exchange = parts[1];
                    contract.SecType = parts[2];
                    break;
                case 4:
                    contract.LocalSymbol = parts[0];
                    contract.Exchange = parts[1];
                    contract.SecType = parts[2];
                    contract.Currency = parts[3];
                    break;
            }
            return await reqContractDetailsAsync(contract);
        }
        public async Task<IBContract> reqContractDetailsAsync(Contract contract)
        {
            string symbol = contract.LocalSymbol + "-" + contract.Exchange + "-" + contract.SecType
                + (contract.Currency != null ? "-" + contract.Currency : "");
            if (Contracts.ContainsKey(symbol))
                return Contracts[symbol];
            
            try
            {
                IBContract c = await IBTaskExt.FromEvent<EventArgs, IBContract>(
                handler =>
                {
                    EventDispatcher.ContractDetails += new EventHandler<ContractDetailsEventArgs>(handler);
                    //EventDispatcher.MarketRule += new EventHandler<MarketRuleEventArgs>(handler);
                    EventDispatcher.Error += new EventHandler<ErrorEventArgs>(handler);
                },
                (reqId) => ClientSocket.reqContractDetails(reqId, contract),
                handler =>
                {
                    EventDispatcher.ContractDetails -= new EventHandler<ContractDetailsEventArgs>(handler);
                    //EventDispatcher.MarketRule -= new EventHandler<MarketRuleEventArgs>(handler);
                    EventDispatcher.Error -= new EventHandler<ErrorEventArgs>(handler);
                },
                CancellationToken.None, this);
                Contracts.Add(symbol, c);
                return c;
            }
            catch (Exception ex)
            {
                return null;
            }            
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

        private void eh_Position(object sender, PositionEventArgs e)
        {            
            // get portfolio data
           
            if (last_req_account != e.Account)
                ClientSocket.reqAccountUpdates(true, e.Account);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountInfo"></param>
        /// <param name="orderType"></param>
        /// <param name="orderAction"></param>
        /// <param name="BarIndex"></param>
        /// <param name="posSize"></param>
        /// <returns></returns>
        private Order TransformIBOrder(AccountInfo accountInfo, Strategy strategy, BaseOrderType orderType, OrderAction orderAction, int barIndex, int posSize = 1)
        {
            Order order = new Order();
            IBOrderType ot = orderType as IBOrderType;
            SymbolInAction symbol = strategy.Symbol;
            order.Transmit = ot.Transmit;
            if (orderAction == OrderAction.Buy || orderAction == OrderAction.Short)
            {
                order.TotalQuantity = posSize * symbol.RoundLotSize;
            }
            else
            {
                int pos = 0;
                if (orderAction == OrderAction.Sell)
                    pos = strategy.AccountStat[accountInfo.Name].LongPosition;
                else if (orderAction == OrderAction.Cover)
                    pos = strategy.AccountStat[accountInfo.Name].ShortPosition;
                order.TotalQuantity = pos * symbol.RoundLotSize;
                if (pos > posSize)
                {
                    mainVM.Log(new Log
                    {
                        Time = DateTime.Now,
                        Text = string.Format("Warning: existing position(%d) is greater than specified one(%d).", pos, posSize),
                        Source = symbol.Name + "." + strategy.Name + "." + accountInfo.Name
                    });
                }
            }
            if (symbol.MinOrderSize > 0 && symbol.MaxOrderSize > 0 && 
                (order.TotalQuantity < symbol.MinOrderSize || order.TotalQuantity > symbol.MaxOrderSize))
            {
                mainVM.Log(new Log
                {
                    Time = DateTime.Now,
                    Text = string.Format("Total quantity %d is out of range(%d - %d)", order.TotalQuantity, symbol.MinOrderSize, symbol.MaxOrderSize),
                    Source = symbol.Name + "." + strategy.Name + "." + accountInfo.Name
                });
            }
            order.Action = (orderAction == OrderAction.Buy || orderAction == OrderAction.Cover) ? "BUY" : "SELL";
            order.OrderType = ot.IBCode;
            order.Account = accountInfo.Name;
            order.GoodAfterTime = ot.GoodAfterTime.ToString();
            order.GoodTillDate = ot.GoodTilDate.ToString();
            if (order.GoodTillDate != string.Empty)
                order.Tif = IBTifType.GTD.ToString();
            else
                order.Tif = ot.Tif.ToString();

            PropertyInfo[] pInfos = ot.GetType().GetProperties();
            PropertyInfo pi = pInfos.FirstOrDefault(x => x.Name == "LmtPrice");
            if (pi != null)
            {
                order.LmtPrice = (new ATAfl(pi.GetValue(ot).ToString())).GetArray()[barIndex];
                if (orderAction == OrderAction.Buy || orderAction == OrderAction.Cover)
                    order.LmtPrice += orderType.Slippage * symbol.MinTick;
                else if (orderAction == OrderAction.Short || orderAction == OrderAction.Sell)
                    order.LmtPrice -= orderType.Slippage * symbol.MinTick;
            }
            pi = pInfos.FirstOrDefault(x => x.Name == "AuxPrice");
            if (pi != null)
                order.AuxPrice = (new ATAfl(pi.GetValue(ot).ToString())).GetArray()[barIndex];
            pi = pInfos.FirstOrDefault(x => x.Name == "TrailingPercent");
            if (pi != null)
                order.TrailingPercent = (new ATAfl(pi.GetValue(ot).ToString())).GetArray()[barIndex];
            pi = pInfos.FirstOrDefault(x => x.Name == "TrailStopPrice");
            if (pi != null)
                order.TrailStopPrice = (new ATAfl(pi.GetValue(ot).ToString())).GetArray()[barIndex];

            return order;
        }
        /// <summary>
        /// Place order, return OrderId if successful, otherwise, return -1
        /// </summary>
        /// <param name="accountInfo"></param>
        /// <param name="symbol"></param>
        /// <returns>-1 means failure</returns>
        public async Task<int> PlaceOrder(AccountInfo accountInfo, Strategy strategy, string symbolName, BaseOrderType orderType, OrderAction orderAction, int barIndex, int posSize = 1)
        {
            Order order = TransformIBOrder(accountInfo, strategy, orderType, orderAction, barIndex, posSize);
            Contract contract = new Contract();
            string[] parts = symbolName.Split(new char[] { '-' });
            switch (parts.Length)
            {
                case 1:
                    contract.LocalSymbol = parts[0];
                    contract.Exchange = "SMART";
                    contract.SecType = "STK";
                    break;
                case 2:
                    contract.LocalSymbol = parts[0];
                    contract.Exchange = parts[1];
                    contract.SecType = "STK";
                    break;
                case 3:
                    contract.LocalSymbol = parts[0];
                    contract.Exchange = parts[1];
                    contract.SecType = parts[2];
                    break;
                case 4:
                    contract.LocalSymbol = parts[0];
                    contract.Exchange = parts[1];
                    contract.SecType = parts[2];
                    contract.Currency = parts[3];
                    break;
            }
            IBContract c = await ((IBController)accountInfo.Controller).reqContractDetailsAsync(contract);
            contract = c.Contract;
            if (contract != null)
            {
                int orderId = NextValidOrderId;
                ClientSocket.placeOrder(orderId, contract, order);
                NextValidOrderId++;
                return orderId;
            }
            return -1;
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
                    mainVM.MessageList.Insert(0, new Message()
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
            Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                if (acc == null)
                {
                    Accounts.Add(new AccountInfo(e.Account, this));
                    mainVM.LogList.Add(new Log()
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
            Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                foreach (var acc in accounts)
                {
                    AccountInfo acc1 = Accounts.FirstOrDefault<AccountInfo>(x => x.Name == acc);
                    if (acc1 == null)
                        Accounts.Add(new AccountInfo(acc, this));
                    // make the first one as selected account
                    if (SelectedAccount == null && Accounts.Count > 0)
                        SelectedAccount = Accounts[0];
                }
            });
            // reqAccountUpdate to get portfolio for each account
            for (int i = 1; i < accounts.Count(); i++)
            {
                ClientSocket.reqAccountUpdates(true, accounts[i]);
            }
            ClientSocket.reqAccountUpdates(true, accounts[0]);
            last_req_account = accounts[0];
        }
        private void eh_OpenOrder(object sender, OpenOrderEventArgs e)
        {
            Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                DisplayedOrder dOrder = mainVM.Orders.FirstOrDefault<DisplayedOrder>(x => x.OrderId == e.OrderId);
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
                        Time = DateTime.Now,
                        Contract = e.Contract,
                        Vendor = Vendor
                    };
                    mainVM.Orders.Insert(0, dOrder);
                }                
            });

        }
        private void RevertActionStatus(BaseStat stat, OrderAction orderAction)
        {
            if (orderAction == OrderAction.Buy)
            {
                stat.AccoutStatus &= ~AccountStatus.BuyPending;
            }
            else if (orderAction == OrderAction.Short)
            {
                stat.AccoutStatus &= ~AccountStatus.ShortPending;
            }
            else if (orderAction == OrderAction.Sell)
            {
                stat.AccoutStatus &= ~AccountStatus.SellPending;
            }
            else if (orderAction == OrderAction.Cover)
            {
                stat.AccoutStatus &= ~AccountStatus.CoverPending;
            }
        }
        private void eh_OrderStatus(object sender, OrderStatusEventArgs e)
        {
            DisplayedOrder dOrder = mainVM.Orders.FirstOrDefault<DisplayedOrder>(x => x.OrderId == e.OrderId);
            if (dOrder == null)
            {
                mainVM.Log(new Log() { Text = String.Format("Order Id: %d cannot be found", e.OrderId), Time = DateTime.Now });
            }
            else
            {
                // make a copy if every step needs keeping
                if (mainVM.UserPreference.KeepTradeSteps)
                {
                    dOrder = dOrder.ShallowCopy();
                    dOrder.Time = DateTime.Now;
                    Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                    {
                        mainVM.Orders.Insert(0, dOrder);
                    });
                }
                Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                {
                    dOrder.Status = e.Status;
                    dOrder.Filled = e.Filled;
                    dOrder.Remaining = e.Remaining;
                    dOrder.AvgPrice = e.AvgFillPrice;
                });
            }
            if (mainVM.OrderInfoList.ContainsKey(e.OrderId))
            {
                if (dOrder != null)
                {
                    OrderInfo oi = mainVM.OrderInfoList[e.OrderId];
                    oi.OrderStatus = dOrder;
                    BaseStat strategyStat = oi.Strategy.AccountStat[oi.Account.Name];
                    Script script = oi.Strategy.Script;
                    BaseStat scriptStat = script.AccountStat[oi.Account.Name];
                    switch (dOrder.Status)
                    {
                        case "PendingSubmit":
                        case "Submitted":
                        case "Inactive":
                            break;
                        case "ApiCancelled":
                        case "Cancelled":
                            RevertActionStatus(strategyStat, oi.OrderAction);
                            break;
                        case "Filled":
                            int filled = (int)(((int)dOrder.Filled) / script.Symbol.RoundLotSize);
                            if (oi.OrderAction == OrderAction.Buy)
                            {
                                strategyStat.AccoutStatus &= ~AccountStatus.BuyPending;
                                strategyStat.AccoutStatus &= AccountStatus.Long;
                                strategyStat.LongPosition += filled;
                                scriptStat.LongPosition += filled;
                                strategyStat.LongEntry++;
                                scriptStat.LongEntry++;
                            }
                            else if (oi.OrderAction == OrderAction.Short)
                            {
                                strategyStat.AccoutStatus &= ~AccountStatus.ShortPending;
                                strategyStat.AccoutStatus &= AccountStatus.Short;
                                strategyStat.ShortPosition += filled;
                                scriptStat.ShortPosition += filled;
                                strategyStat.ShortEntry++;
                                scriptStat.ShortEntry++;
                            }
                            else if (oi.OrderAction == OrderAction.Sell)
                            {
                                strategyStat.AccoutStatus &= ~AccountStatus.SellPending;
                                strategyStat.LongPosition -= filled;
                                scriptStat.LongPosition -= filled;
                                if (strategyStat.LongPosition == 0)
                                    strategyStat.AccoutStatus &= ~AccountStatus.Long;
                            }
                            else if (oi.OrderAction == OrderAction.Cover)
                            {
                                strategyStat.AccoutStatus &= ~AccountStatus.CoverPending;
                                strategyStat.ShortPosition -= filled;
                                scriptStat.ShortPosition -= filled;
                                if (strategyStat.ShortPosition == 0)
                                    strategyStat.AccoutStatus &= ~AccountStatus.Short;
                            }
                            break;
                    }
                }                    
            }
            else
            {
                //throw new Exception("OrderId:" + e.OrderId + " not found");
            }
        }
        private void eh_UpdatePortfolio(object sender, UpdatePortfolioEventArgs e)
        {
            bool isInPortfolio = true;
            string symbol_name = e.Contract.Symbol;
            SymbolInMkt symbol = mainVM.Portfolio.FirstOrDefault<SymbolInMkt>(x => x.Symbol == symbol_name);
            if (symbol == null)
            {
                symbol = new SymbolInMkt();
                isInPortfolio = false;
            }
            System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                if (isInPortfolio)
                {
                    symbol.MktValue = e.MarketValue;
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
                    symbol.MktValue = e.MarketValue;
                    symbol.Position = e.Position;
                    symbol.RealizedPNL = e.RealizedPNL;
                    symbol.UnrealizedPNL = e.UnrealizedPNL;
                    symbol.Source = DisplayName;
                    symbol.Vendor = Vendor;
                    symbol.Contract = e.Contract;
                    mainVM.Portfolio.Insert(0, symbol);
                }
            });
        }
        private void eh_Error(object sender, IB.CSharpApiClient.Events.ErrorEventArgs e)
        {
            string msg = e.Exception != null ? e.Exception.Message : e.Message;
            if (mainVM.OrderInfoList.ContainsKey(e.RequestId))
            {
                OrderInfo oi = mainVM.OrderInfoList[e.RequestId];
                if (oi.Strategy.AccountStat[oi.Account.Name].AccoutStatus.ToString().ToLower().Contains("pending"))
                {
                    oi.Error = e.Message;
                    RevertActionStatus(oi.Strategy.AccountStat[oi.Account.Name], oi.OrderAction);
                    Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                    {
                        mainVM.Log(new Log()
                        {
                            Time = DateTime.Now,
                            Text = e.Message,
                            Source = oi.Strategy.Script.Symbol.Name + "." + oi.Strategy.Name
                        });
                    });
                }                
            }
            else if (e.RequestId > 0)
            {
                int i = 0;
            }
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
                mainVM.MessageList.Insert(0, new Message()
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
                    mainVM.MessageList.Insert(0, new Message()
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
            int i = 0;
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

    public static class IBTaskExt
    {
        private static int reqIdCount = 0;
        public static async Task<T> FromEvent<TEventArgs, T>(
            Action<EventHandler<TEventArgs>> registerEvent,
            System.Action<int> action,
            Action<EventHandler<TEventArgs>> unregisterEvent,
            CancellationToken token,
            object controller = null)
        {
            int reqId = reqIdCount++;
            if (reqIdCount >= int.MaxValue)
                reqIdCount = 0;

            var tcs = new TaskCompletionSource<T>();
            Contract contract = new Contract();
            EventHandler<TEventArgs> handler = (sender, args) =>
            {
                if (args.GetType() == typeof(IB.CSharpApiClient.Events.ErrorEventArgs))
                {
                    IB.CSharpApiClient.Events.ErrorEventArgs arg = args as IB.CSharpApiClient.Events.ErrorEventArgs;
                    if (arg.RequestId == reqId)
                    {
                        Exception ex = new Exception(arg.Message);
                        ex.Data.Add("RequestId", arg.RequestId);
                        ex.Data.Add("ErrorCode", arg.ErrorCode);
                        ex.Source = "IBTaskExt.FromEvent";
                        tcs.TrySetException(ex);
                    }
                }
                else if (args.GetType() == typeof(ContractDetailsEventArgs))
                {
                    ContractDetailsEventArgs arg = args as ContractDetailsEventArgs;
                    contract.ConId = arg.ContractDetails.Summary.ConId;
                    //contract.LastTradeDateOrContractMonth = arg.ContractDetails.Summary.LastTradeDateOrContractMonth;
                    contract.LocalSymbol = arg.ContractDetails.Summary.LocalSymbol;
                    contract.SecType = arg.ContractDetails.Summary.SecType;
                    contract.Symbol = arg.ContractDetails.Summary.Symbol;
                    contract.Exchange = arg.ContractDetails.Summary.Exchange;
                    contract.Currency = arg.ContractDetails.Summary.Currency;
                    IBContract ibContract = new IBContract { Contract = contract };
                    ibContract.MinTick = arg.ContractDetails.MinTick;
                    try
                    {
                        tcs.SetResult((T)(object)ibContract);
                    }
                    catch (Exception)
                    {

                        int i = 0;
                    }
                    //tcs.TrySetResult((T)(object)ibContract);
                    /*
                    string[] ruleIds = arg.ContractDetails.MarketRuleIds.Split(new char[] { ',' });
                    if (controller != null)
                    {
                        for (int i = 0; i < ruleIds.Length; i++)
                        {
                            ((IBController)controller).Client.reqMarketRule(int.Parse(ruleIds[i]));
                        }
                    }
                      */  
                }
                // get min price increment for each exchange
                else if (args.GetType() == typeof(MarketRuleEventArgs))
                {
                    MarketRuleEventArgs arg = args as MarketRuleEventArgs;
                    List<object> list = new List<object>();
                    list.Add(contract);
                    list.Add(arg.PriceIncrements);
                    tcs.TrySetResult((T)(object)list);
                }
            };
            registerEvent(handler);

            try
            {
                using (token.Register(() => tcs.SetCanceled()))
                {
                    action(reqId);
                    return await tcs.Task;
                }
            }
            finally
            {
                unregisterEvent(handler);
            }
        }
    }
}
