using UnityEngine;

public class NewQuestAddedButton : MonoBehaviour
{
	public GameObject newQuestAdded;

	private ButtonHighlight buttonHighlight;

	private void Start()
	{
		buttonHighlight = GetComponent<ButtonHighlight>();
	}

	private void Update()
	{
		buttonHighlight.isHighlighted = newQuestAdded.activeInHierarchy;
	}
}
