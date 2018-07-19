using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkMaker : MonoBehaviour 
{
	public Mesh mesh;
	public Solver solver;
	public Atom AtomPrefab;
	public float DIAMETER = 1f;
	public float STIFFNESS = 1f;

	[ContextMenu("Spawn")]
	void Spawn()
	{
		if (mesh == null)
			return;

		if (solver == null)
			return;

		if (AtomPrefab == null)
			return;
		
		var vertices = mesh.vertices;
		var triangles = mesh.triangles;
		var positionToAtomMap = new Dictionary<Vector3, Atom>();

		foreach(var a in solver.atoms)
		{
			Destroy(a.gameObject);
		}
		solver.atoms.Clear();
		solver.positionConstraints.Clear();
		solver.distanceConstraints.Clear();

		// create all atoms that occupy unique positions
		for (var i = 0; i < vertices.Length; i++)
		{
			var modelSpacePosition = vertices[i];

			if (positionToAtomMap.ContainsKey(modelSpacePosition))
				continue;

			var worldSpacePosition = transform.TransformPoint(modelSpacePosition);
			var atom = Instantiate(AtomPrefab, transform);

			atom.transform.position = worldSpacePosition;
			atom.velocity = Vector3.zero;
			atom.predicted = Vector3.zero;
			atom.name = i.ToString();
			atom.diameter = DIAMETER;
			solver.atoms.Add(atom);
			positionToAtomMap.Add(modelSpacePosition, atom);
		}

		/* 
		For use in old spring-network model

		for (var i = 0; i < triangles.Length; i += 3)
		{
			var i1 = triangles[i + 0];
			var i2 = triangles[i + 1];
			var i3 = triangles[i + 2];
			var p1 = vertices[i1];
			var p2 = vertices[i2];
			var p3 = vertices[i3];
			var a1 = positionToAtomMap[p1];
			var a2 = positionToAtomMap[p2];
			var a3 = positionToAtomMap[p3];
			var d1 = Vector3.Distance(a1.transform.position, a2.transform.position);
			var d2 = Vector3.Distance(a1.transform.position, a3.transform.position);
			var d3 = Vector3.Distance(a2.transform.position, a3.transform.position);

			solver.distanceConstraints.Add(new DistanceConstraint() { a = a1, b = a2, distance = d1, stiffness = STIFFNESS });
			solver.distanceConstraints.Add(new DistanceConstraint() { a = a1, b = a3, distance = d2, stiffness = STIFFNESS });
			solver.distanceConstraints.Add(new DistanceConstraint() { a = a2, b = a3, distance = d3, stiffness = STIFFNESS });
		}
		*/

		var sc = new ShapeConstraint(solver.atoms, STIFFNESS);

		solver.shapeConstraints.Add(sc);
	}
}