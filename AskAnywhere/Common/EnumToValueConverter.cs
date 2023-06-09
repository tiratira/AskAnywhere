﻿using AskAnywhere.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AskAnywhere.Common
{
    public class EnumToValueConverter<T> : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) { return false; }
            return (int)value == int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) { return null; }
            if ((bool)value) { return (T)(object)int.Parse(parameter.ToString()); }
            return null;
        }
    }


    public class ConnectionModeToIntConverter : EnumToValueConverter<ConnectionMode> { }

    public class SettingPageToIntConverter : EnumToValueConverter<SettingPage> { }

    public class DisplayLanguageToIntConverter : EnumToValueConverter<Language> { }
}
