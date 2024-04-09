using BepInEx;
using R2API;
using RoR2;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using System.Linq;
using System.IO;

namespace StatsMod
{

    // don't touch these
    [BepInDependency(LanguageAPI.PluginGUID)]

    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class StatsMod : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "pond";
        public const string PluginName = "StatsMod";
        public const string PluginVersion = "1.0.0";

        private List<PlayerStatsDatabase> StatsDatabase;
        
        public void Awake() // Run at the very start when the game is initialized.
        {
            Log.Init(Logger); // Init our logging class so that we can properly log for debugging

            Enable();
            // On.RoR2.Networking.NetworkManagerSystemSteam.OnClientConnect += (s, u, t) => { };  // This just allows connecting to a local server (for multiplayer testing with only one device)
        }

        private void Enable()  // When this method is called, enabling all mod features
        {
            CustomStatsTracker.Enable();
            Run.onRunStartGlobal += OnRunStart;
            SceneExitController.onBeginExit += OnBeginExit;
            Run.onServerGameOver += OnRunEnd;
        }

        private void Disable() // When this method is called, disabling all mod features
        {
            CustomStatsTracker.Disable();
            Run.onRunStartGlobal -= OnRunStart;
            SceneExitController.onBeginExit -= OnBeginExit;
            Run.onServerGameOver -= OnRunEnd;
        }

        private void Update() // This method is called on every frame of the game.
        {
            if (Input.GetKeyDown(KeyCode.F2) & NetworkServer.active) { ReportToLog(); }// TEST: Allows for easy testing
            else if (Input.GetKeyDown(KeyCode.F3) & NetworkServer.active) { TakeRecord(); }
        }

        // Event and hooking methods

        private void OnRunStart(Run run) // Empties all the data dictionaries & sets up a new database
        {
            if (!NetworkServer.active) { return; }

            Log.Info("New run, resetting data dicts and database");

            CustomStatsTracker.ResetData();
            SetupDatabase();

            CharacterBody.onBodyStartGlobal += OnBodyStart;
        }

        private int bodiesCounter = 0; 
        private void OnBodyStart(CharacterBody self) // For a record to be taken at the start of each stage it is ensured that the body for each player exists
        {
            if (!NetworkServer.active) { return; }

            if (self.isPlayerControlled)
            {
                bodiesCounter++;
                if (bodiesCounter == StatsDatabase.Count)
                {
                    CharacterBody.onBodyStartGlobal -= OnBodyStart; // Avoids this method being called after a record has been made for the stage
                    TakeRecord();
                }
            }
        }

        private void OnBeginExit(SceneExitController x) // Sets up for a new record to be made by OnBodyStart on the next stage
        {
            if (!NetworkServer.active) { return; }

            bodiesCounter = 0;
            CharacterBody.onBodyStartGlobal += OnBodyStart;
        }     

        private void OnRunEnd(Run x, GameEndingDef y)
        {
            if (!NetworkServer.active) { return; }

            ReportToLog(); // TEST: Automatically logs the end of game stats
        }

        // Misc methods

        private void SetupDatabase() // Sets up StatsDatabase for the players of a new run
        {
            if (!NetworkServer.active) { return; }

            StatsDatabase = [];
            foreach (PlayerCharacterMasterController player in PlayerCharacterMasterController.instances) 
            { 
                StatsDatabase.Add(new PlayerStatsDatabase(player));
            }
            Log.Info($"Successfully setup full database for {StatsDatabase.Count} players");
        }

        private void TakeRecord() // Takes a record in StatsDatabase of each players associated stats
        {
            if (!NetworkServer.active) { return; }

            foreach (PlayerStatsDatabase i in StatsDatabase)
            {
                float timestamp = i.TakeRecord();
                Log.Info($"Successfully made record at {timestamp} for {i.GetPlayerName()}");
            }
        }

        private void ReportToLog() // Makes a nice report of all the recorded stats to the log.
        {
            if (!NetworkServer.active) { return; }

            StringBuilder a = new();
            foreach (PlayerStatsDatabase i in StatsDatabase)
            {
                a.AppendLine($"{i.GetPlayerName()}");
                i.GetStatSeriesAsString("timestamp");
                foreach (string j in PlayerStatsDatabase.allStats)
                {
                    a.AppendLine(i.GetStatSeriesAsString(j));
                }
                a.AppendLine("");
            }
            Log.Info(a.ToString());
        }

        // Old implementation of the shrine hit method using hooking
        /*
        // Tracks all the times shrines are hit. This method is only ever called on the host side
        private void ShrineTrack(On.RoR2.ShrineChanceBehavior.orig_AddShrineStack orig, global::RoR2.ShrineChanceBehavior self, Interactor activator)
        {
            var networkPlayer = activator.GetComponent<CharacterBody>().master.playerCharacterMasterController.networkUser;  // Getting the networkUser (unique identification in multiplayer), calling it networkPlayer
            if (!shrinePurchases.ContainsKey(networkPlayer))  // If networkPlayer isn't in the shrinePurchases dictionary, adding them. Otherwise, incrementing counter by 1
            {
                shrinePurchases.Add(networkPlayer, 1);
            }
            else
            {
                shrinePurchases[networkPlayer]++;
            }

            Log.Info(shrinePurchases[networkPlayer]); // Just for demonstration purposes, you can see in the log that the counter goes up seperately for each player and is maintained across stages
            Log.Info(networkPlayer);

            orig(self, activator);  // Calling original method

        }
        */

        // Old test code for the spawning a behemoth on every bullet spawn
        /*
        private static bool OnShot(On.RoR2.BulletAttack.orig_DefaultHitCallbackImplementation orig, BulletAttack self, ref BulletAttack.BulletHit hitInfo)
        {
            if (NetworkServer.active)
            {
                Log.Info(LocalUserManager.GetFirstLocalUser().cachedBody);
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2Content.Items.Behemoth.itemIndex), transform.position, transform.up * 20f);
            }
            bool OrigReturn =  orig(self, ref hitInfo);

            return OrigReturn;
        }
        */

        // Old test code for spawning a behemoth and killing the player on f2 press
        /*
        // The Update() method is run on every frame of the game.
        private void Update()
        {

            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                Log.Info("Changes were applied");

                PlayerCharacterMasterController.instances[0].master.TrueKill();
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2Content.Items.Behemoth.itemIndex), transform.position, transform.forward * 20f);
            }
            
        }
        */

    }


}
