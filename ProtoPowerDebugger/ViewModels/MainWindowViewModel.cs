using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoPowerDebugger.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            adcDebugContent = AdcDebugView = new AdcDebugViewModel();
        }

        ViewModelBase adcDebugContent;

        public ViewModelBase AdcDebugContent
        {
            get => adcDebugContent;
            private set => this.RaiseAndSetIfChanged(ref adcDebugContent, value);
        }

        public AdcDebugViewModel AdcDebugView { get; }
    }
}
