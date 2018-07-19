using static Unity.Mathematics.math;
using System.Collections;
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
		var direction = normalize(delta);
		var C = length(delta) - distance;
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
	public float3x3 invMassMatrix;
	public float3 restCenterOfMass;
	public float3[] restPositions;

	public ShapeConstraint(List<Atom> Atoms, float Stiffness)
	{
		float a00 = 0f;
		float a01 = 0f;
		float a02 = 0f; 
		float a10 = 0f;
		float a11 = 0f;
		float a12 = 0f;
		float a20 = 0f;
		float a21 = 0f;
		float a22 = 0f;
		float mass = 1f; // TODO: wtf... should this be from atoms?
		float totalMass = 0f;

		restCenterOfMass = Vector3.zero;
		atoms = Atoms.ToArray();
		stiffness = Stiffness;

		// copy rest positions
		restPositions = new float3[atoms.Length];
		for (var i = 0; i < atoms.Length; i++)
		{
			restPositions[i] = atoms[i].position;
		}

		// compute center of mass of resting pose
		foreach(var a in atoms)
		{
			restCenterOfMass += a.position * mass;
			totalMass += mass;
		}
		restCenterOfMass /= totalMass;

		// compute rest matrix
		foreach (var p in restPositions)
		{
			float3 q = new float3(
				p.x - restCenterOfMass.x, 
				p.y - restCenterOfMass.y,
				p.z - restCenterOfMass.z);

			a00 += mass * q.x * q.x;
			a01 += mass * q.x * q.y;
			a02 += mass * q.x * q.z;
			a10 += mass * q.y * q.x;
			a11 += mass * q.y * q.y;
			a12 += mass * q.z * q.y;
			a20 += mass * q.z * q.x;
			a21 += mass * q.z * q.y;
			a22 += mass * q.z * q.z;
		}

		float3x3 restMatrix = new float3x3(
			a00, a01, a02,
			a10, a11, a12,
			a20, a21, a22
		);

		invMassMatrix = restMatrix.Inverse();
	}

	public void Project(float invIterations)
	{
		float totalMass = 0f;
		float mass = 1f; // TODO: should be particle mass...
		float3 centerOfMass = Vector3.zero;

		// compute current center of mass
		foreach(var a in atoms)
		{
			centerOfMass += a.predicted * mass;
			totalMass += mass;
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

			a00 += mass * p.x * q.x;
			a01 += mass * p.x * q.y;
			a02 += mass * p.x * q.z;
			a10 += mass * p.y * q.x;
			a11 += mass * p.y * q.y;
			a12 += mass * p.z * q.y;
			a20 += mass * p.z * q.x;
			a21 += mass * p.z * q.y;
			a22 += mass * p.z * q.z;
		}

		float epsilon = 1e-6f;
		float3x3 currentMatrix = new float3x3(
			a00, a01, a02,
			a10, a11, a12,
			a20, a21, a22
		);
		float3x3 covarianceMatrix = currentMatrix.Multiply(invMassMatrix);
		float3x3 rotationMatrix = covarianceMatrix.PolarDecomposition(epsilon);

		for (var i = 0; i < atoms.Length; i++)
		{
			float3 goal = centerOfMass + rotationMatrix.Multiply(restPositions[i]);		

			// Debug.Log(goal.x + " " + goal.y + " " + goal.z);
			// atoms[i].predicted += (goal - atoms[i].predicted) * stiffness;
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
			a.position = a.transform.position;
			// apply gravity
			a.velocity = a.velocity + partialGravity;
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
				a.predicted.y = Mathf.Max(Mathf.Cos(a.position.x / 4f) * 4f, a.predicted.y);
				// a.predicted.y = Mathf.Max(0f, a.predicted.y);

				if (!SOLVE_PARTICLE_PARTICLE_COLLISIONS)
					continue;

				// particle particle collisions
				foreach(var b in atoms)
				{
					if (a == b)
						continue;
					
					var delta = a.predicted - b.predicted;
					var direction = normalize(delta);
					var C = length(delta) - (a.diameter / 2f + b.diameter / 2f);
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

		// DRAWDEBUG SHIT
		foreach(var sc in shapeConstraints)
		{
			Debug.DrawLine(Vector3.zero, sc.restCenterOfMass, Color.green);
		}
	}
}