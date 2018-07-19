using Unity.Mathematics;
using UnityEngine.Assertions;

public class UnityMathPolarDecomp 
{
	private static float InfNorm(float3x3 M)
	{
		float sum = math.abs(M.c0.x) + math.abs(M.c0.y) + math.abs(M.c0.z);
		float sum2 = math.abs(M.c1.x) + math.abs(M.c1.y) + math.abs(M.c1.z);
		float sum3 = math.abs(M.c2.x) + math.abs(M.c2.y) + math.abs(M.c2.z);
		float max = sum > sum2 ? sum : sum2;

		return max > sum3 ? max : sum3;
	}

	private static float OneNorm(float3x3 M)
	{
		float sum = math.abs(M.c0.x) + math.abs(M.c1.x) + math.abs(M.c2.x);
		float sum2 = math.abs(M.c0.y) + math.abs(M.c1.y) + math.abs(M.c2.y);
		float sum3 = math.abs(M.c0.z) + math.abs(M.c1.z) + math.abs(M.c2.z);
		float max = sum > sum2 ? sum : sum2;

		return max > sum3 ? max : sum3;
	}

	private static void SetRow(int row_num, ref float3x3 matrix, float3 vector)
	{
		if(row_num == 0)
			matrix.c0 = vector;
		else if(row_num == 1)
			matrix.c1 = vector;
		else if(row_num == 2)
			matrix.c2 = vector;
		else
			Assert.IsTrue(false, "Invalid Matrix Row Index : UnityMathPolarDecomp.SetRow()\n");
	}
	private static float3 GetRow(int row_num, float3x3 matrix)
	{
		switch(row_num)
		{
			case 0:
				return matrix.c0;
			case 1:
				return matrix.c1;
			case 2:
				return matrix.c2;
			default:	
				Assert.IsTrue(false, "Invalid Matrix Row Index : UnityMathPolarDecomp.SetRow()\n");
				return new float3();
		}
	}

	private static void SetValue(int row_num, int column_num, ref float3x3 matrix, float value)
	{
		float3 row = new float3();
		if(row_num == 0)
			row = matrix.c0;
		else if(row_num == 1)
			row = matrix.c1;
		else if(row_num == 2)
			row = matrix.c2;
		else
		{
			Assert.IsTrue(false, "Invalid Matrix Row Index : UnityMathPolarDecomp.SetValue()\n");
		}

		if(column_num == 0)
			row.x = value;
		else if(column_num == 1)
			row.y = value;
		else if(column_num == 2)
			row.z = value;
		else
		{
			Assert.IsTrue(false, "Invalid Matrix Row Index : UnityMathPolarDecomp.SetValue()\n");
		}

		SetRow(row_num, ref matrix, row);
	}
	private static float GetValue(int row_num, int column_num, float3x3 matrix)
	{
		float3 row;

		if(row_num == 0)
			row = matrix.c0;
		else if(row_num == 1)
			row = matrix.c1;
		else if(row_num == 2)
			row = matrix.c2;
		else
		{
			Assert.IsTrue(false, "Invalid Matrix Row Index : UnityMathPolarDecomp.GetValue()\n");
			return 0.0f;
		}

		switch (column_num)
		{
			case 0: 
				return row.x;
			case 1: 
				return row.y;
			case 2: 
				return row.z;
			default:
				Assert.IsTrue(false, "Invalid Matrix Column Index : UnityMathPolarDecomp.GetValue()\n");
				return 0.0f;
		}
	}

	public static string Convert3x3ToString(float3x3 M)
	{
		return string.Format("{0}f, {1}f, {2}f\n{3}f, {4}f, {5}f\n{6}f, {7}f, {8}f", M.c0.x, M.c0.y, M.c0.z, M.c1.x, M.c1.y, M.c1.z, M.c2.x, M.c2.y, M.c2.z);
	}

	public static float3x3 PolarDecomposition(float3x3 M, float tolerance)
	{
		const float epsilon = 1.0e-15f;

		float3x3 mTranspose = math.transpose(M);
		float mOne = OneNorm(M);
		float mInf = InfNorm(M);
		float3x3 MadjTt = new float3x3();
		float3x3 Et = new float3x3();
		float Eone;

		do
		{
			MadjTt.c0 = math.cross(mTranspose.c1, mTranspose.c2);
			MadjTt.c1 = math.cross(mTranspose.c2, mTranspose.c0);
			MadjTt.c2 = math.cross(mTranspose.c0, mTranspose.c1);

			float det = math.dot(MadjTt.c0, mTranspose.c0);

			if(math.abs(det) < epsilon)
			{
				int index = int.MaxValue;
				if(math.dot(MadjTt.c0, MadjTt.c0) > epsilon)
					index = 0;
				else if(math.dot(MadjTt.c1, MadjTt.c1) > epsilon)
					index = 1;
				else if(math.dot(MadjTt.c2, MadjTt.c2) > epsilon)
					index = 2;

				if (index == int.MaxValue)
				{
					return float3x3.identity;
				}
				else
				{
					SetRow(index, ref mTranspose, math.cross(
						GetRow((index + 1) % 3, mTranspose), 
						GetRow((index + 2) % 3, mTranspose)
					));
					
					SetRow((index + 1) % 3, ref MadjTt, math.cross(
						GetRow((index + 2) % 3, mTranspose),
						GetRow(index, mTranspose)
					));

					SetRow((index + 2) % 3, ref MadjTt, math.cross(
						GetRow(index, mTranspose),
						GetRow((index + 1) % 3, mTranspose)
					));

					float3x3 m2 = math.transpose(mTranspose);

					mOne = OneNorm(m2);
					mInf = InfNorm(m2);
					det = math.dot(mTranspose.c0, MadjTt.c0);
				}
			}

			float MadjTone = OneNorm(MadjTt);
			float MadjTinf = InfNorm(MadjTt);
			float gamma = math.sqrt(math.sqrt((MadjTone * MadjTinf) / (mOne * mInf)) / math.abs(det));
			float g1 = gamma * 0.5f;
			float g2 = 0.5f / (gamma * det);

			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					SetValue(i, j, ref Et, GetValue(i, j, mTranspose));
					SetValue(i, j, ref mTranspose, g1 * GetValue(i, j, mTranspose) + g2 * GetValue(i, j, MadjTt));
					SetValue(i, j, ref Et, GetValue(i, j, Et) - GetValue(i, j, mTranspose));
				}
			}

			Eone = OneNorm(Et);
			mOne = OneNorm(mTranspose);
			mInf = InfNorm(mTranspose);

		} while(Eone > mOne * tolerance);

		return math.transpose(mTranspose);
	}
}