using FftDataAnalyzer.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FftDataAnalyzer.Views
{
    public partial class UploadPage : Page
    {
        public UploadPage()
        {
            InitializeComponent();
        }

        private void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var viewModel = DataContext as UploadViewModel;
                viewModel?.HandleFileDrop(files);
            }
        }
    }
}
