using UnityEngine;

public class InputBlocker : MonoBehaviour
{
	private void OnEnable()
	{
		Singleton<InputManager>.Instance.currentInputBlocker = this;
	}

	private void OnDisable()
	{
		Singleton<InputManager>.Instance.currentInputBlocker = null;
	}

	private void OnDestroy()
	{
		if (Singleton<InputManager>.Instance != null)
		{
			Singleton<InputManager>.Instance.currentInputBlocker = null;
		}
	}
}
