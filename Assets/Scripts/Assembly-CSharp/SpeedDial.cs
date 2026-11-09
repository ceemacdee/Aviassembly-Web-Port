using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpeedDial : MonoBehaviour
{
	public float speed;

	public float minAngle;

	public float maxAngle;

	public float maxSpeed;

	public Image redMeter;

	public Transform pointer;

	public TMP_Text machMeter;

	public TMP_Text machMeterShadow;

	public TMP_Text knotsMeter;

	public TMP_Text knotsMeterShadow;

	private void Start()
	{
		float num = float.MaxValue;
		Wing[] componentsInChildren = Singleton<PlaneContainer>.Instance.GetComponentsInChildren<Wing>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			num = Mathf.Min(componentsInChildren[i].maxSpeed * 0.8f, num);
		}
		redMeter.fillAmount = Mathf.InverseLerp(0f, 300f, Mathf.Clamp(300f - num, 0f, 300f)) * 0.75f;
	}

	private void Update()
	{
		speed = Singleton<PlaneContainer>.Instance.GetVelocityMagintude();
		float z = Mathf.Lerp(minAngle, maxAngle, Mathf.InverseLerp(0f, maxSpeed, speed));
		pointer.eulerAngles = new Vector3(0f, 0f, z);
		machMeter.text = CustomMath.SnapToIncrement(speed / 666f, 0.1f).ToString();
		machMeterShadow.text = machMeter.text;
		knotsMeter.text = Mathf.RoundToInt(speed).ToString();
		knotsMeterShadow.text = knotsMeter.text;
	}
}
