using FftDataAnalyzer.Helpers;
using FftDataAnalyzer.ViewModels;
using Microsoft.Win32;
using NLog;
using System;
using System.Linq;
using System.Windows;

namespace FftDataAnalyzer.Views
{
    public partial class FftDetailView : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly FftDetailViewModel _viewModel;
        private bool _chartRendered = false;

        public FftDetailView(Guid recordId)
        {
            InitializeComponent();

            _viewModel = new FftDetailViewModel(recordId);
            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.Samples.CollectionChanged += Samples_CollectionChanged;
        }

        private void Samples_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!_chartRendered && _viewModel.Samples.Count > 0)
            {
                _chartRendered = true;
                Dispatcher.BeginInvoke(new Action(() => RenderChart()), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.Samples))
            {
                RenderChart();
            }
        }

        private void RenderChart()
        {
            try
            {
                if (_viewModel.Samples == null || _viewModel.Samples.Count == 0)
                    return;

                FftChart.Plot.Clear();

                var frequencies = _viewModel.Samples.Select(s => s.Frequency).ToArray();
                var amplitudes = _viewModel.Samples.Select(s => s.Amplitude).ToArray();

                var scatter = FftChart.Plot.AddScatter(frequencies, amplitudes);
                scatter.MarkerSize = 0;
                scatter.LineWidth = 2;
                scatter.Color = System.Drawing.Color.FromArgb(59, 130, 246);

                FftChart.Plot.Title($"FFT Analysis - {_viewModel.Record?.DisplayName}");
                FftChart.Plot.XLabel("Frequency (Hz)");
                FftChart.Plot.YLabel("Amplitude");

                FftChart.Plot.Style(ScottPlot.Style.Blue1);
                FftChart.Plot.Style(figureBackground: System.Drawing.Color.FromArgb(15, 23, 42));
                FftChart.Plot.Style(dataBackground: System.Drawing.Color.FromArgb(11, 18, 32));

                FftChart.Plot.AxisAuto();
                FftChart.Refresh();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to render chart");
                Utilities.ShowError($"Failed to render chart: {ex.Message}");
            }
        }

        private void ExportPngButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "PNG Files (*.png)|*.png",
                    FileName = $"{_viewModel.Record?.DisplayName}_FFT.png",
                    Title = "Export Chart to PNG"
                };

                if (dialog.ShowDialog() == true)
                {
                    FftChart.Plot.SaveFig(dialog.FileName);
                    Logger.Info($"Exported chart to PNG: {dialog.FileName}");
                    Utilities.ShowInfo($"Chart exported successfully to:\n{dialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to export chart to PNG");
                Utilities.ShowError($"Failed to export chart: {ex.Message}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
