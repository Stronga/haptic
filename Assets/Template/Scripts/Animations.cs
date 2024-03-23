using System;
public class Easing
{
	static public double ElasticIn(double value)
	{
		return Math.Sin(6.5d * Math.PI * value) * Math.Pow(2, 10 * (value - 1));
	}
	static public double ElasticOut(double value)
	{
		return Math.Sin(-6.5d * Math.PI * (value + 1)) * Math.Pow(2, -10 * value) + 1;
	}
	static public double ElasticInOut(double value)
	{
		if (value < 0.5f) return 0.5f * Math.Sin(6.5f * Math.PI * (2 * value)) * Math.Pow(2, 10 * ((2 * value) - 1));
		return 0.5f * (Math.Sin(-6.5f * Math.PI * ((2 * value - 1) + 1)) * Math.Pow(2, -10 * (2 * value - 1)) + 2);
	}
}