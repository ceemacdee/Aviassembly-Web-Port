using System.IO;

internal struct HistoryEntry
{
	public float money;

	public MemoryStream plane;

	public HistoryEntry(float money, MemoryStream plane)
	{
		this.money = money;
		this.plane = plane;
	}
}
