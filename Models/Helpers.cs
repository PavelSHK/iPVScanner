using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace iPVScannerWin.Models
{
    public class Helpers
    {
        public static async void DisplayNoInternetDialog()
        {
            MessageDialog noInternetDialog = new MessageDialog("")
            {
                Title = App.loader.GetString("InternetErrorTitle"),
                Content = App.loader.GetString("InternetErrorContent"),                
            };

            noInternetDialog.Commands.Add(new UICommand(App.loader.GetString("TryAgain"),
        new UICommandInvokedHandler(CommandInvokedHandler)));

            await noInternetDialog.ShowAsync();
        }

        private static void CommandInvokedHandler(IUICommand command)
        {
            iPVScannerWin.Views.ScanningPage scanningPage = new Views.ScanningPage();
            scanningPage.Scan();
        }
    }
}
