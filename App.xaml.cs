using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using iPVScannerWin.Views;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.Foundation.Metadata;

namespace iPVScannerWin
{    
    sealed partial class App : Application
    {
        public static Windows.ApplicationModel.Resources.ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }     
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            //проверка устройства и установка статусбара
            if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            {
                StatusBar statusBar = StatusBar.GetForCurrentView();
                await statusBar.HideAsync();
            }
            else
            {
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(320, 200));         
                ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
                if (titleBar != null)
                {
                    Color titleBarColor = (Color)App.Current.Resources["SystemChromeMediumColor"];
                    titleBar.BackgroundColor = titleBarColor;
                    titleBar.ButtonBackgroundColor = titleBarColor;
                }
            }
                
            MainPage mainPage = Window.Current.Content as MainPage;

            if (mainPage == null)
            {
                mainPage = new MainPage();

                mainPage.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                mainPage.AppFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                }
            }

            Window.Current.Content = mainPage;

            if (mainPage.AppFrame.Content == null)
            {
                mainPage.AppFrame.Navigate(typeof(ScanningPage), e.Arguments, new Windows.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
            }

            Window.Current.Activate();
        }

       
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

       
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
           
            deferral.Complete();
        }
    }
}
