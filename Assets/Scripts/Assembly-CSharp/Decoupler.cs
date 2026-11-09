using UnityEngine;
using UnityEngine.InputSystem;

public class Decoupler : PlanePart
{
	public InputActionReference sourceInputAction;

	[HideInInspector]
	public InputAction inputOverride;

	private BuildingPart part;

	private void Awake()
	{
		part = GetComponent<BuildingPart>();
		inputOverride = InputActionCopy.FromAsset(sourceInputAction.action);
		inputOverride.Enable();
	}

	private void OnDestroy()
	{
		inputOverride.Dispose();
	}

	public override void UpdatePart(PlaneContainer container)
	{
		if (inputOverride.triggered)
		{
			Decouple();
		}
	}

	private void Decouple()
	{
		for (int i = 0; i < part.children.Count; i++)
		{
			Singleton<PartExploder>.Instance.ExplodePart(part.children[i].gameObject.GetComponentInChildren<PlanePart>(), rootExplosion: true, disableExplosion: true);
		}
	}

	public override PartStat[] GetPartStats()
	{
		return new PartStat[0];
	}

	public override void Save(GameDataWriter writer)
	{
		writer.Write(inputOverride.SaveBindingOverridesAsJson());
	}

	public override void Load(GameDataReader reader)
	{
		if (reader.version < 30)
		{
			string text = reader.ReadString();
			if (!string.IsNullOrEmpty(text))
			{
				inputOverride.ApplyBindingOverride(0, text);
			}
			if (reader.version > 28)
			{
				string text2 = reader.ReadString();
				if (!string.IsNullOrEmpty(text2))
				{
					inputOverride.ApplyBindingOverride(1, text2);
				}
			}
		}
		if (reader.version >= 30)
		{
			inputOverride.LoadBindingOverridesFromJson(reader.ReadString());
		}
	}
}
