using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PlaneStorage : Singleton<PlaneStorage>
{
	private PlaneContainer planeContainer;

	public DecalContainer decalContainer;

	public PartPlacer partPlacer;

	private List<HistoryEntry> history = new List<HistoryEntry>();

	private bool updateHistory;

	private float moneyThisFrame;

	public event Action ResetPlaneEvent;

	public event Action PlaneUpdated;

	protected override void Awake()
	{
		base.Awake();
		planeContainer = GetComponent<PlaneContainer>();
	}

	public void Save(GameDataWriter writer)
	{
		writer.Write(base.transform.childCount);
		for (int i = 0; i < base.transform.childCount; i++)
		{
			SavePart(writer, base.transform.GetChild(i).GetComponent<BuildingPart>());
		}
		for (int j = 0; j < base.transform.childCount; j++)
		{
			base.transform.GetChild(j).GetComponent<BuildingPart>().Save(writer, this);
		}
	}

	public void Load(GameDataReader reader)
	{
		int num = reader.ReadInt();
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < num; i++)
		{
			list.Add(LoadPart(reader));
		}
		for (int j = 0; j < list.Count; j++)
		{
			list[j].GetComponent<BuildingPart>().Load(reader, this);
		}
	}

	public string SaveToFile(string filename)
	{
		string text = Path.Combine(Path.Combine(Application.persistentDataPath, "Plane Designs"), filename);
		Directory.CreateDirectory(text);
		string text2 = Path.Combine(text, filename + ".planedesign");
		WebGLFileSync.Flush();
		using BinaryWriter writer = new BinaryWriter(File.Open(text2, FileMode.Create));
		GameDataWriter gameDataWriter = new GameDataWriter(writer);
		gameDataWriter.Write(30);
		gameDataWriter.Write(GetPlaneCost());
		gameDataWriter.Write(planeContainer.transform.childCount);
		for (int i = 0; i < planeContainer.transform.childCount; i++)
		{
			gameDataWriter.Write(planeContainer.transform.GetChild(i).name.Replace("(Clone)", ""));
		}
		MemoryStream memoryStream = SaveToStream();
		memoryStream.Position = 0L;
		gameDataWriter.Write(memoryStream);
		return text2;
	}

	public float GetPlaneCost()
	{
		float num = 0f;
		for (int i = 0; i < planeContainer.transform.childCount; i++)
		{
			BuildingPart component = planeContainer.transform.GetChild(i).GetComponent<BuildingPart>();
			if (component != null && component.hasBeenPlaced && component != null)
			{
				num += component.price;
			}
		}
		return num;
	}

	public bool CheckLoadConditions(GameDataReader gameDataReader, bool applyConditions, bool researchOnly = false)
	{
		float num = gameDataReader.ReadFloat();
		List<string> list = new List<string>();
		int num2 = gameDataReader.ReadInt();
		for (int i = 0; i < num2; i++)
		{
			string item = gameDataReader.ReadString();
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			if (!Singleton<ResearchManager>.Instance.unlockedParts.Contains(Singleton<ResearchManager>.Instance.FindPart(list[j])))
			{
				return false;
			}
		}
		if (researchOnly)
		{
			return true;
		}
		float planeCost = GetPlaneCost();
		if (planeCost + Singleton<MoneyManager>.Instance.money < num)
		{
			return false;
		}
		if (applyConditions)
		{
			Singleton<MoneyManager>.Instance.money += planeCost - num;
		}
		return true;
	}

	public bool ResearchUnlocked(string filePath)
	{
		using BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open));
		GameDataReader gameDataReader = new GameDataReader(reader);
		GetVersion(gameDataReader, filePath);
		return CheckLoadConditions(gameDataReader, applyConditions: false, researchOnly: true);
	}

	public bool ResearchUnlocked(string[] requieredParts)
	{
		for (int i = 0; i < requieredParts.Length; i++)
		{
			if (!Singleton<ResearchManager>.Instance.unlockedParts.Contains(Singleton<ResearchManager>.Instance.FindPart(requieredParts[i])))
			{
				return false;
			}
		}
		return true;
	}

	public bool IsLocked(string filePath)
	{
		using BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open));
		GameDataReader gameDataReader = new GameDataReader(reader);
		GetVersion(gameDataReader, filePath);
		return !CheckLoadConditions(gameDataReader, applyConditions: false);
	}

	public float GetSaveFileCost(string filePath)
	{
		using BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open));
		GameDataReader gameDataReader = new GameDataReader(reader);
		GetVersion(gameDataReader, filePath);
		return gameDataReader.ReadFloat();
	}

	public string[] GetSaveFileRequiredParts(string filePath)
	{
		using BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open));
		GameDataReader gameDataReader = new GameDataReader(reader);
		GetVersion(gameDataReader, filePath);
		gameDataReader.ReadFloat();
		List<string> list = new List<string>();
		int num = gameDataReader.ReadInt();
		for (int i = 0; i < num; i++)
		{
			string item = gameDataReader.ReadString();
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}

	public bool CanAffordPlane(int planeCost)
	{
		float planeCost2 = GetPlaneCost();
		return Singleton<MoneyManager>.Instance.money + planeCost2 >= (float)planeCost;
	}

	private static void GetVersion(GameDataReader reader, string savePath)
	{
		long streamPosition = reader.GetStreamPosition();
		reader.ReadInt();
		reader.ReadInt();
		string text = reader.ReadString();
		reader.SetStreamPosition(streamPosition);
		if ((bool)Singleton<ResearchManager>.Instance.FindPart(text))
		{
			reader.version = 18;
		}
		else
		{
			reader.version = reader.ReadInt();
		}
	}

	public bool LoadFromFile(string filePath)
	{
		using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
		{
			GameDataReader gameDataReader = new GameDataReader(reader);
			GetVersion(gameDataReader, filePath);
			if (!CheckLoadConditions(gameDataReader, applyConditions: true))
			{
				return false;
			}
			MemoryStream memoryStream = new MemoryStream();
			gameDataReader.GetStream().CopyTo(memoryStream);
			memoryStream.Position = 0L;
			LoadFromStream(memoryStream, gameDataReader.version);
		}
		Singleton<BuildingCamera>.Instance.UpdateOrigin(instantant: true);
		return true;
	}

	public MemoryStream SaveToStream()
	{
		MemoryStream memoryStream = new MemoryStream();
		GameDataWriter writer = new GameDataWriter(new BinaryWriter(memoryStream));
		Save(writer);
		return memoryStream;
	}

	public void LoadFromStream(MemoryStream stream, int version)
	{
		ResetPlane();
		stream.Position = 0L;
		GameDataReader gameDataReader = new GameDataReader(new BinaryReader(stream));
		gameDataReader.version = version;
		Load(gameDataReader);
		planeContainer.OnBuildModeLoaded();
	}

	public void ResetPlane()
	{
		ResetPlaneEvent();
		for (int num = planeContainer.transform.childCount - 1; num >= 0; num--)
		{
			GameObject obj = planeContainer.transform.GetChild(num).gameObject;
			obj.transform.parent = null;
			UnityEngine.Object.Destroy(obj);
		}
		Singleton<DecalContainer>.Instance.ResetDecals();
	}

	private void Update()
	{
		if (((Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z)) || (Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKey(KeyCode.Z))) && Singleton<InputManager>.Instance.currentInputBlocker == null)
		{
			Undo();
		}
		if (base.transform.childCount == 0)
		{
			history.Clear();
		}
	}

	private static bool CompareMemoryStreams(MemoryStream ms1, MemoryStream ms2)
	{
		if (ms1.Length != ms2.Length)
		{
			return false;
		}
		ms1.Position = 0L;
		ms2.Position = 0L;
		byte[] first = ms1.ToArray();
		byte[] second = ms2.ToArray();
		return first.SequenceEqual(second);
	}

	public void Undo()
	{
		if (GameManager.gameMode == GameMode.Building && !Singleton<GameManager>.Instance.Loading && history.Count >= 2)
		{
			float planeCost = GetPlaneCost();
			LoadFromStream(history[history.Count - 2].plane, 30);
			float planeCost2 = GetPlaneCost();
			if (!Singleton<GameManager>.Instance.gameModeData.creativeMode)
			{
				Singleton<MoneyManager>.Instance.money += planeCost - planeCost2;
			}
			history.RemoveAt(history.Count - 1);
		}
	}

	public void UpdateHistory()
	{
		updateHistory = true;
	}

	private void LateUpdate()
	{
		if (updateHistory)
		{
			ExecuteHistoryUpdate();
		}
		updateHistory = false;
		moneyThisFrame = Singleton<MoneyManager>.Instance.money;
	}

	private void ExecuteHistoryUpdate()
	{
		MemoryStream memoryStream = SaveToStream();
		if (history.Count <= 0 || !CompareMemoryStreams(memoryStream, history[history.Count - 1].plane))
		{
			if (history.Count > 1000)
			{
				history.RemoveAt(0);
			}
			HistoryEntry item = new HistoryEntry(moneyThisFrame, memoryStream);
			history.Add(item);
			if (PlaneUpdated != null)
			{
				PlaneUpdated();
			}
		}
	}

	public void SaveDecal(Decal decal, GameDataWriter writer)
	{
		Singleton<DecalContainer>.Instance.UpdateTransform();
		writer.Write(decal.GetComponent<MeshRenderer>().materials[0].GetTexture("_MainTex").name);
		writer.Write(decal.currentColor);
		writer.Write(decal.meshDecal.layer);
		writer.Write(decal.transform.localPosition);
		writer.Write(decal.transform.localRotation);
		writer.Write(decal.transform.localScale);
	}

	public Decal LoadDecal(GameDataReader reader)
	{
		Decal component = UnityEngine.Object.Instantiate(PartPrefabs.GetDecalPrefab()).GetComponent<Decal>();
		component.transform.SetParent(decalContainer.transform, worldPositionStays: true);
		string decalName = reader.ReadString();
		component.gameObject.GetComponent<MeshRenderer>().materials[0].SetTexture("_MainTex", PartPrefabs.GetDecalTexture(decalName));
		component.SetColor(reader.ReadColor(), apply: true);
		component.meshDecal.layer = reader.ReadInt();
		component.transform.localPosition = reader.ReadVector3();
		component.transform.localRotation = reader.ReadQuaternion();
		component.transform.localScale = reader.ReadVector3();
		decalContainer.AddDecal(component);
		component.meshDecal.Recalculate();
		component.hasBeenPlaced = true;
		return component;
	}

	public void SaveBuildingPart(BuildingPart buildingPart, GameDataWriter writer)
	{
		writer.Write(buildingPart == null);
		if (!(buildingPart == null))
		{
			writer.Write(buildingPart.transform.localPosition);
			writer.Write(buildingPart.gameObject.name.Replace("(Clone)", ""));
		}
	}

	public BuildingPart LoadBuildingPart(GameDataReader reader, BuildingPart ignorePart = null)
	{
		if (reader.ReadBool())
		{
			return null;
		}
		Vector3 a = reader.ReadVector3();
		string text = reader.ReadString();
		float num = float.MaxValue;
		BuildingPart result = null;
		for (int i = 0; i < base.transform.childCount; i++)
		{
			BuildingPart component = base.transform.GetChild(i).GetComponent<BuildingPart>();
			if (!(component == ignorePart) && component.gameObject.name.Replace("(Clone)", "") == text)
			{
				float num2 = Vector3.Distance(a, component.transform.localPosition);
				if (num2 < num)
				{
					num = num2;
					result = component;
				}
			}
		}
		return result;
	}

	private void SavePart(GameDataWriter writer, BuildingPart part)
	{
		writer.Write(part.gameObject.name);
		writer.Write(part.transform.localPosition);
		writer.Write(part.transform.localRotation);
		writer.Write(part.transform.localScale);
		ProceduralFuselage component = part.GetComponent<ProceduralFuselage>();
		if ((bool)component)
		{
			component.Save(writer);
		}
	}

	private GameObject LoadPart(GameDataReader reader)
	{
		GameObject obj = UnityEngine.Object.Instantiate(PartPrefabs.GetPartPrefab(reader.ReadString()));
		obj.transform.SetParent(base.transform, worldPositionStays: true);
		obj.transform.localPosition = reader.ReadVector3();
		obj.transform.localRotation = reader.ReadQuaternion();
		obj.transform.localScale = reader.ReadVector3();
		ProceduralFuselage component = obj.GetComponent<ProceduralFuselage>();
		if ((bool)component)
		{
			component.Load(reader);
		}
		return obj;
	}
}
