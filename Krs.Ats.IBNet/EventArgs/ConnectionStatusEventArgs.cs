using System;

namespace Krs.Ats.IBNet
{
    /// <summary>
    /// Connection Closed Event Arguments
    /// </summary>
    [Serializable()]
    public class ConnectionStatusEventArgs : EventArgs
    {
        /// <summary>
        /// Uninitialized Constructor for Serialization
        /// </summary>
        public ConnectionStatusEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }
        /// <summary>
        /// Connection is established.
        /// </summary>
        public bool IsConnected { get; set; }
    }
}
