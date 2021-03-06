﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using CompiledAvaloniaXaml;
using GraphPrototype;
using ScottPlot.Avalonia;
using Serilog;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;


namespace GraphPrototype.BMP3
{
	public class Sensor : ISensor
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

		public static Sensor Create()
        {
			var newSensor = new Sensor();
			newSensor.Initialise();
			return newSensor;
		}

		public Sensor(int deviceId = 119)
		{
			Pi.Init<BootstrapWiringPi>();
			Device = Pi.I2C.AddDevice(deviceId);
		}

		public void Initialise()
		{
			Log.Information("Prepairing Sensor");
			byte deviceId = Device.ReadAddressByte(BMP388RegisterChipId);
			if (deviceId != 80)
			{
				Log.Error($"Expected register 0 to contain the value 0x50, actually 0x{deviceId:x}");
				return;
			}

			SoftReset();
			CalibrationData = ReadCalibrationData();
			SetPowerState();
			SetOdrFilter();
			SetIirFilter();
			SetMode();
			ReadPressure();
		}

		public void SoftReset()
		{
			Status sensorStatus = (Status)Device.ReadAddressByte(BMP388RegisterStatus);
			if (sensorStatus == Status.Ready)
			{
				Device.WriteAddressByte(BMP388RegisterCommand, (byte)Commands.SoftReset);
				Thread.Sleep(2);
				byte errorCode = Device.ReadAddressByte(BMP388RegisterErrorCode);
				Log.Information($"Soft Reset Outcome: 0x{errorCode:x}");
			}
			else
			{
				Log.Error($"Expected status to be 0x{Status.Ready:x}, actually 0x{sensorStatus:x}");
			}
		}

		public double ReadPressure()
		{
			TriggerReading();
			Thread.Sleep(2);

			uint pressureData = ReadThreeBytesData(4);
			uint temperatureData = ReadThreeBytesData(7);
			double temperature = CalibrationData.CompensateTemperature(temperatureData);
			double millibars = CalibrationData.CompensatePressure(pressureData) / 100.0 + 8.34;
			Log.Information($"Read temperature {temperature}, pressure {millibars}");
			return millibars;
		}

		private QuantizedCalibrationData ReadCalibrationData()
		{
			Device.Write((byte)BMP388RegisterCalibration);
			return QuantizedCalibrationData.ParseCalibrationData(Device.Read(21));
		}

		/// <summary>
		/// Wakes up the sensor to do a single reading
		/// </summary>
		private void TriggerReading()
		{
			byte powerState = Device.ReadAddressByte(BMP388RegisterPowerControl);
			powerState = (byte)(powerState & 0b11001111);
			powerState = (byte)(powerState | 0b00100000); // Set bit to trigger a reading
			Device.WriteAddressByte(BMP388RegisterPowerControl, powerState);
		}

		/// <summary>
		///  Puts the chip into sleep mode and turns on the sensors for future readings
		/// </summary>
		private void SetPowerState()
		{
			byte powerState3 = Device.ReadAddressByte(BMP388RegisterPowerControl);
			powerState3 = (byte)(powerState3 & 0b11001100);
			powerState3 = (byte)(powerState3 | 0b00000011);
			Device.WriteAddressByte(BMP388RegisterPowerControl, powerState3);
		}

		private void SetOdrFilter()
		{
			byte osrState3 = Device.ReadAddressByte(BMP388RegisterOsr);
			osrState3 = (byte)(osrState3 & 0b11000000);
			osrState3 = (byte)(osrState3 | 0b00001011);
			Device.WriteAddressByte(BMP388RegisterOsr, osrState3);
			byte odrState3 = Device.ReadAddressByte(BMP388RegisterOdr);
			odrState3 = (byte)(odrState3 & 0xE0);
			odrState3 = (byte)(odrState3 | 3);
			Device.WriteAddressByte(BMP388RegisterOdr, odrState3);
		}

		private void SetIirFilter()
		{
			byte iirFilter3 = Device.ReadAddressByte(BMP388RegisterIirFilter);
			iirFilter3 = (byte)(iirFilter3 & 0b11110001);
			iirFilter3 = (byte)(iirFilter3 | 0b00001000);
			Device.WriteAddressByte(BMP388RegisterIirFilter, iirFilter3);
		}

		private void SetMode()
		{
			byte powerState3 = Device.ReadAddressByte(BMP388RegisterPowerControl);
			powerState3 = (byte)(powerState3 & 0xCF);
			powerState3 = (byte)(powerState3 | 0x10);
			Device.WriteAddressByte(BMP388RegisterPowerControl, powerState3);
		}

		private uint ReadThreeBytesData(int baseAddress)
		{
			return BitConverter.ToUInt32(new byte[4]
			{
				Device.ReadAddressByte(baseAddress),
				Device.ReadAddressByte(baseAddress + 1),
				Device.ReadAddressByte(baseAddress + 2),
				0
			});
		}

		private II2CDevice Device { get; set; }
		private QuantizedCalibrationData CalibrationData { get; set; }
	}
}
