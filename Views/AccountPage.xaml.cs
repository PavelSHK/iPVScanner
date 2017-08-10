using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Phone.UI.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace iPVScannerWin.Views
{
    public sealed partial class AccountPage : Page
    {
        public static WebView browser;
        public AccountPage()
        {
            this.InitializeComponent();
            InitializeBrowser();
            browser = WebBrowser;
        }
      

        public void InitializeBrowser()
        {
            WebBrowser.Source = new Uri("http://46.101.111.46/");
            WebBrowser.Loading += WebBrowser_Loading;
            WebBrowser.Loaded += WebBrowser_Loaded;
        }

        private void WebBrowser_Loading(FrameworkElement sender, object args)
        {
            Ring.IsActive = true;
        }

        private void WebBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            Ring.IsActive = false;
        }
    }
}
