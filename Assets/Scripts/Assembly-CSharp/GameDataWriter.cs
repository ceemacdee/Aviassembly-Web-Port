using System;
using System.IO;
using UnityEngine;

public class GameDataWriter
{
	private BinaryWriter writer;

	public GameDataWriter(BinaryWriter writer)
	{
		this.writer = writer;
	}

	public void Write(bool value)
	{
		writer.Write(value);
	}

	public void Write(string value)
	{
		bool flag = string.IsNullOrEmpty(value);
		writer.Write(flag);
		if (!flag)
		{
			writer.Write(value);
		}
	}

	public void Write(float value)
	{
		writer.Write(value);
	}

	public void Write(double value)
	{
		writer.Write(value);
	}

	public void Write(int value)
	{
		writer.Write(value);
	}

	public void Write(Quaternion value)
	{
		writer.Write(value.x);
		writer.Write(value.y);
		writer.Write(value.z);
		writer.Write(value.w);
	}

	public void Write(Vector2 value)
	{
		writer.Write(value.x);
		writer.Write(value.y);
	}

	public void Write(Vector3 value)
	{
		writer.Write(value.x);
		writer.Write(value.y);
		writer.Write(value.z);
	}

	public void Write(Vector4 value)
	{
		writer.Write(value.x);
		writer.Write(value.y);
		writer.Write(value.z);
		writer.Write(value.w);
	}

	public void Write(Color value)
	{
		writer.Write(value.r);
		writer.Write(value.g);
		writer.Write(value.b);
		writer.Write(value.a);
	}

	public void Write(Type type)
	{
		writer.Write(type.AssemblyQualifiedName);
	}

	public void Write(Quest quest)
	{
		writer.Write(quest == null);
		if (quest != null)
		{
			Type type = quest.GetType();
			Write(type);
			quest.Save(this);
		}
	}

	public void Write(Mesh mesh)
	{
		Write(mesh.vertices.Length);
		for (int i = 0; i < mesh.vertices.Length; i++)
		{
			Write(mesh.vertices[i]);
		}
		Write(mesh.triangles.Length);
		for (int j = 0; j < mesh.triangles.Length; j++)
		{
			Write(mesh.triangles[j]);
		}
		Write(mesh.uv.Length);
		for (int k = 0; k < mesh.uv.Length; k++)
		{
			Write(mesh.uv[k]);
		}
	}

	public long GetPosition()
	{
		return writer.BaseStream.Position;
	}

	public void Write(long value)
	{
		writer.Write(value);
	}

	public void Write(Stream stream)
	{
		stream.Position = 0L;
		stream.CopyTo(writer.BaseStream);
	}
}
