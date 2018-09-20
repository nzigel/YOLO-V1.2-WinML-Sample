using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TinyYOLO;
using Windows.AI.MachineLearning;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace YoLo
{
    /// <summary>
    /// Based of work by https://elbruno.com/2018/06/28/winml-how-to-create-a-windows10-app-using-yolo-for-object-detection/
    /// and https://github.com/sevans4067/WinMl-TinyYOLO
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string _kModelFileName = "tiny-yolov2-1.2.onnx";
        private const LearningModelDeviceKind _kModelDeviceKind = LearningModelDeviceKind.Default;
        private Model _model = null;
        private Input _input = new Input();
        private Output _output = new Output();

        private uint _canvasActualWidth;
        private uint _canvasActualHeight;
        private Stopwatch _stopwatch;

        private readonly SolidColorBrush _lineBrushYellow = new SolidColorBrush(Windows.UI.Colors.Yellow);
        private readonly SolidColorBrush _lineBrushGreen = new SolidColorBrush(Windows.UI.Colors.Green);
        private readonly SolidColorBrush _fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
        private readonly double _lineThickness = 2.0;

        private IList<YoloBoundingBox> _boxes = new List<YoloBoundingBox>();
        private readonly YoloWinMlParser _parser = new YoloWinMlParser();

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Load the model
            await LoadModelAsync();

            GetCameraSize();
            Window.Current.SizeChanged += Current_SizeChanged;

            await CameraPreview.StartAsync();
            CameraPreview.CameraHelper.FrameArrived += CameraHelper_FrameArrived;
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            GetCameraSize();
        }

        private void GetCameraSize()
        {
            _canvasActualWidth = (uint)CameraPreview.ActualWidth;
            _canvasActualHeight = (uint)CameraPreview.ActualHeight;
        }

        private async Task LoadModelAsync()
        {
            // just load the model one time.
            if (_model != null) return;

            Debug.WriteLine($"Loading {_kModelFileName} ... patience ");

            try
            {
                // Load and create the model 
                var modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{_kModelFileName}"));
                _model = await Model.CreateFromStreamAsync(modelFile, new LearningModelDevice(_kModelDeviceKind));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"error: {ex.Message}");
                _model = null;
            }
        }
        
        private async void CameraHelper_FrameArrived(object sender, Microsoft.Toolkit.Uwp.Helpers.FrameEventArgs e)
        {
            if (e?.VideoFrame?.SoftwareBitmap == null) return;

            SoftwareBitmap softwareBitmap = SoftwareBitmap.Convert(e.VideoFrame.SoftwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            VideoFrame inputFrame = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);
            _input.image = ImageFeatureValue.CreateFromVideoFrame(inputFrame);

            // Evaluate the model
            _stopwatch = Stopwatch.StartNew();
            _output = await _model.EvaluateAsync(_input);
            _stopwatch.Stop();

            IReadOnlyList<float> VectorImage = _output.grid.GetAsVectorView();
            float[] ImageAry = VectorImage.ToArray();

            _boxes = _parser.ParseOutputs(ImageAry);

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                TextBlockInformation.Text = $"{1000f / _stopwatch.ElapsedMilliseconds,4:f1} fps on Width {_canvasActualWidth} x Height {_canvasActualHeight}";
                DrawOverlays(e.VideoFrame);
            });

            //Debug.WriteLine(ImageList.ToString());
        }

        private void DrawOverlays(VideoFrame inputImage)
        {
            YoloCanvas.Children.Clear();
            if (_boxes.Count <= 0) return;
            var filteredBoxes = _parser.NonMaxSuppress(_boxes, 5, .5F);

            foreach (var box in filteredBoxes)
                DrawYoloBoundingBox(box, YoloCanvas);
        }

        private void DrawYoloBoundingBox(YoloBoundingBox box, Canvas overlayCanvas)
        {
            // process output boxes
            var x = (uint)Math.Max(box.X, 0);
            var y = (uint)Math.Max(box.Y, 0);
            var w = (uint)Math.Min(overlayCanvas.ActualWidth - x, box.Width);
            var h = (uint)Math.Min(overlayCanvas.ActualHeight - y, box.Height);

            // fit to current canvas and webcam size
            x = _canvasActualWidth * x / 416;
            y = _canvasActualHeight * y / 416;
            w = _canvasActualWidth * w / 416;
            h = _canvasActualHeight * h / 416;

            var rectStroke = box.Label == "person" ? _lineBrushGreen : _lineBrushYellow;

            var r = new Windows.UI.Xaml.Shapes.Rectangle
            {
                Tag = box,
                Width = w,
                Height = h,
                Fill = _fillBrush,
                Stroke = rectStroke,
                StrokeThickness = _lineThickness,
                Margin = new Thickness(x, y, 0, 0)
            };

            var tb = new TextBlock
            {
                Margin = new Thickness(x + 4, y + 4, 0, 0),
                Text = $"{box.Label} ({Math.Round(box.Confidence, 4)})",
                FontWeight = FontWeights.Bold,
                Width = 126,
                Height = 21,
                HorizontalTextAlignment = TextAlignment.Center
            };

            var textBack = new Windows.UI.Xaml.Shapes.Rectangle
            {
                Width = 134,
                Height = 29,
                Fill = rectStroke,
                Margin = new Thickness(x, y, 0, 0)
            };

            overlayCanvas.Children.Add(textBack);
            overlayCanvas.Children.Add(tb);
            overlayCanvas.Children.Add(r);
        }
    }
}
