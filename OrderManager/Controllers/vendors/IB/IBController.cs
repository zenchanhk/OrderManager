using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using Krs.Ats.IBNet;
using Krs.Ats.IBNet.Contracts;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Sdl.MultiSelectComboBox.API;
using Easy.MessageHub;
using AmiBroker.OrderManager;
using System.Reflection;
using FastMember;

namespace AmiBroker.Controllers
{    
    public enum PriceAlignDirection
    {
        Ceiling=0,
        Floor=1
    }
    public class IBContract
    {
        public Contract Contract { get; set; }
        public decimal MinTick { get; set; }
        public double RoundLotSize { get; set; }
        public string TradingHours { get; set; }
        public int ReqId { get; set; }
        public static SecurityType SecTypeConverter(string secType)
        {
            SecurityType securityType = SecurityType.Stock;
            switch (secType.ToLower())
            {
                case "stk":
                    securityType = SecurityType.Stock;
                    break;
                case "cash":
                    securityType = SecurityType.Cash;
                    break;
                case "fut":
                    securityType = SecurityType.Future;
                    break;
                case "opt":
                    securityType = SecurityType.Option;
                    break;
                case "ind":
                    securityType = SecurityType.Index;
                    break;
                case "fop":
                    securityType = SecurityType.FutureOption;
                    break;
                case "bag":
                    securityType = SecurityType.Bag;
                    break;
                case "bond":
                    securityType = SecurityType.Bond;
                    break;
                case "war":
                    securityType = SecurityType.Warrant;
                    break;
                case "cmdty":
                    securityType = SecurityType.Commodity;
                    break;
                case "bill":
                    securityType = SecurityType.Bill;
                    break;
                default:
                    securityType = SecurityType.Undefined;
                    break;
            }
            return securityType;
        }
    }
    public class IBController : IController, INotifyPropertyChanged
    {
        private readonly object idLock = new object();
        //private readonly object ibLock = new object();
        // there is one instance for one account, so lock should not be static
        //private readonly AsyncLock m_lock = new AsyncLock();
        private int OrderIdCount = 0;

