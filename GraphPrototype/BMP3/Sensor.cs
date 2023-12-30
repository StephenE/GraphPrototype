using Serilog;
using System;
using System.Device.I2c;
using System.Threading;

namespace GraphPrototype.BMP3
{
    public class Sensor : ISensor
    {
        private byte BMP388RegisterChipId => 0;
        private byte BMP388RegisterErrorCode => 2;
        private byte BMP388RegisterStatus => 3;
        private byte BMP388RegisterPressure => 4;
        private byte BMP388RegisterTemperature => 7;
        private byte BMP388RegisterPowerControl => 27;
        private byte BMP388RegisterOsr => 28;
        private byte BMP388RegisterOdr => 29;
        private byte BMP388RegisterIirFilter => 31;
        private byte BMP388RegisterCalibration => 49;
        private byte BMP388RegisterCommand => 126;

        public static Sensor Create(byte deviceAddress = 119, byte deviceId = 80)
        {
            var i2cSettings = new I2cConnectionSettings(1, deviceAddress);
            var device = I2cDevice.Create(i2cSettings);
            var newSensor = new Sensor(device);
            newSensor.Initialise(deviceId);
            return newSensor;
        }

        public Sensor(I2cDevice device)
        {
            Device = device;
        }

        public void Initialise(byte deviceId)
        {
            Log.Information("Prepairing Sensor");
            byte actualDeviceId = ReadAddressByte(BMP388RegisterChipId);
            if (deviceId != actualDeviceId)
            {
                Log.Error($"Expected register 0 to contain the value 0x{deviceId:x}, actually 0x{actualDeviceId:x}");
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
            Status sensorStatus = (Status)ReadAddressByte(BMP388RegisterStatus);
            if (sensorStatus == Status.Ready)
            {
                WriteAddressByte(BMP388RegisterCommand, (byte)Commands.SoftReset);
                Thread.Sleep(2);
                byte errorCode = ReadAddressByte(BMP388RegisterErrorCode);
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

            uint pressureData = ReadThreeBytesData(BMP388RegisterPressure);
            uint temperatureData = ReadThreeBytesData(BMP388RegisterTemperature);
            double temperature = CalibrationData.CompensateTemperature(temperatureData);
            double millibars = CalibrationData.CompensatePressure(pressureData) / 100.0 + 8.34;
            Log.Information($"Read temperature {temperature}, pressure {millibars}");
            return millibars;
        }

        private QuantizedCalibrationData ReadCalibrationData()
        {
            Device.WriteByte(BMP388RegisterCalibration);
            byte[] buffer = new byte[21];
            Device.Read(buffer);
            Log.Debug($"Calibration data: {BitConverter.ToString(buffer)}");
            return QuantizedCalibrationData.ParseCalibrationData(buffer);
        }

        /// <summary>
        /// Wakes up the sensor to do a single reading
        /// </summary>
        private void TriggerReading()
        {
            byte powerState = ReadAddressByte(BMP388RegisterPowerControl);
            powerState = (byte)(powerState & 0b11001111);
            powerState = (byte)(powerState | 0b00100000); // Set bit to trigger a reading
            WriteAddressByte(BMP388RegisterPowerControl, powerState);
        }

        /// <summary>
        ///  Puts the chip into sleep mode and turns on the sensors for future readings
        /// </summary>
        private void SetPowerState()
        {
            byte powerState3 = ReadAddressByte(BMP388RegisterPowerControl);
            powerState3 = (byte)(powerState3 & 0b11001100);
            powerState3 = (byte)(powerState3 | 0b00000011);
            WriteAddressByte(BMP388RegisterPowerControl, powerState3);
        }

        private void SetOdrFilter()
        {
            byte osrState3 = ReadAddressByte(BMP388RegisterOsr);
            osrState3 = (byte)(osrState3 & 0b11000000);
            osrState3 = (byte)(osrState3 | 0b00001011);
            WriteAddressByte(BMP388RegisterOsr, osrState3);
            byte odrState3 = ReadAddressByte(BMP388RegisterOdr);
            odrState3 = (byte)(odrState3 & 0xE0);
            odrState3 = (byte)(odrState3 | 3);
            WriteAddressByte(BMP388RegisterOdr, odrState3);
        }

        private void SetIirFilter()
        {
            byte iirFilter3 = ReadAddressByte(BMP388RegisterIirFilter);
            iirFilter3 = (byte)(iirFilter3 & 0b11110001);
            iirFilter3 = (byte)(iirFilter3 | 0b00001000);
            WriteAddressByte(BMP388RegisterIirFilter, iirFilter3);
        }

        private void SetMode()
        {
            byte powerState3 = ReadAddressByte(BMP388RegisterPowerControl);
            powerState3 = (byte)(powerState3 & 0xCF);
            powerState3 = (byte)(powerState3 | 0x10);
            WriteAddressByte(BMP388RegisterPowerControl, powerState3);
        }

        private uint ReadThreeBytesData(byte baseAddress)
        {
            Device.WriteByte(baseAddress);
            var destination = new byte[4];
            Device.Read(new Span<byte>(destination, 0, 3));
            Log.Debug($"Read {BitConverter.ToString(destination)}");
            return BitConverter.ToUInt32(destination);
        }

        private byte ReadAddressByte(byte address)
        {
            Device.WriteByte(address);
            byte value = Device.ReadByte();
            Log.Debug($"Read 0x{value:x} from 0x{address}");
            return value;
        }

        private void WriteAddressByte(byte address, byte value)
        {
            Device.Write(new byte[2] { address, value });
            Log.Debug($"Wrote 0x{value:x} to 0x{address}");
        }


        private I2cDevice Device { get; set; }
        private QuantizedCalibrationData CalibrationData { get; set; }
    }
}
