using iPVScannerWin.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Phone.Devices.Notification;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using ZXing;

using static ZXing.RGBLuminanceSource;
using iPVScannerWin.Models;
using ZXing.Mobile;

namespace iPVScannerWin.Views
{
    public sealed partial class ScanningPage : Page
    {
        #region DisplayParameters
        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;
        private readonly DisplayRequest _displayRequest = new DisplayRequest();
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");
        #endregion //параметры для работы с дисплеем

        #region CameraParameters
            MediaCapture mediaCapture;
            TorchControl torchControl;
            FocusControl focusControl;
        #endregion //параметры для работы с камерой

        #region ScanningParameters
            BarcodeReader barcodeReader;
            DispatcherTimer timer;
            Result result;
            LuminanceSource luminanceSource;
            VideoFrame videoFrame;
        #endregion

        SetImage setImg = new SetImage();
        public static bool _isInitialized = false;
        public static bool _isPreviewing = false;
        private bool _mirroringPreview = false;
        private bool _externalCamera = false;
        double _width = 350;
        double _height = 350;
        
        public ScanningPage()
        {
            this.InitializeComponent();          
            setImg.Uri = "ms-appx:///Assets/Icons/FlashOff.png";
            FlashIcon.DataContext = setImg;
            App.Current.Suspending += OnSuspending;
            App.Current.Resuming += OnResuming;
        }

        private async void OnResuming(object sender, object e)
        {
            if(this.Frame.CurrentSourcePageType == typeof(ScanningPage))
                await InitCaptureElement();
        }

        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            if (this.Frame.CurrentSourcePageType == typeof(ScanningPage)) await CleanCaptureElement();
            else return;

            deferral.Complete();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _displayOrientation = _displayInformation.CurrentOrientation;
            _displayInformation.OrientationChanged += DisplayInformation_OrientationChanged;
            if (!_isInitialized) await InitCaptureElement();

        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _displayInformation.OrientationChanged -= DisplayInformation_OrientationChanged;
            await CleanCaptureElement();
        }

        private async void DisplayInformation_OrientationChanged(DisplayInformation sender, object args)
        {
            _displayOrientation = _displayInformation.CurrentOrientation;

            if (_isInitialized)
                await SetPreviewRotationAsync();
        }

        public async Task InitCaptureElement()
        {
            Ring.IsActive = true;
            if (mediaCapture == null)
            {
                try
                {
                    mediaCapture = new MediaCapture();
                    mediaCapture.Failed += MediaCapture_Failed;
                    var device = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Back);
                    await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                    {
                        VideoDeviceId = device.Id,
                        AudioDeviceId = string.Empty,
                        StreamingCaptureMode = StreamingCaptureMode.Video,
                        PhotoCaptureSource = PhotoCaptureSource.VideoPreview
                    });
                    _isInitialized = true;
                    if (_isInitialized)
                    {
                        if (device.EnclosureLocation == null || device.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                            _externalCamera = true;
                        else
                        {
                            _externalCamera = false;
                            _mirroringPreview = (device.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
                        }
                        await StartPreview(); //начинаем "трансляцию"
                        await SetResolution();

                    }
                    Scan();
                    _displayOrientation = _displayInformation.CurrentOrientation;
                    _displayInformation.OrientationChanged += DisplayInformation_OrientationChanged;
                    //установка вспышки
                    torchControl = mediaCapture.VideoDeviceController.TorchControl;
                    if (torchControl.Supported)
                    {
                        ToggleButtonFlash.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }

        private async Task SetResolution()
        {
            IReadOnlyList<IMediaEncodingProperties> res;
            res = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview);
            uint maxResolution = 0;
            int indexMaxResolution = 0;
            if (res.Count >= 1)
            {
                for (int i = 0; i < res.Count; i++)
                {
                    VideoEncodingProperties vp = (VideoEncodingProperties)res[i];

                    if (vp.Width > maxResolution)
                    {
                        indexMaxResolution = i;
                        maxResolution = vp.Width;
                        _width = vp.Width;
                        _height = vp.Height;
                    }
                }
                await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, res[indexMaxResolution]);
            }
        }

        private async Task StartPreview()
        {
            _displayRequest.RequestActive();
            PreviewElement.Source = mediaCapture;
            PreviewElement.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            await mediaCapture.StartPreviewAsync();
            _isPreviewing = true;
            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
                Ring.IsActive = false;
                CodeFrame.Visibility = Visibility.Visible;
            }
        }

