using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NameServerCheck
{
    /// <summary>
    /// Save & restore position and dimentions with registry
    /// </summary>
    public static class FormHelper
    {
        private const string _registry_region = @"SOFTWARE\SMISO\NameServerCheck";
        private static RegistryKey _key = Registry.CurrentUser.CreateSubKey(_registry_region);

        public static void SaveFormSettings(Window window)
        {
            try
            {
                if (window.WindowState != WindowState.Maximized)
                {
                    _key.SetValue("Width", window.Width, RegistryValueKind.String);
                    _key.SetValue("Height", window.Height, RegistryValueKind.String);
                    _key.SetValue("Maximized", "false", RegistryValueKind.String);
                }
                else
                {
                    _key.SetValue("Maximized", "true", RegistryValueKind.String);
                }
            }
            catch { }
        }

        public static void LoadFormSettings(Window window)
        {
            try
            {
                window.Width = double.Parse(_key.GetValue("Width", window.Width.ToString()).ToString());
                window.Height = double.Parse(_key.GetValue("Height", window.Height.ToString()).ToString());

                if (_key.GetValue("Maximized", "false") as string == "true")
                    window.WindowState = WindowState.Maximized;
            }
            catch { }
        }
    }
}
