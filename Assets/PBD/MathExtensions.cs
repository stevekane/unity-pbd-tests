using UnityEngine;
using Unity.Mathematics;

public static class MathExtensions
{
	public static float3 Multiply(this float3x3 m, float3 v)
	{
		var x = m.m0[0] * v.x + m.m0[1] * v.y + m.m0[2] * v.z;
		var y = m.m1[0] * v.x + m.m1[1] * v.y + m.m1[2] * v.z;
		var z = m.m2[0] * v.x + m.m2[1] * v.y + m.m2[2] * v.z;

		return new float3(x, y, z);
	}
	public static float3x3 Multiply(this float3x3 m1, float3x3 m2)
	{
		var m00 = m1.m0[0] * m2.m0[0] + m1.m0[1] * m2.m1[0] + m1.m0[2] * m2.m2[0];
		var m01 = m1.m0[0] * m2.m0[1] + m1.m0[1] * m2.m1[1] + m1.m0[2] * m2.m2[1];
		var m02 = m1.m0[0] * m2.m0[2] + m1.m0[1] * m2.m1[2] + m1.m0[2] * m2.m2[2];
		var m10 = m1.m1[0] * m2.m0[0] + m1.m1[1] * m2.m1[0] + m1.m1[2] * m2.m2[0];
		var m11 = m1.m1[0] * m2.m0[1] + m1.m1[1] * m2.m1[1] + m1.m1[2] * m2.m2[1];
		var m12 = m1.m1[0] * m2.m0[2] + m1.m1[1] * m2.m1[2] + m1.m1[2] * m2.m2[2];
		var m20 = m1.m2[0] * m2.m0[0] + m1.m2[1] * m2.m1[0] + m1.m2[2] * m2.m2[0];
		var m21 = m1.m2[0] * m2.m0[1] + m1.m2[1] * m2.m1[1] + m1.m2[2] * m2.m2[1];
		var m22 = m1.m2[0] * m2.m0[2] + m1.m2[1] * m2.m1[2] + m1.m2[2] * m2.m2[2];

		return new float3x3(
			m00, m01, m02,
			m10, m11, m12,
			m20, m21, m22
		);
	}
	public static float3x3 Inverse(this float3x3 m)
	{
		var minv00 = m.m1[1] * m.m2[2] - m.m1[2] * m.m2[1];
		var minv01 = m.m0[2] * m.m2[1] - m.m0[1] * m.m2[2];
		var minv02 = m.m0[1] * m.m1[2] - m.m0[2] * m.m1[1];
		var minv10 = m.m1[2] * m.m2[0] - m.m1[0] * m.m2[2];
		var minv11 = m.m0[0] * m.m2[2] - m.m0[2] * m.m2[0];
		var minv12 = m.m0[2] * m.m1[0] - m.m0[0] * m.m1[2];
		var minv20 = m.m1[0] * m.m2[1] - m.m1[1] * m.m2[0];
		var minv21 = m.m0[1] * m.m2[0] - m.m0[0] * m.m2[1];
		var minv22 = m.m0[0] * m.m1[1] - m.m0[1] * m.m1[0];
		var det = m.m0[0] * minv00 + m.m0[1] * minv10 + m.m0[2] * minv20;

		// TODO: Is there not an abs function from Unity.Mathematics?
		// If determinant is 0 then there is no inverse... fuck it
		if (Mathf.Abs(det) <= 1e-09)
			return new float3x3(1f, 1f, 1f);
		
		var invdet = 1f / det;

		minv00 *= invdet;
		minv01 *= invdet;
		minv02 *= invdet;
		minv10 *= invdet;
		minv11 *= invdet;
		minv12 *= invdet;
		minv20 *= invdet;
		minv21 *= invdet;
		minv22 *= invdet;

		return new float3x3(
			minv00, minv01, minv02,
			minv10, minv11, minv12,
			minv20, minv21, minv22
		);
	}

	public static float3x3 PolarDecomposition(this float3x3 m, float epsilon)
	{
		return m;
	}
}