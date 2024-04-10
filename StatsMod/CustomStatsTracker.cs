﻿using RoR2;
using RoR2.Stats;
using BepInEx;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Text;

namespace StatsMod
{
    public static class CustomStatsTracker
    {
        // This class implements and holds the values of the custom stats
        private static Dictionary<PlayerCharacterMasterController, uint> shrinePurchases = [];  // Dictionary for recording how many times each player has used a shrine of chance
        private static Dictionary<PlayerCharacterMasterController, uint> shrineWins = [];  // Dictionary for recording how many times each player has won a shrine of chance
        private static Dictionary<PlayerCharacterMasterController, uint> orderHits = [];  // Dictionary for recording how many times each player has used a shrine of order
        private static Dictionary<PlayerCharacterMasterController, uint> timeStill = [];  // Dictionary for recording how long each player has been standing still
        private static Dictionary<PlayerCharacterMasterController, uint> timeStillPreTP = [];  // Dictionary for recording how long each player has been standing still BEFORE the end of TP event

        public static void Enable()
        {
            ShrineChanceBehavior.onShrineChancePurchaseGlobal += ShrineTrack;
            On.RoR2.ShrineRestackBehavior.AddShrineStack += OrderTrack;
            On.RoR2.Run.OnFixedUpdate += stillTrack;
        }

        public static void Disable()
        {
            ShrineChanceBehavior.onShrineChancePurchaseGlobal -= ShrineTrack;
            On.RoR2.ShrineRestackBehavior.AddShrineStack -= OrderTrack;
            On.RoR2.Run.OnFixedUpdate -= stillTrack;
        }

        public static void ResetData()
        {
            shrinePurchases = [];
            shrineWins = [];
            orderHits = [];
            timeStill = [];
            timeStillPreTP = [];
        }

        public static uint GetStat(PlayerCharacterMasterController player, string statName)
        {
            try
            {
                switch (statName)
                {

                    case "shrinePurchases":
                        return shrinePurchases[player];

                    case "shrineWins":
                        return shrineWins[player];

                    case "orderHits":
                        return orderHits[player];
                    case "timeStill":
                        return timeStill[player];

                    case "timeStillPreTP":
                        return timeStillPreTP[player];

                    default:
                        Log.Error("Cannot find specified custom stat, returning 0");
                        return 0;

                }
            }
            catch (KeyNotFoundException) { return 0; }  // If a player is not in a dict., then it is because that stat is 0
        }

        private static void ShrineTrack(bool failed, Interactor activator)
        {
            var player = activator.GetComponent<CharacterBody>().master.playerCharacterMasterController;  // Getting the networkUser (unique identification in multiplayer), calling it player
            if (!shrinePurchases.ContainsKey(player))  // If player isn't in the shrinePurchases dictionary, adding them. Otherwise, incrementing counter by 1
            {
                shrinePurchases.Add(player, 1);
                shrineWins.Add(player, 0);
            }
            else
            {
                shrinePurchases[player]++;
            }

            if (!failed)  // If won shrine, increment won counter
            {
                shrineWins[player]++;
            }

        }

        // Counting how many times a player has hit a shrine of order
        static void OrderTrack(On.RoR2.ShrineRestackBehavior.orig_AddShrineStack orig, ShrineRestackBehavior self, Interactor interactor)
        {
            var player = interactor.GetComponent<CharacterBody>().master.playerCharacterMasterController;  // Getting the networkUser (unique identification in multiplayer), calling it player
            if (!orderHits.ContainsKey(player))  // If player isn't in the orderHits dictionary, adding them. Otherwise, incrementing counter by 1
            {
                orderHits.Add(player, 1);
            }
            else
            {
                orderHits[player]++;
            }

            orig(self, interactor);
        }

        // Counting how long a player has stopped moving for
        private static void stillTrack(On.RoR2.Run.orig_OnFixedUpdate orig, Run self)
        {
            if (NetworkServer.active && (PlayerCharacterMasterController.instances.Count > 0))  // These checks may not be necessary but I am too lazy to confirm, it works at least
            {
                foreach (PlayerCharacterMasterController player in PlayerCharacterMasterController.instances)
                {
                    try
                    {
                        var isStill = player.master.GetBody().GetNotMoving();
                        var preTP = TeleporterInteraction.instance.isCharged;
                        if (isStill)
                        {
                            try
                            {
                                timeStill[player]++;
                                if (preTP)
                                {
                                    timeStillPreTP[player]++;
                                }
                                
                            }
                            catch (KeyNotFoundException) 
                            {
                                timeStill.Add(player, 1);
                                if (preTP)
                                {
                                    timeStillPreTP.Add(player, 1);
                                }
                                else
                                {
                                    timeStillPreTP.Add(player, 0);
                                }
                            }

                            //Log.Info(timeStill[player] * Time.fixedDeltaTime);  // FixedUpdate is called a different amount of times depending on the framerate. Time.fixedDeltaTime is the frequency that it is called
                        }
                    }
                    catch (NullReferenceException) { }
                     // Player may be dead, or not properly spawned yet
                }
            }
            orig(self);
        }
    }
}
