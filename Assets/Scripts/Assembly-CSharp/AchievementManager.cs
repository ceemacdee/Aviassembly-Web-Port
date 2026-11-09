using System.Collections.Generic;
using Steamworks.Data;
using UnityEngine;

public class AchievementManager : Singleton<AchievementManager>
{
	private struct AchievementStats
	{
		public float speed;

		public float height;

		public float groundSpeed;

		public int unfinishedMissions;

		public void UpdateStats()
		{
			PlaneContainer instance = Singleton<PlaneContainer>.Instance;
			speed = instance.GetVelocityMagintude();
			height = instance.transform.position.y;
			unfinishedMissions = Singleton<AirportManager>.Instance.GetUnfinishedMissionCount();
		}
	}

	private AchievementStats currentStats;

	private List<string> unlockedAchievements = new List<string>();

	public bool onlyUsedWoodEngine;

	private bool leftGroundSinceLastCargoLoad;

	private float maxSpeedSinceTakeoff;

	private int missionsCompletedThisFrame;

	protected override void Awake()
	{
		base.Awake();
		onlyUsedWoodEngine = true;
	}

	public void BeatCampaign()
	{
		UnlockAchievement("ACH_CAMPAIGN");
		if (onlyUsedWoodEngine)
		{
			UnlockAchievement("ACH_WOOD_ENGINE");
		}
	}

	public void UnlockAchievement(string achievementID)
	{
		if (!Singleton<GameManager>.Instance.gameModeData.creativeMode && !unlockedAchievements.Contains(achievementID))
		{
			unlockedAchievements.Add(achievementID);
			new Achievement(achievementID).Trigger();
		}
	}

	public void OnCompleteMission(bool partial)
	{
		if (!partial)
		{
			missionsCompletedThisFrame++;
		}
		if (missionsCompletedThisFrame > 1)
		{
			UnlockAchievement("ACH_ONE_FLIGHT");
		}
		PlaneContainer instance = Singleton<PlaneContainer>.Instance;
		bool flag = false;
		for (int i = 0; i < instance.planeParts.Count; i++)
		{
			if (instance.planeParts[i].GetType() == typeof(Wing))
			{
				flag = true;
			}
			if (instance.planeParts[i].GetType() == typeof(Engine) && !instance.planeParts[i].gameObject.name.Contains("Wood"))
			{
				onlyUsedWoodEngine = false;
			}
		}
		if (!flag)
		{
			UnlockAchievement("ACH_WINGLESS");
		}
		if (!leftGroundSinceLastCargoLoad)
		{
			UnlockAchievement("ACH_CARGO_TRUCK");
		}
		if (maxSpeedSinceTakeoff <= 50f && leftGroundSinceLastCargoLoad)
		{
			UnlockAchievement("ACH_AIRSHIP");
		}
	}

	private void Update()
	{
		PlaneContainer instance = Singleton<PlaneContainer>.Instance;
		bool num = instance.IsGrounded();
		missionsCompletedThisFrame = 0;
		if (Mathf.Approximately(Singleton<CargoInventory>.Instance.GetCargoMass(), 0f))
		{
			leftGroundSinceLastCargoLoad = false;
			maxSpeedSinceTakeoff = 0f;
		}
		if (instance.DistanceFromGround() > 5f)
		{
			leftGroundSinceLastCargoLoad = true;
		}
		if (!num)
		{
			Vector3 velocity = instance.GetVelocity();
			velocity.y = 0f;
			maxSpeedSinceTakeoff = Mathf.Max(velocity.magnitude, maxSpeedSinceTakeoff);
		}
		currentStats.UpdateStats();
		if (currentStats.speed >= 500f)
		{
			UnlockAchievement("ACH_SPEED_1");
		}
		if (currentStats.speed >= 1000f)
		{
			UnlockAchievement("ACH_SPEED_2");
		}
		if (currentStats.speed >= 3000f)
		{
			UnlockAchievement("ACH_SPEED_3");
		}
		if (currentStats.height >= 10000f)
		{
			UnlockAchievement("ACH_HEIGHT");
		}
	}

	public void Save(GameDataWriter writer)
	{
		writer.Write(onlyUsedWoodEngine);
	}

	public void Load(GameDataReader reader)
	{
		if (reader.version > 22)
		{
			onlyUsedWoodEngine = reader.ReadBool();
		}
		else
		{
			onlyUsedWoodEngine = false;
		}
	}
}
