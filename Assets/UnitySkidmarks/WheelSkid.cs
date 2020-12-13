﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Example skid script. Put this on a WheelCollider.
// Copyright 2017 Nition, BSD licence (see LICENCE file). http://nition.co
[RequireComponent(typeof(WheelCollider))]
public class WheelSkid : MonoBehaviour {

	// INSPECTOR SETTINGS

	[SerializeField]
	Rigidbody rb;
	[SerializeField]
	Skidmarks skidmarksController;
	
	public bool PlayingAudio { get; private set; }
	private AudioSource m_AudioSource;
	[SerializeField] private float maxVolume = 1f;
	

	// END INSPECTOR SETTINGS

	WheelCollider wheelCollider;
	WheelHit wheelHitInfo;
	Car carController;

	const float SKID_FX_SPEED = 0.5f; // Min side slip speed in m/s to start showing a skid
	const float MAX_SKID_INTENSITY = 20.0f; // m/s where skid opacity is at full intensity
	const float WHEEL_SLIP_MULTIPLIER = 10.0f; // For wheelspin. Adjust how much skids show
	int lastSkid = -1; // Array index for the skidmarks controller. Index of last skidmark piece this wheel used
	float lastFixedUpdateTime;

	// #### UNITY INTERNAL METHODS ####

	private void OnEnable()
	{
		GameManager.OnFinishRaceHandler += StopSound;
	}

	private void OnDisable()
	{
		GameManager.OnFinishRaceHandler -= StopSound;
	}
	
	protected void Awake() {
		wheelCollider = GetComponent<WheelCollider>();
		carController = transform.parent.GetComponent<Car>();
		lastFixedUpdateTime = Time.time;

		m_AudioSource = GetComponent<AudioSource>();

		skidmarksController = FindObjectOfType<Skidmarks>();
	}

	protected void FixedUpdate() {
		lastFixedUpdateTime = Time.time;
	}

	protected void LateUpdate() {
		if (wheelCollider.GetGroundHit(out wheelHitInfo) && carController.GetCurrentTerrain() == 0)
		{
			// Check sideways speed

			// Gives velocity with +z being the car's forward axis
			Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
			float skidTotal = Mathf.Abs(localVelocity.x);

			// Check wheel spin as well

			float wheelAngularVelocity = wheelCollider.radius * ((2 * Mathf.PI * wheelCollider.rpm) / 60);
			float carForwardVel = Vector3.Dot(rb.velocity, transform.forward);
			float wheelSpin = Mathf.Abs(carForwardVel - wheelAngularVelocity) * WHEEL_SLIP_MULTIPLIER;

			// NOTE: This extra line should not be needed and you can take it out if you have decent wheel physics
			// The built-in Unity demo car is actually skidding its wheels the ENTIRE time you're accelerating,
			// so this fades out the wheelspin-based skid as speed increases to make it look almost OK
			wheelSpin = Mathf.Max(0, wheelSpin * (10 - Mathf.Abs(carForwardVel)));

			skidTotal += wheelSpin;

			// Skid if we should
			if (skidTotal >= SKID_FX_SPEED) {
				float intensity = Mathf.Clamp01(skidTotal / MAX_SKID_INTENSITY);
				if(rb.velocity.magnitude > 0.2f && Time.timeScale > 0f){
					if (intensity > 0.5f && !PlayingAudio)
					{
						m_AudioSource.Play();
						PlayingAudio = true;
						m_AudioSource.volume = maxVolume * intensity;
					}
					else if (intensity <= 0.5f && PlayingAudio)
					{
						m_AudioSource.Stop();
						PlayingAudio = false;
						m_AudioSource.volume = maxVolume * intensity;
					}
				}
				else
				{
					m_AudioSource.Stop();
					PlayingAudio = false;
				}

				// Account for further movement since the last FixedUpdate
				Vector3 skidPoint = wheelHitInfo.point + (rb.velocity * (Time.time - lastFixedUpdateTime));
				lastSkid = skidmarksController.AddSkidMark(skidPoint, wheelHitInfo.normal, intensity, lastSkid);
			}
			else {
				lastSkid = -1;
			}
		}
		else {
			lastSkid = -1;
		}
	}

	// #### PUBLIC METHODS ####

	// #### PROTECTED/PRIVATE METHODS ####
	private void StopSound(int _id)
	{
		Destroy(m_AudioSource);
		Destroy(this);
	}

}