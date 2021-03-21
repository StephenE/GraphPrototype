using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ReactiveUI;
using ScottPlot.Avalonia;
using System;

namespace GraphPrototype
{
    public class MainWindowViewModel : ReactiveObject
    {
        public double Pressure
        {
            get => m_Pressure;
            set => this.RaiseAndSetIfChanged(ref m_Pressure, value);
        }

        private double m_Pressure = 999;
    }

    public class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = new MainWindowViewModel();

            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        TimeSpan GraphDuration => TimeSpan.FromHours(12);
        TimeSpan ReadingFrequency => TimeSpan.FromMinutes(5);

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Prepare sensor
            Sensor = new BMP3.Sensor();
            Sensor.Initialise();

            // Take first reading
            var viewModel = DataContext as MainWindowViewModel;
            viewModel.Pressure = Sensor.ReadPressure();

            // Prepare graph
            Graph = this.Find<AvaPlot>("avaPlot1");
            int numberOfElements = (int)(GraphDuration.TotalSeconds / ReadingFrequency.TotalSeconds);
            double[] dataX = new double[numberOfElements];
            double[] dataY = new double[numberOfElements];
            for (int counter = 0; counter < numberOfElements; ++counter)
            {
                dataX[counter] = 0;
                dataY[counter] = viewModel.Pressure;
            }

            Series = Graph.plt.PlotScatter(dataX, dataY);
            Graph.plt.AxisAuto();
            Graph.Render();

            // Set refresh frequency
            RefreshTimer.Interval = ReadingFrequency;
            RefreshTimer.Tick += RefreshTimer_Tick;
            RefreshTimer.Start();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            viewModel.Pressure = Sensor.ReadPressure();

            Array.Copy(Series.xs, 1, Series.xs, 0, Series.xs.Length - 1);
            Array.Copy(Series.ys, 1, Series.ys, 0, Series.ys.Length - 1);
            Series.xs[Series.xs.Length - 1] = ++readingIndex;
            Series.ys[Series.ys.Length - 1] = viewModel.Pressure;

            Graph.plt.AxisAuto();
            Graph.Render();
        }

        private DispatcherTimer RefreshTimer { get; set; } = new DispatcherTimer();
        private BMP3.Sensor Sensor { get; set; }
        private AvaPlot Graph { get; set; }
        private ScottPlot.PlottableScatter Series { get; set; }
        private int readingIndex = 0;
    }
}
