//using System.Windows;
using NumberIt.Infrastructure;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            return !(pictureVM.emguImage?.IsEmpty ?? true);
        }

        

        public ImageModel pictureVM { get;  }
     
        public WizardStep CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }
      

        public IDialogService DialogService { get; }


        public MainVM(IDialogService DialogService)
        {
            this.DialogService = DialogService;
            this.pictureVM = new();

            Steps = [
                new OpenImageVM(this),
                new AnalyzeVM(this),
                new LabelsVM(this),
                new SaveImageVM(this)
            ];
                     
            _currentStep = Steps[0];
        }

        public string Title => $"AutoNumber V{Environment.GetEnvironmentVariable("ClickOnce_CurrentVersion") ?? "0.0.0.0"}";
               
        List<WizardStep> Steps;
        WizardStep _currentStep;        
                
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
}
