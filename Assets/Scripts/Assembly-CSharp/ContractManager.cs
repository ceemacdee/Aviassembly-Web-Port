using System.Collections.Generic;
using UnityEngine;

public class ContractManager : Singleton<ContractManager>
{
	private Dictionary<Vector3, List<Contract>> contracts = new Dictionary<Vector3, List<Contract>>();

	public void UpdateContractPersistence(Vector3 position, List<Contract> contracts)
	{
		if (!this.contracts.ContainsKey(position))
		{
			this.contracts.Add(position, contracts);
		}
		for (int num = contracts.Count - 1; num >= 0; num--)
		{
			contracts[num].completed = this.contracts[position][num].completed;
			this.contracts[position][num] = contracts[num];
		}
	}
}
