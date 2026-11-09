using UnityEngine;

public class WheelParticle : PlaneParticle
{
	private ParticleSystem.EmitParams emitParams;

	private float value;

	protected override void Awake()
	{
		emitParams = default;
	}

	private void Update()
	{
		ParticleSystem.ShapeModule shape = systems[0].shape;
		shape.scale = new Vector3(0.3f, Singleton<PlaneContainer>.Instance.GetVelocityMagintude() * Time.deltaTime, 0f);
		systems[0].Emit(emitParams, Mathf.RoundToInt(shape.scale.x * shape.scale.y * 25f * (float)((value > 0.01f) ? 1 : 0)));
	}

	public override void SetValue(float value)
	{
		this.value = value;
	}
}
