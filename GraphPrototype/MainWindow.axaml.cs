using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

        public bool EnableAutoScrollButton
        {
            get => m_EnableAutoScrollButton;
            set => this.RaiseAndSetIfChanged(ref m_EnableAutoScrollButton, value);
        }

        public bool DoAutoScroll { get; set; } = false;

        public void OnResetAutoScroll()
        {
            DoAutoScroll = true;
        }

        private double m_Pressure = 999;
        private DateTime m_CurrentTime = DateTime.MinValue;
        private DateTime m_ReadingTime = DateTime.MinValue;
        private bool m_EnableAutoScrollButton = false;
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
        TimeSpan GraphDuration => TimeSpan.FromDays(2);
        /// <summary>
        /// How often we read the pressure
        /// </summary>
        TimeSpan ReadingFrequency => TimeSpan.FromMinutes(5);
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
        /// <summary>
        /// Set to true to use the auto axis provided by ScottPlot.
        /// Set to false to use our own custom scrolling logic.
        /// </summary>
        bool AutoAxis => false;
        /// <summary>
        /// X Axis will auto-scroll if the time shown on the right is within this threshold
        /// </summary>
        TimeSpan XAxisAutoScrollSnap => ReadingFrequency * 2;
        /// <summary>
        /// When auto-scrolling the Y axis, what faction of padding to add when shifting up/down
        /// </summary>
        /// <remarks>
        /// 1/100.0f is 1%
        /// </remarks>
        double YAxisScrollPadding => 1 / 100.0f;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Prepare sensor. Use a real sensor on Arm, or a fake on Windows
            if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm || System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
            {
                PressureSensor = BMP3.Sensor.Create();
                ClockSensor = new Clock.SystemClock();
            }
            else
            {
                PressureSensor = new BMP3.FakeSensor();
                ClockSensor = new Clock.FakeClock { SpeedOfTime = 200 };
            }
            
            
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
            UpdateAxis(viewModel, autoAxis: true);
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
                    UpdateAxis(viewModel, autoAxis: AutoAxis || viewModel.DoAutoScroll);
                    viewModel.DoAutoScroll = false;

                    Log.Information("Rendering Graph");
                    Graph.Render();
                }
                else if(viewModel.DoAutoScroll)
                {
                    UpdateAxis(viewModel, autoAxis: true);
                    viewModel.DoAutoScroll = false;
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
            viewModel.CurrentTime = ClockSensor.Now;

            // Update the reading once the duration set by ReadingFrequency has passed
            if (viewModel.ReadingTime + ReadingFrequency <= viewModel.CurrentTime)
            {
                viewModel.Pressure = PressureSensor.ReadPressure();
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

        private void UpdateAxis(MainWindowViewModel viewModel, bool autoAxis)
        {
            if (autoAxis)
            {
                // Adjust X axis to current duration, latest reading on the right
                Graph.plt.Axis(x1: (viewModel.ReadingTime - GraphDuration).ToOADate(), x2: viewModel.ReadingTime.ToOADate());
                Graph.plt.AxisAutoY();
            }
            else
            {
                var axisSettings = Graph.plt.Axis();

                if (axisSettings[AxisXMaxIndex] > viewModel.ReadingTime.ToOADate())
                {
                    // The graph is showing the future. Take no action
                    viewModel.EnableAutoScrollButton = true;
                }
                else if(axisSettings[AxisXMaxIndex] >= (viewModel.ReadingTime - XAxisAutoScrollSnap).ToOADate())
                {
                    // The graph was showing the future, but our new reading goes off the end. Auto-scroll the X
                    axisSettings[AxisXMaxIndex] = viewModel.ReadingTime.ToOADate();

                    // Expand the Y so the new value fits on
                    double readingValue = Series.ys[Series.ys.Length - 1];
                    double scrollAmount = 0;
                    if (axisSettings[AxisYMinIndex] > readingValue)
                    {
                        // New value is off the bottom, so scroll down
                        double padding = (axisSettings[AxisYMaxIndex] - axisSettings[AxisYMinIndex]) * YAxisScrollPadding;
                        scrollAmount = (readingValue - axisSettings[AxisYMinIndex]) - padding;
                    }
                    else if (axisSettings[AxisYMaxIndex] < readingValue)
                    {
                        // New value is off the top, so scroll up
                        double padding = (axisSettings[AxisYMaxIndex] - axisSettings[AxisYMinIndex]) * YAxisScrollPadding;
                        scrollAmount = (readingValue - axisSettings[AxisYMaxIndex]) + padding;
                    }

                    axisSettings[AxisYMinIndex] += scrollAmount;
                    axisSettings[AxisYMaxIndex] += scrollAmount;

                    // Apply the axis settings
                    Graph.plt.Axis(axisSettings);
                    viewModel.EnableAutoScrollButton = false;
                }
                else
                {
                    // The graph is showing the past. Take no action
                    viewModel.EnableAutoScrollButton = true;
                }
            }
        }

        

        private int AxisXMinIndex => 0;
        private int AxisXMaxIndex => 1;
        private int AxisYMinIndex => 2;
        private int AxisYMaxIndex => 3;

        private DispatcherTimer RefreshTimer { get; set; } = new DispatcherTimer();
        private BMP3.ISensor PressureSensor { get; set; }
        private Clock.IClock ClockSensor { get; set; }
        private AvaPlot Graph { get; set; }
        private ScottPlot.PlottableScatter Series { get; set; }
    }
}
