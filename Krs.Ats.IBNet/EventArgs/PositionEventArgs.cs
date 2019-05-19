using System;

namespace Krs.Ats.IBNet
{
    /// <summary>
    /// Position event args.
    /// </summary>
    [Serializable()]
    public class PositionEventArgs : EventArgs
    {
        private string _account;
        private ContractDetails _contract;
        private double _pos;
        private double _avgCost;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="account">Account.</param>
        /// <param name="contract">Contract.</param>
        /// <param name="pos">Position.</param>
        /// <param name="avgCost">Avg. Cost.</param>
        public PositionEventArgs(string account, ContractDetails contract, double pos, double avgCost)
        {
            _account = account;
            _contract = contract;
            _pos = pos;
            _avgCost = avgCost;
        }

        /// <summary>
        /// Account.
        /// </summary>
        public string Account
        {
            get { return _account; }
            set { _account = value; }
        }

        /// <summary>
        /// Contract Details.
        /// </summary>
        public ContractDetails ContractDetails
        {
            get { return _contract; }
            set { _contract = value; }
        }

        /// <summary>
        /// Position.
        /// </summary>
        public double Position
        {
            get { return _pos; }
            set { _pos = value; }
        }

        /// <summary>
        /// Avg. Cost.
        /// </summary>
        public double AvgCost
        {
            get { return _avgCost; }
            set { _avgCost = value; }
        }
    }
}
