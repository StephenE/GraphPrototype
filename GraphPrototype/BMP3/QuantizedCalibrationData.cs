using System;
using System.Collections.Generic;
using System.Text;

namespace GraphPrototype.BMP3
{
	public class QuantizedCalibrationData
	{
		public double par_t1;

		public double par_t2;

		public double par_t3;

		public double par_p1;

		public double par_p2;

		public double par_p3;

		public double par_p4;

		public double par_p5;

		public double par_p6;

		public double par_p7;

		public double par_p8;

		public double par_p9;

		public double par_p10;

		public double par_p11;

		public double t_lin;

		private static ushort BMP3_CONCAT_BYTES(byte msb, byte lsb)
		{
			return (ushort)((msb << 8) | lsb);
		}

		public static QuantizedCalibrationData ParseCalibrationData(byte[] reg_data)
		{
			CalibrationData reg_calib_data = new CalibrationData();
			QuantizedCalibrationData quantized_calib_data = new QuantizedCalibrationData();
			double temp_var14 = 0.00390625;
			reg_calib_data.par_t1 = BMP3_CONCAT_BYTES(reg_data[1], reg_data[0]);
			quantized_calib_data.par_t1 = (double)(int)reg_calib_data.par_t1 / temp_var14;
			reg_calib_data.par_t2 = BMP3_CONCAT_BYTES(reg_data[3], reg_data[2]);
			temp_var14 = 1073741824.0;
			quantized_calib_data.par_t2 = (double)(int)reg_calib_data.par_t2 / temp_var14;
			reg_calib_data.par_t3 = (sbyte)reg_data[4];
			temp_var14 = 281474976710656.0;
			quantized_calib_data.par_t3 = (double)reg_calib_data.par_t3 / temp_var14;
			reg_calib_data.par_p1 = (short)BMP3_CONCAT_BYTES(reg_data[6], reg_data[5]);
			temp_var14 = 1048576.0;
			quantized_calib_data.par_p1 = (double)(reg_calib_data.par_p1 - 16384) / temp_var14;
			reg_calib_data.par_p2 = (short)BMP3_CONCAT_BYTES(reg_data[8], reg_data[7]);
			temp_var14 = 536870912.0;
			quantized_calib_data.par_p2 = (double)(reg_calib_data.par_p2 - 16384) / temp_var14;
			reg_calib_data.par_p3 = (sbyte)reg_data[9];
			temp_var14 = 4294967296.0;
			quantized_calib_data.par_p3 = (double)reg_calib_data.par_p3 / temp_var14;
			reg_calib_data.par_p4 = (sbyte)reg_data[10];
			temp_var14 = 137438953472.0;
			quantized_calib_data.par_p4 = (double)reg_calib_data.par_p4 / temp_var14;
			reg_calib_data.par_p5 = BMP3_CONCAT_BYTES(reg_data[12], reg_data[11]);
			temp_var14 = 0.125;
			quantized_calib_data.par_p5 = (double)(int)reg_calib_data.par_p5 / temp_var14;
			reg_calib_data.par_p6 = BMP3_CONCAT_BYTES(reg_data[14], reg_data[13]);
			temp_var14 = 64.0;
			quantized_calib_data.par_p6 = (double)(int)reg_calib_data.par_p6 / temp_var14;
			reg_calib_data.par_p7 = (sbyte)reg_data[15];
			temp_var14 = 256.0;
			quantized_calib_data.par_p7 = (double)reg_calib_data.par_p7 / temp_var14;
			reg_calib_data.par_p8 = (sbyte)reg_data[16];
			temp_var14 = 32768.0;
			quantized_calib_data.par_p8 = (double)reg_calib_data.par_p8 / temp_var14;
			reg_calib_data.par_p9 = (short)BMP3_CONCAT_BYTES(reg_data[18], reg_data[17]);
			temp_var14 = 281474976710656.0;
			quantized_calib_data.par_p9 = (double)reg_calib_data.par_p9 / temp_var14;
			reg_calib_data.par_p10 = (sbyte)reg_data[19];
			temp_var14 = 281474976710656.0;
			quantized_calib_data.par_p10 = (double)reg_calib_data.par_p10 / temp_var14;
			reg_calib_data.par_p11 = (sbyte)reg_data[20];
			temp_var14 = 3.6893488147419103E+19;
			quantized_calib_data.par_p11 = (double)reg_calib_data.par_p11 / temp_var14;
			reg_calib_data.t_lin = 0L;
			quantized_calib_data.t_lin = 0.0;
			return quantized_calib_data;
		}

		public double CompensateTemperature(uint uncomp_temp)
		{
			double partial_data3 = uncomp_temp - par_t1;
			double partial_data2 = partial_data3 * par_t2;
			t_lin = partial_data2 + partial_data3 * partial_data3 * par_t3;
			return t_lin;
		}

		public double CompensatePressure(uint uncomp_data)
		{
			double partial_data13 = par_p6 * t_lin;
			double partial_data12 = par_p7 * Math.Pow(t_lin, 2.0);
			double partial_data11 = par_p8 * Math.Pow(t_lin, 3.0);
			double num = par_p5 + partial_data13 + partial_data12 + partial_data11;
			partial_data13 = par_p2 * t_lin;
			partial_data12 = par_p3 * Math.Pow(t_lin, 2.0);
			partial_data11 = par_p4 * Math.Pow(t_lin, 3.0);
			double partial_out2 = (double)uncomp_data * (par_p1 + partial_data13 + partial_data12 + partial_data11);
			partial_data13 = Math.Pow(uncomp_data, 2.0);
			partial_data12 = par_p9 + par_p10 * t_lin;
			partial_data11 = partial_data13 * partial_data12;
			double partial_data4 = partial_data11 + Math.Pow(uncomp_data, 3.0) * par_p11;
			return num + partial_out2 + partial_data4;
		}
	}
}
