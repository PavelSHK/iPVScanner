using iPVScannerWin.Controls;
using iPVScannerWin.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Phone.UI.Input;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace iPVScannerWin
{
    public sealed partial class MainPage : Page
    {
        public Frame AppFrame { get { return this.frame; } }

        public static MainPage Current = null;

        private List<NavMenuItem> navlist = new List<NavMenuItem>(new[]
        {
            new NavMenuItem()
            {
                Label = App.loader.GetString("Scanning"),
                Symbol = Symbol.Scan,
                DestPage = typeof(ScanningPage)
            },
            new NavMenuItem()
            {
                Label= App.loader.GetString("MyVideos"),
                Symbol = Symbol.Video,
                DestPage = typeof(AccountPage)
            },
            new NavMenuItem()
            {
                Label = App.loader.GetString("Manual"),
                Symbol = Symbol.Document,
                DestPage = typeof(ManualPage)
            },
            new NavMenuItem()
            {
                Label = App.loader.GetString("Terms"),
                Symbol = Symbol.ProtectedDocument,
                DestPage = typeof(TermsPage)
            },
            new NavMenuItem()
            {
                Label = App.loader.GetString("Settings"),
                Symbol = Symbol.Setting,
                DestPage = typeof(SettingsPage)
            },
            new NavMenuItem()
            {
                Label= App.loader.GetString("About"),
                Symbol = Symbol.More,
                DestPage = typeof(AboutPage)
            }
        });

        public MainPage()
        {
            this.InitializeComponent();

            NavMenuList.ItemsSource = navlist;

            this.Loaded += (sender, args) =>
            {
                Current = this;
                this.CheckTogglePaneButtonSizeChanged();
            };

            this.RootSplitView.RegisterPropertyChangedCallback(
                SplitView.DisplayModeProperty,
                (s, a) =>
                {
                    this.CheckTogglePaneButtonSizeChanged();
                });

            if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            {
                HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            }
            else
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                SystemNavigationManager.GetForCurrentView().BackRequested += SystemNavigationManager_BackRequested;
            }
        }

        private void SystemNavigationManager_BackRequested(object sender, BackRequestedEventArgs e)
        {
            bool handled = e.Handled;
            this.BackRequested(ref handled);
            e.Handled = handled;
        }

        private void BackRequested (ref bool handled)
        {
            if (this.AppFrame == null) return;
            else if (this.AppFrame.CanGoBack && !handled && !AccountPage.browser.CanGoBack)
            {
                handled = true;
                this.AppFrame.GoBack();
            }
            else if(AccountPage.browser.CanGoBack)
            {
                AccountPage.browser.GoBack();
            }
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            bool handled = e.Handled;
            this.BackRequested(ref handled);
            e.Handled = handled;
        }

        private void TogglePaneButton_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void TogglePaneButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void RootSplitView_PaneClosed(SplitView sender, object args)
        {

        }

        private void OnNavigatingToPage(object sender, NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                var item = (from p in this.navlist where p.DestPage == e.SourcePageType select p).SingleOrDefault();
                if (item == null && this.AppFrame.BackStackDepth > 0)
                {
                    foreach (var entry in this.AppFrame.BackStack.Reverse())
                    {
                        item = (from p in this.navlist where p.DestPage == entry.SourcePageType select p).SingleOrDefault();
                        if (item != null)
                            break;
                    }
                }

                foreach (var i in navlist)
                {
                    i.IsSelected = false;
                }
                if (item != null)
                {
                    item.IsSelected = true;
                }

                var container = (ListViewItem)NavMenuList.ContainerFromItem(item);

                if (container != null) container.IsTabStop = false;
                NavMenuList.SetSelectedItem(container);
                if (container != null) container.IsTabStop = true;

                
            }
        }

        private void NavMenuItemContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!args.InRecycleQueue && args.Item != null && args.Item is NavMenuItem)
            {
                args.ItemContainer.SetValue(AutomationProperties.NameProperty, ((NavMenuItem)args.Item).Label);
            }
            else
            {
                args.ItemContainer.ClearValue(AutomationProperties.NameProperty);
            }
        }

        private void NavMenuList_ItemInvoked(object sender, ListViewItem e)
        {
            foreach (var i in navlist)
            {
                i.IsSelected = false;
            }

            var item = (NavMenuItem)((NavMenuListView)sender).ItemFromContainer(e);

            if (item != null)
            {
                item.IsSelected = true;
                if (item.DestPage != null &&
                    item.DestPage != this.AppFrame.CurrentSourcePageType)
                {
                    this.AppFrame.Navigate(item.DestPage, item.Arguments);
                }
            }

            

        }
     
        public Rect TogglePaneButtonRect
        {
            get;
            private set;
        }

        public event TypedEventHandler<MainPage, Rect> TogglePaneButtonRectChanged;

        private void CheckTogglePaneButtonSizeChanged()
        {
            if (this.RootSplitView.DisplayMode == SplitViewDisplayMode.Inline ||
                this.RootSplitView.DisplayMode == SplitViewDisplayMode.Overlay)
            {
                var transform = this.TogglePaneButton.TransformToVisual(this);
                var rect = transform.TransformBounds(new Rect(0, 0, this.TogglePaneButton.ActualWidth, this.TogglePaneButton.ActualHeight));
                this.TogglePaneButtonRect = rect;
            }
            else
            {
                this.TogglePaneButtonRect = new Rect();
            }

            var handler = this.TogglePaneButtonRectChanged;
            if (handler != null)
            {
                handler.DynamicInvoke(this, this.TogglePaneButtonRect);
            }
        }

        private void OnNavigatedToPage(object sender, NavigationEventArgs e)
        {
            if (this.AppFrame.CurrentSourcePageType != typeof(ScanningPage))
            {
                TogglePaneButton.Foreground = new SolidColorBrush(Colors.Black);
            }
            else TogglePaneButton.Foreground = new SolidColorBrush(Colors.White);
        }
    }
}
