using System.Collections.Generic;
using UnityEngine;

public class ResearchManager : Singleton<ResearchManager>
{
	public int researchPoints;

	public int advancedResearchPoints;

	public AudioDef reward;

	public bool researchedItem;

	public ResearchTree researchTree;

	public List<BuildingPart> unlockedParts = new List<BuildingPart>();

	private List<BuildingPart> allParts = new List<BuildingPart>();

	private int defaultReseachedParts;

	public bool UsedResearch()
	{
		if (unlockedParts.Count > defaultReseachedParts)
		{
			return true;
		}
		return false;
	}

	public void PrintTotalResearchCost()
	{
		int num = 0;
		for (int i = 0; i < researchTree.items.Count; i++)
		{
			num += researchTree.items[i].Cost1;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		PrintTotalResearchCost();
		List<GameObject> allPrefabs = PartPrefabs.GetAllPrefabs();
		for (int i = 0; i < allPrefabs.Count; i++)
		{
			allParts.Add(allPrefabs[i].GetComponent<BuildingPart>());
			if (!researchTree.Contains(allPrefabs[i]))
			{
				unlockedParts.Add(allPrefabs[i].GetComponent<BuildingPart>());
				defaultReseachedParts++;
			}
		}
		for (int j = 0; j < researchTree.items.Count; j++)
		{
			if (researchTree.items[j].Prefab != null && researchTree.items[j].Cost1 == 0 && researchTree.items[j].Cost2 == 0)
			{
				unlockedParts.Add(researchTree.items[j].Prefab.GetComponent<BuildingPart>());
				defaultReseachedParts++;
			}
		}
	}

	private void Start()
	{
		if (Singleton<GameManager>.Instance.gameModeData.unlockAll)
		{
			UnlockAll();
		}
	}

	public ResearchCost GetResearchCost(GameObject part)
	{
		for (int i = 0; i < researchTree.items.Count; i++)
		{
			if (researchTree.items[i].Prefab == part)
			{
				return new ResearchCost(researchTree.items[i].Cost1, researchTree.items[i].Cost2);
			}
		}
		return new ResearchCost(0, 0);
	}

	public ResearchCost GetCurrentScrap()
	{
		return new ResearchCost(researchPoints, advancedResearchPoints);
	}

	public void UnlockAll()
	{
		for (int i = 0; i < allParts.Count; i++)
		{
			if (!unlockedParts.Contains(allParts[i]))
			{
				unlockedParts.Add(allParts[i]);
			}
		}
	}

	public bool IsPartUnlocked(BuildingPart part)
	{
		return unlockedParts.Contains(part);
	}

	public bool UnlockPart(BuildingPart part)
	{
		ResearchCost researchCost = GetResearchCost(part.gameObject);
		if (researchPoints < researchCost.scrap || advancedResearchPoints < researchCost.advancedScrap || unlockedParts.Contains(part))
		{
			return false;
		}
		researchPoints -= researchCost.scrap;
		advancedResearchPoints -= researchCost.advancedScrap;
		unlockedParts.Add(part);
		Singleton<AudioManager>.Instance.PlaySound(reward);
		return true;
	}

	public void Save(GameDataWriter writer)
	{
		writer.Write(researchPoints);
		writer.Write(advancedResearchPoints);
		writer.Write(unlockedParts.Count);
		for (int i = 0; i < unlockedParts.Count; i++)
		{
			writer.Write(unlockedParts[i].name);
		}
	}

	public void Load(GameDataReader reader)
	{
		if (reader.version > 2)
		{
			researchPoints = reader.ReadInt();
		}
		if (reader.version > 11)
		{
			advancedResearchPoints = reader.ReadInt();
		}
		int num = reader.ReadInt();
		for (int i = 0; i < num; i++)
		{
			unlockedParts.Add(FindPart(reader.ReadString()));
		}
	}

	public BuildingPart FindPart(string name)
	{
		for (int i = 0; i < allParts.Count; i++)
		{
			if (allParts[i].name == name)
			{
				return allParts[i];
			}
		}
		return null;
	}
}
