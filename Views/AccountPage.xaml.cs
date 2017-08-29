using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Phone.UI.Input;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;

namespace iPVScannerWin.Views
{
    public sealed partial class AccountPage : Page
    {
        public static WebView browser;

        HttpRequestMessage httpRequest;

        HttpClient httpClient = new HttpClient();
        HttpResponseMessage response;

        public AccountPage()
        {
            this.InitializeComponent();
            InitializeBrowser();
            browser = WebBrowser;
        }
      

        public async void InitializeBrowser()
        {

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                WebBrowser.Source = new Uri("http://46.101.111.46/");
            }
            else
            {
                MessageDialog messageDialog = new MessageDialog("");
                messageDialog.CancelCommandIndex = 0;
                messageDialog.DefaultCommandIndex = 1;

                messageDialog.Commands.Add(new UICommand(App.loader.GetString("TryAgain"), new UICommandInvokedHandler(this.CommandInvokedHandler)));

            }
            

            //if (App.localSettings.Values["SessionId"] != null)
            //{
            //    Debug.WriteLine("Сессия есть");
            //    var name = App.localSettings.Values["SessionName"] as string;
            //    var value = App.localSettings.Values["SessionId"] as string;
            //    httpRequest = new HttpRequestMessage();
            //    httpRequest.Method = HttpMethod.Post;
            //    httpRequest.RequestUri = new Uri("http://46.101.111.46/");
            //    httpRequest.Headers.Add("Cookie", name + "=" + value);
            //    response = await httpClient.SendRequestAsync(httpRequest);
            //}
            //WebBrowser.NavigateWithHttpRequestMessage(httpRequest);

            WebBrowser.NavigationStarting += OnStarting;
            WebBrowser.NavigationCompleted += OnCompleted;
            WebBrowser.NavigationFailed += OnFailed;
        }


        private void CommandInvokedHandler(IUICommand command)
        {
            WebBrowser.Source = new Uri("http://46.101.111.46/");
        }

        private void OnFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            Ring.IsEnabled = false;
        }

        private async void OnCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            Ring.IsEnabled = false;


            //if (App.localSettings.Values["SessionId"] == null)
            //{
            //    Debug.WriteLine("Сессии нет");
            //    httpRequest = new HttpRequestMessage();
            //    httpRequest.Method = HttpMethod.Post;
            //    httpRequest.RequestUri = new Uri("http://46.101.111.46/");


            //    response = await httpClient.SendRequestAsync(httpRequest);
            //    HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
            //    HttpCookieManager cookieManager = filter.CookieManager;
            //    var cookies = cookieManager.GetCookies(new Uri("http://46.101.111.46/"));
            //    if (cookies.Count > 1)
            //    {
            //        App.localSettings.Values["SessionName"] = cookies[1].Name;
            //        App.localSettings.Values["SessionId"] = cookies[1].Value;
            //        Debug.WriteLine(cookies[1].Name);
            //    }
            //    else return;
            //}
            //else return;
            
        }

        private void OnStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            Ring.IsEnabled = true;
        }        
    }
}
