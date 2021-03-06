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
