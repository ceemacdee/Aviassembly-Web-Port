using UnityEngine;

public class DirtTrail : PlaneParticle
{
	private TrailRenderer trailRenderer;

	protected override void Awake()
	{
		trailRenderer = GetComponent<TrailRenderer>();
	}

	public override void SetValue(float value)
	{
		trailRenderer.emitting = value > 0.01f;
	}
}
