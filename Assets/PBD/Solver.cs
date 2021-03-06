﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Mathematics.Experimental;

[System.Serializable]
public struct PositionConstraint
{
	public Atom a;
	public Vector3 position;
}

[System.Serializable]
public struct DistanceConstraint
{
	public Atom a;
	public Atom b;
	public float distance;
	public float stiffness;

	public void Project(float invIterations)
	{
		var delta = a.predicted - b.predicted;
		var direction = math.normalize(delta);
		var C = math.length(delta) - distance;
		var k = 1f - Mathf.Pow(1f - stiffness, invIterations);

		if (C > -float.Epsilon && C < float.Epsilon)
			return;
		
		a.predicted += (-a.inverseMass / (a.inverseMass + b.inverseMass)) * k * C * direction;
		b.predicted += (b.inverseMass / (a.inverseMass + b.inverseMass)) * k * C * direction;
	}
}

[System.Serializable]
public struct ShapeConstraint
{
	public Atom[] atoms;
	public float stiffness;
	float3x3 inverseMassMatrix;
	float3 restCenterOfMass;
	float3[] restPositions;

	public ShapeConstraint(List<Atom> Atoms, float Stiffness)
	{
		int numAtoms = Atoms.Count;
		float a00 = 0f;
		float a01 = 0f;
		float a02 = 0f; 
		float a10 = 0f;
		float a11 = 0f;
		float a12 = 0f;
		float a20 = 0f;
		float a21 = 0f;
		float a22 = 0f;
		float totalMass = 0f;

		restCenterOfMass = Vector3.zero;
		atoms = Atoms.ToArray();
		stiffness = Stiffness;
		restPositions = new float3[atoms.Length];

		// compute center of mass of resting pose
		foreach(var a in atoms)
		{
			restCenterOfMass += new float3(a.transform.position) * a.inverseMass;
			totalMass += a.inverseMass;
		}
		restCenterOfMass /= totalMass;

		// compute rest matrix
		for (var i = 0; i < numAtoms; i++)
		{
			float3 q = new float3(atoms[i].transform.position) - restCenterOfMass;

			a00 += atoms[i].inverseMass * q.x * q.x;
			a01 += atoms[i].inverseMass * q.x * q.y;
			a02 += atoms[i].inverseMass * q.x * q.z;

			a10 += atoms[i].inverseMass * q.y * q.x;
			a11 += atoms[i].inverseMass * q.y * q.y;
			a12 += atoms[i].inverseMass * q.y * q.z;

			a20 += atoms[i].inverseMass * q.z * q.x;
			a21 += atoms[i].inverseMass * q.z * q.y;
			a22 += atoms[i].inverseMass * q.z * q.z;
			restPositions[i] = q;
		}

		float3x3 restMatrix = new float3x3(
			a00, a01, a02,
			a10, a11, a12,
			a20, a21, a22
		);

		inverseMassMatrix = math.inverse(restMatrix);
	}

