﻿/*============================================================================*/
/*  VehicleStandardInput.cs                                 Module for Unity  */
/*                                                                            */
/*  Script for User Control of vehicles based on Edy's Vehicle Physics.       */
/*	http://www.edy.es														  */
/*  Version of 07-13-2020                             Copyright by Ziran Wang */
/*	Toyota Motor North America, InfoTech Labs   	   ryanwang11@hotmail.com */
/*============================================================================*/

/// Log:
/// 06-07-2020 Ziran Wang
/// 1. Changed the steerInput variable from private to public to allow global access.
///   
/// 06-27-2020 Ziran Wang
/// 1. Changed the input for seperate axis option.
/// 2. Modified the axes for Logitech G29 control due to its bugs.
/// 
/// 07-13-2020 Ziran Wang
/// 1. Make forwardInput and reverseInput public, so they can be accessed from other scripts.

using UnityEngine;

namespace EVP
{

	public class VehicleStandardInput : MonoBehaviour
	{
		public VehicleController target;

		public bool continuousForwardAndReverse = true;

		public enum ThrottleAndBrakeInput { SingleAxis, SeparateAxes };
		public ThrottleAndBrakeInput throttleAndBrakeInput = ThrottleAndBrakeInput.SingleAxis;

		[HideInInspector]
		public string steerAxis;
		[HideInInspector]
		public string throttleAndBrakeAxis = "ThrottleBrake";
		[HideInInspector]
		public string throttleAxis = "Acceleration";
		[HideInInspector]
		public string brakeAxis = "Braking";
		[HideInInspector]
		public string handbrakeAxis = "Jump";
		public KeyCode resetVehicleKey = KeyCode.Return;

		bool m_doReset = false;

		[HideInInspector]
		public float steerInput;
		[HideInInspector]
		public float forwardInput = 0.0f;
		[HideInInspector]
		public float reverseInput = 0.0f;

		[HideInInspector]
		public bool isKeyboardInput = false;
		[HideInInspector]
		public bool reverse = false;

		void OnEnable()
		{
			// Cache vehicle

			if (target == null)
				target = GetComponent<VehicleController>();
		}

		void Update()
		{
			if (target == null) return;

			if (Input.GetKeyDown(resetVehicleKey)) m_doReset = true;

			if (Input.GetJoystickNames().Length == 0) isKeyboardInput = true;
			else isKeyboardInput = false;
			// TODO: Test with steering wheel
		}


		void FixedUpdate()
		{
			if (target == null) return;

			if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S))
			{
				isKeyboardInput = true;
			}

