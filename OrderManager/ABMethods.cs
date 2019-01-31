using AmiBroker.PlugIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiBroker.Controllers
{
    public class ABMethods : IndicatorBase
    {
        private static bool _isInit = false;
        private static OrderManager _om;
        private static int _taskId = 0;
        public ABMethods()
        {
            if (!_isInit)
            {
                new OrderManager();
                _isInit = true;
            }
        }

        [ABMethod]
        public void IBController1(string scriptName)
        {
            var task = Task.Run<int>(() =>
            {
                _om.IBC(scriptName);
                return 0;
            });
            task.ContinueWith(result =>
            {
                Console.WriteLine(result);
            });
        }
    }
}
