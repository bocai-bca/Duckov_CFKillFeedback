using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace CFKillFeedback
{
    public class Utils
    {
        // 获取当前dll的所在目录
        public static string GetDllDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
}
