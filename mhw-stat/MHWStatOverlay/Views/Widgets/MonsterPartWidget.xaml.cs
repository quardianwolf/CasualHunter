using System.Windows.Input;
using MHWStatOverlay.Models;
using MHWStatOverlay.ViewModels;

namespace MHWStatOverlay.Views.Widgets;

public partial class MonsterPartWidget : System.Windows.Controls.UserControl
{
    public MonsterPartWidget()
    {
        InitializeComponent();
    }

    private void EyeIcon_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement fe && fe.DataContext is MonsterPart part)
        {
            part.IsHidden = true;

            // Persist in ViewModel
            if (DataContext is OverlayViewModel vm)
                vm.HidePart(part.Name);

            e.Handled = true;
        }
    }
}
