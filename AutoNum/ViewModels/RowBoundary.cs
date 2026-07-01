namespace AutoNumber.ViewModels
{
    public class RowBoundary : BaseViewModel
    {
        public double LeftY
        {
            get => _leftY;
            set => SetProperty(ref _leftY, value);
        }

        public double RightY
        {
            get => _rightY;
            set => SetProperty(ref _rightY, value);
        }

        public RowBoundary()
        {
        }

        public RowBoundary(double leftY, double rightY)
        {
            LeftY = leftY;
            RightY = rightY;
        }

        public double GetYAtX(double x, double imageWidth)
        {
            if (imageWidth <= 0)
            {
                return LeftY;
            }

            var t = Math.Clamp(x / imageWidth, 0.0, 1.0);
            return LeftY + (RightY - LeftY) * t;
        }

        private double _leftY;
        private double _rightY;
    }
}
