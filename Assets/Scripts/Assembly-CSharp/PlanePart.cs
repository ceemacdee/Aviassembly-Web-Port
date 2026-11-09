using UnityEngine;

public abstract class PlanePart : MonoBehaviour
{
	[HideInInspector]
	public Rigidbody rb;

	public float weight;

	[Space(10f)]
	public GameObject settingsPrefab;

	public GameObject keybindSettingsPrefab;

	public virtual void Activate()
	{
	}

	public virtual void Deactivate()
	{
	}

	public virtual void Save(GameDataWriter writer)
	{
	}

	public virtual void Load(GameDataReader reader)
	{
	}

	public abstract PartStat[] GetPartStats();

	public abstract void UpdatePart(PlaneContainer container);
}
