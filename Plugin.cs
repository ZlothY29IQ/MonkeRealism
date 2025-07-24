using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Utilla;
using Utilla.Attributes;

namespace MonkeRealism
{
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]

    //Incompatibilities
    [BepInIncompatibility("com.zloth.recroomrig")]
    [BepInIncompatibility("??????????????????????")] //graze's recroomrig/bodyestimations
    [BepInIncompatibility("com.graze.gorillatag.analogturn")]
    [BepInIncompatibility("Graze.AnalogTurn-CI")]
    [BepInIncompatibility("Graze.AnalogTurn-GC")]

    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public ConfigEntry<string> trackerName;
        public static Plugin Instance { get; private set; }

        bool inRoom;
        float hipYRotationOffset = 0f;
        private float recenterHoldTime = 0f;
        private const float recenterThreshold = 4f;
        private Quaternion trackerOffset = Quaternion.identity;
        private float recenterYaw = 0f; 
        float yawOffset = 0f;



        void Start()
        {
            Utilla.Events.GameInitialized += OnGameInitialized;

            PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable()
            {
                {
                    PluginInfo.HashKey, PluginInfo.Version
                }
            });
        }

        void Awake()
        {
            trackerName = Config.Bind<string>(
                "Tracker Settings",        
                "Tracker",                 
                "WAIST",                   
                new ConfigDescription(
                    "Tracker to use.\n" +
                    "Recommended Usage: WAIST or CHEST.\n" +
                    "You can use any tracker you want as long as you know the serial name for it.\n" +
                    "I recommend using WAIST or CHEST only."
                )
            );

            Logger.LogInfo($"Using tracker: {trackerName.Value}");
        }

        void OnEnable()
        {
            HarmonyPatches.ApplyHarmonyPatches();
        }

        void OnDisable()
        {
            HarmonyPatches.RemoveHarmonyPatches();
        }

        void OnGameInitialized(object sender, EventArgs e)
        {
            TrackerManager.Initialize();
            TrackerManager.hipTrackerSerial = trackerName.Value.ToUpperInvariant();
            SetTurnModeToNone();
        }


        private void SetTurnModeToNone()        // https://github.com/DecalFree/GorillaInterface/blob/main/ComputerInterface/BaseGameInterface.cs
        {
            GorillaComputer computer = GameObject.FindObjectOfType<GorillaComputer>();
            if (computer == null)
            {
                Logger.LogError("Could not find GorillaComputer in the scene.");
                return;
            }

            var turnTypeField = typeof(GorillaComputer).GetField("turnType", BindingFlags.NonPublic | BindingFlags.Instance);
            if (turnTypeField != null)
                turnTypeField.SetValue(computer, "NONE");

            var turnValueField = typeof(GorillaComputer).GetField("turnValue", BindingFlags.NonPublic | BindingFlags.Instance);
            int turnValue = turnValueField != null ? (int)turnValueField.GetValue(computer) : 0;

            PlayerPrefs.SetString("stickTurning", "NONE");
            PlayerPrefs.Save();

            var snapTurn = GorillaTagger.Instance.GetComponent<GorillaSnapTurn>();
            if (snapTurn != null)
            {
                snapTurn.ChangeTurnMode("NONE", turnValue);
            }
            else
            {
                Logger.LogWarning("GorillaSnapTurn not found on GorillaTagger.");
            }

            Logger.LogInfo("Turn mode set to NONE.");
        }



        void Update()
        {
            if (!inRoom || GTPlayer.Instance == null || GTPlayer.Instance.headCollider == null)
                return;

            if (ControllerInputPoller.instance.leftControllerSecondaryButton)
            {
                recenterHoldTime += Time.deltaTime;

                if (recenterHoldTime >= recenterThreshold)
                {
                    Quaternion? hipRot = TrackerManager.GetHipTrackerRotation();
                    if (hipRot.HasValue)
                    {
                        float hipYaw = hipRot.Value.eulerAngles.y;
                        float headsetYaw = GorillaTagger.Instance.mainCamera.transform.rotation.eulerAngles.y;
                        yawOffset = Mathf.DeltaAngle(-hipYaw, headsetYaw);



                        Debug.Log($"[MonkeRealism] Recentered. HipYaw: {hipYaw}, HeadsetYaw: {headsetYaw}, YawOffset: {yawOffset}");
                    }

                    recenterHoldTime = -999f; 
                }
            }
            else
            {
                recenterHoldTime = 0f;
            }

            Quaternion? hipRotation = TrackerManager.GetHipTrackerRotation();
            if (hipRotation == null)
                return;

            float hipCurrentYaw = hipRotation.Value.eulerAngles.y;

            float adjustedYaw = -hipCurrentYaw + yawOffset;

            adjustedYaw = (adjustedYaw + 360f) % 360f;

            Transform headCollider = GTPlayer.Instance.headCollider.transform;
            Vector3 currentEuler = headCollider.rotation.eulerAngles;

            headCollider.rotation = Quaternion.Euler(currentEuler.x, adjustedYaw, currentEuler.z);
        }





        [ModdedGamemodeJoin]
        public void OnJoin(string gamemode)
        {
            inRoom = true;
        }

        [ModdedGamemodeLeave]
        public void OnLeave(string gamemode)
        {
            inRoom = false;
        }
    }
}
