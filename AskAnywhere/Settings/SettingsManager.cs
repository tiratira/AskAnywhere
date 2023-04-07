using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskAnywhere.Settings
{
    public class SettingsManager
    {
        public static T Get<T>(string key)
        {
            return (T)Properties.Settings.Default[key];
        }

        public static void Set<T>(string key, T value, bool save = false)
        {
            Properties.Settings.Default[key] = value;
            if (save) Properties.Settings.Default.Save();
        }

        public static void SaveAll()
        {
            Properties.Settings.Default.Save();
        }
    }
}
