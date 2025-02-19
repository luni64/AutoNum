namespace NumberIt.ViewModels
{
    public abstract class WizardStep : BaseViewModel
    {
        public virtual void Enter(object? o) { }
        public virtual void Leave(object? o) { }
    }
}
