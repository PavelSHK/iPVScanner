using iPVScannerWin.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using ZXing;
using ZXing.Mobile;
using static ZXing.RGBLuminanceSource;

namespace iPVScannerWin.Views
{
    public sealed partial class ScanningPage : Page
    {
        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;
        private readonly DisplayRequest _displayRequest = new DisplayRequest();
        SetImage setImg = new SetImage();
        MediaCapture mediaCapture;
        TorchControl torchControl;
        FocusControl focusControl;
        public static bool _isInitialized = false;
        public static bool _isPreviewing = false;
        private bool _mirroringPreview = false;
        private bool _externalCamera = false;
        double _width = 640;
        double _height = 480;
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

        #region ScanningParameters
        BarcodeReader barcodeReader;
        SoftwareBitmapLuminanceSource luminanceSource;
        DispatcherTimer timer;
        Result result;
        #endregion

        public ScanningPage()
        {
            this.InitializeComponent();
            
            setImg.Uri = "ms-appx:///Assets/Icons/FlashOff.png";
            FlashIcon.DataContext = setImg;            
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
            {
                await SetPreviewRotationAsync();
            }
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
                    var device = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Unknown);
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
                        {
                            _externalCamera = true;
                        }
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
                        mediaCapture.VideoDeviceController.FlashControl.Auto = false;
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
            {
                rotationDegrees = (360 - rotationDegrees) % 360;
            }
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
            await mediaCapture.StopPreviewAsync();
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
                if (_isPreviewing) await StopPreview();
                _isInitialized = false;
                StopScan();
                if (mediaCapture != null)
                {
                    mediaCapture.Failed -= MediaCapture_Failed;
                    mediaCapture.Dispose();
                    mediaCapture = null;
                    CodeFrame.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            await CleanCaptureElement();
            // MainModel.Error(App.res.GetString("Error"), App.res.GetString("ConnectCamError"));
        }

        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
        {
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);
            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }

        private async void Scan()
        {
            if (_isInitialized)
            {
                // zxing = MobileBarcodeScanningOptions.Default.BuildBarcodeReader();

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

                if (_isPreviewing)
                {
                    timer = new DispatcherTimer();
                    timer.Tick += Timer_Tick;
                    timer.Interval = new TimeSpan(0, 0, 1);
                    timer.Start();
                    await SetAutoFocus();
                }
            }
        }       

        private async void Timer_Tick(object sender, object e)
        {
            if(_isInitialized && _isPreviewing)
            {
                VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)_width, (int)_height);
                await mediaCapture.GetPreviewFrameAsync(videoFrame);

                var bytes = await SaveSoftwareBitmapToBufferAsync(videoFrame.SoftwareBitmap);
                await ScanImageAsync(bytes);
                }
        }


        private async Task<byte[]> SaveSoftwareBitmapToBufferAsync(SoftwareBitmap softwareBitmap)
        {
            byte[] bytes = null;

            try
            {
                IRandomAccessStream stream = new InMemoryRandomAccessStream();
                {

                    // Create an encoder with the desired format
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);
                    encoder.SetSoftwareBitmap(softwareBitmap);
                    encoder.IsThumbnailGenerated = false;
                    await encoder.FlushAsync();

                    bytes = new byte[stream.Size];

                    // This returns IAsyncOperationWithProgess, so you can add additional progress handling
                    await stream.ReadAsync(bytes.AsBuffer(), (uint)stream.Size, Windows.Storage.Streams.InputStreamOptions.None);
                }
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return bytes;
        }


        private async Task ScanImageAsync(byte[] pixelsArray)
        {           
            try
            {
                if (_isPreviewing && _isInitialized)
                {
                    var result = ScanBitmap(pixelsArray, (int)_width, (int)_height);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);     // Wasn't able to find a barcode    
            }
           
        }

        internal Result ScanBitmap(byte[] pixelsArray, int width, int height)
        {
            var result = barcodeReader.Decode(pixelsArray, width, height, BitmapFormat.Unknown);

            if (result != null)
            {
                Tick.Begin();
                Tick.Completed += Tick_Completed;
                Debug.WriteLine("ScanBitmap : " + result.Text);
            }
            this.result = result;
            return result;
        }

        private async void Tick_Completed(object sender, object e)
        {
            var linkRes = await Link(result.Text);
            switch (linkRes)
            {
                case 0:
                    FlyoutBase.ShowAttachedFlyout((Grid)VideoContent);
                    break;
                case 1:
                    break;
                case 2:
                    FlyoutBase.ShowAttachedFlyout((Grid)WebContent);
                    break;
            }
            
        }

        public async Task<int> Link(string url)
        {
            if (url.EndsWith(".mp4") || url.EndsWith(".avi"))
                return 0;
            else if (url.EndsWith(".jpg") || url.EndsWith(".jpeg") || url.EndsWith(".png"))
                return 1;
            else return 2;
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
            if (torchControl != null && torchControl.Supported) torchControl.Enabled = false;
        }

        public class SetImage
        {
            public string Uri { get; set; }
        }

        private async void ToggleButtonFlash_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void FlyoutClose(object sender, RoutedEventArgs e)
        {
            VideoFlyout.Hide();
            WebFlyout.Hide();
        }
    }
}
