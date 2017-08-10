using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace iPVScannerWin.Controls
{
    public class NavMenuListView : ListView
    {
        private SplitView splitViewHost;

        public NavMenuListView()
        {
            this.SelectionMode = ListViewSelectionMode.Single;
            this.IsItemClickEnabled = true;
            this.ItemClick += ItemClickedHandler;

            this.Loaded += (s, a) =>
            {
                var parent = VisualTreeHelper.GetParent(this);
                while (parent != null && !(parent is SplitView))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
                if (parent != null)
                {
                    this.splitViewHost = parent as SplitView;
                    splitViewHost.RegisterPropertyChangedCallback(SplitView.IsPaneOpenProperty, (sender, args) =>
                    {
                        this.OnPaneToggled();
                    });
                    splitViewHost.RegisterPropertyChangedCallback(SplitView.DisplayModeProperty, (sender, args) =>
                    {
                        this.OnPaneToggled();
                    });
                    this.OnPaneToggled();
                }
            };
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            for (int i = 0; i < this.ItemContainerTransitions.Count; i++)
            {
                if (this.ItemContainerTransitions[i] is EntranceThemeTransition)
                {
                    this.ItemContainerTransitions.RemoveAt(i);
                }
            }
        }

        public void SetSelectedItem(ListViewItem item)
        {
            int index = -1;
            if (item != null)
            {
                index = this.IndexFromContainer(item);
            }
            for (int i = 0; i < this.Items.Count; i++)
            {
                var lvi = (ListViewItem)this.ContainerFromIndex(i);
                if (i != index)
                {
                    lvi.IsSelected = false;
                }
                else if (i == index)
                {
                    lvi.IsSelected = true;
                }
            }
        }

        public event EventHandler<ListViewItem> ItemInvoked;

        private void ItemClickedHandler(object sender, ItemClickEventArgs e)
        {
            var item = this.ContainerFromItem(e.ClickedItem);
            this.InvokeItem(item);
        }

        private void InvokeItem(object focusedItem)
        {
            this.SetSelectedItem(focusedItem as ListViewItem);
            this.ItemInvoked?.Invoke(this, focusedItem as ListViewItem);

            if (this.splitViewHost.IsPaneOpen && (
                this.splitViewHost.DisplayMode == SplitViewDisplayMode.CompactOverlay ||
                this.splitViewHost.DisplayMode == SplitViewDisplayMode.Overlay))
            {
                this.splitViewHost.IsPaneOpen = false;
            }

            if (focusedItem is ListViewItem)
            {
                ((ListViewItem)focusedItem).Focus(FocusState.Programmatic);
            }
        }

        private void OnPaneToggled()
        {
            if (this.splitViewHost.IsPaneOpen)
            {
                this.ItemsPanelRoot.ClearValue(FrameworkElement.WidthProperty);
                this.ItemsPanelRoot.ClearValue(FrameworkElement.HorizontalAlignmentProperty);
            }
            else if (this.splitViewHost.DisplayMode == SplitViewDisplayMode.CompactInline ||
                this.splitViewHost.DisplayMode == SplitViewDisplayMode.CompactOverlay)
            {
                this.ItemsPanelRoot.SetValue(FrameworkElement.WidthProperty, this.splitViewHost.CompactPaneLength);
                this.ItemsPanelRoot.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            }
        }
    }
}
