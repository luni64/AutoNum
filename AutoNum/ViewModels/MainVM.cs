using NumberIt.Infrastructure;

namespace NumberIt.ViewModels
{
    public class MainVM : BaseViewModel
    {
        public OpenImageVM openImageVM { get; }
        public NameManager namesVM { get; }
        public TitleManager TitleVM { get; }       
        public ImageModel pictureVM { get; }
        public LabelsVM labelsVM { get; }
        public AnalyzeVM analyzeVM { get; }

        public IDialogService DialogService { get; }
        public string Title => $"AutoNumber V{Environment.GetEnvironmentVariable("ClickOnce_CurrentVersion") ?? "x.x"}";

        public MainVM(IDialogService DialogService)
        {
            this.DialogService = DialogService;
            this.pictureVM = new(this);

            openImageVM = new OpenImageVM(this);
            analyzeVM = new AnalyzeVM(this);
            labelsVM = new LabelsVM(this);
            namesVM = new NameManager(this);
            TitleVM = new TitleManager(this);
        }
    }

    //public static class Extensions
    //{
    //    public static double StdDev<T>(this IEnumerable<T> list, Func<T, double> values)
    //    {
    //        // ref: https://stackoverflow.com/questions/2253874/linq-equivalent-for-standard-deviation
    //        // ref: http://warrenseen.com/blog/2006/03/13/how-to-calculate-standard-deviation/ 
    //        var mean = 0.0;
    //        var sum = 0.0;
    //        var stdDev = 0.0;
    //        var n = 0;
    //        foreach (var value in list.Select(values))
    //        {
    //            n++;
    //            var delta = value - mean;
    //            mean += delta / n;
    //            sum += delta * (value - mean);
    //        }
    //        if (1 < n)
    //            stdDev = Math.Sqrt(sum / (n - 1));

    //        return stdDev;

    //    }
    //}
}
