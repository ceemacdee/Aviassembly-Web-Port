using System;

public class DecalPair
{
	public Decal decal1;

	public Decal decal2;

	public DecalPair(Decal decal1, Decal decal2)
	{
		if (decal1.GetHashCode() <= decal2.GetHashCode())
		{
			this.decal1 = decal1;
			this.decal2 = decal2;
		}
		else
		{
			this.decal1 = decal2;
			this.decal2 = decal1;
		}
	}

	public bool Equals(DecalPair other)
	{
		if (decal1 == other.decal1)
		{
			return decal2 == other.decal2;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is DecalPair other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(decal1, decal2);
	}
}
