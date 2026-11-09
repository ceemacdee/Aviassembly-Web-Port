using System.Collections.Generic;
using UnityEngine;

public class RotorCollider : MonoBehaviour
{
	public float rotorLength;

	public int rotorCount;

	public float rotationOffset;

	public float rotorRotation;

	public bool helicopter;

	public Vector3 rotorPosition;

	public LayerMask layerMask;

	public GameObject[] rotors;

	private bool[] propellerBroken;

	public Mesh normalMesh;

	public Mesh brokenMesh;

	private List<Collider> colliders;

	private Engine engine;

	private float defaultTrust;

	private RaycastHit[] hitBuffer;

	private void Start()
	{
		engine = GetComponent<Engine>();
		colliders = new List<Collider>(GetComponentsInChildren<Collider>());
		hitBuffer = new RaycastHit[16];
		defaultTrust = engine.thrust;
		propellerBroken = new bool[rotors.Length];
	}

	public void Reset()
	{
		Start();
		for (int i = 0; i < rotors.Length; i++)
		{
			rotors[i].SetActive(value: true);
			if (normalMesh != null)
			{
				rotors[i].GetComponent<MeshFilter>().mesh = normalMesh;
			}
			propellerBroken[i] = false;
		}
		engine.thrustHandicap = 0f;
	}

	private void FixedUpdate()
	{
		if (GameManager.gameMode == GameMode.Building || Singleton<GameManager>.Instance.Loading)
		{
			return;
		}
		float num = 360f / (float)rotorCount;
		Vector3 vector = base.transform.TransformPoint(rotorPosition);
		Vector3 axis = (helicopter ? Vector3.up : Vector3.right);
		Vector3 vector2 = (helicopter ? Vector3.forward : Vector3.up);
		for (int i = 0; i < rotorCount; i++)
		{
			Quaternion quaternion = Quaternion.AngleAxis(num * (float)i + rotationOffset + rotorRotation * Mathf.Sign(base.transform.localScale.y), axis);
			Vector3 vector3 = base.transform.TransformVector(quaternion * vector2);
			if (Linecast(vector, vector + vector3 * rotorLength) || Linecast(vector + vector3 * rotorLength, vector))
			{
				if (!propellerBroken[i])
				{
					Singleton<PartExploder>.Instance.SpawnEffects(rotors[i].transform.position, root: true);
					engine.thrustHandicap += 1f / (float)rotorCount * 0.35f;
				}
				if (brokenMesh != null)
				{
					rotors[i].GetComponent<MeshFilter>().mesh = brokenMesh;
				}
				else
				{
					rotors[i].SetActive(value: false);
				}
				propellerBroken[i] = true;
			}
		}
	}

	public bool Linecast(Vector3 start, Vector3 end)
	{
		Vector3 direction = end - start;
		float magnitude = direction.magnitude;
		direction.Normalize();
		int b = Physics.RaycastNonAlloc(start, direction, hitBuffer, magnitude, layerMask);
		b = Mathf.Min(hitBuffer.Length, b);
		for (int i = 0; i < b; i++)
		{
			if (!colliders.Contains(hitBuffer[i].collider))
			{
				return true;
			}
		}
		return false;
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		float num = 360f / (float)rotorCount;
		Vector3 vector = base.transform.TransformPoint(rotorPosition);
		Vector3 axis = (helicopter ? Vector3.up : Vector3.right);
		Vector3 vector2 = (helicopter ? Vector3.forward : Vector3.up);
		for (int i = 0; i < rotorCount; i++)
		{
			if (i == 0)
			{
				Gizmos.color = Color.blue;
			}
			else
			{
				Gizmos.color = Color.red;
			}
			if (i == 1)
			{
				Gizmos.color = Color.green;
			}
			Quaternion quaternion = Quaternion.AngleAxis(num * (float)i + rotationOffset + rotorRotation * Mathf.Sign(base.transform.localScale.y), axis);
			Vector3 vector3 = base.transform.TransformVector(quaternion * vector2);
			Gizmos.DrawLine(vector, vector + vector3 * rotorLength);
		}
	}
}