        readonly MessageHub _hub = MessageHub.Instance;
        private bool disconnectByManual = false;
        public Type Type { get { return this.GetType(); } }
        public IBClient Client { get; } = new IBClient();
        public static Dictionary<string, IBContract> Contracts { get; } = new Dictionary<string, IBContract>();
        public static Dictionary<int, Order> Orders { get; } = new Dictionary<int, Order>();

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
        public string VendorFullName { get; } = "Interactive Broker";

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
            get
            {
                return Client.Connected;
            }
            set
            {
                if (_pIsConnected != value)
                {
                    _pIsConnected = value;
                    if (value) timer.Stop();
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
            Client.NextValidId += eh_NextValidId; ;
            Client.ConnectionClosed += eh_ConnectionClosed;
            Client.Error += eh_Error;
            Client.OrderStatus += eh_OrderStatus;
            Client.OpenOrder += eh_OpenOrder;
            Client.ManagedAccounts += eh_ManagedAccounts;
            Client.Position += eh_Position;
            Client.UpdatePortfolio += eh_UpdatePortfolio;
            //EventDispatcher.AccountSummary += eh_AccountSummary;
            Client.UpdateAccountValue += eh_AccountValue;

            //Client.ConnectionStatus += eh_ConnectionStatus; 
        }

        private void eh_NextValidId(object sender, NextValidIdEventArgs e)
        {
            AfterConnected(e.OrderId);
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
        public async Task<IBContract> reqContractDetailsAsync(string symbolName, bool refresh = false, int? timeout = null)
        {
            Contract contract = new Contract();
            string[] parts = symbolName.Split(new char[] { '-' });
            switch (parts.Length)
            {
                case 1:
                    contract.LocalSymbol = parts[0];
                    contract.Exchange = "SMART";
                    contract.SecurityType = SecurityType.Stock;
                    break;
                case 2:
                    contract.LocalSymbol = parts[0];
                    contract.Exchange = parts[1];
                    contract.SecurityType = SecurityType.Stock;
                    break;
                case 3:
                    contract.LocalSymbol = parts[0];
                    contract.Exchange = parts[1];
                    contract.SecurityType = IBContract.SecTypeConverter(parts[2]);
                    break;
                case 4:
                    contract.LocalSymbol = parts[0];
                    contract.Exchange = parts[1];
                    contract.SecurityType = IBContract.SecTypeConverter(parts[2]);
                    contract.Currency = parts[3];
                    break;
            }
            return await reqContractDetailsAsync(contract, refresh, timeout);
        }
        public async Task<IBContract> reqContractDetailsAsync(Contract contract, bool refresh = false, int? timeout = null)
        {
            string symbol = contract.LocalSymbol + "-" + contract.Exchange + "-" 
                + contract.SecurityType.GetEnumDescription()
                + (contract.Currency != null ? "-" + contract.Currency : "");
            if (!refresh && Contracts.ContainsKey(symbol))
                return Contracts[symbol];
            
            try
            {
                IBContract c = await IBTaskExt.FromEvent<EventArgs, IBContract>(
                handler =>
                {
                    Client.ContractDetails += new EventHandler<ContractDetailsEventArgs>(handler);
                    //EventDispatcher.MarketRule += new EventHandler<MarketRuleEventArgs>(handler);
                    Client.Error += new EventHandler<ErrorEventArgs>(handler);
                },
                (reqId) => Client.RequestContractDetails(reqId, contract),
                handler =>
                {
                    Client.ContractDetails -= new EventHandler<ContractDetailsEventArgs>(handler);
                    //EventDispatcher.MarketRule -= new EventHandler<MarketRuleEventArgs>(handler);
                    Client.Error -= new EventHandler<ErrorEventArgs>(handler);
                },
                CancellationToken.None, this, timeout);
                if (!Contracts.ContainsKey(symbol))
                    Contracts.Add(symbol, c);
                return c;
            }
            catch (Exception ex)
            {
                //throw ex;
                if (!ex.Message.Contains("Request Contract Data Sending Error") && 
                    !ex.Message.Contains("A task was canceled"))
                    GlobalExceptionHandler.HandleException("IBController.reqContractDetailsAsync", ex);
                return null;
            }            
        }
        public async Task<int> reqIdsAsync()
        {
            try
            {
                var c = await IBTaskExt.FromEventToAsync<NextValidIdEventArgs>(
                handler =>
                {
                    Client.NextValidId += new EventHandler<NextValidIdEventArgs>(handler);
                },
                () => Client.RequestIds(1),
                handler =>
                {
                    Client.NextValidId -= new EventHandler<NextValidIdEventArgs>(handler);
                },
                CancellationToken.None,
                () => Client.NextValidId -= eh_NextValidId,
                () => Client.NextValidId += eh_NextValidId);
                return c.OrderId;
            }
            catch (Exception ex)
            {
                //throw ex;
                GlobalExceptionHandler.HandleException("IBController.reqIdsAsync", ex);
                return -1;
            }
        }
        private void Request()
        {
                Client.RequestManagedAccts();
                Client.RequestAllOpenOrders();
                Client.RequestAutoOpenOrders(true);
                Client.RequestPositions();    // notify to swich reqAccountUpdates() to update portfolio
                                              //Client.r(1, "All", AccountSummaryTags.GetAllTags());
                                              //ClientSocket.reqAccountUpdates(); 
        }

        private void eh_Position(object sender, PositionEventArgs e)
        {            
            // get portfolio data
           
            if (last_req_account != e.Account)
            {
                    Client.RequestAccountUpdates(true, e.Account);
            }
                
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
        private Order TransformIBOrder(AccountInfo accountInfo, Strategy strategy, BaseOrderType orderType, OrderAction orderAction, out string message, out decimal orgLmtPrice, double? posSize = null)
        {
            message = string.Empty;
            orgLmtPrice = -1;
            Order order = new Order();
            IBOrderType ot = orderType as IBOrderType;
            SymbolInAction symbol = strategy?.Symbol;
            order.Transmit = ot.Transmit;
            
            if (posSize == null)
            {
                float ps = 0;
                if (!string.IsNullOrEmpty(orderType.PositionSize))
                    ps = (float)(strategy.CurrentPosSize[orderType.PositionSize]);
                else
                    ps = strategy.DefaultPositionSize;

                if (orderAction == OrderAction.Buy || orderAction == OrderAction.Short)
                {
                    order.TotalQuantity = (int)(ps * symbol.RoundLotSize);
                }
                else if (orderAction == OrderAction.Sell || orderAction == OrderAction.Cover)
                {
                    double pos = 0; // get existing size of open position
                    if (orderAction == OrderAction.Sell)
                        pos = strategy.AccountStat[accountInfo.Name].LongPosition;
                    else if (orderAction == OrderAction.Cover)
                        pos = strategy.AccountStat[accountInfo.Name].ShortPosition;

                    order.TotalQuantity = (int)(Math.Min(pos, ps) * symbol.RoundLotSize);

                    if (pos > ps)
                    {
                        mainVM.Log(new Log
                        {
                            Time = DateTime.Now,
                            Text = string.Format("Warning: existing position({0:0}) is greater than specified one(({1:0}).", pos, strategy.PositionSize),
                            Source = symbol.Name + "." + strategy.Name + "." + orderAction.ToString() + "." + accountInfo.Name
                        });
                    }
                }
                else if (orderAction == OrderAction.APSLong || orderAction == OrderAction.APSShort)
                {
                    double pos = 0; // get existing size of open position
                    if (orderAction == OrderAction.APSLong)
                        pos = strategy.AccountStat[accountInfo.Name].LongPosition;
                    else if (orderAction == OrderAction.APSShort)
                        pos = strategy.AccountStat[accountInfo.Name].ShortPosition;
                    order.TotalQuantity = (int)(pos * symbol.RoundLotSize);
                }
            }
            else if (posSize == -1)
            {
                // do nothing
                // multiple slippages need calculating
            }
            else
            {
                order.TotalQuantity = (int)posSize;
            }   

            if (symbol != null && posSize != -1)
            {
                if (symbol.MinOrderSize > 0 && symbol.MaxOrderSize > 0 &&
                (order.TotalQuantity < symbol.MinOrderSize || order.TotalQuantity > symbol.MaxOrderSize))
                {
                    mainVM.Log(new Log
                    {
                        Time = DateTime.Now,
                        Text = string.Format("Total quantity {0:0} is out of range({1:0} - {2:0})", order.TotalQuantity, symbol.MinOrderSize, symbol.MaxOrderSize),
                        Source = symbol?.Name + "." + strategy?.Name + "." + accountInfo.Name
                    });
                }
            }            

            order.Action = (orderAction == OrderAction.Buy || orderAction == OrderAction.Cover 
                || orderAction == OrderAction.StoplossShort || orderAction == OrderAction.APSShort
                || orderAction == OrderAction.PreForceExitShort || orderAction == OrderAction.FinalForceExitShort) ? ActionSide.Buy : ActionSide.Sell;
            order.OrderType = ot.OrderType;
            order.Account = accountInfo.Name;
            order.GoodAfterTime = ot.GoodAfterTime.ToString();
            order.GoodTillDate = ot.GoodTilDate.ToString();
            if (order.GoodTillDate != null)
                order.Tif = TimeInForce.GoodTillDate;
            else
                order.Tif = ot.Tif;

            /*
             * Using FastMember for better performance than .Net reflection
             */
            // get prices from AFL scripts or OrderType directly

            // get MinTick for calculating prices comforming to OrderPlace
            decimal minTick = strategy != null ? strategy.Symbol.MinTick : -1;

            //get TypeAccessor
            TypeAccessor accessor = BaseOrderTypeAccessor.GetAccessor(orderType);            
            
            MemberSet members = accessor.GetMembers();
            for (int i = 0; i < members.Count; i++)
            {
                Member member = members[i];
                string fieldName = "LmtPrice";
                if (member.Name == fieldName)
                {
                    string str = (string)accessor[orderType, fieldName];
                    if (string.IsNullOrEmpty(str)) continue;
                    decimal val = 0;
                    if (double.TryParse(str, out double d))
                        val = (decimal)d;
                    else
                        val = strategy.CurrentPrices[str];

                    orgLmtPrice = val;
                    order.LimitPrice = minTick != -1 ? TruncatePrice(val, minTick) : val;
                    orderType.RealPrices[fieldName] = val;
                    continue;
                }

                fieldName = "AuxPrice";
                if (member.Name == fieldName)
                {
                    string str = (string)accessor[orderType, fieldName];
                    if (string.IsNullOrEmpty(str)) continue;
                    decimal val = 0;
                    if (double.TryParse(str, out double d))
                        val = (decimal)d;
                    else
                        val = strategy.CurrentPrices[str];

                    order.AuxPrice = minTick != -1 ? TruncatePrice(val, minTick) : val;
                    orderType.RealPrices[fieldName] = val;
                    continue;
                }

                fieldName = "TrailingPercent";
                if (member.Name == fieldName)
                {
                    string str = (string)accessor[orderType, fieldName];
                    if (string.IsNullOrEmpty(str)) continue;
                    decimal val = 0;
                    if (double.TryParse(str, out double d))
                        val = (decimal)d;
                    else
                        val = strategy.CurrentPrices[str];

                    order.TrailingPercent = (double)val;
                    orderType.RealPrices[fieldName] = val;
                    continue;
                }

                fieldName = "TrailStopPrice";
                if (member.Name == fieldName)
                {
                    string str = (string)accessor[orderType, fieldName];
                    if (string.IsNullOrEmpty(str)) continue;
                    decimal val = 0;
                    if (double.TryParse(str, out double d))
                        val = (decimal)d;
                    else
                        val = strategy.CurrentPrices[str];

                    order.TrailStopPrice = minTick != -1 ? TruncatePrice(val, minTick) : val;
                    orderType.RealPrices[fieldName] = val;
                    continue;
                }
            }

            /*
            PropertyInfo[] pInfos = ot.GetType().GetProperties();
            PropertyInfo pi = pInfos.FirstOrDefault(x => x.Name == "LmtPrice");
            string aflVar = pi?.GetValue(ot)?.ToString();
            decimal minTick = strategy != null ? strategy.Symbol.MinTick : -1;
            if (pi != null)
            {
                if (aflVar != null)
                {
                    // order.LmtPrice = TruncatePrice(strategy.CurrentPrices[aflVar], minTick);
                    // keep value as to store original value as comparision                    
                    if (double.TryParse(aflVar, out double d))
                        order.LimitPrice = (decimal)d;
                    else
                        order.LimitPrice = (decimal)(strategy.CurrentPrices[aflVar]);
                }                    
                else
                    message += "Limit price is not available. ";
            }

            pi = pInfos.FirstOrDefault(x => x.Name == "AuxPrice");
            aflVar = pi?.GetValue(ot)?.ToString();
            if (pi != null)
            {
                if (aflVar != null)
                {
                    decimal val = 0;
                    if (!double.TryParse(aflVar, out double d))
                        val = (decimal)d;
                    else
                        val = strategy.CurrentPrices[aflVar];                    
                        
                    order.AuxPrice = minTick != -1 ? TruncatePrice(val, minTick) : val;
                }
                else
                    message += "Auxarily price is not available. ";
            }

            pi = pInfos.FirstOrDefault(x => x.Name == "TrailingPercent");
            aflVar = pi?.GetValue(ot)?.ToString();
            if (pi != null)
            {
                if (aflVar != null)
                {
                    decimal val = 0;
                    if (!double.TryParse(aflVar, out double d))
                        val = (decimal)d;
                    else
                        val = strategy.CurrentPrices[aflVar];

                    order.TrailingPercent = (double)(minTick != -1 ? TruncatePrice(val, minTick) : val);
                }                    
                else
                    message += "Trailing precent is not available. ";
            }

            pi = pInfos.FirstOrDefault(x => x.Name == "TrailStopPrice");
            aflVar = pi?.GetValue(ot)?.ToString();
            if (pi != null)
            {
                if (aflVar != null)
                {
                    decimal val = 0;
                    if (!double.TryParse(aflVar, out double d))
                        val = (decimal)d;
                    else
                        val = strategy.CurrentPrices[aflVar];

                    order.TrailStopPrice = minTick != -1 ? TruncatePrice(val, minTick) : val;
                }                
                else
                    message += "Trail stop price is not available.";
            }*/

            return order;
        }
        public bool ModifyOrder(AccountInfo accountInfo, Strategy strategy, OrderAction orderAction, BaseOrderType orderType)
        {
            try 
            { 
                string src = strategy.Symbol.Name + "." + strategy.Script.Name + "." + strategy.Name;
                OrderInfo oi = strategy.AccountStat[accountInfo.Name].OrderInfos[orderAction].LastOrDefault();
                if (oi == null)
                {
                    mainVM.Log(new Log
                    {
                        Source = src,
                        Text = "Order info cannot be found in [" + accountInfo.Name + "]",
                        Time = DateTime.Now
                    });
                    return false;
                }

                IEnumerable<OrderInfo> orderInfos = strategy.AccountStat[accountInfo.Name].OrderInfos[orderAction].Where(x => x.BatchNo == oi.BatchNo);
            
                // get all prices from new OrderType
                decimal lmtPrice = -1;
                decimal auxPrice = -1;
                decimal trailStopPrice = -1;
                double trailingPercent = -1;
                
                TypeAccessor accessor = BaseOrderTypeAccessor.GetAccessor(orderType);
                decimal minTick = strategy != null ? strategy.Symbol.MinTick : -1;

                MemberSet members = accessor.GetMembers();
                for (int i = 0; i < members.Count; i++)
                {
                    Member member = members[i];
                    string fieldName = "LmtPrice";
                    if (member.Name == fieldName)
                    {
                        string str = (string)accessor[orderType, fieldName];
                        if (string.IsNullOrEmpty(str)) continue;
                        decimal val = 0;
                        if (double.TryParse(str, out double d))
                            val = (decimal)d;
                        else
                            val = strategy.CurrentPrices[str];

                        lmtPrice = minTick != -1 ? TruncatePrice(val, minTick) : val;
                        continue;
                    }

                    fieldName = "AuxPrice";
                    if (member.Name == fieldName)
                    {
                        string str = (string)accessor[orderType, fieldName];
                        if (string.IsNullOrEmpty(str)) continue;
                        decimal val = 0;
                        if (double.TryParse(str, out double d))
                            val = (decimal)d;
                        else
                            val = strategy.CurrentPrices[str];

                        auxPrice = minTick != -1 ? TruncatePrice(val, minTick) : val;
                        continue;
                    }

                    fieldName = "TrailingPercent";
                    if (member.Name == fieldName)
                    {
                        string str = (string)accessor[orderType, fieldName];
                        if (string.IsNullOrEmpty(str)) continue;
                        decimal val = 0;
                        if (double.TryParse(str, out double d))
                            val = (decimal)d;
                        else
                            val = strategy.CurrentPrices[str];

                        trailingPercent = (double)(minTick != -1 ? TruncatePrice(val, minTick) : val);
                        continue;
                    }

                    fieldName = "TrailStopPrice";
                    if (member.Name == fieldName)
                    {
                        string str = (string)accessor[orderType, fieldName];
                        if (string.IsNullOrEmpty(str)) continue;
                        decimal val = 0;
                        if (double.TryParse(str, out double d))
                            val = (decimal)d;
                        else
                            val = strategy.CurrentPrices[str];

                        trailStopPrice = minTick != -1 ? TruncatePrice(val, minTick) : val;
                        continue;
                    }
                }// END of update of order's prices

                // The size of slippages of original and new must be equal
                if (orderInfos.Count() != orderType.Slippages.Count || oi.OrderType.Slippages.Count != orderType.Slippages.Count)
                {
                    mainVM.Log(new Log
                    {
                        Source = src,
                        Time = DateTime.Now,
                        Text = "ERROR: BatchNo[" + oi.BatchNo + "]: The sizes of Slipages are not equal",
                    });
                    return false;
                }


                foreach (var orderInfo in orderInfos)
                {  
                    Order order = orderInfo.Order;
                    if (auxPrice > 0)
                        order.AuxPrice = auxPrice;
                    if (trailingPercent > 0)
                        order.TrailingPercent = trailingPercent;
                    if (trailStopPrice > 0)
                        order.TrailStopPrice = trailStopPrice;
                    if (lmtPrice > 0)
                    {
                        if (order.Action == ActionSide.Buy)
                            order.LimitPrice = lmtPrice + minTick * orderInfo.Slippage;
                        if (order.Action == ActionSide.Sell)
                            order.LimitPrice = lmtPrice - minTick * orderInfo.Slippage;
                    }
                        
                    Contract contract = orderInfo.Contract;                    
                    Task.Run(() => {
                            Client.PlaceOrder(orderInfo.RealOrderId, contract, order);
                            orderInfo.Order = order; }
                    );                    
                }

                mainVM.Log(new Log
                {
                    Source = src,
                    Time = DateTime.Now,
                    Text = "Orders [ " + string.Join(", ", orderInfos.Select(x => x.RealOrderId).ToList()) + " ] modified with new prices:"
                    + "StopPrice - " + auxPrice + ", LmtPrice - " + lmtPrice,
                });
                return true;
            }
            catch (Exception ex)
            {                
                GlobalExceptionHandler.HandleException("IBController.ModifyOrder", ex);
                return false;
            }
        }
        // modify the price to conform to minTick requirement
        private decimal TruncatePrice(decimal price, decimal minTick, PriceAlignDirection priceAlign = PriceAlignDirection.Floor)
        {
            decimal redundant = price % minTick;
            decimal modifiedPrice = priceAlign == PriceAlignDirection.Ceiling ? price - redundant + minTick : price - redundant;
            return modifiedPrice;
        }
        /// <summary>
        /// Place order, return OrderId if successful, otherwise, return -1
        /// </summary>
        /// <param name="accountInfo"></param>
        /// <param name="symbol"></param>
        /// <returns>-1 means failure</returns>
        /// TODO:
        /// 
        public async Task<List<OrderLog>> PlaceOrder(AccountInfo accountInfo, Strategy strategy, BaseOrderType orderType, 
            OrderAction orderAction, int batchNo, OrderInfo oi0 = null, double? posSize = null, Contract security = null,
            bool errorSuppressed = false, bool addToInfoList = true)
        {
            try
            {                
                // When a long position opened, 
                // If there is a future short order, position size must be added for reverse exit
                // oi saves the short order info
                if (oi0 != null)
                {
                    int oid = 0;
                    lock (idLock)
                    {
                        oid = OrderIdCount++;
                        Client.PlaceOrder(oid, oi0.Contract, oi0.Order);
                    }
                    OrderLog olog = new OrderLog();
                    olog.RealOrderId = oid;
                    olog.OrderId = ConnParam.AccName + oid;
                    olog.OrderSentTime = DateTime.Now;

                    // add into list immediately to prevent from adding later than error hanlding,
                    // which leads to order status cannot be reverted as expected
                    if (strategy != null && addToInfoList)
                    {
                        OrderInfo oi1 = oi0.CloneObject();
                        oi1.IsReversedOrder = true;
                        MainViewModel.Instance.OrderInfoList.Add(olog.OrderId, oi1);
                    }

                    List<OrderLog> logs = new List<OrderLog>();
                    logs.Add(olog);
                    return logs;
                }

                OrderLog orderLog = new OrderLog();
                string message = string.Empty;
                Order order = new Order();
                // original limit price
                decimal orgLmtPrice = -1; //used for displaying info in Log for double-checking

                if (posSize == null)
                {
                    // prevent caculating TotalQuantity
                    if (orderType.Slippages != null && orderType.Slippages.Count > 0)
                        posSize = -1;
                }
                
                if (Orders.ContainsKey(batchNo))
                    order = Orders[batchNo];
                else
                {
                    order = TransformIBOrder(accountInfo, strategy, orderType, orderAction, out message, out orgLmtPrice, posSize);
                    order.OutsideRth = strategy == null ? true : strategy.OutsideRTH;
                    Orders.Add(batchNo, order);
                }
                
                orderLog.Error = message;
                if (message != string.Empty && !errorSuppressed)
                {
                    orderLog.OrderId = "-1";
                    return new List<OrderLog> { orderLog };
                }

                Contract contract = new Contract();
                if (security == null)
                {
                    string symbolName = strategy.Symbol.SymbolDefinition.FirstOrDefault(x => x.Controller.Vendor == Vendor)?.ContractId;
                    string[] parts = symbolName.Split(new char[] { '-' });
                    switch (parts.Length)
                    {
                        case 1:
                            contract.LocalSymbol = parts[0];
                            contract.Exchange = "SMART";
                            contract.SecurityType = SecurityType.Stock;
                            break;
                        case 2:
                            contract.LocalSymbol = parts[0];
                            contract.Exchange = parts[1];
                            contract.SecurityType = SecurityType.Stock;
                            break;
                        case 3:
                            contract.LocalSymbol = parts[0];
                            contract.Exchange = parts[1];
                            contract.SecurityType = IBContract.SecTypeConverter(parts[2]);
                            break;
                        case 4:
                            contract.LocalSymbol = parts[0];
                            contract.Exchange = parts[1];
                            contract.SecurityType = IBContract.SecTypeConverter(parts[2]);
                            contract.Currency = parts[3];
                            break;
                    }
                    IBContract iBContract = await ((IBController)accountInfo.Controller).reqContractDetailsAsync(contract);
                    contract = iBContract.Contract;
                }
                else
                {
                    contract = security;
                    if (contract.Exchange == null)
                        contract.Exchange = contract.PrimaryExchange;
         
                }

                if (contract != null)
                {
                    List<OrderLog> orderLogs = new List<OrderLog>();
                    
                    // get position size from either AFL or default value
                    float _ps = 0;
                    if (strategy != null)
                    {
                        if (!string.IsNullOrEmpty(orderType.PositionSize))
                            _ps = (float)(strategy.CurrentPosSize[orderType.PositionSize]);
                        else
                            _ps = strategy.DefaultPositionSize;
                    }                    

                    // calculate total position size
                    int ttlPosSize = 0;
                    if (strategy != null)
                    {
                        if (orderAction == OrderAction.Buy || orderAction == OrderAction.Short)
                            ttlPosSize = (int)_ps;
                        else if (orderAction == OrderAction.Cover || orderAction == OrderAction.APSShort || orderAction == OrderAction.StoplossShort
                                || orderAction == OrderAction.FinalForceExitShort || orderAction == OrderAction.PreForceExitShort)
                            ttlPosSize = Math.Min((int)strategy.AccountStat[accountInfo.Name].ShortPosition, (int)_ps);
                        else if (orderAction == OrderAction.Sell || orderAction == OrderAction.APSLong || orderAction == OrderAction.StoplossLong
                                || orderAction == OrderAction.FinalForceExitLong || orderAction == OrderAction.PreForceExitLong)
                            ttlPosSize = Math.Min((int)strategy.AccountStat[accountInfo.Name].LongPosition, (int)_ps);
                    }

                    // in case of slippage table defined
                    if (orderType.Slippages != null && orderType.Slippages.Count > 0)
                    {
                        // assign position size according to the pre-defined slippage table
                        int round = 0;
                        int accuPosSize = 0;    // accumulated position size
                        List<CSlippage> slippages = new List<CSlippage>();
                        List<int> posSizes = new List<int>();
                        for (int i = 0; i < orderType.Slippages.Count; i++)
                        {
                            int ps = 0;
                            int ps_pre = orderType.Slippages[i].PosSize;
                            if (accuPosSize + ps_pre <= ttlPosSize)
                                ps = ps_pre;
                            else
                                ps = ttlPosSize - accuPosSize;

                            if (round == 0)
                            {
                                slippages.Add(orderType.Slippages[i]);
                                posSizes.Add(ps);
                            }
                            else
                                posSizes[i] += ps;

                            accuPosSize += ps;
                            if (accuPosSize == ttlPosSize) break;
                            if (accuPosSize > ttlPosSize) throw new Exception("Error in calculating Position Size (IBController.PlaceOrder)");

                            if (accuPosSize < ttlPosSize && i == orderType.Slippages.Count - 1)
                            {
                                i = -1;
                                round++;
                            }
                        }
                        // END of re-arrangement of position size

                        decimal _orgLmtP = order.LimitPrice;
                        for (int i = 0; i < slippages.Count; i++)
                        {
                            int ps = posSizes[i];
                            CSlippage slippage = orderType.Slippages[i];
                            OrderLog olog = new OrderLog();
                            olog.Error = message;

                            // assign actual position size
                            order.TotalQuantity = (int)(ps * strategy.Symbol.RoundLotSize);
                            
                            if (order.Action == ActionSide.Buy)
                            {
                                order.LimitPrice = _orgLmtP + slippage.Slippage * strategy.Symbol.MinTick;
                            }
                            else if (order.Action == ActionSide.Sell)
                            {
                                order.LimitPrice = _orgLmtP - slippage.Slippage * strategy.Symbol.MinTick;
                            }

                            // sent order
                            int orderId = 0;
                            if (!ConnParam.IsMulti)
                            {
                                //using (await m_lock.LockAsync())
                                lock(idLock)
                                {
                                    orderId = OrderIdCount++;
                                    Client.PlaceOrder(orderId, contract, order);
                                    olog.RealOrderId = orderId;
                                    olog.OrderId = ConnParam.AccName + orderId;
                                }
                            }
                            else
                            {
                                // if multi instances are running, valid order id must be obtained from IB server
                                // this will degrade the performance seriously
                                orderId = await reqIdsAsync();   
                                Client.PlaceOrder(orderId, contract, order);
                                olog.RealOrderId = orderId;
                                olog.OrderId = ConnParam.AccName + orderId;
                            }

                            olog.OrgPrice = orgLmtPrice;
                            olog.LmtPrice = order.LimitPrice;
                            olog.AuxPrice = order.AuxPrice;
                            olog.TrailingPercent = order.TrailingPercent;
                            olog.TrailStopPrice = order.TrailStopPrice;
                            olog.OrderSentTime = DateTime.Now;
                            olog.PosSize = ps;
                            olog.Slippage = slippage.Slippage;
                            // add to orderLogs as return value
                            orderLogs.Add(olog);

                            // add into list immediately to prevent from adding later than error hanlding,
                            // which leads to order status cannot be reverted as expected
                            if (strategy != null && addToInfoList)
                            {
                                OrderInfo oi = new OrderInfo
                                {
                                    RealOrderId = olog.RealOrderId,
                                    OrderId = olog.OrderId,
                                    BatchNo = batchNo,
                                    Strategy = strategy,
                                    Account = accountInfo,
                                    OrderAction = orderAction,
                                    PosSize = olog.PosSize,
                                    Slippage = (int)olog.Slippage,
                                    PlacedTime = olog.OrderSentTime,
                                    Contract = contract,
                                    Order = order,
                                    OrderType = orderType
                                };
                                MainViewModel.Instance.OrderInfoList.Add(olog.OrderId, oi);
                            }
                            
                        }
                    }
                    else
                    // no slippage defined for e.g. MarketOrder
                    {
                        int orderId = 0;
                        if (strategy != null)
                            order.TotalQuantity = (int)(ttlPosSize * strategy.Symbol.RoundLotSize);
                        else
                            order.TotalQuantity = (int)orderType.TotalQuantity;

                        if (!ConnParam.IsMulti)
                        {
                            //using (await m_lock.LockAsync())
                            lock (idLock)
                            {
                                orderId = OrderIdCount++;
                                Client.PlaceOrder(orderId, contract, order);
                                orderLog.RealOrderId = orderId;
                                orderLog.OrderId = ConnParam.AccName + orderId;
                            }
                        }
                        else
                        {
                            // if multi instances are running, valid order id must be obtained from IB server
                            // this will degrade the performance seriously
                            orderId = await reqIdsAsync();
                            Client.PlaceOrder(orderId, contract, order);
                            orderLog.RealOrderId = orderId;
                            orderLog.OrderId = ConnParam.AccName + orderId;
                        }
                        orderLog.OrderSentTime = DateTime.Now;
                        // strategy.PosSize cannot be used here because PosSize may not equal to that number.
                        orderLog.PosSize = strategy != null ? (int)(order.TotalQuantity / strategy.Symbol.RoundLotSize) : order.TotalQuantity;
                        orderLog.Slippage = null;
                        orderLogs.Add(orderLog);

                        // add into list immediately to prevent from adding later than error hanlding,
                        // which leads to order status cannot be reverted as expected
                        if (strategy != null && addToInfoList)
                        {
                            OrderInfo oi = new OrderInfo
                            {
                                RealOrderId = orderLog.RealOrderId,
                                OrderId = orderLog.OrderId,
                                BatchNo = batchNo,
                                Strategy = strategy,
                                Account = accountInfo,
                                OrderAction = orderAction,
                                PosSize = orderLog.PosSize,
                                Slippage = orderLog.Slippage != null ? (int)orderLog.Slippage : 0,
                                PlacedTime = orderLog.OrderSentTime,
                                Contract = contract,
                                Order = order,
                                OrderType = orderType
                            };
                            MainViewModel.Instance.OrderInfoList.Add(orderLog.OrderId, oi);
                        }
                        
                    }

                    return orderLogs;
                }
                else
                {
                    orderLog.OrderId = "-1";
                    orderLog.Error = "Contract cannot be found.";
                    return new List<OrderLog> { orderLog };
                }
            }
            catch (Exception ex)
            {                
                GlobalExceptionHandler.HandleException("IBController.PlaceOrder", ex);
                OrderLog orderLog = new OrderLog();
                orderLog.OrderId = "-1";
                orderLog.Error = ex.Message;
                return new List<OrderLog> { orderLog };
            }                        
        }
        public void CancelOrder(int orderId)
        {
                Client.CancelOrder(orderId);
        }
        public async Task<bool> CancelOrderAsync(int orderId)
        {
            try
            {
                bool c = await IBTaskExt.FromEvent<EventArgs, bool>(
                handler =>
                {
                    Client.OrderStatus += new EventHandler<OrderStatusEventArgs>(handler);
                    Client.Error += new EventHandler<ErrorEventArgs>(handler);
                },
                (reqId) => Client.CancelOrder(orderId),
                handler =>
                {
                    Client.OrderStatus -= new EventHandler<OrderStatusEventArgs>(handler);
                    Client.Error -= new EventHandler<ErrorEventArgs>(handler);
                },
                CancellationToken.None, orderId);
                return c;
            }
            catch (Exception ex)
            {
                //throw ex;
                GlobalExceptionHandler.HandleException("IBControler.CancelOrderAsync", ex);
                return false;
            }
        }

        // auto reconnect
        private System.Timers.Timer timer = new System.Timers.Timer();
        private void AutoReconnect()
        {
            // isconnected must be put in the first place
            if (IsConnected || disconnectByManual || timer.Enabled) return;
            int interval = mainVM.UserPreference.ConnectAttempInterval;
            if (interval > 0 && mainVM.UserPreference.ReconnectEnabled)
            {
                timer.Interval = interval * 1000;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            }            
        }
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!mainVM.UserPreference.ReconnectEnabled)
            {
                timer.Stop();
                return;
            }
            if (IsConnected) return;
            mainVM.AddMessage(new Message { Time = DateTime.Now, Text = "Trying to reconnect..." });
            if (!IsConnected)
                Connect();
            
            timer.Stop();
        }

        public void Disconnect()
        {
            Client.Disconnect();
            disconnectByManual = true;
            if (!Client.Connected)
                ConnectionStatus = "Disconnected";
        }

        public void Connect()
        {
            try
            {
                disconnectByManual = false;
                Client.Connect(ConnParam.Host, ConnParam.Port, ConnParam.ClientId);
                if (Client.Connected)
                {
                    Request();
                }                
            }
            catch (Exception ex)
            {
                mainVM.AddMessage(new Message
                {
                    Time = DateTime.Now,
                    Text = "Exception: " + ex.Message
                });
                AutoReconnect();
            }
        }
        private void eh_ManagedAccounts(object sender, ManagedAccountsEventArgs e)
        {
            List<string> accounts = e.AccountsList.Split(new char[] { ',' }).OfType<string>().ToList();
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
                Client.RequestAccountUpdates(true, accounts[i]);
            }
            Client.RequestAccountUpdates(true, accounts[0]);
            last_req_account = accounts[0];
        }
        private void eh_OpenOrder(object sender, OpenOrderEventArgs e)
        {
            Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                DisplayedOrder dOrder = mainVM.Orders.FirstOrDefault<DisplayedOrder>(x => x.OrderId == ConnParam.AccName + e.OrderId);
                if (dOrder == null)
                {
                    dOrder = new DisplayedOrder()
                    {
                        RealOrderId = e.Order.OrderId,
                        OrderId = ConnParam.AccName + e.Order.OrderId,
                        Action = e.Order.Action.ToString(),
                        Type = e.Order.OrderType.ToString(),
                        Symbol = e.Contract.Symbol,
                        Currency = e.Contract.Currency,
                        Status = e.OrderState.Status,
                        Account = e.Order.Account,
                        Tif = e.Order.Tif.ToString(),
                        GTD = e.Order.GoodTillDate,
                        GAT = e.Order.GoodAfterTime,
                        StopPrice = e.Order.AuxPrice,
                        LmtPrice = e.Order.LimitPrice,
                        Quantity = e.Order.TotalQuantity,
                        Remaining = e.Order.TotalQuantity,
                        Exchange = e.Contract.Exchange,
                        ParentId = e.Order.ParentId,
                        OcaGroup = e.Order.OcaGroup,
                        OcaType = e.Order.OcaType.ToString(),
                        Source = DisplayName,
                        Time = DateTime.Now,
                        Contract = e.Contract,
                        Vendor = Vendor
                    };
                    if (mainVM.OrderInfoList.Any(x => x.Value.RealOrderId == e.OrderId))
                    {
                        OrderInfo oi = mainVM.OrderInfoList.FirstOrDefault(x => x.Value.RealOrderId == e.OrderId).Value;
                        Strategy strategy = oi.Strategy;
                        if (strategy != null)
                            dOrder.Strategy = strategy.Symbol.Name + "." + strategy.Script.Name + "." + strategy.Name
                                            + "." + oi.Slippage;

                    }
                    mainVM.Orders.Insert(0, dOrder);
                }                
            });

        }        
        private void eh_OrderStatus(object sender, OrderStatusEventArgs e)
        {
            DisplayedOrder dOrder = mainVM.Orders.FirstOrDefault<DisplayedOrder>(x => x.OrderId == ConnParam.AccName + e.OrderId);
            if (dOrder == null)
            {
                mainVM.Log(new Log() { Text = string.Format("Order Id: {0:0} cannot be found", e.OrderId), Time = DateTime.Now });
            }
            else
            {
                // make a copy if every step needs keeping
                if (mainVM.UserPreference.KeepTradeSteps)
                {
                    DisplayedOrder tmp = null;
                    if (mainVM.UserPreference.IgnoreDuplicatedRecord)
                    {
                        tmp = mainVM.Orders.FirstOrDefault<DisplayedOrder>(x => x.OrderId == ConnParam.AccName + e.OrderId &&
                                                                    x.Remaining == e.Remaining && x.Status == e.Status);
                        if (tmp != null)
                            dOrder = tmp;   
                    }
                    if (tmp == null)
                    {
                        dOrder = dOrder.ShallowCopy();
                        dOrder.Time = DateTime.Now;
                        Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                        {
                            mainVM.Orders.Insert(0, dOrder);
                        });
                    }
                }
                Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                {
                    dOrder.Status = e.Status;
                    dOrder.Filled = e.Filled;
                    dOrder.Remaining = e.Remaining;
                    dOrder.AvgPrice = e.AverageFillPrice;
                });
            }
            if (mainVM.OrderInfoList.ContainsKey(ConnParam.AccName + e.OrderId))
            {
                if (dOrder != null)
                {
                    OrderInfo oi = mainVM.OrderInfoList[ConnParam.AccName + e.OrderId];
                    oi.OrderStatus = dOrder;
                    BaseStat strategyStat = oi.Strategy.AccountStat[oi.Account.Name];
                    Script script = oi.Strategy.Script;
                    Strategy strategy = oi.Strategy;
                    AdaptiveProfitStop apsLong = oi.Strategy.AdaptiveProfitStopforLong;
                    AdaptiveProfitStop apsShort = oi.Strategy.AdaptiveProfitStopforShort;
                    BaseStat scriptStat = script.AccountStat[oi.Account.Name];
                    dOrder.PlacedTime = oi.PlacedTime;
                    switch (dOrder.Status)
                    {
                        case OrderStatus.PendingSubmit:
                        case OrderStatus.Submitted:
                        case OrderStatus.Inactive:
                            break;
                        case OrderStatus.ApiCancelled:
                        case OrderStatus.Canceled:
                            AccountStatusOp.RevertActionStatus(ref strategyStat, ref scriptStat, strategy.Name, oi.OrderAction, true);
                            break;
                        case OrderStatus.Filled:
                        case OrderStatus.PartiallyFilled:
                            int filled = (int)Math.Round(dOrder.Filled / script.Symbol.RoundLotSize, 0);   
                            int remaining = (int)Math.Round(dOrder.Remaining / script.Symbol.RoundLotSize, 0);
                            if (oi.PosSize - oi.Filled > remaining)
                            {
                                oi.Filled += filled;
                                if (oi.OrderAction == OrderAction.Buy)
                                { 
                                    // at first, reduct ShortPosition of current strategy by filled 
                                    if (strategy.ReverseSignalForcesExit && strategyStat.ShortPosition > 0)
                                    {
                                        if (strategyStat.ShortPosition >= filled)
                                        {
                                            strategyStat.ShortPosition -= filled;
                                            scriptStat.ShortPosition -= filled;
                                            filled = 0;
                                        }
                                        else
                                        {
                                            filled -= (int)strategyStat.ShortPosition;
                                            scriptStat.ShortPosition -= strategyStat.ShortPosition;
                                            strategyStat.ShortPosition = 0;
                                        }
                                    }
                                    // secondly, reduct ShortPosition of other strategies by filled
                                    if (filled > 0 && script.ReverseSignalForcesExit && scriptStat.ShortPosition > 0)
                                    {
                                        foreach (Strategy strat in script.Strategies)
                                        {
                                            if (filled == 0) break;
                                            BaseStat stat = strat.AccountStat[oi.Account.Name];
                                            if (stat.ShortPosition > 0)
                                            {
                                                if (stat.ShortPosition >= filled)
                                                {
                                                    stat.ShortPosition -= filled;
                                                    scriptStat.ShortPosition -= filled;
                                                    filled = 0;
                                                }
                                                else
                                                {
                                                    filled -= (int)stat.ShortPosition;
                                                    scriptStat.ShortPosition -= stat.ShortPosition;
                                                    stat.ShortPosition = 0;
                                                }
                                            }
                                        }
                                    }
                                    if (filled > 0)
                                    {
                                        strategyStat.LongAvgPrice = (strategyStat.LongAvgPrice * strategyStat.LongPosition + (double)e.AverageFillPrice * filled) / (strategyStat.LongPosition + filled);
                                        strategyStat.LongPosition += filled;                                        
                                        scriptStat.LongPosition += filled;
                                        apsLong.EntryPrice = (float)strategyStat.LongAvgPrice; // (float)e.AverageFillPrice;
                                        oi.Strategy.ForceExitOrderForLong.EntryPrice = (float)e.AverageFillPrice;
                                        Entry entry = new Entry(oi.OrderId, oi.BatchNo);
                                        strategyStat.LongEntry.Add(entry);
                                        scriptStat.LongEntry.Add(entry);
                                    }                                    
                                }

                                else if (oi.OrderAction == OrderAction.Short)
                                {
                                    // at first, reduct ShortPosition of current strategy by filled 
                                    if (strategy.ReverseSignalForcesExit && strategyStat.LongPosition > 0)
                                    {
                                        if (strategyStat.LongPosition >= filled)
                                        {
                                            strategyStat.LongPosition -= filled;
                                            scriptStat.LongPosition -= filled;
                                            filled = 0;
                                        }
                                        else
                                        {
                                            filled -= (int)strategyStat.LongPosition;
                                            scriptStat.LongPosition -= strategyStat.LongPosition;
                                            strategyStat.LongPosition = 0;
                                        }
                                    }
                                    // secondly, reduct ShortPosition of other strategies by filled
                                    if (filled > 0 && script.ReverseSignalForcesExit && scriptStat.LongPosition > 0)
                                    {
                                        foreach (Strategy strat in script.Strategies)
                                        {
                                            if (filled == 0) break;
                                            BaseStat stat = strat.AccountStat[oi.Account.Name];
                                            if (stat.LongPosition > 0)
                                            {
                                                if (stat.LongPosition >= filled)
                                                {
                                                    stat.LongPosition -= filled;
                                                    scriptStat.LongPosition -= filled;
                                                    filled = 0;
                                                }
                                                else
                                                {
                                                    filled -= (int)stat.LongPosition;
                                                    scriptStat.LongPosition -= stat.LongPosition;
                                                    stat.LongPosition = 0;
                                                }
                                            }
                                        }
                                    }
                                    if (filled > 0)
                                    {
                                        strategyStat.ShortAvgPrice = (strategyStat.ShortAvgPrice * strategyStat.ShortPosition + (double)e.AverageFillPrice * filled) / (strategyStat.ShortPosition + filled);
                                        strategyStat.ShortPosition += filled;
                                        scriptStat.ShortPosition += filled;
                                        apsShort.EntryPrice = (float)strategyStat.ShortAvgPrice; // (float)e.AverageFillPrice;
                                        oi.Strategy.ForceExitOrderForShort.EntryPrice = (float)e.AverageFillPrice;
                                        // adding entry
                                        Entry entry = new Entry(oi.OrderId, oi.BatchNo);
                                        strategyStat.ShortEntry.Add(entry);
                                        scriptStat.ShortEntry.Add(entry);
                                    }
                                }

                                else if (oi.OrderAction == OrderAction.Sell || oi.OrderAction == OrderAction.APSLong
                                    || oi.OrderAction == OrderAction.StoplossLong)
                                {
                                    strategyStat.LongPosition -= filled;
                                    scriptStat.LongPosition -= filled;
                                }

                                else if (oi.OrderAction == OrderAction.Cover || oi.OrderAction == OrderAction.APSShort
                                    || oi.OrderAction == OrderAction.StoplossShort)
                                {
                                    strategyStat.ShortPosition -= filled;
                                    scriptStat.ShortPosition -= filled;
                                }
                                AccountStatusOp.RevertActionStatus(ref strategyStat, ref scriptStat, strategy.Name, oi.OrderAction);
                                AccountStatusOp.SetPositionStatus(ref strategyStat, ref scriptStat, ref strategy, oi.OrderAction);
                            }                            
                            break;
                    }
                }                    
            }
            else
            {
                //throw new Exception("OrderId:" + e.OrderId + " not found");
                //Exception ex = new Exception("OrderId:" + e.OrderId + " not found");
                //GlobalExceptionHandler.HandleException("IBController.eh_OrderStatus", ex);
                mainVM.Log(new Log() { Text = string.Format("Order Id: {0:0} cannot be found, this order may be issued mannually", e.OrderId), Time = DateTime.Now });
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
            Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                if (isInPortfolio)
                {
                    symbol.MktValue = e.MarketValue;
                    symbol.AvgCost = e.AverageCost;
                    symbol.MktPrice = e.MarketPrice;
                    symbol.Position = e.Position;
                    symbol.RealizedPNL = e.RealizedPnl;
                    symbol.UnrealizedPNL = e.UnrealizedPnl;
                    
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
                    symbol.RealizedPNL = e.RealizedPnl;
                    symbol.UnrealizedPNL = e.UnrealizedPnl;
                    symbol.Source = DisplayName;
                    symbol.Vendor = Vendor;
                    symbol.Contract = e.Contract;
                    mainVM.Portfolio.Insert(0, symbol);
                }
            });
        }
        private void eh_Error(object sender, ErrorEventArgs e)
        {
            string msg = e.ErrorMsg;
            OrderInfo oi = mainVM.OrderInfoList.FirstOrDefault(x => x.Value.OrderId == ConnParam.AccName + e.TickerId).Value;
            if (oi != null)
            {                
                oi.Error = e.ErrorMsg;
                if (oi.Strategy != null && oi.Strategy.AccountStat.ContainsKey(oi.Account.Name))
                {
                    Strategy strategy = oi.Strategy;
                    BaseStat strategyStat = oi.Strategy.AccountStat[oi.Account.Name];
                    BaseStat scriptStat = oi.Strategy.Script.AccountStat[oi.Account.Name];
                    string prevStatus = string.Join(",", Helper.TranslateAccountStatus(strategyStat.AccountStatus));
                    // ignore future placed order error message
                    //if (!e.ErrorMsg.ToLower().Contains("until"))
                    //    AccountStatusOp.RevertActionStatus(ref strategyStat, ref scriptStat, strategy.Name, oi.OrderAction);
                    Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                    {
                        mainVM.Log(new Log()
                        {
                            Time = DateTime.Now,
                            Text = e.ErrorMsg + "(OrderId:" + oi.OrderId + ", previous status:[" + prevStatus
                            + "], current status:[" + string.Join(",", Helper.TranslateAccountStatus(strategyStat.AccountStatus)) + "])",
                            Source = oi.Strategy.Script.Symbol.Name + "." + oi.Strategy.Name + "." + oi.Slippage
                        });
                    });
                }
                else
                {
                    Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                    {
                        mainVM.Log(new Log()
                        {
                            Time = DateTime.Now,
                            Text = e.ErrorMsg + "(OrderId:" + oi.OrderId + ", error: account status not found)",
                            Source = oi.Strategy != null ? oi.Strategy.Script.Symbol.Name + "." + oi.Strategy.Name + "." + oi.Slippage : "Source not found, may be issued mannually."
                        });
                    });
                }
                
            }
            else if (e.TickerId > 0)
            {
                int i = 0;
                Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                {
                    mainVM.Log(new Log()
                    {
                        Time = DateTime.Now,
                        Text = "(orderId not found)" + e.ErrorMsg,
                        Source = e.TickerId.ToString()
                    });
                });
            }
            if (e.ErrorMsg != null && (e.ErrorMsg.Contains("Connectivity between IB and Trader Workstation has been lost")
                || e.ErrorMsg.Contains("Connectivity between Trader Workstation and server is broken")))
            {
                ConnectionStatus = "Error";
            }
            if (e.ErrorMsg != null && e.ErrorMsg.Contains("Connectivity between IB and Trader Workstation has been restored"))
            {
                ConnectionStatus = "Connected";
            }            
            Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                mainVM.MessageList.Insert(0, new Message()
                {
                    Time = DateTime.Now,
                    //Code = e.ErrorCode.,
                    Text = e.TickerId != -1 ? "Request ID:" + e.TickerId + ", msg:" + msg : msg,
                    Source = DisplayName
                });
            });
            if ((int)e.ErrorCode == 326)
            {
                ConnParam.ClientId++;
                //Connect();
            }
        }
        private void eh_ConnectionClosed(object sender, ConnectionClosedEventArgs e)
        {
            ConnectionStatus = "Disconnected";
            if (!disconnectByManual)               
            {
                if (!SystemHelper.IsTWSOpen())
                {
                    mainVM.AddMessage(new Message
                    {
                        Time = DateTime.Now,
                        Text = "Connection closed due to TWS shutdown."
                    });
                }
                else
                {
                    mainVM.AddMessage(new Message
                    {
                        Time = DateTime.Now,
                        Text = "Connection closed due to unknown reason - sender:" + sender.ToString()
                    });
                }
                AutoReconnect();
            }
        }
        private bool init = false;
        private async void Init()
        {
            if (init) return;
            init = true;    
            // place an order impossible traded for JITed performance
            Log log = new Log { Time = DateTime.Now, Text = "Faked order generated." };
            mainVM.Log(log);
            SymbolInAction symbol = new SymbolInAction("QQQ", 60);
            symbol.AppliedControllers.Add(this);
            symbol.MinTick = 0.01M;
            symbol.RoundLotSize = 10;
            Script script = new Script("script1", symbol);
            symbol.Scripts.Add(script);
            Strategy strategy = new Strategy("strategy1", script);
            script.Strategies.Add(strategy);
            strategy.DefaultPositionSize = 3;
            IBLimitOrder orderType = new IBLimitOrder();
            //orderType.Slippage = 0;
            strategy.BuyOrderTypes.Add(orderType);
            int bn = OrderManager.BatchNo;
            
            await PlaceOrder(Accounts[0], strategy, orderType, OrderAction.Buy, bn, null, null, null, true, false);
            Log log1 = new Log { Time = DateTime.Now, Text = "Faked order1 placed." };
            mainVM.Log(log1);
            //Client.CancelOrder(ol1.OrderId);
            /*
            orderType.Slippages.Add(new CSlippage { PosSize = 1, Slippage = 1 });
            orderType.Slippages.Add(new CSlippage { PosSize = 1, Slippage = 2 });
            List<OrderLog> logs = await PlaceOrder(Accounts[0], strategy, orderType, OrderAction.Buy, bn, null, null, null, true, false);
            foreach (OrderLog olog in logs)
            {
                mainVM.Log(new Log { Time = olog.OrderSentTime, Text = "Faked order2 placed." + olog.Slippage });
            }
            

            orderType.Slippages.Add(new CSlippage { PosSize = 1, Slippage = 3 });
            orderType.Slippages.Add(new CSlippage { PosSize = 1, Slippage = 4 });
            logs = await PlaceOrder(Accounts[0], strategy, orderType, OrderAction.Buy, bn, null, null, null, true, false);
            foreach (OrderLog olog in logs)
            {
                mainVM.Log(new Log { Time = olog.OrderSentTime, Text = "Faked order3 placed." + olog.Slippage });
            }*/
            //ClientSocket.cancelOrder(ol1.OrderId);
            //ClientSocket.cancelOrder(ol2.OrderId);
            //ClientSocket.cancelOrder(ol3.OrderId);
        }
        private async void AfterConnected(int id = -1)
        {            
            _hub.Publish<IController>(this);
            ConnectionStatus = "Connected";
            if (id == -1)
            {
                id = await reqIdsAsync();
            }
            Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                mainVM.MessageList.Insert(0, new Message()
                {
                    Source = DisplayName,
                    Time = DateTime.Now,
                    Text = "Connected -- NextValidOrderID: " + id
                });
                if (OrderIdCount < id)
                    OrderIdCount = id;
            });
            Init();
        }
        private void eh_AccountValue(object sender, UpdateAccountValueEventArgs e)
        {
            AccountInfo acc = Accounts.FirstOrDefault<AccountInfo>(x => x.Name == e.AccountName);
            Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                if (acc == null)
                {
                    Accounts.Add(new AccountInfo(e.AccountName, this));
                    mainVM.LogList.Add(new Log()
                    {
                        Source = DisplayName,
                        Time = DateTime.Now,
                        Text = "A new account has been added"
                    });
                }
                AccountTag tag = acc.Properties.FirstOrDefault<AccountTag>(x => x.Tag == e.Key);
                if (tag == null)
                    acc.Properties.Add(new AccountTag() { Tag = e.Key, Currency = e.Currency, Value = e.Value });
                else
                    tag.Value = e.Value;
            });
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
        private static int reqIdCount = -10000;
        public static async Task<T> FromEvent<TEventArgs, T>(
            Action<EventHandler<TEventArgs>> registerEvent,
            System.Action<int> action,
            Action<EventHandler<TEventArgs>> unregisterEvent,
            CancellationToken token,
            object parameter = null,
            int? timeout = null)
        {
            int reqId = reqIdCount--;
            if (reqIdCount >= int.MaxValue)
                reqIdCount = 0;

            var tcs = new TaskCompletionSource<T>();
            if (timeout != null)
            {
                var ct = new CancellationTokenSource((int)timeout);
                ct.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);
            }

            Contract contract = new Contract();
            EventHandler<TEventArgs> handler = (sender, args) =>
            {
                if (args.GetType() == typeof(ErrorEventArgs))
                {
                    ErrorEventArgs arg = args as ErrorEventArgs;
                    if (arg.TickerId == reqId)
                    {
                        Exception ex = new Exception(arg.ErrorMsg);
                        ex.Data.Add("RequestId", arg.TickerId);
                        ex.Data.Add("ErrorCode", arg.ErrorCode);
                        ex.Data.Add("Message", arg.ErrorMsg);
                        ex.Source = "IBTaskExt.FromEvent";
                        tcs.TrySetException(ex);
                    }
                }
                else if (args.GetType() == typeof(ContractDetailsEventArgs))
                {
                    ContractDetailsEventArgs arg = args as ContractDetailsEventArgs;
                    if (arg.RequestId == reqId)
                    {
                        contract.ContractId = arg.ContractDetails.Summary.ContractId;
                        //contract.LastTradeDateOrContractMonth = arg.ContractDetails.Summary.LastTradeDateOrContractMonth;
                        contract.LocalSymbol = arg.ContractDetails.Summary.LocalSymbol;
                        contract.SecurityType = arg.ContractDetails.Summary.SecurityType;
                        contract.Symbol = arg.ContractDetails.Summary.Symbol;
                        contract.Exchange = arg.ContractDetails.Summary.Exchange;
                        contract.PrimaryExchange = arg.ContractDetails.Summary.PrimaryExchange;
                        contract.Currency = arg.ContractDetails.Summary.Currency;  
                        IBContract ibContract = new IBContract {
                            Contract = contract,
                            TradingHours = arg.ContractDetails.TradingHours,
                            ReqId = reqId
                        };
                        ibContract.MinTick = (decimal)arg.ContractDetails.MinTick;
                        tcs.TrySetResult((T)(object)ibContract);
                        /*
                        string[] ruleIds = arg.ContractDetails.MarketRuleIds.Split(new char[] { ',' });
                        if (parameter != null)
                        {
                            for (int i = 0; i < ruleIds.Length; i++)
                            {
                                ((IBController)parameter).Client.reqMarketRule(int.Parse(ruleIds[i]));
                            }
                        }
                          */
                    }
                }
                else if (args.GetType() == typeof(OrderStatusEventArgs))
                {
                    OrderStatusEventArgs arg = args as OrderStatusEventArgs;
                    if (arg.OrderId == (int)parameter)
                    {
                        if (arg.Status == OrderStatus.ApiCancelled || arg.Status == OrderStatus.Canceled)
                            tcs.TrySetResult((T)(object)true);
                        else
                            tcs.TrySetResult((T)(object)false);
                    }
                }
                // get min price increment for each exchange
                /*
                else if (args.GetType() == typeof(MarketRuleEventArgs))
                {
                    MarketRuleEventArgs arg = args as MarketRuleEventArgs;
                    List<object> list = new List<object>();
                    list.Add(contract);
                    list.Add(arg.PriceIncrements);
                    tcs.TrySetResult((T)(object)list);
                }*/
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

        public static async Task<TEventArgs> FromEventToAsync<TEventArgs>(
            Action<EventHandler<TEventArgs>> registerEvent,
            System.Action action,
            Action<EventHandler<TEventArgs>> unregisterEvent,
            CancellationToken token,
            System.Action beforeAction = null,
            System.Action afterAction = null
            )
        {
            beforeAction?.Invoke();
            var tcs = new TaskCompletionSource<TEventArgs>();
            EventHandler<TEventArgs> handler = (sender, args) => tcs.TrySetResult(args);
            registerEvent(handler);

            try
            {
                using (token.Register(() => tcs.SetCanceled()))
                {
                    action();
                    return await tcs.Task;
                }
            }
            finally
            {
                unregisterEvent(handler);
                afterAction?.Invoke();
            }
        }
    }
}