	public void Project(float invIterations)
	{
		const float epsilon = 1e-6f;

		float k = 1f - Mathf.Pow(1f - stiffness, invIterations);
		float totalMass = 0f;
		float3 centerOfMass = Vector3.zero;

		// compute current center of mass
		foreach(var a in atoms)
		{
			centerOfMass += a.predicted * a.inverseMass;
			totalMass += a.inverseMass;
		}
		centerOfMass /= totalMass;

		// compute rest matrix
		float a00 = 0f;
		float a01 = 0f;
		float a02 = 0f; 
		float a10 = 0f;
		float a11 = 0f;
		float a12 = 0f;
		float a20 = 0f;
		float a21 = 0f;
		float a22 = 0f;

		for (var i = 0; i < atoms.Length; i++)
		{
			float3 q = restPositions[i];
			float3 p = atoms[i].predicted - centerOfMass;

			a00 += atoms[i].inverseMass * p.x * q.x;
			a01 += atoms[i].inverseMass * p.x * q.y;
			a02 += atoms[i].inverseMass * p.x * q.z;

			a10 += atoms[i].inverseMass * p.y * q.x;
			a11 += atoms[i].inverseMass * p.y * q.y;
			a12 += atoms[i].inverseMass * p.y * q.z;

			a20 += atoms[i].inverseMass * p.z * q.x;
			a21 += atoms[i].inverseMass * p.z * q.y;
			a22 += atoms[i].inverseMass * p.z * q.z;
		}

		float3x3 currentMatrix = new float3x3(
			a00, a01, a02,
			a10, a11, a12,
			a20, a21, a22
		);
		float3x3 covarianceMatrix = math.mul(currentMatrix, inverseMassMatrix);
		float3x3 rotationMatrix = UnityMathPolarDecomp.PolarDecomposition(covarianceMatrix, epsilon);

		for (var i = 0; i < atoms.Length; i++)
		{
			float3 goal = centerOfMass + math.mul(rotationMatrix, restPositions[i]);		

			atoms[i].predicted += (goal - atoms[i].predicted) * stiffness * k;
		}
	}
}

public class Solver : MonoBehaviour 
{
	public bool SOLVE_PARTICLE_PARTICLE_COLLISIONS = true;
	public float3 GRAVITY = new float3(0, -100, 0);
	public float DAMPING = .96f;
	public int ITERATIONS = 10;
	public List<Atom> atoms = new List<Atom>();
	public List<PositionConstraint> positionConstraints = new List<PositionConstraint>();
	public List<DistanceConstraint> distanceConstraints = new List<DistanceConstraint>();
	public List<ShapeConstraint> shapeConstraints = new List<ShapeConstraint>();

	void FixedUpdate()
	{
		var dt = Time.fixedDeltaTime;
		var partialGravity = GRAVITY * dt;
		var invdt = 1f / dt;
		var invIterations = 1f / (float)ITERATIONS;

		foreach (var a in atoms)
		{
			if (a.inverseMass == 0)
				continue;

			a.position = a.transform.position;
			// apply gravity
			a.velocity += partialGravity;
			// apply damping to velocity
			a.velocity *= DAMPING;
			// predict next position
			a.predicted = a.position + a.velocity * Time.fixedDeltaTime;
		}

		// solver iterations
		for (var i = 0; i < ITERATIONS; i++)
		{
			// project distance constraints
			foreach(var dc in distanceConstraints)
			{
				dc.Project(invIterations);
			}

			// project shape constraints
			foreach(var sc in shapeConstraints)
			{
				sc.Project(invIterations);
			}

			// project collisions
			foreach(var a in atoms)
			{
				// ground plane collisions
				// a.predicted.y = Mathf.Max(Mathf.Cos(a.position.x / 4f) * 4f, a.predicted.y);
				a.predicted.y = Mathf.Max(0f, a.predicted.y);

				if (!SOLVE_PARTICLE_PARTICLE_COLLISIONS)
					continue;

				// particle particle collisions
				foreach(var b in atoms)
				{
					if (a == b)
						continue;
					
					var delta = a.predicted - b.predicted;
					var direction = math.normalize(delta);
					var C = math.length(delta) - (a.diameter / 2f + b.diameter / 2f);
					var k = 1f;

					if (C >= 0)
						continue;
					
					a.predicted += (-a.inverseMass / (a.inverseMass + b.inverseMass)) * k * C * direction;
					b.predicted += (b.inverseMass / (a.inverseMass + b.inverseMass)) * k * C * direction;
				}
			}
			// apply position constraints
			foreach(var pc in positionConstraints)
			{
				pc.a.predicted = pc.position;
			}
		}

		foreach(var a in atoms)
		{
			a.velocity = (a.predicted - a.position) * invdt;
			a.position = a.predicted;
			a.transform.position = a.position;
		}
	}
}