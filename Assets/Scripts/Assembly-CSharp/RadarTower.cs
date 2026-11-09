using UnityEngine;

public class RadarTower : MonoBehaviour
{
	public Transform radar;

	private float angle;

	private void Update()
	{
		angle += Time.deltaTime * 100f;
		angle %= 360f;
		radar.transform.localEulerAngles = new Vector3(270f, angle, 0f);
	}
}
