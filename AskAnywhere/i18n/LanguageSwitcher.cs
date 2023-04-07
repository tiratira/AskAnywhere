using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AskAnywhere.i18n
{
    public class LanguageSwitcher
    {
        public static void Change(string targetLanguage)
        {
            Application.Current.Resources.MergedDictionaries.RemoveAt(0);
            var dict = new ResourceDictionary();
            switch (targetLanguage)
            {
                case "en-us":
                    dict.Source = new Uri("i18n/StringResources.xaml", UriKind.Relative);
                    break;
                case "zh-cn":
                    dict.Source = new Uri("i18n/StringResources.zh-cn.xaml", UriKind.Relative);
                    break;
                default:
                    dict.Source = new Uri("i18n/StringResources.xaml", UriKind.Relative);
                    break;
            }
            Application.Current.Resources.MergedDictionaries.Insert(0, dict);
        }
    }
}
