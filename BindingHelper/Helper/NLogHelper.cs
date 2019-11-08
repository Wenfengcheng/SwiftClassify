using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace BindingHelper
{
    public class NLogHelper
    {
        private static Logger _logger = null;

        public static Logger Logger
        {
            get
            {
                if (_logger == null)
                {
                    //初始化日志对象
                    _logger = LogManager.GetCurrentClassLogger();
                }
                return _logger;
            }
        }
    }
}
