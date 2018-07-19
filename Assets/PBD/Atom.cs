using UnityEngine;
using Unity.Mathematics;

public class Atom : MonoBehaviour 
{
	public float3 predicted = Vector3.zero;
	public float3 position = Vector3.zero;
	public float3 velocity = Vector3.zero;
	public float inverseMass = 1;
	public float diameter = 1f;

	void Start()
	{
		position = transform.position;
		predicted = transform.position;
	}

	void Update()
	{
		transform.localScale = new Vector3(diameter, diameter, diameter);
	}
}