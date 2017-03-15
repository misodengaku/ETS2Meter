using Ets2SdkClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ets2meter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Ets2SdkTelemetry Telemetry;
        public Ets2Telemetry data;
        public Stopwatch stoptimer = new Stopwatch();
        private int speedTestFlag = 0;
        private double rpm = 0;
        private UInt64 chartCnt = 0;
        private Series seriesRpm = new Series();
        private List<Tuple<UInt64, double>> rpmList = new List<Tuple<UInt64, double>>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            Telemetry = new Ets2SdkTelemetry();
            Telemetry.Data += Telemetry_Data;

            Telemetry.JobFinished += TelemetryOnJobFinished;
            Telemetry.JobStarted += TelemetryOnJobStarted;

            if (Telemetry.Error != null)
            {
                MessageBox.Show(Telemetry.Error.Message + "\r\n\r\nStacktrace:\r\n" + Telemetry.Error.StackTrace);
            }

            var chart = (Chart)windowsFormsHost.Child;

            seriesRpm.ChartType = SeriesChartType.Line;

            chart.Series.Add(seriesRpm);

        }


        private void TelemetryOnJobFinished(object sender, EventArgs args)
        {
            MessageBox.Show("Job finished, or at least unloaded nearby cargo destination.");
        }

        private void TelemetryOnJobStarted(object sender, EventArgs e)
        {
            MessageBox.Show("Just started job OR loaded game with active.");
        }

        private void Telemetry_Data(Ets2Telemetry _data, bool updated)
        {
            try
            {
                if (speedTestFlag == 1)
                {
                    if (_data.Drivetrain.SpeedKmh > 100.0000)
                    {
                        var result = stoptimer.ElapsedMilliseconds;
                        Dispatcher.Invoke(() =>
                        {
                            listBox.Items.Add("0-100km/h\t" + (result / 1000.0) + "s");
                            listBox.SelectedIndex = listBox.Items.Count - 1;
                        });
                        speedTestFlag = 2;
                    }
                    else if (stoptimer.ElapsedMilliseconds > 1500 && _data.Drivetrain.SpeedKmh < 0.2)
                    {
                        speedTestFlag = 0;
                    }
                }
                else if (speedTestFlag == 2)
                {
                    if (_data.Drivetrain.SpeedKmh > 200.0000)
                    {
                        var result = stoptimer.ElapsedMilliseconds;
                        Dispatcher.Invoke(() =>
                        {
                            listBox.Items.Add("0-200km/h\t" + (result / 1000.0) + "s");
                            listBox.SelectedIndex = listBox.Items.Count - 1;
                        });
                        speedTestFlag = 3;
                    }
                    else if (stoptimer.ElapsedMilliseconds > 1500 && _data.Drivetrain.SpeedKmh < 0.2)
                    {
                        speedTestFlag = 0;
                    }
                }
                else if (speedTestFlag == 3)
                {
                    if (_data.Drivetrain.SpeedKmh < 0.2)
                    {
                        speedTestFlag = 0;
                        //Dispatcher.Invoke(() => listBox.Items.Add("stop"));
                    }
                }
                else
                {
                    if (_data.Drivetrain.SpeedKmh > 0.2000)
                    {
                        stoptimer.Reset();
                        stoptimer.Start();
                        //Dispatcher.Invoke(() => listBox.Items.Add("start"));
                        speedTestFlag = 1;
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    var speed = _data.Drivetrain.SpeedKmh;
                    if (speed < 0)
                        speed *= -1;
                    speedLabel.Content = speed.ToString("0");
                    speedDecimalLabel.Content = (int)((speed - (int)speed) * 10);
                    brakeTempLabel.Content = _data.Drivetrain.BrakeTemperature;
                    if (_data.Drivetrain.Gear < 0)
                        gearLabel.Content = "R" + (_data.Drivetrain.Gear * -1);
                    else if (_data.Drivetrain.Gear == 0)
                        gearLabel.Content = "N";
                    else
                        gearLabel.Content = _data.Drivetrain.Gear;

                    rpm = rpm * 0.9 + _data.Drivetrain.EngineRpm * 0.1;

                    if (rpm < 1200)
                        rpmBar.Value = rpmBar.Minimum;
                    else if (rpm > 3000)
                        rpmBar.Value = rpmBar.Maximum;
                    else
                        rpmBar.Value = rpm;

                    rpmLabel.Content = rpm.ToString("0");
                    if (rpm > 2300)
                        shiftIndicator.Fill = Brushes.Red;
                    else if (rpm < 1500)
                        shiftIndicator.Fill = Brushes.Blue;
                    else
                        shiftIndicator.Fill = Brushes.White;

                    rpmList.Add(new Tuple<ulong, double>(chartCnt, rpm));

                    seriesRpm.Points.Clear();
                    foreach(var x in rpmList.Skip(Math.Max(0, rpmList.Count - 200)))
                    {
                        seriesRpm.Points.AddXY(x.Item1, x.Item2);
                    }
                    
                    chartCnt++;


                });
                
            }
            catch
            {
            }
        }
    }

    public class Test
    {
        public double Speed { get; set; }
        public int Gear { get; set; }

    }
}
