using TMPro;
using UnityEngine;

public class WarningSystem : MonoBehaviour
{
	[Header("UI")]
	public TMP_Text text;

	public TMP_Text header;

	public TMP_Text headerShadow;

	public CanvasGroup warningCanvas;

	private float targetAlpha;

	private float alphaVelocity;

	private void Start()
	{
		targetAlpha = 0f;
		warningCanvas.alpha = 0f;
	}

	public void Back()
	{
		targetAlpha = 0f;
	}

	public void StartFlightMode()
	{
		if (SetWarning())
		{
			targetAlpha = 1f;
		}
		else
		{
			Singleton<GameManager>.Instance.StartFlyMode();
		}
	}

	public void ForceFlightMode()
	{
		Singleton<GameManager>.Instance.StartFlyMode();
	}

	private bool SetWarning()
	{
		header.text = "Warning";
		if (NoWheels())
		{
			text.text = "The plane has no wheels.";
			return true;
		}
		if (WheelCG())
		{
			text.text = "Front wheels too close to center of gravity";
			header.text = "Unstable plane";
			return true;
		}
		if (AngleOfAttack())
		{
			text.text = "Front wheel lower than rear wheel";
			header.text = "Angle of attack issue";
			return true;
		}
		return false;
	}

	private void Update()
	{
		warningCanvas.alpha = Mathf.SmoothDamp(warningCanvas.alpha, targetAlpha, ref alphaVelocity, 0.05f);
		if (targetAlpha < 0.5f && warningCanvas.alpha < 0.02f)
		{
			warningCanvas.gameObject.SetActive(value: false);
		}
		if ((double)targetAlpha > 0.5)
		{
			warningCanvas.gameObject.SetActive(value: true);
		}
		headerShadow.text = header.text;
	}

	private bool NoWheels()
	{
		return !Singleton<PlaneContainer>.Instance.gameObject.GetComponentInChildren<Wheel>();
	}

	private bool WheelCG()
	{
		Wheel[] componentsInChildren = Singleton<PlaneContainer>.Instance.gameObject.GetComponentsInChildren<Wheel>();
		Vector3 vector = Singleton<PlaneContainer>.Instance.CalculateCenterOfMass(worldPosition: true);
		float num = float.MinValue;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			float num2 = Vector3.Dot(Singleton<PlaneContainer>.Instance.UpdateForwardDirection(), componentsInChildren[i].transform.position - vector);
			if (num2 > num)
			{
				num = num2;
			}
		}
		if (num < 0.5f)
		{
			return true;
		}
		return false;
	}

	private bool AngleOfAttack()
	{
		Wheel[] componentsInChildren = Singleton<PlaneContainer>.Instance.gameObject.GetComponentsInChildren<Wheel>();
		Vector3 vector = Singleton<PlaneContainer>.Instance.CalculateCenterOfMass(worldPosition: true);
		float num = -1f;
		float num2 = -1f;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			bool num3 = Vector3.Dot(Singleton<PlaneContainer>.Instance.UpdateForwardDirection(), componentsInChildren[i].transform.position - vector) < 0f;
			float num4 = 0f - Vector3.Dot(Singleton<PlaneContainer>.Instance.transform.up, componentsInChildren[i].transform.position - vector);
			if (num3 && num4 > num2)
			{
				num2 = num4;
			}
			if (!num3 && num4 > num)
			{
				num = num4;
			}
		}
		if (num2 > 0f && num > 0f && num2 > num && Mathf.Abs(num2 - num) > 0.15f)
		{
			return true;
		}
		return false;
	}
}
