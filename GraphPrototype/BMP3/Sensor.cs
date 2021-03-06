using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using CompiledAvaloniaXaml;
using GraphPrototype;
using ScottPlot.Avalonia;
using Serilog;
using System;
using System.ComponentModel;
using System.Threading;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;


namespace GraphPrototype.BMP3
{
	public class Sensor
	{
		private int BMP388RegisterChipId => 0;
		private int BMP388RegisterErrorCode => 2;
		private int BMP388RegisterStatus => 3;
		private int BMP388RegisterPowerControl => 27;
		private int BMP388RegisterOsr => 28;
		private int BMP388RegisterOdr => 29;
		private int BMP388RegisterIirFilter => 31;
		private int BMP388RegisterCalibration => 49;
		private int BMP388RegisterCommand => 126;

		public Sensor()
		{
			Pi.Init<BootstrapWiringPi>();
			Device = Pi.I2C.AddDevice(119);
			Log.Information("Prepairing Sensor");
			byte deviceId = Device.ReadAddressByte(BMP388RegisterChipId);
			if (deviceId != 80)
			{
				Log.Error($"Expected register 0 to contain the value 0x50, actually 0x{deviceId:x}");
				return;
			}
			else
			{
				Log.Information($"Expected register 0 contained the value 0x50");
			}

			/*SoftReset();
			Bmp3QuantizedCalibData calibrationData = ReadCalibrationData();
			SetPowerState();
			SetOdrFilter();
			SetIirFilter();
			SetMode();
			uint pressureData = ReadThreeBytesData(4);
			uint temperatureData = ReadThreeBytesData(7);
			double temperature = calibrationData.CompensateTemperature(temperatureData);
			double millibars = calibrationData.CompensatePressure(pressureData) / 100.0 + 8.34;
			Log.Information($"Read temperature {temperature}, pressure {millibars}");*/
		}

		private II2CDevice Device { get; set; }
	}
}