			if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S))
			{
				isKeyboardInput = false;
			}

			// Read the user input

			float handbrakeInput = Mathf.Clamp01(Input.GetAxis(handbrakeAxis));

			/// Logitech G29 input
			if (throttleAndBrakeInput == ThrottleAndBrakeInput.SeparateAxes)
			{
				// steerAxis = "Steering";
				if (isKeyboardInput)
				{
					steerInput = Mathf.Clamp(Input.GetAxis(steerAxis) * 0.2f, -1.0f, 1.0f);
					forwardInput = Mathf.Clamp01(Input.GetAxis(throttleAndBrakeAxis));
					reverseInput = Mathf.Clamp01(-Input.GetAxis(throttleAndBrakeAxis));
				}
				else
				{
					steerInput = Mathf.Clamp(Input.GetAxis(steerAxis) * 0.2f, -1.0f, 1.0f);
					forwardInput = 0.5f * (Input.GetAxis(throttleAxis) + 1);
					reverseInput = -Input.GetAxis(brakeAxis);
				}
				//forwardInput = Mathf.Clamp01(Input.GetAxis(throttleAxis));
				//reverseInput = Mathf.Clamp01(Input.GetAxis(brakeAxis));
			}
			/// Keyboard input
			else
			{
				// steerAxis = "Horizontal";
				// Debug.Log("Steer: " + Input.GetAxis(steerAxis) + ", Throttle: " + Input.GetAxis(throttleAndBrakeAxis));
				steerInput = Mathf.Clamp(Input.GetAxis(steerAxis), -1.0f, 1.0f);
				forwardInput = Mathf.Clamp01(Input.GetAxis(throttleAndBrakeAxis));
				reverseInput = Mathf.Clamp01(-Input.GetAxis(throttleAndBrakeAxis));
			}
			// Debug.Log(Time.timeSinceLevelLoad + ", keyboard? " + isKeyboardInput + ", throttle: " + forwardInput +
			// ", brake: " + reverseInput + ", steer: " + steerInput);
			// Translate forward/reverse to vehicle input

			float throttleInput = 0.0f;
			float brakeInput = 0.0f;

			if (continuousForwardAndReverse)
			{
				float minSpeed = 0.1f;
				float minInput = 0.1f;

				if (target.speed > minSpeed)
				{
					throttleInput = forwardInput;
					brakeInput = reverseInput;
				}
				else
				{
					if (reverseInput > minInput)
					{
						throttleInput = -reverseInput;
						brakeInput = 0.0f;
					}
					else if (forwardInput > minInput)
					{
						if (target.speed < -minSpeed)
						{
							throttleInput = 0.0f;
							brakeInput = forwardInput;
						}
						else
						{
							throttleInput = forwardInput;
							brakeInput = 0;
						}
					}
				}
			}
			else
			{
				// bool reverse = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
				if ((Input.GetKeyDown(KeyCode.JoystickButton4) || Input.GetKeyDown(KeyCode.E)) && reverse)
				{
					reverse = !reverse;
				}

				if ((Input.GetKeyDown(KeyCode.JoystickButton5) || Input.GetKeyDown(KeyCode.Q)) && !reverse)
				{
					reverse = !reverse;
				}

				// if (Input.GetKeyUp(KeyCode.R))
				// {
				// 	reverse = !reverse;
				// }

				if (!reverse)
				{
					throttleInput = forwardInput;
					brakeInput = reverseInput;
				}
				else
				{
					// throttleInput = -reverseInput;
					// brakeInput = 0;
					throttleInput = -forwardInput;
					brakeInput = reverseInput;
				}
			}

			// Apply input to vehicle

			target.steerInput = steerInput;
			target.throttleInput = throttleInput;
			target.brakeInput = brakeInput;
			target.handbrakeInput = handbrakeInput;
			//Debug.Log(Time.timeSinceLevelLoad + ", throttle: " + throttleInput +
			//", brake: " + brakeInput + ", steer: " + steerInput);
			// Do a vehicle reset

			if (m_doReset)
			{
				target.ResetVehicle();
				m_doReset = false;
			}
		}

		/* Code from Tim Korving for better handling the continuous forward
			and reverse mode. To be adapted and tested.

		void HandleVerticalInputModeInterrupt()                                         // Handle Interrupt input mode for forward reverse
			{
			if (m_MoveState == VERTICAL_INPUT_STATE.STATIONARY)
				{
				if (m_ForwardInput >= m_MinInput)                                       // If forward input...
					{
					ChangeVerticalInputState(VERTICAL_INPUT_STATE.FORWARD);
					m_ThrottleInput = m_ForwardInput;                                   // Throttle is forward input
					m_BrakeInput = 0f;                                                  // Release the brakes
					}
				else if (m_ReverseInput >= m_MinInput)                                  // If reverse input...
					{
					ChangeVerticalInputState(VERTICAL_INPUT_STATE.REVERSE);
					m_ThrottleInput = -m_ReverseInput;                                  // Throttle is inverse reverse input (eek)
					m_BrakeInput = 0f;                                                  // Release the brakes
					}
				else
					{
					ChangeVerticalInputState(VERTICAL_INPUT_STATE.STATIONARY);
					}
				}
			else if (m_MoveState == VERTICAL_INPUT_STATE.FORWARD)
				{
				if (m_EVPController.speed >= m_MinSpeed)                                // Currently in forward motion
					{
					m_ThrottleInput = m_ForwardInput;                                   // Throttle is forward input
					m_BrakeInput = m_ReverseInput;                                      // Brake is reverse input
					}
				else if (m_ForwardInput < m_MinInput && m_ReverseInput < m_MinInput)
					{
					ChangeVerticalInputState(VERTICAL_INPUT_STATE.STATIONARY);
					m_BrakeInput = 0f;
					m_ForwardInput = 0f;
					m_ReverseInput = 0f;
					}
				}
			else if (m_MoveState == VERTICAL_INPUT_STATE.REVERSE)
				{
				if (m_EVPController.speed <= -m_MinSpeed)                               // Currently in backward motion
					{
					m_ThrottleInput = -m_ReverseInput;                                  // Throttle is inverse reverse input (?)
					m_BrakeInput = m_ForwardInput;                                      // Brake is forward input
					}
				else if (m_ForwardInput < m_MinInput && m_ReverseInput < m_MinInput)
					{
					ChangeVerticalInputState(VERTICAL_INPUT_STATE.STATIONARY);
					m_BrakeInput = 0f;
					m_ForwardInput = 0f;
					m_ReverseInput = 0f;
					}
				}
				*/
	}
	
}