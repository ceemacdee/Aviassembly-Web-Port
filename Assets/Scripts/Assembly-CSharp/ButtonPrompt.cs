using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonPrompt : MonoBehaviour
{
	private Image background;

	public KeyUI key1;

	public KeyUI key2;

	public TMP_Text discription;

	public Image shadow;

	private InputManager inputManager;

	private void Start()
	{
		inputManager = Singleton<InputManager>.Instance;
		background = GetComponent<Image>();
	}

	private void Update()
	{
		float num = (key1.gameObject.activeInHierarchy ? ((RectTransform)key1.transform).sizeDelta.x : 0f);
		float num2 = (key2.gameObject.activeInHierarchy ? ((RectTransform)key2.transform).sizeDelta.x : 0f);
		float x = num + num2 + discription.preferredWidth + 60f;
		((RectTransform)base.transform).sizeDelta = new Vector2(x, 67.8539f);
		ButtonPromptData buttonPromptData = Singleton<CurrentGameData>.Instance.buttonPromptData;
		base.transform.localScale = Vector3.one * (Mathf.Sin(Time.time * 10f) * 0.025f + 1f);
		background.enabled = num + num2 > 50f;
		shadow.enabled = background.enabled;
		Vector3 position = Singleton<PlaneContainer>.Instance.transform.position;
		Airport closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(position);
		float velocityMagintude = Singleton<PlaneContainer>.Instance.GetVelocityMagintude();
		float num3 = Vector3.Dot(Singleton<PlaneContainer>.Instance.Forward, Singleton<PlaneContainer>.Instance.GetTotalThrust());
		buttonPromptData.maxHeight = Mathf.Max(position.y, buttonPromptData.maxHeight);
		buttonPromptData.maxVelocity = Mathf.Max(velocityMagintude, buttonPromptData.maxVelocity);
		if (num3 < 0f)
		{
			buttonPromptData.reverseAmount += (0f - num3) * Time.deltaTime;
		}
		if (closestAirport.position.magnitude > 200f && Singleton<PlaneContainer>.Instance.IsAtAirport())
		{
			buttonPromptData.landed = true;
		}
		buttonPromptData.rollAmount += Mathf.Abs(inputManager.rollInput) * Time.deltaTime;
		buttonPromptData.yawAmount += Mathf.Abs(inputManager.yawInput) * Time.deltaTime;
		buttonPromptData.groundSteerAmount += (float)((Mathf.Abs(inputManager.yawInput) > 0.1f && Singleton<PlaneContainer>.Instance.IsGrounded()) ? 1 : 0) * Time.deltaTime;
		if (Singleton<PlaneContainer>.Instance.hasRetractableGear && Singleton<InputManager>.Instance.wheelRetraction)
		{
			buttonPromptData.retractedGear = true;
		}
		SetContent("", "");
		if (Singleton<GameManager>.Instance.currentSaveFile != "")
		{
			return;
		}
		if (Singleton<PlaneContainer>.Instance.hasRetractableGear && !buttonPromptData.retractedGear)
		{
			SetContent("Landing gear", inputManager.GetKeyPath(Keys.RetractGear, inputManager.CurrentControlScheme));
		}
		if (buttonPromptData.yawAmount < 1.5f && !Singleton<PlaneContainer>.Instance.IsGrounded())
		{
			SetContent("Yaw", inputManager.GetKeyPath(Keys.YawLeft, inputManager.CurrentControlScheme), inputManager.GetKeyPath(Keys.YawRight, inputManager.CurrentControlScheme));
		}
		if (buttonPromptData.rollAmount < 1.5f && !Singleton<PlaneContainer>.Instance.IsGrounded())
		{
			SetContent("Roll", inputManager.GetKeyPath(Keys.RollLeft, inputManager.CurrentControlScheme), inputManager.GetKeyPath(Keys.RollRight, inputManager.CurrentControlScheme));
		}
		if (!buttonPromptData.landed)
		{
			if (closestAirport.position.magnitude > 200f && Vector3.Distance(position, closestAirport.position) < 1500f && position.y > 250f)
			{
				SetContent("Go down", inputManager.GetKeyPath(Keys.PitchDown, inputManager.CurrentControlScheme));
			}
			if (closestAirport.position.magnitude > 200f && Vector3.Distance(position, closestAirport.position) < 1500f && velocityMagintude > 70f && num3 > 0f)
			{
				SetContent("Throttle down", inputManager.GetKeyPath(Keys.Break, inputManager.CurrentControlScheme));
			}
		}
		if (Vector3.Distance(Singleton<AirportManager>.Instance.LastAirport, closestAirport.position) > 100f && buttonPromptData.reverseAmount < 5f && closestAirport.position.magnitude > 200f && closestAirport.flatnessObject.GetDistanceToRectangle(position) < 100f && velocityMagintude > 25f && Singleton<PlaneContainer>.Instance.IsGrounded())
		{
			SetContent("Reverse thrust", inputManager.GetKeyPath(Keys.Break, inputManager.CurrentControlScheme));
		}
		if (buttonPromptData.maxHeight < 50f)
		{
			SetContent("Go up", inputManager.GetKeyPath(Keys.PitchUp, inputManager.CurrentControlScheme));
		}
		if (buttonPromptData.maxVelocity < 40f && position.magnitude < 1000f)
		{
			SetContent("Throttle up", inputManager.GetKeyPath(Keys.Forward, inputManager.CurrentControlScheme));
		}
	}

	private void SetContent(string discription, string key1, string key2 = "")
	{
		this.key1.SetKey(key1);
		this.key2.SetKey(key2);
		this.discription.text = discription;
	}
}
