using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.AccessControl;

namespace NumberIt.ViewModels
{
    public class NamesVM : WizardStep
    {
        public ICollectionView Labels => parent.pictureVM.Labels;

        public MainVM parent { get; set; }

        bool _addNames = true;
        public bool AddNames
        {
            get => _addNames;
            set => SetProperty(ref _addNames, value);
        }

        public NamesVM(MainVM parent)
        {
            this.parent = parent;
        }
    }
}
