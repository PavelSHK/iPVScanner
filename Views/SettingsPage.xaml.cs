using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;



namespace iPVScannerWin.Views
{
    public sealed partial class SettingsPage : Page
    {
        //список языков
        private List<string> languagesList = new List<string>(new string[]{
            "Русский",
            "English",
        });
        public SettingsPage()
        {
            this.InitializeComponent();
            LanguageSelector.ItemsSource = languagesList;
            SetPlaceholder();
            LanguageSelector.SelectionChanged += LanguageSelector_SelectionChanged;
        }

        private async void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch((sender as ComboBox).SelectedIndex)
            {
                case 0:
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "ru";
                    break;
                case 1:
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en";
                    break;
            }
            await new MessageDialog("","Необходим перезапуск").ShowAsync();
        }

        public void SetPlaceholder()
        {
            if(Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride != "")
            {
                var topUserLanguage = Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
                var userLanguage = new Language(topUserLanguage);
                LanguageSelector.PlaceholderText = userLanguage.NativeName;
            }
            else
            {
                var topUserLanguage = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];
                var userLanguage = new Language(topUserLanguage);
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = userLanguage.LanguageTag;
                LanguageSelector.PlaceholderText = userLanguage.NativeName;
            }
        }
    }
}
