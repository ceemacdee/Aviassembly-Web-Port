using UnityEngine;

public class FeedbackMenu : MonoBehaviour
{
	private void Start()
	{
		if (!GameManager.firstMenuOpen)
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