        private async Task SetPreviewRotationAsync()
        {
            if (_externalCamera) return;
            int rotationDegrees = ConvertDisplayOrientationToDegrees(_displayOrientation);
            if (_mirroringPreview)
                rotationDegrees = (360 - rotationDegrees) % 360;
            var props = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, rotationDegrees);
            await mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }

        public static int ConvertDisplayOrientationToDegrees(DisplayOrientations orientation)
        {
            switch (orientation)
            {
                case DisplayOrientations.Portrait:
                    return 90;
                case DisplayOrientations.LandscapeFlipped:
                    return 180;
                case DisplayOrientations.PortraitFlipped:
                    return 270;
                case DisplayOrientations.Landscape:
                default:
                    return 0;
            }
        }

        private async Task StopPreview()
        {
            _isPreviewing = false;
            if(!_isInitialized) await mediaCapture.StopPreviewAsync();

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _displayRequest.RequestRelease();
                PreviewElement.Source = null;
            });
            if (torchControl.Supported)
            {
                if (torchControl.Enabled)
                {
                    torchControl.Enabled = false;
                    setImg.Uri = "ms-appx:///Assets/Icons/FlashOff.png";
                }
            }
        }

        private async Task CleanCaptureElement()
        {
            if (_isInitialized)
            {
                StopScan();
                if (_isPreviewing) await StopPreview();
                _isInitialized = false;               
                if (mediaCapture != null)
                {
                    mediaCapture.Failed -= MediaCapture_Failed;
                    mediaCapture.Dispose();
                    mediaCapture = null;
                    CodeFrame.Visibility = Visibility.Collapsed;
                }
                barcodeReader = null;
                videoFrame = null;
            }
        }

        private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            
            // MainModel.Error(App.res.GetString("Error"), App.res.GetString("ConnectCamError"));
        }

        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
        {
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);
            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }

        public async void Scan()
        {
            barcodeReader = new BarcodeReader()
            {
                AutoRotate = true
            };

            barcodeReader.Options.PossibleFormats = new BarcodeFormat[]
            {
                BarcodeFormat.QR_CODE,
                BarcodeFormat.DATA_MATRIX
            };
            barcodeReader.Options.TryHarder = true;

            if (_isInitialized && _isPreviewing)
            {
                timer = new DispatcherTimer();
                timer.Tick += Timer_Tick;
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Start();
                await SetAutoFocus();               
            }
        }

        private async void Timer_Tick(object sender, object e)
        {
            if(_isInitialized && _isPreviewing)
            {
                try
                {
                    videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)_width, (int)_height);
                    var frame = await mediaCapture.GetPreviewFrameAsync(videoFrame);
                    luminanceSource = new SoftwareBitmapLuminanceSource(frame.SoftwareBitmap);
                }
                catch(Exception ex)
                {

                }
                result = null;
                //продолжаем выполнение
                try
                {
                    if (luminanceSource != null)
                        result = barcodeReader.Decode(luminanceSource);
                }
                catch (Exception ex)
                {

                }
                if (result != null && !string.IsNullOrEmpty(result.Text))
                {
                    Debug.WriteLine(result.Text);
                    Tick.Begin();
                    Tick.Completed += Tick_Completed;
                }
            }
        }    
        private async void Tick_Completed(object sender, object e)
        {
            videoFrame = null;
            if (ApiInformation.IsTypePresent("Windows.Phone.Devices.Notification.VibrationDevice"))
            {
                VibrationDevice vibrationDevice = VibrationDevice.GetDefault();
                vibrationDevice.Vibrate(new TimeSpan(0, 0, 0, 0, 50));
            }          
                    var linkRes = await Link(result.Text);
                    switch (linkRes)
                    {
                        case 0:
                            FlyoutBase.ShowAttachedFlyout((Grid)VideoContent);
                            Player.Source = new Uri(result.Text);
                            Player.Play();
                            break;
                        case 1:
                            FlyoutBase.ShowAttachedFlyout((Grid)PhotoContent);
                            ImageContainer.Source = new BitmapImage(new Uri(result.Text));
                            break;
                        case 2:
                            FlyoutBase.ShowAttachedFlyout((Grid)WebContent);
                            Web.Navigate(new Uri(result.Text));
                            break;
                        default:
                            return;
                    }

         }
        

        public async Task<int> Link(string url)
        {
            if (url.EndsWith(".mp4") || url.EndsWith(".avi"))
                return 0;
            else if (url.EndsWith(".jpg") || url.EndsWith(".jpeg") || url.EndsWith(".png"))
                return 1;
            else if (url.StartsWith("http://") && (url.EndsWith("/") || url.EndsWith("")))
            {
                return 2;
            }
            else return 3;
        }

        private async Task SetAutoFocus()
        {
            focusControl = mediaCapture.VideoDeviceController.FocusControl;
            if (focusControl.Supported)
            {
                var focusSettings = new FocusSettings();
                focusSettings.AutoFocusRange = focusControl.SupportedFocusRanges.Contains(AutoFocusRange.FullRange)
                    ? AutoFocusRange.FullRange
                    : focusControl.SupportedFocusRanges.FirstOrDefault();

                var supportedFocusModes = focusControl.SupportedFocusModes;
                if (supportedFocusModes.Contains(FocusMode.Continuous))
                {
                    focusSettings.Mode = FocusMode.Continuous;
                }
                else if (supportedFocusModes.Contains(FocusMode.Auto))
                {
                    focusSettings.Mode = FocusMode.Auto;
                }

                if (focusSettings.Mode == FocusMode.Continuous || focusSettings.Mode == FocusMode.Auto)
                {
                    focusSettings.WaitForFocus = false;
                    focusControl.Configure(focusSettings);
                    await focusControl.FocusAsync();
                }
            }
        }

        private void StopScan()
        {
            timer.Stop();
            timer.Tick -= Timer_Tick;
            barcodeReader = null;
            timer = null;
            if (torchControl != null && torchControl.Supported) torchControl.Enabled = false;
        }

        public class SetImage
        {
            public string Uri { get; set; }
        }

        private void ToggleButtonFlash_Click(object sender, RoutedEventArgs e)
        {
            torchControl.Enabled = !torchControl.Enabled;
            if (torchControl.Enabled)
            {
                setImg.Uri = "ms-appx:///Assets/Icons/FlashOn.png";
            }
            else setImg.Uri = "ms-appx:///Assets/Icons/FlashOff.png";
        }

        private void FlyoutClose(object sender, RoutedEventArgs e)
        {            
            VideoFlyout.Hide();
            PhotoFlyout.Hide();
            WebFlyout.Hide();
        }

        private async void OnClosedFlyout(object sender, object e)
        {
            if(sender == VideoFlyout)
            {
                Player.Stop();
            }
            else if (sender == PhotoFlyout)
            {
                ImageContainer.Source = null;
            }
            else
            {
                await WebView.ClearTemporaryWebDataAsync();
            }
            if (!_isInitialized) await InitCaptureElement();
        }

        private async void FlyoutOpened(object sender, object e)
        {
            await CleanCaptureElement();
            //DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            VideoFlyout.Hide();
        }
    }
}
