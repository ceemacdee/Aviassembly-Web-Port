using UnityEngine;

public class InverseKinematics : MonoBehaviour
{
	public Transform point1;

	public Transform point2;

	[Space(10f)]
	public Transform arm1;

	public Transform arm2;

	[Space(10f)]
	public float armLengths;

	public bool invert;

	private void Update()
	{
		Vector3 vector = Vector3.Lerp(point1.position, point2.position, 0.5f);
		Vector3 vector2 = Vector3.Cross((point1.position - point2.position).normalized, base.transform.right);
		float num = armLengths * base.transform.lossyScale.x;
		float num2 = Vector3.Distance(vector, point1.position);
		float num3 = Mathf.Sqrt(Mathf.Max(num * num - num2 * num2, 0f));
		Vector3 vector3 = vector + vector2 * (num3 * (float)((!invert) ? 1 : (-1)) * Mathf.Sign(base.transform.lossyScale.y));
		arm1.position = point1.position;
		arm2.position = point2.position;
		arm1.rotation = Quaternion.LookRotation((vector3 - point1.position).normalized, base.transform.up);
		arm2.rotation = Quaternion.LookRotation((vector3 - point2.position).normalized, base.transform.up);
	}
}
