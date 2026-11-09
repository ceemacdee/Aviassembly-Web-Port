using System;
using System.IO;
using UnityEngine;

public class GameDataReader
{
	private BinaryReader reader;

	public int version;

	public GameDataReader(BinaryReader reader)
	{
		this.reader = reader;
	}

	public string ReadString()
	{
		if (reader.ReadBoolean())
		{
			return "";
		}
		return reader.ReadString();
	}

	public bool ReadBool()
	{
		return reader.ReadBoolean();
	}

	public float ReadFloat()
	{
		return reader.ReadSingle();
	}

	public int ReadInt()
	{
		return reader.ReadInt32();
	}

	public double ReadDouble()
	{
		return reader.ReadDouble();
	}

	public Quaternion ReadQuaternion()
	{
		Quaternion result = default;
		result.x = reader.ReadSingle();
		result.y = reader.ReadSingle();
		result.z = reader.ReadSingle();
		result.w = reader.ReadSingle();
		return result;
	}

	public Vector2 ReadVector2()
	{
		Vector2 result = default;
		result.x = reader.ReadSingle();
		result.y = reader.ReadSingle();
		return result;
	}

	public Vector3 ReadVector3()
	{
		Vector3 result = default;
		result.x = reader.ReadSingle();
		result.y = reader.ReadSingle();
		result.z = reader.ReadSingle();
		return result;
	}

	public Vector4 ReadVector4()
	{
		Vector4 result = default;
		result.x = reader.ReadSingle();
		result.y = reader.ReadSingle();
		result.z = reader.ReadSingle();
		result.w = reader.ReadSingle();
		return result;
	}

	public Color ReadColor()
	{
		Color result = default;
		result.r = reader.ReadSingle();
		result.g = reader.ReadSingle();
		result.b = reader.ReadSingle();
		result.a = reader.ReadSingle();
		return result;
	}

	public Type ReadType()
	{
		string text = reader.ReadString();
		Type type = Type.GetType(text);
		if (type != null)
		{
			return type;
		}
		Debug.LogError("Type doesn't existname: " + text);
		return null;
	}

	public long ReadLong()
	{
		return reader.ReadInt64();
	}

	public Stream GetStream()
	{
		return reader.BaseStream;
	}

	public long GetStreamPosition()
	{
		return reader.BaseStream.Position;
	}

	public void SetStreamPosition(long position)
	{
		reader.BaseStream.Position = position;
	}
}
