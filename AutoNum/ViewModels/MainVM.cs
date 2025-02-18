//using System.Windows;
using NumberIt.Infrastructure;
using System.Linq.Expressions;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Deployment;
using System.Deployment.Application;
using System.Windows;

namespace NumberIt.ViewModels
{

    public class MainVM : BaseViewModel
    {
        public RelayCommand cmdNext => _cmdNext ??= new RelayCommand(doNext, canDoNext);
        private void doNext(object? o)
        {
            if (o is string dir)
            {
                int cur = Steps.IndexOf(CurrentStep);
                if (dir == "up" && cur < Steps.Count - 1)
                {
                    CurrentStep = Steps[cur + 1];
                    CurrentStep.Enter(null);
                }
                else if (dir == "down" && cur > 0)
                {
                    CurrentStep = Steps[cur - 1];
                    CurrentStep.Enter(null);
                }
            }
        }

        bool canDoNext(object? o)
        {
            return pictureVM.ImageSource != null;
        }


        WizardStep? _currentStep;
        public WizardStep CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }


        List<WizardStep> Steps;

        //public string ImageFilename
        //{
        //    get => _imageFilename;
        //    set
        //    {
        //        SetProperty(ref _imageFilename, value);
        //    }
        //}

        //private string _imageFilename = "";

        ImageModel _pictureVM;
        public ImageModel pictureVM
        {
            get => _pictureVM;
            set
            {
                SetProperty(ref _pictureVM, value);
                //_pictureVM.CanvasSize = CanvasSize;
            }
        }

        
        public Size CanvasSize {
            get;
            set; }
        
           
        

        public IDialogService DialogService { get; }

        public MainVM(IDialogService DialogService)
        {
            this.DialogService = DialogService;
            this._pictureVM = new();

            Steps = [
                new OpenImageVM(this),
                new AnalyzeVM(this),
                new LabelsVM(this),
                new SaveImageVM(this)
            ];

            foreach (var step in Steps)
            {
                step.PropertyChanged += Step_PropertyChanged;
            }

            CurrentStep = Steps[0];
        }

        public string Title => $"AutoNumber V{versionString}";

        string versionString => Environment.GetEnvironmentVariable("ClickOnce_CurrentVersion") ?? "0.0.0.0";

        private void Step_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is OpenImageVM vm && e.PropertyName == "ImageFile")
            {
                //    (Steps[1] as AnalyzeVM)!.ImageFile = vm.ImageFile;

                //    using var EmguImage = CvInvoke.Imread(vm.ImageFile, Emgu.CV.CvEnum.ImreadModes.Color);
                //    ImageSource = EmguImage.ToBitmapSource();
                //    SetProperty(ref _imageFilename, vm.ImageFile);
            }
        }

        private BitmapSource? _imageSource;


        private RelayCommand? _cmdChangeImage;
        private RelayCommand? _cmdNext;
    }

    public static class Extensions
    {
        public static double StdDev<T>(this IEnumerable<T> list, Func<T, double> values)
        {
            // ref: https://stackoverflow.com/questions/2253874/linq-equivalent-for-standard-deviation
            // ref: http://warrenseen.com/blog/2006/03/13/how-to-calculate-standard-deviation/ 
            var mean = 0.0;
            var sum = 0.0;
            var stdDev = 0.0;
            var n = 0;
            foreach (var value in list.Select(values))
            {
                n++;
                var delta = value - mean;
                mean += delta / n;
                sum += delta * (value - mean);
            }
            if (1 < n)
                stdDev = Math.Sqrt(sum / (n - 1));

            return stdDev;

        }
    }

    public static class VersionHelper
    {

        public static string GetVersion()
        {
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                return ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            return "Debug Mode - No ClickOnce Version";
        }

    }
}
