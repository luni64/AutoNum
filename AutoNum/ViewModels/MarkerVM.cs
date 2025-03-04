namespace AutoNumber.ViewModels
{
    public class MarkerVM : BaseViewModel
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        virtual public double X { get => _x; set => SetProperty(ref _x, value); }
        virtual public double Y { get => _y; set => SetProperty(ref _y, value); }
        public double W { get => _w; set => SetProperty(ref _w, value); }
        public double H { get => _h; set => SetProperty(ref _h, value); }
        public double StrokeThickness => Math.Max(2, W / 25.0);
        public bool isLocked { get => _isLocked; set => SetProperty(ref _isLocked, value); }
        public bool visible { get => _visible; set => SetProperty(ref _visible, value); }

        private double _x, _y, _w, _h;                
        private bool _isLocked = true, _visible = true;        
    }
}
