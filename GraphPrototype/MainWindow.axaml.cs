using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ReactiveUI;
using ScottPlot.Avalonia;
using Serilog;
using System;

namespace GraphPrototype
{
    public class MainWindowViewModel : ReactiveObject
    {
        public DateTime CurrentTime
        {
            get => m_CurrentTime;
            set => this.RaiseAndSetIfChanged(ref m_CurrentTime, value);
        }

        public DateTime ReadingTime
        {
            get => m_ReadingTime;
            set => this.RaiseAndSetIfChanged(ref m_ReadingTime, value);
        }

        public double Pressure
        {
            get => m_Pressure;
            set => this.RaiseAndSetIfChanged(ref m_Pressure, value);
        }

        private double m_Pressure = 999;
        private DateTime m_CurrentTime = DateTime.MinValue;
        private DateTime m_ReadingTime = DateTime.MinValue;
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

        /// <summary>
        /// How much time is shown on the graph
        /// </summary>
        TimeSpan GraphDuration => TimeSpan.FromHours(1);
        /// <summary>
        /// How often we read the pressure
        /// </summary>
        TimeSpan ReadingFrequency => TimeSpan.FromMinutes(1);
        /// <summary>
        /// How many decimal places to show on the X Axis
        /// </summary>
        /// <remarks>
        /// F2: Two decimal places. F3: Three decimal places
        /// </remarks>
        string XAxisFormat = "F2";
        /// <summary>
        /// What colour are the grid lines
        /// </summary>
        /// <remarks>
        /// Use Paint to figure out the red, green and blue values
        /// </remarks>
        System.Drawing.Color GridLinesColor => System.Drawing.Color.FromArgb(red: 150, green: 150, blue: 150);

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Prepare sensor
            Sensor = new BMP3.Sensor();
            Sensor.Initialise();

            // Take first reading
            var viewModel = DataContext as MainWindowViewModel;
            UpdateViewModel(viewModel);

            // Prepare graph
            Graph = this.Find<AvaPlot>("avaPlot1");
            int numberOfElements = (int)(GraphDuration.TotalSeconds / ReadingFrequency.TotalSeconds);
            double[] dataX = new double[numberOfElements];
            double[] dataY = new double[numberOfElements];

            for (int counter = 0; counter < numberOfElements; ++counter)
            {
                dataX[counter] = viewModel.ReadingTime.ToOADate();
                dataY[counter] = viewModel.Pressure;
            }

            Series = Graph.plt.PlotScatter(dataX, dataY);
            Graph.plt.Ticks(numericFormatStringY: XAxisFormat, dateTimeX: true, dateTimeFormatStringX: "HH:mm");
            Graph.plt.Grid(color: GridLinesColor);
            UpdateAxis(viewModel.ReadingTime);
            Graph.Render();

            // Set refresh frequency
            RefreshTimer.Interval = TimeSpan.FromMilliseconds(100);
            RefreshTimer.Tick += RefreshTimer_Tick;
            RefreshTimer.Start();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            Log.Information("Starting tick");
            try
            {
                var viewModel = DataContext as MainWindowViewModel;
                bool hasNewReading = UpdateViewModel(viewModel);

                if (hasNewReading)
                {
                    Log.Information("Updating Graph");
                    Array.Copy(Series.xs, 1, Series.xs, 0, Series.xs.Length - 1);
                    Array.Copy(Series.ys, 1, Series.ys, 0, Series.ys.Length - 1);
                    Series.xs[Series.xs.Length - 1] = viewModel.ReadingTime.ToOADate();
                    Series.ys[Series.ys.Length - 1] = viewModel.Pressure;
                    UpdateAxis(viewModel.ReadingTime);

                    Log.Information("Rendering Graph");
                    Graph.Render();
                }
                Log.Information("Ending tick");
            }
            catch(Exception error)
            {
                Log.Fatal("Unhandled exception {0}", error);
                throw error;
            }
        }

        private bool UpdateViewModel(MainWindowViewModel viewModel)
        {
            viewModel.CurrentTime = DateTime.Now;

            // Update the reading once the duration set by ReadingFrequency has passed
            if (viewModel.ReadingTime + ReadingFrequency <= viewModel.CurrentTime)
            {
                viewModel.Pressure = Sensor.ReadPressure();
                viewModel.ReadingTime = RoundTime(viewModel.CurrentTime);

                return true;
            }
            else
            {
                return false;
            }
        }

        private DateTime RoundTime(DateTime currentTime)
        {
            if (currentTime.Second > 30)
            {
                // Round up to the next minute
                return currentTime.AddSeconds(60 - currentTime.Second);
            }
            else if (currentTime.Second > 0)
            {
                // Round down to the previous minute
                return currentTime.AddSeconds(currentTime.Second * -1);
            }
            else
            {
                // Already rounded to a minute
                return currentTime;
            }
        }

        private void UpdateAxis(DateTime readingTime)
        {
            // Adjust X axis to current duration, latest reading on the right
            Graph.plt.Axis(x1: (readingTime - GraphDuration).ToOADate(), x2: readingTime.ToOADate());
            Graph.plt.AxisAutoY();
        }

        private DispatcherTimer RefreshTimer { get; set; } = new DispatcherTimer();
        private BMP3.Sensor Sensor { get; set; }
        private AvaPlot Graph { get; set; }
        private ScottPlot.PlottableScatter Series { get; set; }
        private int readingIndex = 0;
    }
}
