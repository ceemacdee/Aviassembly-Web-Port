using UnityEngine;

public class MenuBird : MonoBehaviour
{
	public Vector3 movementAmount;

	public Vector3 movementSpeeds;

	public float rotationAmount;

	public float rotationSpeed;

	public float forwardOffset;

	public float cameraMoveAmount;

	public float cameraMoveSpeed;

	public float cameraBaseHeight;

	public float moveSpeed;

	[Range(0f, 1f)]
	public float flight;

	private float time;

	private void Update()
	{
		if (flight < 0.001f)
		{
			time = 0f;
		}
		time += Time.deltaTime * Mathf.Pow(flight, 2f);
		base.transform.localPosition = Vector3.right * Mathf.Sin(time * movementSpeeds.x) * movementAmount.x;
		base.transform.localPosition += Vector3.forward * (Mathf.Sin(time * movementSpeeds.z) * movementAmount.z + forwardOffset);
		base.transform.localPosition += Vector3.up * (Mathf.Sin(time * movementSpeeds.y) - 1f) * movementAmount.y;
		base.transform.parent.transform.position += new Vector3(base.transform.forward.x, 0f, base.transform.forward.z).normalized * Time.deltaTime * moveSpeed;
		base.transform.parent.transform.position = new Vector3(base.transform.parent.transform.position.x, cameraBaseHeight + Mathf.Sin(time * cameraMoveSpeed) * cameraMoveAmount, base.transform.parent.transform.position.z);
		base.transform.localPosition = Vector3.Lerp(new Vector3(0f, -0.6f, 0.75f), base.transform.localPosition, flight);
	}
}
