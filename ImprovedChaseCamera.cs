using System;
using UnityEngine;

namespace ImprovedChaseCamera
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class ImprovedChaseCamera : MonoBehaviour
	{
		public static KeyBinding ENABLE_CHASE = new KeyBinding(KeyCode.Tab);
		public static KeyBinding ADJUST_LOOK = new KeyBinding(KeyCode.Mouse1);
		public static KeyBinding ENABLE_MOUSE_JOYSTICK = new KeyBinding(KeyCode.RightAlt);
		public static KeyBinding SET_IVA_SNAP = new KeyBinding(KeyCode.KeypadEnter);
		public bool adjustLook = false;
		public bool adjustLookIVA = false;
		
		public bool enableChase = false;
		public bool enableFreeChase = false;
		public bool vtolMode = false;
		
		public float snapHeading = 0;
		public float snapPitch = 0;
		
		public Quaternion snapIVARotation;

		public float targetHeading = 0;
		public float targetPitch = 0;
		
		public float timeCheck = 0;
		
		public bool mouseJoystickEnabled = false;
		public bool isIVA = false;
		
		
		//Configurable values
		public float defaultAngle = 10;
		public float setFov = 60f;
		public bool defaultOn = false;
		public bool disableAuto = false;
		public bool autoSnap = false;
		public bool enableIVASnap = false;
		
		private string defaultFiredMode = "AUTO"; //detecting current camera mode
		
		
		//Messages
		private ScreenMessage chaseOn = new ScreenMessage("Improved Chase Cam ON", 5, ScreenMessageStyle.UPPER_CENTER);
		private ScreenMessage chaseOff = new ScreenMessage("Improved Chase Cam OFF", 5, ScreenMessageStyle.UPPER_CENTER);
		private ScreenMessage freeChaseOn = new ScreenMessage("Improved Free Chase Cam ON", 5, ScreenMessageStyle.UPPER_CENTER);
		private ScreenMessage freeChaseOff = new ScreenMessage("Improved Free Chase Cam OFF", 5, ScreenMessageStyle.UPPER_CENTER);
		private ScreenMessage msgMouseJoyOn = new ScreenMessage("Mouse Control On", 5, ScreenMessageStyle.UPPER_CENTER);
		private ScreenMessage msgMouseJoyOff = new ScreenMessage("Mouse Control Off", 5, ScreenMessageStyle.UPPER_CENTER);


		
		
		void Start()
		{
			LoadConfig ();	
			//snapIVARotation = InternalCamera.Instance.camera.transform.localRotation;
			
		}
		

		void Update()
		{
			if(ADJUST_LOOK.GetKeyDown())
			{
				adjustLook=true;	
			}
			if(ADJUST_LOOK.GetKeyUp())
			{
				adjustLook=false;	
			}
			if(ENABLE_MOUSE_JOYSTICK.GetKeyDown())
			{
				if(!mouseJoystickEnabled && (enableFreeChase || enableChase || isIVA))
				{
					EnableMouseJoystick();
				}
				else
				{
					DisableMouseJoystick();
				}
			}
			if(!enableFreeChase && !enableChase && mouseJoystickEnabled)
			{
				DisableMouseJoystick();	
			}
			
			if(InternalCamera.Instance!=null && InternalCamera.Instance.isActive)  //checking if camera is IVA
			{
				if(!isIVA){isIVA = true;}
			}
			else
			{
				if(isIVA){isIVA = false;}

			}
			
			#region Chase mode
			if(FlightCamera.fetch.mode == FlightCamera.Modes.CHASE && !MapView.MapIsEnabled && !FlightGlobals.ActiveVessel.isEVA)
			{
				Vector3 lookVector = Quaternion.Inverse(FlightGlobals.ActiveVessel.transform.rotation) * FlightGlobals.ActiveVessel.GetSrfVelocity();
				//lookvector X is left/right velocity, Y is forward velocity, Z is up/down velocity
				
				Vector3 forwardVector = new Vector3(0f, 1f, 0f);
				Quaternion pitchAngleQ = Quaternion.FromToRotation(forwardVector, lookVector);
				//float lerpRate = Mathf.Clamp((float) FlightGlobals.ActiveVessel.srf_velocity.magnitude / 50f, 0f, 1f);
				float lerpRate;
				if(Time.time - timeCheck < 2)
				{
					lerpRate = 0.1f;
				}
				else
				{	
					lerpRate = 1;
				}
			
					
				if(adjustLook)
				{
					snapHeading = FlightCamera.fetch.camHdg - (0 - pitchAngleQ.Roll());
					snapPitch = FlightCamera.fetch.camPitch - (0 + pitchAngleQ.Pitch ());
				}
				
				if(defaultOn && defaultFiredMode != "CHASE")
				{
					enableChase = true;
					defaultFiredMode = "CHASE";
					
					ScreenMessages.RemoveMessage(chaseOn);
					ScreenMessages.RemoveMessage(chaseOff);
					ScreenMessages.RemoveMessage(freeChaseOn);
					ScreenMessages.RemoveMessage(freeChaseOff);
					
					ScreenMessages.PostScreenMessage(chaseOn, true);
					timeCheck = Time.time;
					snapHeading = 0;
					snapPitch = defaultAngle*Mathf.Deg2Rad;
					FlightCamera.fetch.SetFoV(setFov);
				}
				
				if(ENABLE_CHASE.GetKeyDown())
				{
					if(enableChase)
					{
						ScreenMessages.RemoveMessage(chaseOn);
						ScreenMessages.RemoveMessage(chaseOff);
						ScreenMessages.RemoveMessage(freeChaseOn);
						ScreenMessages.RemoveMessage(freeChaseOff);
						
						ScreenMessages.PostScreenMessage(chaseOff, true);
						FlightCamera.fetch.SetFoV(FlightCamera.fetch.fovDefault);
					}
					if(!enableChase)
					{
						ScreenMessages.RemoveMessage(chaseOn);
						ScreenMessages.RemoveMessage(chaseOff);
						ScreenMessages.RemoveMessage(freeChaseOn);
						ScreenMessages.RemoveMessage(freeChaseOff);
						
						ScreenMessages.PostScreenMessage(chaseOn, true);
						timeCheck = Time.time;
						snapHeading = 0;
						snapPitch = defaultAngle*Mathf.Deg2Rad;
						FlightCamera.fetch.SetFoV(setFov);
					}
					enableChase = !enableChase;
				}
				
				if(enableChase && FlightGlobals.ActiveVessel != null && !adjustLook) //runs continuously while in chase cam on an active vessel when not adjusting look angle
				{
					
					if(FlightGlobals.ActiveVessel.srf_velocity.magnitude>0.5){
						targetPitch = (0 + pitchAngleQ.Pitch () + snapPitch);
						FlightCamera.fetch.camPitch = Mathf.Lerp(FlightCamera.fetch.camPitch, targetPitch, lerpRate);
						targetHeading = (0 - pitchAngleQ.Roll() + snapHeading);
						FlightCamera.fetch.camHdg = Mathf.Lerp(FlightCamera.fetch.camHdg, targetHeading, lerpRate);
					}
				}
			}
			if(FlightCamera.fetch.mode != FlightCamera.Modes.CHASE && enableChase) //runs once on switching out of chase cam
			{
				//setCam = false;
				enableChase = false;
			}
			#endregion
			
			
			
			
			#region Free chase mode
			if(FlightCamera.fetch.mode == FlightCamera.Modes.FREE && !MapView.MapIsEnabled && !FlightGlobals.ActiveVessel.isEVA)
			{
				Vessel v = FlightGlobals.ActiveVessel;
				Quaternion vesselRot = getSrfRotation(v);
				float headingRads = vesselRot.eulerAngles.y * Mathf.Deg2Rad;
				float pitchRads = ((vesselRot.eulerAngles.x > 180f) ? (360.0f - vesselRot.eulerAngles.x) : -vesselRot.eulerAngles.x) * Mathf.Deg2Rad;  //cred to r4m0n
				float rollDegrees = ((vesselRot.eulerAngles.z > 180f) ? (vesselRot.eulerAngles.z - 360.0f) : vesselRot.eulerAngles.z);
				
				Vector3 lookVector = Quaternion.Inverse(FlightGlobals.ActiveVessel.transform.rotation) * FlightGlobals.ActiveVessel.GetSrfVelocity();
				//lookvector X is left/right velocity, Y is forward velocity, Z is up/down velocity
				Vector3 forwardVector = new Vector3(0f, 1f, 0f);
				Quaternion rollAdjust = Quaternion.AngleAxis (rollDegrees, forwardVector);
				lookVector = rollAdjust * lookVector;
				
				Quaternion viewAngleQ = Quaternion.FromToRotation (forwardVector, lookVector);
				
				
				
				
				if(defaultOn && defaultFiredMode != "FREE")
				{
					enableFreeChase = true;
					defaultFiredMode = "FREE";
						
					ScreenMessages.RemoveMessage(chaseOn);
					ScreenMessages.RemoveMessage(chaseOff);
					ScreenMessages.RemoveMessage(freeChaseOn);
					ScreenMessages.RemoveMessage(freeChaseOff);
						
					ScreenMessages.PostScreenMessage(freeChaseOn, true);
					snapHeading = 0;
					snapPitch = defaultAngle*Mathf.Deg2Rad;
					FlightCamera.fetch.SetFoV(setFov);
					timeCheck = Time.time;
				}
				
				if(ENABLE_CHASE.GetKeyDown())
				{
					if(enableFreeChase)
					{
						ScreenMessages.RemoveMessage(chaseOn);
						ScreenMessages.RemoveMessage(chaseOff);
						ScreenMessages.RemoveMessage(freeChaseOn);
						ScreenMessages.RemoveMessage(freeChaseOff);
						
						ScreenMessages.PostScreenMessage(freeChaseOff, true);
						FlightCamera.fetch.SetFoV(FlightCamera.fetch.fovDefault);
					}
					if(!enableFreeChase)
					{
						ScreenMessages.RemoveMessage(chaseOn);
						ScreenMessages.RemoveMessage(chaseOff);
						ScreenMessages.RemoveMessage(freeChaseOn);
						ScreenMessages.RemoveMessage(freeChaseOff);
							
						ScreenMessages.PostScreenMessage(freeChaseOn, true);
						snapHeading = 0;
						snapPitch = defaultAngle*Mathf.Deg2Rad;
						FlightCamera.fetch.SetFoV(setFov);
						timeCheck = Time.time;


					}
					enableFreeChase = !enableFreeChase;
				}
				
				
				if(adjustLook)
				{
					if(!autoSnap)
					{

						if(!vtolMode)
						{
							snapPitch =  FlightCamera.fetch.camPitch - (0-pitchRads + (1f*viewAngleQ.Pitch ())); //adjusted
							snapHeading = FlightCamera.fetch.camHdg - (headingRads - (1f*viewAngleQ.Roll())); //adjusted
						}
						else
						{
							snapPitch = FlightCamera.fetch.camPitch - (0 + Mathf.Clamp ( viewAngleQ.Pitch ()/10, 0, 90f));
							snapHeading = FlightCamera.fetch.camHdg - (headingRads - (viewAngleQ.Roll()/10));
						}
						if(Mathf.Abs ( snapHeading) < 5*Mathf.Deg2Rad)  //snap heading straight if close enough
						{
							snapHeading = 0;
						}
					}
					else
					{
						snapHeading = 0;
						snapPitch = defaultAngle*Mathf.Deg2Rad;
						timeCheck = Time.time;
					}
					
				}
				
				float lerpRate;
				if(Time.time - timeCheck < 1 || v.srfSpeed<90)
				{
					lerpRate = 0.1f;
				}
				else
				{	
					lerpRate = 1;
				}
				
				if(enableFreeChase && FlightGlobals.ActiveVessel !=null && !adjustLook)
				{
					
					
					if(FlightGlobals.ActiveVessel.srf_velocity.magnitude>0.5){
						if(lookVector.y<35 && FlightGlobals.ActiveVessel.srfSpeed<50)//vtol mode when fwd velocity is less than 50m/s
						{
							if(!vtolMode){vtolMode = true;}
							lerpRate = 0.1f;
							FlightCamera.fetch.camPitch = Mathf.LerpAngle(FlightCamera.fetch.camPitch, (0 + Mathf.Clamp ( viewAngleQ.Pitch ()/10, 0, 90f) + snapPitch), lerpRate); 
							FlightCamera.fetch.camHdg = (Mathf.LerpAngle(FlightCamera.fetch.camHdg * Mathf.Rad2Deg, ((headingRads - (viewAngleQ.Roll()/10) + snapHeading) * Mathf.Rad2Deg), lerpRate)) * Mathf.Deg2Rad;
						}
						else
						{
							if(vtolMode){vtolMode = false;}
							FlightCamera.fetch.camPitch = Mathf.LerpAngle(FlightCamera.fetch.camPitch, 0-pitchRads + viewAngleQ.Pitch () + snapPitch, lerpRate);  //doesn't really need to be LerpAngle unless converted to degrees.
							FlightCamera.fetch.camHdg = (Mathf.LerpAngle(FlightCamera.fetch.camHdg * Mathf.Rad2Deg, (headingRads - viewAngleQ.Roll() + snapHeading) * Mathf.Rad2Deg, lerpRate)) * Mathf.Deg2Rad;
						}
					}
					
				}
			}
			
			
			
			if(FlightCamera.fetch.mode != FlightCamera.Modes.FREE && enableFreeChase)
			{
				enableFreeChase = false;	
			}
			#endregion
			
			if(FlightCamera.fetch.mode == FlightCamera.Modes.AUTO)
			{
				if(disableAuto)
				{
					FlightCamera.fetch.setMode(FlightCamera.GetAutoModeForVessel(FlightGlobals.ActiveVessel));	
					if(defaultOn && FlightCamera.fetch.mode == FlightCamera.Modes.FREE)
					{
						enableFreeChase = true;	
					}
					snapHeading = 0;
					snapPitch = defaultAngle*Mathf.Deg2Rad;
				}
				
				
				
				if(defaultFiredMode!="AUTO")
				{
					ScreenMessages.RemoveMessage(chaseOn);
					ScreenMessages.RemoveMessage(chaseOff);
					ScreenMessages.RemoveMessage(freeChaseOn);
					ScreenMessages.RemoveMessage(freeChaseOff);	
				}
			}
			
			#region IVA
			/*
			if(isIVA && !MapView.MapIsEnabled)
			{
				if(enableIVASnap)
				{
					adjustLookIVA = InternalCamera.Instance.mouseLocked;
					if(adjustLookIVA)
					{
						if(SET_IVA_SNAP.GetKeyDown())
						{
							snapIVARotation = InternalCamera.Instance.camera.transform.localRotation;	
						}
						if(ADJUST_LOOK.GetKeyUp())
						{
							InternalCamera.Instance.UnlockMouse();	
						}
					}
					else
					{
						InternalCamera.Instance.camera.transform.localRotation = snapIVARotation;
					}
				}
			}
			*/
			#endregion
			
			
		}
		
		
		private Quaternion getSrfRotation(Vessel vessel)  //credits to r4m0n -- modified
		{
		      Vector3d CoM;
		      Vector3d MoI;
		      Vector3d up;
		      Quaternion rotationSurface;
		      Quaternion rotationVesselSurface;
		
		      CoM = vessel.findWorldCenterOfMass();
		      MoI = vessel.findLocalMOI(CoM);
		      up = (CoM - vessel.mainBody.position).normalized;
		
		      Vector3d north = Vector3.Exclude(up, (vessel.mainBody.position + vessel.mainBody.transform.up * (float)vessel.mainBody.Radius) - CoM).normalized;
		      rotationSurface = Quaternion.LookRotation(north, up);
		      rotationVesselSurface = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.transform.rotation) * rotationSurface);
		
				//y = heading, x = pitch in degrees
			
			return rotationVesselSurface;
		}
		
		private void LoadConfig()
		{
			try
			{
				foreach(ConfigNode cfg in GameDatabase.Instance.GetConfigNodes("ImprovedChaseCameraConfig"))
				{
					if(cfg.HasNode("ENABLE_CHASE"))
					{
						ENABLE_CHASE.Load(cfg.GetNode("ENABLE_CHASE"));
					}
					if(cfg.HasValue("defaultAngle"))
					{
						defaultAngle = float.Parse(cfg.GetValue ("defaultAngle"));
					}
					if(cfg.HasValue("setFov"))
					{
						setFov = float.Parse(cfg.GetValue ("setFov"));
					}
					if(cfg.HasValue("defaultOn"))
					{
						defaultOn = bool.Parse(cfg.GetValue ("defaultOn"));	
					}
					if(cfg.HasValue("disableAuto"))
					{
						disableAuto = bool.Parse (cfg.GetValue("disableAuto"));
					}
					if(cfg.HasValue ("autoSnap"))
					{
						autoSnap = bool.Parse (cfg.GetValue("autoSnap"));
					}
					if(cfg.HasNode("SET_IVA_SNAP"))
					{
						SET_IVA_SNAP.Load(cfg.GetNode("SET_IVA_SNAP"));
					}
					if(cfg.HasValue ("enableIVASnap"))
					{
						enableIVASnap = bool.Parse (cfg.GetValue("enableIVASnap"));
					}
				}
			}
			catch(Exception e)
			{
				print ("Error loading config file:  " + e.ToString());	
			}
		}
		
		public void EnableMouseJoystick()
		{
			FlightGlobals.ActiveVessel.OnFlyByWire += new FlightInputCallback(MouseJoystick);
			mouseJoystickEnabled = true;
			ScreenMessages.RemoveMessage (msgMouseJoyOn);
			ScreenMessages.RemoveMessage (msgMouseJoyOff);
			ScreenMessages.PostScreenMessage(msgMouseJoyOn, true);
		}
		
		public void DisableMouseJoystick()
		{
			FlightGlobals.ActiveVessel.OnFlyByWire -= new FlightInputCallback(MouseJoystick);
			mouseJoystickEnabled = false;
			ScreenMessages.RemoveMessage (msgMouseJoyOn);
			ScreenMessages.RemoveMessage (msgMouseJoyOff);
			ScreenMessages.PostScreenMessage(msgMouseJoyOff, true);
		}
		
		public void MouseJoystick(FlightCtrlState s)
		{
			Vessel v = FlightGlobals.ActiveVessel;
			Quaternion vesselRot = getSrfRotation(v);
			float rollDegrees = ((vesselRot.eulerAngles.z > 180f) ? (vesselRot.eulerAngles.z - 360.0f) : vesselRot.eulerAngles.z);
			Quaternion rollAdjust = Quaternion.AngleAxis(rollDegrees, new Vector3(0, 0, -1));
			float circleRadius = Screen.height/4;
			Vector3 circleMousePos = new Vector3((Input.mousePosition.x-(Screen.width/2))/circleRadius, (Input.mousePosition.y-(Screen.height/2))/circleRadius, 0);
			Vector3 adjustedMousePos = rollAdjust * circleMousePos;
			if(enableFreeChase)
			{
				s.yaw = adjustedMousePos.x;
				s.pitch = adjustedMousePos.y;
			}
			if(enableChase || isIVA)
			{
				s.yaw = circleMousePos.x;
				s.pitch = circleMousePos.y;
			}
			
			
		}
		
	}
}

