﻿/*
 * Idmr.Platform.dll, X-wing series mission library file, XW95-XWA
 * Copyright (C) 2009-2021 Michael Gaisser (mjgaisser@gmail.com)
 * Licensed under the MPL v2.0 or later
 * 
 * Full notice in ../help/Idmr.Platform.chm
 * Version: 5.4
 */

/* CHANGELOG
 * v5.4, 210404
 * [FIX] TIE goal check exception type
 * [FIX YOGEME#55] FG goal amounts when converting XW [JB]
 * v5.3, 210328
 * [FIX YOGEME#53] removed zoom multiplier in XW-XWA BRF MoveMap event
 * [FIX] Strip out Title strings from XW Description conversion
 * [FIX YOGEME#51] Removed auto-add of Page Break event when XW ClearText detected
 * [FIX YOGEME#51] in XW BRF conversion, skip over "None" events
 * v4.0, 200809
 * [UPD] cleanup
 * v3.0, 180309
 * [UPD] capped TIE AI [JB]
 * [FIX] TIE PlayerCraft [JB]
 * [FIX] loop error in TIE Briefing.Events [JB]
 * [FIX] Message.Delay from XWA [JB]
 * [NEW] XWing upgrade conversions with various helper functions [JB]
 * v2.6, 160606
 * [FIX] XWA to TIE player craft
 * v2.3, 150405
 * [UPD] XvT.Globals.Goal.Trigger implementation
 * v2.1, 141214
 * [UPD] change to MPL
 * v2.0, 120525
 * - rewrote Trigger and Waypoint conversions
 * - rewrote for Waypoint interface removal
 * - removed craftCheck() to use *.Mission.CraftCheck
 * - XvtBopToTie: Briefing.Events to short[]
 */
using System;
using System.Collections.Generic;

namespace Idmr.Platform
{
    /// <summary>Object for Mission Platform conversions</summary>
    /// <remarks>Primarily handles downgrading of platforms, due to existing utilities for upgrading<br/>
    /// Converted files will use same MissionPath with platform included ("test.tie" to "test_Xvt.tie")</remarks>
    public static class Converter
    {
        /// <summary>Downgrades XvT and BoP missions to TIE95</summary>
        /// <remarks>G/PLT, SHPYD, REPYD and M/SC craft will have their indexes changed to reflect IDMR TIE95 Ships patch numbering. Triggers and orders will update.<br/>
        /// FG.Radio is not converted, since TIE behaviour is different<br/>
        /// Maximum FG.Formation value of 12 allowed<br/>
        /// AI level capped at Top Ace<br/>
        /// For Triggers, maximum Trigger index of 24, maximum VariableType of 9, Amounts will be adjusted as 66% to 75%, 33% to 50% and "each" to 100%<br/>
        /// Maximum Abort index of 5<br/>
        /// Maximum FG.Goal Amount index of 6, 75% converted to 100%, 25% to 50%. First three XvT Goals will be used as Primary, Secondary and Bonus goals. Bonus points will be scaled appropriately. Goals only used if set for Team[0] and Enabled<br/>
        /// First two Arrival triggers used, first Departure trigger used. First three Orders used. All standard WPs and first Briefing WP used.<br/>
        /// For Messages, first two triggers used.<br/>
        /// For the Briefing, entire thing should be able to be used unless the original actually uses close to 200 commands (yikes). There is a conversion on the Zoom factor, this is a legacy factor from my old Converter program, I don't remember why.<br/>
        /// Primary Global goals used, XvT Secondary goals converted to Bonus goals. Prevent goals ignored<br/>
        /// Team[0] EndOfMissionMessages used, Teams[2-6] Name and Hostility towards Team[0] used for IFF<br/>
        /// BriefingQuestions generated using MissionSucc/Fail/Desc strings. Flight Officer has a single pre-mission entry for the Description, two post-mission entries for the Success and Fail. Line breaks must be entered manually<br/>
        /// Filename will end in "_TIE.tie"</remarks>
        /// <param name="miss">XvT/BoP mission to convert</param>
        /// <returns>Downgraded mission</returns>
        /// <exception cref="ArgumentException">Properties incompatable with TIE95 were detected in <paramref name="miss"/></exception>
        public static Tie.Mission XvtBopToTie(Xvt.Mission miss)
        {
            Tie.Mission tie = new Tie.Mission();
            // FG limit is okay, since XvT < TIE for some reason
            if (miss.Messages.Count > Tie.Mission.MessageLimit) throw maxException(true, false, Tie.Mission.MessageLimit);
            tie.FlightGroups = new Tie.FlightGroupCollection(miss.FlightGroups.Count);
            if (miss.Messages.Count > 0) tie.Messages = new Tie.MessageCollection(miss.Messages.Count);
            #region FGs
            for (int i = 0; i < tie.FlightGroups.Count; i++)
            {
                #region Craft
                // Radio is omitted intentionally
                tie.FlightGroups[i].Name = miss.FlightGroups[i].Name;
                tie.FlightGroups[i].Cargo = miss.FlightGroups[i].Cargo;
                tie.FlightGroups[i].SpecialCargo = miss.FlightGroups[i].SpecialCargo;
                tie.FlightGroups[i].SpecialCargoCraft = miss.FlightGroups[i].SpecialCargoCraft;
                tie.FlightGroups[i].RandSpecCargo = miss.FlightGroups[i].RandSpecCargo;
                tie.FlightGroups[i].CraftType = Tie.Mission.CraftCheck(miss.FlightGroups[i].CraftType);
                if (tie.FlightGroups[i].CraftType == 255) throw flightException(4, i, Xwa.Strings.CraftType[miss.FlightGroups[i].CraftType]);
                tie.FlightGroups[i].NumberOfCraft = miss.FlightGroups[i].NumberOfCraft;
                tie.FlightGroups[i].Status1 = miss.FlightGroups[i].Status1;
                tie.FlightGroups[i].Missile = miss.FlightGroups[i].Missile;
                tie.FlightGroups[i].Beam = miss.FlightGroups[i].Beam;
                tie.FlightGroups[i].IFF = miss.FlightGroups[i].IFF;
                tie.FlightGroups[i].AI = miss.FlightGroups[i].AI;
                if (tie.FlightGroups[i].AI > 4) tie.FlightGroups[i].AI = 4;  //[JB] Jedi in XvT should be Top Ace in TIE, not invul.
                tie.FlightGroups[i].Markings = miss.FlightGroups[i].Markings;
                if (miss.FlightGroups[i].Formation > 12) throw flightException(1, i, Xwa.Strings.Formation[miss.FlightGroups[i].Formation]);
                else tie.FlightGroups[i].Formation = miss.FlightGroups[i].Formation;
                tie.FlightGroups[i].FormDistance = miss.FlightGroups[i].FormDistance;
                tie.FlightGroups[i].GlobalGroup = miss.FlightGroups[i].GlobalGroup;
                tie.FlightGroups[i].FormLeaderDist = miss.FlightGroups[i].FormLeaderDist;
                tie.FlightGroups[i].NumberOfWaves = miss.FlightGroups[i].NumberOfWaves;
                tie.FlightGroups[i].PlayerCraft = (byte)(miss.FlightGroups[i].PlayerNumber == 1 ? miss.FlightGroups[i].PlayerCraft + 1 : 0);  //[JB] Fixed player craft assignment.  Check for XvT player slot #1 (not the craft slot).  Then make sure the craft slot is non-zero (since XvT default is zero)
                tie.FlightGroups[i].Yaw = miss.FlightGroups[i].Yaw;
                tie.FlightGroups[i].Pitch = miss.FlightGroups[i].Pitch;
                tie.FlightGroups[i].Roll = miss.FlightGroups[i].Roll;
                #endregion Craft
                #region ArrDep
                tie.FlightGroups[i].Difficulty = miss.FlightGroups[i].Difficulty;
                for (int j = 0; j < 3; j++)
                {
                    try { tie.FlightGroups[i].ArrDepTriggers[j] = (Tie.Mission.Trigger)miss.FlightGroups[i].ArrDepTriggers[j]; }
                    catch (Exception x) { throw new ArgumentException("FG[" + i + "] ArrDep[" + j + "]: " + x.Message, x); }
                }
                tie.FlightGroups[i].AT1AndOrAT2 = miss.FlightGroups[i].ArrDepAO[0];
                tie.FlightGroups[i].ArrivalDelayMinutes = miss.FlightGroups[i].ArrivalDelayMinutes;
                tie.FlightGroups[i].ArrivalDelaySeconds = miss.FlightGroups[i].ArrivalDelaySeconds;
                tie.FlightGroups[i].DepartureTimerMinutes = miss.FlightGroups[i].DepartureTimerMinutes;
                tie.FlightGroups[i].DepartureTimerSeconds = miss.FlightGroups[i].DepartureTimerSeconds;
                if (miss.FlightGroups[i].AbortTrigger > 5) throw flightException(2, i, Xwa.Strings.Abort[miss.FlightGroups[i].AbortTrigger]);
                else tie.FlightGroups[i].AbortTrigger = miss.FlightGroups[i].AbortTrigger;
                tie.FlightGroups[i].ArrivalCraft1 = miss.FlightGroups[i].ArrivalCraft1;
                tie.FlightGroups[i].ArrivalMethod1 = miss.FlightGroups[i].ArrivalMethod1;
                tie.FlightGroups[i].ArrivalCraft2 = miss.FlightGroups[i].ArrivalCraft2;
                tie.FlightGroups[i].ArrivalMethod2 = miss.FlightGroups[i].ArrivalMethod2;
                tie.FlightGroups[i].DepartureCraft1 = miss.FlightGroups[i].DepartureCraft1;
                tie.FlightGroups[i].DepartureMethod1 = miss.FlightGroups[i].DepartureMethod1;
                tie.FlightGroups[i].DepartureCraft2 = miss.FlightGroups[i].DepartureCraft2;
                tie.FlightGroups[i].DepartureMethod2 = miss.FlightGroups[i].DepartureMethod2;
                #endregion ArrDep
                #region Goals
                if (miss.FlightGroups[i].Goals[0].GetEnabledForTeam(0))
                {
                    tie.FlightGroups[i].Goals.PrimaryCondition = miss.FlightGroups[i].Goals[0].Condition;
                    tie.FlightGroups[i].Goals.PrimaryAmount = miss.FlightGroups[i].Goals[0].Amount;
                }
                if (miss.FlightGroups[i].Goals[1].GetEnabledForTeam(0))
                {
                    tie.FlightGroups[i].Goals.SecondaryCondition = miss.FlightGroups[i].Goals[1].Condition;
                    tie.FlightGroups[i].Goals.SecondaryAmount = miss.FlightGroups[i].Goals[1].Amount;
                }
                if (miss.FlightGroups[i].Goals[2].GetEnabledForTeam(0))
                {
                    tie.FlightGroups[i].Goals.BonusCondition = miss.FlightGroups[i].Goals[2].Condition;
                    tie.FlightGroups[i].Goals.BonusAmount = miss.FlightGroups[i].Goals[2].Amount;
                    tie.FlightGroups[i].Goals.RawBonusPoints = miss.FlightGroups[i].Goals[2].RawPoints;
                }
                tieGoalsCheck("FlightGroup " + i, tie.FlightGroups[i].Goals);
                #endregion Goals
                for (int j = 0; j < 3; j++)
                {
                    try { tie.FlightGroups[i].Orders[j] = (Tie.FlightGroup.Order)miss.FlightGroups[i].Orders[j]; }
                    catch (Exception x) { throw new ArgumentException("FG[" + i + "] Order[" + j + "]: " + x.Message, x); }
                }
                for (int j = 0; j < 15; j++)
                    tie.FlightGroups[i].Waypoints[j] = miss.FlightGroups[i].Waypoints[j];
            }
            #endregion FGs
            #region Messages
            for (int i = 0; i < tie.Messages.Count; i++)
            {
                tie.Messages[i].MessageString = miss.Messages[i].MessageString;
                tie.Messages[i].Color = miss.Messages[i].Color;
                tie.Messages[i].Delay = miss.Messages[i].Delay;
                tie.Messages[i].Short = miss.Messages[i].Note;
                tie.Messages[i].Trig1AndOrTrig2 = miss.Messages[i].T1AndOrT2;
                for (int j = 0; j < 2; j++)
                {
                    try { tie.Messages[i].Triggers[j] = (Tie.Mission.Trigger)miss.Messages[i].Triggers[j]; }
                    catch (Exception x) { throw new ArgumentException("Mess[" + i + "] T[" + j + "]: " + x.Message, x); }
                }
            }
            #endregion Messages
            #region Briefing
            for (int i = 0; i < tie.Briefing.BriefingTag.Length; i++) tie.Briefing.BriefingTag[i] = miss.Briefings[0].BriefingTag[i];
            for (int i = 0; i < tie.Briefing.BriefingString.Length; i++) tie.Briefing.BriefingString[i] = miss.Briefings[0].BriefingString[i];
            tie.Briefing.Unknown1 = miss.Briefings[0].Unknown1;
            tie.Briefing.Length = (short)(miss.Briefings[0].Length * Tie.Briefing.TicksPerSecond / Xvt.Briefing.TicksPerSecond);
            for (int i = 0; i < tie.Briefing.Events.Length;)
            {
                short time = miss.Briefings[0].Events[i];
                short evnt = miss.Briefings[0].Events[i + 1];
                tie.Briefing.Events[i + 1] = evnt;
                if (time == 9999 && evnt == 0x22)
                {
                    tie.Briefing.Events[i] = time;
                    break;
                }
                tie.Briefing.Events[i] = (short)(time * Tie.Briefing.TicksPerSecond / Xvt.Briefing.TicksPerSecond);
                i += 2;
                for (int j = 0; j < tie.Briefing.EventParameterCount(evnt); j++, i++)
                    tie.Briefing.Events[i] = miss.Briefings[0].Events[i];
            }
            #endregion Briefing
            #region Globals
            tie.GlobalGoals.Goals[0].T1AndOrT2 = miss.Globals[0].Goals[0].T1AndOrT2;    // Primary
            tie.GlobalGoals.Goals[2].T1AndOrT2 = miss.Globals[0].Goals[2].T1AndOrT2;    // Secondary to Bonus, Prevent will be ignored
            for (int j = 0; j < 4; j++)
            {
                try { tie.GlobalGoals.Goals[j / 2 * 2].Triggers[j % 2] = (Tie.Mission.Trigger)miss.Globals[0].Goals[j / 2 * 2].Triggers[j % 2].GoalTrigger; }
                catch (Exception x) { throw new ArgumentException("Goal[" + (j / 2 * 2) + "] T[" + (j % 2) + "]: " + x.Message, x); }
            }
            #endregion Globals
            #region IFF/Team
            for (int i = 0; i < 6; i++) tie.EndOfMissionMessages[i] = miss.Teams[0].EndOfMissionMessages[i];
            for (int i = 2; i < 6; i++)
            {
                tie.IFFs[i] = miss.Teams[i].Name;
                tie.IffHostile[i] = !miss.Teams[0].AlliedWithTeam[i];
            }
            #endregion IFF/Team
            #region Questions
            if (miss.MissionDescription != "")
            {
                tie.BriefingQuestions.PreMissQuestions[0] = "What are the mission objectives?";
                tie.BriefingQuestions.PreMissAnswers[0] = miss.MissionDescription;  // line breaks will have to be manually placed
            }
            if (miss.MissionSuccessful != "")
            {
                tie.BriefingQuestions.PostMissQuestions[0] = "What have I accomplished?";
                tie.BriefingQuestions.PostMissAnswers[0] = miss.MissionSuccessful;  // again, line breaks
                tie.BriefingQuestions.PostTrigger[0] = 4;
                tie.BriefingQuestions.PostTrigType[0] = 1;
            }
            if (miss.MissionFailed != "")
            {
                tie.BriefingQuestions.PostMissQuestions[1] = "Any suggestions?";
                tie.BriefingQuestions.PostMissAnswers[1] = miss.MissionFailed;  // again, line breaks
                tie.BriefingQuestions.PostTrigger[1] = 5;
                tie.BriefingQuestions.PostTrigType[1] = 1;
            }
            #endregion Questions
            tie.MissionPath = miss.MissionPath.ToUpper().Replace(".TIE", "_TIE.tie");
            return tie;
        }

        /// <summary>Downgrades XWA missions to XvT and BoP</summary>
        /// <remarks>Maximum CraftType of 91. Triggers will update.<br/>
        /// For Triggers, maximum Trigger index of 46, maximum VariableType of 23, Amounts will be adjusted as "each special" to "100% special"<br/>
        /// Only Start and Hyp WPs converted, manual placement for WP1-8 required.<br/>
        /// For the Briefing, first 32 strings and text tags are copied, events are ignored (due to using icons instead of Craft)<br/>
        /// Filename will end in "_XvT.tie" or "_.BoP.tie"</remarks>
        /// <param name="miss">XWA mission to convert</param>
        /// <param name="bop">Determines if mission is to be converted to BoP instead of XvT</param>
        /// <returns>Downgraded mission</returns>
        /// <exception cref="ArgumentException">Properties incompatable with XvT/BoP were detected in <paramref name="miss"/></exception>
        public static Xvt.Mission XwaToXvtBop(Xwa.Mission miss, bool bop)
        {
            Xvt.Mission xvt = new Xvt.Mission
            {
                IsBop = bop
            };
            if (miss.FlightGroups.Count > Xvt.Mission.FlightGroupLimit) throw maxException(false, true, Xvt.Mission.FlightGroupLimit);
            if (miss.Messages.Count > Xvt.Mission.MessageLimit) throw maxException(false, false, Xvt.Mission.MessageLimit);
            xvt.FlightGroups = new Xvt.FlightGroupCollection(miss.FlightGroups.Count);
            if (miss.Messages.Count > 0) xvt.Messages = new Xvt.MessageCollection(miss.Messages.Count);
            xvt.MissionDescription = miss.MissionDescription;
            xvt.MissionFailed = miss.MissionFailed;
            xvt.MissionSuccessful = miss.MissionSuccessful;
            #region FGs
            for (int i = 0; i < xvt.FlightGroups.Count; i++)
            {
                #region craft
                xvt.FlightGroups[i].Name = miss.FlightGroups[i].Name;
                xvt.FlightGroups[i].Cargo = miss.FlightGroups[i].Cargo;
                xvt.FlightGroups[i].SpecialCargo = miss.FlightGroups[i].SpecialCargo;
                xvt.FlightGroups[i].SpecialCargoCraft = miss.FlightGroups[i].SpecialCargoCraft;
                xvt.FlightGroups[i].RandSpecCargo = miss.FlightGroups[i].RandSpecCargo;
                xvt.FlightGroups[i].CraftType = Xvt.Mission.CraftCheck(miss.FlightGroups[i].CraftType);
                if (xvt.FlightGroups[i].CraftType == 255) throw flightException(4, i, Xwa.Strings.CraftType[miss.FlightGroups[i].CraftType]);
                xvt.FlightGroups[i].NumberOfCraft = miss.FlightGroups[i].NumberOfCraft;
                if (xvt.FlightGroups[i].Status1 > 21) throw flightException(0, i, Xwa.Strings.Status[miss.FlightGroups[i].Status1]);
                xvt.FlightGroups[i].Status1 = miss.FlightGroups[i].Status1;
                if (xvt.FlightGroups[i].Status2 > 21) throw flightException(0, i, Xwa.Strings.Status[miss.FlightGroups[i].Status2]);
                xvt.FlightGroups[i].Status2 = miss.FlightGroups[i].Status2;
                xvt.FlightGroups[i].Missile = miss.FlightGroups[i].Missile;
                xvt.FlightGroups[i].Beam = miss.FlightGroups[i].Beam;
                xvt.FlightGroups[i].IFF = miss.FlightGroups[i].IFF;
                xvt.FlightGroups[i].Team = miss.FlightGroups[i].Team;
                xvt.FlightGroups[i].AI = miss.FlightGroups[i].AI;
                xvt.FlightGroups[i].Markings = miss.FlightGroups[i].Markings;
                xvt.FlightGroups[i].Radio = miss.FlightGroups[i].Radio;
                xvt.FlightGroups[i].Formation = miss.FlightGroups[i].Formation;
                xvt.FlightGroups[i].FormDistance = miss.FlightGroups[i].FormDistance;
                xvt.FlightGroups[i].GlobalGroup = miss.FlightGroups[i].GlobalGroup;
                xvt.FlightGroups[i].FormLeaderDist = miss.FlightGroups[i].FormLeaderDist;
                xvt.FlightGroups[i].NumberOfWaves = miss.FlightGroups[i].NumberOfWaves;
                xvt.FlightGroups[i].Unknowns.Unknown1 = miss.FlightGroups[i].Unknowns.Unknown3;
                xvt.FlightGroups[i].PlayerNumber = miss.FlightGroups[i].PlayerNumber;
                xvt.FlightGroups[i].ArriveOnlyIfHuman = miss.FlightGroups[i].ArriveOnlyIfHuman;
                xvt.FlightGroups[i].PlayerCraft = miss.FlightGroups[i].PlayerCraft;
                xvt.FlightGroups[i].Yaw = miss.FlightGroups[i].Yaw;
                xvt.FlightGroups[i].Pitch = miss.FlightGroups[i].Pitch;
                xvt.FlightGroups[i].Roll = miss.FlightGroups[i].Roll;
                #endregion craft
                #region ArrDep
                xvt.FlightGroups[i].Difficulty = miss.FlightGroups[i].Difficulty;
                for (int j = 0; j < 6; j++)
                {
                    try { xvt.FlightGroups[i].ArrDepTriggers[j] = (Xvt.Mission.Trigger)miss.FlightGroups[i].ArrDepTriggers[j]; }
                    catch (Exception x) { throw new ArgumentException("FG[" + i + "] ArrDep[" + j + "]: " + x.Message, x); }
                }
                for (int j = 0; j < 4; j++) xvt.FlightGroups[i].ArrDepAO[j] = miss.FlightGroups[i].ArrDepAndOr[j];
                xvt.FlightGroups[i].ArrivalDelayMinutes = miss.FlightGroups[i].ArrivalDelayMinutes;
                xvt.FlightGroups[i].ArrivalDelaySeconds = miss.FlightGroups[i].ArrivalDelaySeconds;
                xvt.FlightGroups[i].DepartureTimerMinutes = miss.FlightGroups[i].DepartureTimerMinutes;
                xvt.FlightGroups[i].DepartureTimerSeconds = miss.FlightGroups[i].DepartureTimerSeconds;
                xvt.FlightGroups[i].AbortTrigger = miss.FlightGroups[i].AbortTrigger;
                xvt.FlightGroups[i].ArrivalCraft1 = miss.FlightGroups[i].ArrivalCraft1;
                xvt.FlightGroups[i].ArrivalMethod1 = miss.FlightGroups[i].ArrivalMethod1;
                xvt.FlightGroups[i].ArrivalCraft2 = miss.FlightGroups[i].ArrivalCraft2;
                xvt.FlightGroups[i].ArrivalMethod2 = miss.FlightGroups[i].ArrivalMethod2;
                xvt.FlightGroups[i].DepartureCraft1 = miss.FlightGroups[i].DepartureCraft1;
                xvt.FlightGroups[i].DepartureMethod1 = miss.FlightGroups[i].DepartureMethod1;
                xvt.FlightGroups[i].DepartureCraft2 = miss.FlightGroups[i].DepartureCraft2;
                xvt.FlightGroups[i].DepartureMethod2 = miss.FlightGroups[i].DepartureMethod2;
                #endregion ArrDep
                #region Goals
                for (int j = 0; j < 8; j++)
                {
                    for (int k = 0; k < 6; k++) xvt.FlightGroups[i].Goals[j][k] = miss.FlightGroups[i].Goals[j][k];
                    if (xvt.FlightGroups[i].Goals[j].Condition > 46)
                        throw triggerException(0, "FG " + i + " Goal " + j, Xwa.Strings.Trigger[xvt.FlightGroups[i].Goals[j].Condition]);
                    if (xvt.FlightGroups[i].Goals[j].Amount == 19) xvt.FlightGroups[i].Goals[j].Amount = 6;
                    xvt.FlightGroups[i].Goals[j].IncompleteText = miss.FlightGroups[i].Goals[j].IncompleteText;
                    xvt.FlightGroups[i].Goals[j].CompleteText = miss.FlightGroups[i].Goals[j].CompleteText;
                    xvt.FlightGroups[i].Goals[j].FailedText = miss.FlightGroups[i].Goals[j].FailedText;
                }
                #endregion Goals
                for (int j = 0; j < 4; j++)
                {
                    try { xvt.FlightGroups[i].Orders[j] = (Xvt.FlightGroup.Order)miss.FlightGroups[i].Orders[0, j]; }
                    catch (Exception x) { throw new ArgumentException("FG[" + i + "] Order[" + j + "]: " + x.Message, x); }
                }
                xvt.FlightGroups[i].SkipToO4T1AndOrT2 = miss.FlightGroups[i].Orders[0, 3].SkipT1AndOrT2;
                for (int j = 0; j < 2; j++)
                {
                    try { xvt.FlightGroups[i].SkipToOrder4Trigger[j] = (Xvt.Mission.Trigger)miss.FlightGroups[i].Orders[0, 3].SkipTriggers[j]; }
                    catch (Exception x) { throw new ArgumentException("FG[" + i + "] SkipT[" + j + "]: " + x.Message, x); }
                }
                for (int j = 0; j < 3; j++)
                    xvt.FlightGroups[i].Waypoints[j] = (Xvt.FlightGroup.Waypoint)miss.FlightGroups[i].Waypoints[j];
                xvt.FlightGroups[i].Waypoints[13] = (Xvt.FlightGroup.Waypoint)miss.FlightGroups[i].Waypoints[3];
            }
            #endregion FGs
            #region Messages
            for (int i = 0; i < xvt.Messages.Count; i++)
            {
                xvt.Messages[i].MessageString = miss.Messages[i].MessageString;
                xvt.Messages[i].Color = miss.Messages[i].Color;
                int sec = miss.Messages[i].Delay;
                if (sec > 20) sec = 20 + ((sec - 20) * 5);
                if (sec > 1275) sec = 1275;  //[JB] Maximum seconds in XvT when delay is 255.
                xvt.Messages[i].Delay = (byte)(sec / 5);    // should throw if delay > 21:15
                xvt.Messages[i].Note = miss.Messages[i].Note;
                xvt.Messages[i].T1AndOrT2 = miss.Messages[i].TrigAndOr[0];
                xvt.Messages[i].T3AndOrT4 = miss.Messages[i].TrigAndOr[1];
                xvt.Messages[i].T12AndOrT34 = miss.Messages[i].TrigAndOr[2];
                for (int j = 0; j < 10; j++) xvt.Messages[i].SentToTeam[j] = miss.Messages[i].SentTo[j];
                for (int j = 0; j < 4; j++)
                {
                    try { xvt.Messages[i].Triggers[j] = (Xvt.Mission.Trigger)miss.Messages[i].Triggers[j]; }
                    catch (Exception x) { throw new ArgumentException("Mess[" + i + "] T[" + j + "]: " + x.Message, x); }
                }
            }
            #endregion Messages
            #region Briefing
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < xvt.Briefings[i].BriefingTag.Length; j++) xvt.Briefings[i].BriefingTag[j] = miss.Briefings[i].BriefingTag[j];
                for (int j = 0; j < xvt.Briefings[i].BriefingString.Length; j++) xvt.Briefings[i].BriefingString[j] = miss.Briefings[i].BriefingString[j];
                xvt.Briefings[i].Unknown1 = miss.Briefings[i].Unknown1;
                xvt.Briefings[i].Length = (short)(miss.Briefings[i].Length * Xvt.Briefing.TicksPerSecond / Xwa.Briefing.TicksPerSecond);
            }
            #endregion Briefing
            #region Globals
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    xvt.Globals[i].Goals[j].T1AndOrT2 = miss.Globals[i].Goals[j].T1AndOrT2;
                    xvt.Globals[i].Goals[j].T3AndOrT4 = miss.Globals[i].Goals[j].T3AndOrT4;
                    xvt.Globals[i].Goals[j].T12AndOrT34 = miss.Globals[i].Goals[j].T12AndOrT34;
                    for (int k = 0; k < 12; k++) xvt.Globals[i].Goals[j].Triggers[k / 3].GoalStrings[k % 3] = miss.Globals[i].Goals[j].GoalStrings[k / 3, k % 3];
                    xvt.Globals[i].Goals[j].RawPoints = miss.Globals[i].Goals[j].RawPoints;
                    for (int h = 0; h < 4; h++)
                    {
                        try { xvt.Globals[i].Goals[j].Triggers[h].GoalTrigger = (Xvt.Mission.Trigger)miss.Globals[i].Goals[j].Triggers[h]; }
                        catch (Exception x) { throw new ArgumentException("Team[" + i + "] Goal[" + j + "] T[" + h + "]: " + x.Message, x); }
                    }
                }
            }
            #endregion Globals
            #region Team
            for (int i = 0; i < 10; i++)
            {
                xvt.Teams[i].Name = miss.Teams[i].Name;
                for (int j = 0; j < 6; j++)
                    xvt.Teams[i].EndOfMissionMessages[j] = miss.Teams[i].EndOfMissionMessages[j];
                for (int j = 0; j < 10; j++)
                    xvt.Teams[i].AlliedWithTeam[j] = (miss.Teams[i].Allies[j] == Xwa.Team.Allegeance.Friendly);
            }
            #endregion Team
            xvt.MissionPath = miss.MissionPath.ToUpper().Replace(".TIE", "_XVT.tie");
            return xvt;
        }

        /// <summary>Downgrades XWA missions to TIE95</summary>
        /// <remarks>G/PLT, SHPYD, REPYD and M/SC craft will have their indexes changed to reflect IDMR TIE95 Ships patch numbering. Triggers will update.<br/>
        /// FG.Radio is not converted, since TIE behaviour is different<br/>
        /// Maximum FG.Formation value of 12 allowed<br/>
        /// AI level capped at Top Ace<br/>
        /// For Triggers, maximum Trigger index of 24, maximum VariableType of 9, Amounts will be adjusted as 66% to 75%, 33% to 50% and "each" to 100%<br/>
        /// Maximum Abort index of 5<br/>
        /// Maximum FG.Goal Amount index of 6, 75% converted to 100%, 25% to 50%. First three XvT Goals will be used as Primary, Secondary and Bonus goals. Bonus points will be scaled appropriately. Goals only used if set for Team[0] and Enabled<br/>
        /// First two Arrival triggers used, first Departure trigger used. First three Region 1 Orders used, max index of 38.<br/>
        /// Only Start and Hyp WPs converted, manual placement for WP1-8 required.<br/>
        /// For Messages, first two triggers used.<br/>
        /// For the Briefing, first 16 strings and text tags are copied, events are ignored (due to using icons instead of Craft)<br/>
        /// Primary Global goals used, XWA Secondary goals converted to Bonus goals. Prevent goals ignored<br/>
        /// Team[0] EndOfMissionMessages used, Teams[2-6] Name and Hostility towards Team[0] used for IFF<br/>
        /// BriefingQuestions generated using MissionSucc/Fail/Desc strings. Flight Officer has a single pre-mission entry for the Description, two post-mission entries for the Success and Fail. Line breaks must be entered manually<br/>
        /// Filename will end in "_TIE.tie"</remarks>
        /// <param name="miss">XWA mission to convert</param>
        /// <returns>Downgraded mission</returns>
        /// <exception cref="ArgumentException">Properties incompatable with TIE95 were detected in <paramref name="miss"/></exception>
        public static Tie.Mission XwaToTie(Xwa.Mission miss)
        {
            Tie.Mission tie = new Tie.Mission();
            if (miss.FlightGroups.Count > Tie.Mission.FlightGroupLimit) throw maxException(true, true, Tie.Mission.FlightGroupLimit);
            if (miss.Messages.Count > Tie.Mission.MessageLimit) throw maxException(true, false, Tie.Mission.MessageLimit);
            tie.FlightGroups = new Tie.FlightGroupCollection(miss.FlightGroups.Count);
            if (miss.Messages.Count > 0) tie.Messages = new Tie.MessageCollection(miss.Messages.Count);
            #region FGs
            for (int i = 0; i < tie.FlightGroups.Count; i++)
            {
                #region Craft
                // Radio is omitted intentionally
                tie.FlightGroups[i].Name = miss.FlightGroups[i].Name;
                tie.FlightGroups[i].Cargo = miss.FlightGroups[i].Cargo;
                tie.FlightGroups[i].SpecialCargo = miss.FlightGroups[i].SpecialCargo;
                tie.FlightGroups[i].SpecialCargoCraft = miss.FlightGroups[i].SpecialCargoCraft;
                tie.FlightGroups[i].RandSpecCargo = miss.FlightGroups[i].RandSpecCargo;
                tie.FlightGroups[i].CraftType = Tie.Mission.CraftCheck(miss.FlightGroups[i].CraftType);
                if (tie.FlightGroups[i].CraftType == 255) throw flightException(4, i, Xwa.Strings.CraftType[miss.FlightGroups[i].CraftType]);
                tie.FlightGroups[i].NumberOfCraft = miss.FlightGroups[i].NumberOfCraft;
                tie.FlightGroups[i].Status1 = miss.FlightGroups[i].Status1;
                if (tie.FlightGroups[i].Status1 > 19) throw flightException(0, i, Xwa.Strings.Status[miss.FlightGroups[i].Status1]);
                tie.FlightGroups[i].Missile = miss.FlightGroups[i].Missile;
                tie.FlightGroups[i].Beam = miss.FlightGroups[i].Beam;
                tie.FlightGroups[i].IFF = miss.FlightGroups[i].IFF;
                tie.FlightGroups[i].AI = miss.FlightGroups[i].AI;
                if (tie.FlightGroups[i].AI > 4) tie.FlightGroups[i].AI = 4;  //[JB] Super Ace in XWA should be Top Ace in TIE, not invul.
                tie.FlightGroups[i].Markings = miss.FlightGroups[i].Markings;
                if (miss.FlightGroups[i].Formation > 12) throw flightException(1, i, Xwa.Strings.Formation[miss.FlightGroups[i].Formation]);
                else tie.FlightGroups[i].Formation = miss.FlightGroups[i].Formation;
                tie.FlightGroups[i].FormDistance = miss.FlightGroups[i].FormDistance;
                tie.FlightGroups[i].GlobalGroup = miss.FlightGroups[i].GlobalGroup;
                tie.FlightGroups[i].FormLeaderDist = miss.FlightGroups[i].FormLeaderDist;
                tie.FlightGroups[i].NumberOfWaves = miss.FlightGroups[i].NumberOfWaves;
                tie.FlightGroups[i].PlayerCraft = (byte)(miss.FlightGroups[i].PlayerCraft + (miss.FlightGroups[i].PlayerNumber == 1 ? 1 : 0));
                tie.FlightGroups[i].Yaw = miss.FlightGroups[i].Yaw;
                tie.FlightGroups[i].Pitch = miss.FlightGroups[i].Pitch;
                tie.FlightGroups[i].Roll = miss.FlightGroups[i].Roll;
                #endregion Craft
                #region ArrDep
                tie.FlightGroups[i].Difficulty = miss.FlightGroups[i].Difficulty;
                for (int j = 0; j < 3; j++)
                {
                    try { tie.FlightGroups[i].ArrDepTriggers[j] = (Tie.Mission.Trigger)miss.FlightGroups[i].ArrDepTriggers[(j == 2 ? 4 : j)]; }
                    catch (Exception x) { throw new ArgumentException("FG[" + i + "] ArrDep[" + j + "]: " + x.Message, x); }
                }
                tie.FlightGroups[i].AT1AndOrAT2 = miss.FlightGroups[i].ArrDepAndOr[0];
                tie.FlightGroups[i].ArrivalDelayMinutes = miss.FlightGroups[i].ArrivalDelayMinutes;
                tie.FlightGroups[i].ArrivalDelaySeconds = miss.FlightGroups[i].ArrivalDelaySeconds;
                tie.FlightGroups[i].DepartureTimerMinutes = miss.FlightGroups[i].DepartureTimerMinutes;
                tie.FlightGroups[i].DepartureTimerSeconds = miss.FlightGroups[i].DepartureTimerSeconds;
                if (miss.FlightGroups[i].AbortTrigger > 5) throw flightException(2, i, Xwa.Strings.Abort[miss.FlightGroups[i].AbortTrigger]);
                else tie.FlightGroups[i].AbortTrigger = miss.FlightGroups[i].AbortTrigger;
                tie.FlightGroups[i].ArrivalCraft1 = miss.FlightGroups[i].ArrivalCraft1;
                tie.FlightGroups[i].ArrivalMethod1 = miss.FlightGroups[i].ArrivalMethod1;
                tie.FlightGroups[i].ArrivalCraft2 = miss.FlightGroups[i].ArrivalCraft2;
                tie.FlightGroups[i].ArrivalMethod2 = miss.FlightGroups[i].ArrivalMethod2;
                tie.FlightGroups[i].DepartureCraft1 = miss.FlightGroups[i].DepartureCraft1;
                tie.FlightGroups[i].DepartureMethod1 = miss.FlightGroups[i].DepartureMethod1;
                tie.FlightGroups[i].DepartureCraft2 = miss.FlightGroups[i].DepartureCraft2;
                tie.FlightGroups[i].DepartureMethod2 = miss.FlightGroups[i].DepartureMethod2;
                #endregion ArrDep
                #region Goals
                if (miss.FlightGroups[i].Goals[0].Enabled && miss.FlightGroups[i].Goals[0].Team == 0)
                {
                    tie.FlightGroups[i].Goals[0] = miss.FlightGroups[i].Goals[0][1];
                    tie.FlightGroups[i].Goals[1] = miss.FlightGroups[i].Goals[0][2];
                }
                if (miss.FlightGroups[i].Goals[1].Enabled && miss.FlightGroups[i].Goals[1].Team == 0)
                {
                    tie.FlightGroups[i].Goals[2] = miss.FlightGroups[i].Goals[1][1];
                    tie.FlightGroups[i].Goals[3] = miss.FlightGroups[i].Goals[1][2];
                }
                if (miss.FlightGroups[i].Goals[2].Enabled && miss.FlightGroups[i].Goals[2].Team == 0)
                {
                    tie.FlightGroups[i].Goals[6] = miss.FlightGroups[i].Goals[2][1];
                    tie.FlightGroups[i].Goals[7] = miss.FlightGroups[i].Goals[2][2];
                    tie.FlightGroups[i].Goals[8] = miss.FlightGroups[i].Goals[2][3];
                }
                tieGoalsCheck("FlightGroup " + i, tie.FlightGroups[i].Goals);
                #endregion Goals
                for (int j = 0; j < 3; j++)
                {
                    try { tie.FlightGroups[i].Orders[j] = (Tie.FlightGroup.Order)miss.FlightGroups[i].Orders[0, j]; }
                    catch (Exception x) { throw new ArgumentException("FG[" + i + "] Order[" + j + "]: " + x.Message, x); }
                }
                for (int j = 0; j < 3; j++)
                    tie.FlightGroups[i].Waypoints[j] = (Tie.FlightGroup.Waypoint)miss.FlightGroups[i].Waypoints[j];
                tie.FlightGroups[i].Waypoints[13] = (Tie.FlightGroup.Waypoint)miss.FlightGroups[i].Waypoints[3];
            }
            #endregion FGs
            #region Messages
            for (int i = 0; i < tie.Messages.Count; i++)
            {
                tie.Messages[i].MessageString = miss.Messages[i].MessageString;
                tie.Messages[i].Color = miss.Messages[i].Color;
                int sec = miss.GetDelaySeconds(miss.Messages[i].Delay);
                if (sec > 1275) sec = 1275;  //[JB] Maximum seconds in TIE when delay is 255.
                tie.Messages[i].Delay = (byte)(sec / 5);	// should throw if delay > 21:15
                tie.Messages[i].Short = miss.Messages[i].Note;
                tie.Messages[i].Trig1AndOrTrig2 = miss.Messages[i].TrigAndOr[0];
                for (int j = 0; j < 2; j++)
                {
                    try { tie.Messages[i].Triggers[j] = (Tie.Mission.Trigger)miss.Messages[i].Triggers[j]; }
                    catch (Exception x) { throw new ArgumentException("Mess[" + i + "] T[" + j + "]: " + x.Message, x); }
                }
            }
            #endregion Messages
            #region Briefing
            for (int i = 0; i < tie.Briefing.BriefingTag.Length; i++) tie.Briefing.BriefingTag[i] = miss.Briefings[0].BriefingTag[i];
            for (int i = 0; i < tie.Briefing.BriefingString.Length; i++) tie.Briefing.BriefingString[i] = miss.Briefings[0].BriefingString[i];
            tie.Briefing.Unknown1 = miss.Briefings[0].Unknown1;
            tie.Briefing.Length = (short)(miss.Briefings[0].Length * Tie.Briefing.TicksPerSecond / Xwa.Briefing.TicksPerSecond);
            #endregion Briefing
            #region Globals
            tie.GlobalGoals.Goals[0].T1AndOrT2 = miss.Globals[0].Goals[0].T1AndOrT2;    // Primary
            tie.GlobalGoals.Goals[2].T1AndOrT2 = miss.Globals[0].Goals[2].T1AndOrT2;    // Secondary to Bonus, Prevent will be ignored
            for (int j = 0; j < 4; j++)
            {
                try { tie.GlobalGoals.Goals[j / 2 * 2].Triggers[j % 2] = (Tie.Mission.Trigger)miss.Globals[0].Goals[j / 2 * 2].Triggers[j % 2]; }
                catch (Exception x) { throw new ArgumentException("Goal[" + (j / 2 * 2) + "] T[" + (j % 2) + "]: " + x.Message, x); }
            }
            #endregion Globals
            #region IFF/Team
            for (int i = 0; i < 6; i++) tie.EndOfMissionMessages[i] = miss.Teams[0].EndOfMissionMessages[i];
            for (int i = 2; i < 6; i++)
            {
                tie.IFFs[i] = miss.Teams[i].Name;
                tie.IffHostile[i] = ((int)miss.Teams[0].Allies[i] == 0);
            }
            #endregion IFF/Team
            #region Questions
            if (miss.MissionDescription != "")
            {
                tie.BriefingQuestions.PreMissQuestions[0] = "What are the mission objectives?";
                tie.BriefingQuestions.PreMissAnswers[0] = miss.MissionDescription;  // line breaks will have to be manually placed
            }
            if (miss.MissionSuccessful != "")
            {
                tie.BriefingQuestions.PostMissQuestions[0] = "What have I accomplished?";
                tie.BriefingQuestions.PostMissAnswers[0] = miss.MissionSuccessful;  // again, line breaks
                tie.BriefingQuestions.PostTrigger[0] = 4;
                tie.BriefingQuestions.PostTrigType[0] = 1;
            }
            if (miss.MissionFailed != "")
            {
                tie.BriefingQuestions.PostMissQuestions[1] = "Any suggestions?";
                tie.BriefingQuestions.PostMissAnswers[1] = miss.MissionFailed;  // again, line breaks
                tie.BriefingQuestions.PostTrigger[1] = 5;
                tie.BriefingQuestions.PostTrigType[1] = 1;
            }
            #endregion Questions
            tie.MissionPath = miss.MissionPath.ToUpper().Replace(".TIE", "_TIE.tie");
            return tie;
        }

        /// <summary>Upgrades XWING95 missions to TIE95</summary>
        /// <remarks>Attempts to convert Name, Cargo, and SpecialCargo strings from uppercase to initial-case only.<br/>
        /// Maximum FG.Formation value of 12 allowed.<br/>
        /// XWING95 has 3 EndOfMissionMessages, but TIE95 only supports 2.  The last is converted into a Message.<br/>
        /// XWING95's TIE Bombers have both torpedoes and missiles.  Its targets are examined to give it missiles or torpedoes as appropriate.<br/>
        /// XWING95's arrival condition "must be Destroyed" does not actually require all 100% to be destroyed (requires at least one, the rest come and go).  For TIE Fighter it uses 100%, as it lacks XvT's "come and go" condition.<br/>
        /// Some orders perform "autotargeting" based on craft type.  This aspect isn't easily replicated, but is simulated as best it can with additional target slots, plus additional order slots.<br/>
        /// SS Hold Position does not have an equivalent, since it autotargets and does not wait to return fire.  Tries to simulate as best it can with SS Await Launch (works for capital ships) and follows up with two SS Wait orders (with maximum time).<br/>
        /// SS orders in TIE95 don't seem to use Target3 or Target4.  Attempts to work around this by moving the autotargets into Target1 and Target2 if primary or secondary target is not used, but may not always be reliable.<br/>
        /// Attempts to allow converting of custom missions, does not assume the player is IFF Rebel.<br/>
        /// Radio is automatically assigned to same-IFF fighters, transports, shuttles, and tugs.<br/>
        /// BriefingQuestions generated from text-only pages in the briefing.  Flight Officer will have pre-mission questions, and post-mission Fail questions based off the briefing's hint pages.<br/>
        /// Briefing waypoints are estimated by scanning the separate "Briefing FG" list for the most likely match.  If none are found, dummy FGs are inserted to provide icons for the briefing map.<br/>
        /// Briefing text tags do not have colors.  Converted to white since it's the closest match to light blue.<br/>
        /// Filename will end in "_TIE.tie"</remarks>
        /// <param name="miss">XWING95 mission to convert</param>
        /// <returns>Upgraded mission</returns>
        /// <exception cref="ArgumentException">Properties incompatable with TIE95 were detected in <paramref name="miss"/></exception>
        public static Tie.Mission XwingToTie(Xwing.Mission miss)
        {
            Tie.Mission tie = new Tie.Mission
            {
                FlightGroups = new Tie.FlightGroupCollection(miss.FlightGroups.Count)
            };
            #region Mission
            int playerIFF = 0;  //Need to find the player to determine how to set up radio orders.  We'll do it this way if for some reason a custom mission is set up otherwise.
            foreach (var fg in miss.FlightGroups)
            {
                if (fg.PlayerCraft > 0)
                {
                    playerIFF = fg.GetTIEIFF();
                    break;
                }
            }
            byte color = (byte)playerIFF;
            if (color == 0) color = 1;  //In TIE, IFF Rebel is 0 but Color 0 is Red.
            else if (color == 1) color = 0;
            tie.EndOfMissionMessages[0] = (color > 0 ? color.ToString() : "") + miss.EndOfMissionMessages[0];  //X-wing has 3 messages, XvT has two.  Prefix with color codes.
            tie.EndOfMissionMessages[1] = (color > 0 ? color.ToString() : "") + miss.EndOfMissionMessages[1];
            if (miss.EndOfMissionMessages[2] != "")
            {
                Tie.Message msg = new Tie.Message
                {
                    MessageString = miss.EndOfMissionMessages[2],
                    Color = color,
                    Delay = 1   // 5 sec
                };
                msg.Triggers[0].Amount = 0;
                msg.Triggers[0].VariableType = 5; //IFF
                msg.Triggers[0].Variable = 0;
                msg.Triggers[0].Condition = 13; //complete primary mission
                tie.Messages.Add(msg);
            }
            //Not doing mission time.
            #endregion Mission
            #region FGs
            //List<XwingGlobalGroup> ggList = new List<XwingGlobalGroup>();
            for (int i = 0; i < miss.FlightGroups.Count; i++)
            {
                #region Craft
                tie.FlightGroups[i].Name = xwingCaseConversion(miss.FlightGroups[i].Name);
                tie.FlightGroups[i].Cargo = xwingCaseConversion(miss.FlightGroups[i].Cargo);
                tie.FlightGroups[i].SpecialCargo = xwingCaseConversion(miss.FlightGroups[i].SpecialCargo);
                int spec = miss.FlightGroups[i].SpecialCargoCraft + 1;  //X-wing special craft number is zero-based.  Out of bounds indicating no craft.
                if (spec < 0 || tie.FlightGroups[i].SpecialCargo == "") spec = 0;
                else if (spec > miss.FlightGroups[i].NumberOfCraft)
                    spec = 0;
                tie.FlightGroups[i].SpecialCargoCraft = (byte)spec;
                tie.FlightGroups[i].RandSpecCargo = miss.FlightGroups[i].RandSpecCargo;
                tie.FlightGroups[i].CraftType = (byte)miss.FlightGroups[i].GetTIECraftType();
                if (tie.FlightGroups[i].CraftType == 255) throw flightException(4, i, Xwing.Strings.CraftType[miss.FlightGroups[i].CraftType]);
                if (miss.FlightGroups[i].IsObjectGroup())
                {
                    tie.FlightGroups[i].Pitch = miss.FlightGroups[i].Pitch;
                    tie.FlightGroups[i].Yaw = miss.FlightGroups[i].Yaw;
                    tie.FlightGroups[i].Roll = miss.FlightGroups[i].Roll;
                }
                tie.FlightGroups[i].NumberOfCraft = miss.FlightGroups[i].NumberOfCraft;
                if (tie.FlightGroups[i].NumberOfCraft > 9)
                    tie.FlightGroups[i].NumberOfCraft = 9;

                tie.FlightGroups[i].FollowsOrders = (xwingPlayerCommand(miss.FlightGroups[i].CraftType) && miss.FlightGroups[i].GetTIEIFF() == playerIFF);

                byte warhead = 0;
                switch (miss.FlightGroups[i].CraftType)
                {
                    case 1:   //X-W
                    case 2:   //Y-W/B-W
                    case 6:   //T/B
                    case 8:   //TRN
                        warhead = 4;  //Torpedo
                        break;
                    case 3:   //A-W
                    case 7:   //GUN
                        warhead = 3;  //Missile
                        break;
                }
                //Special case for the T/B.  In X-wing they carry both missiles and torpedoes.  We need to check their orders and targets to see if they'll be attacking fighters or larger craft, and choose accordingly.
                if (miss.FlightGroups[i].CraftType == 6)
                {
                    int tPri = miss.FlightGroups[i].TargetPrimary;
                    int tSec = miss.FlightGroups[i].TargetSecondary;
                    int tcPri = (tPri >= 0 && tPri < miss.FlightGroups.Count) ? miss.FlightGroups[tPri].CraftType : 0;
                    int tcSec = (tSec >= 0 && tSec < miss.FlightGroups.Count) ? miss.FlightGroups[tSec].CraftType : 0;
                    bool tfPri = (tcPri >= 0 && tcPri <= 7) || (tcPri == 17);  //Include None as a fighter for the sake of simplicity
                    bool tfSec = (tcSec >= 0 && tcSec <= 7) || (tcSec == 17);
                    if (!tfPri || !tfSec)
                        warhead = 4;
                    else
                    {
                        int orders = miss.FlightGroups[i].Order;
                        if (orders >= 0x14 && orders <= 0x19)  //Attack Transports, Freighters, Starship, Disable Transports, Freighters, Starship
                            warhead = 4;
                        else //Everything else, assuming fighters as default targets since no non-fighters specifically targeted
                            warhead = 3;
                    }
                }

                tie.FlightGroups[i].Missile = warhead;

                byte status = (byte)(miss.FlightGroups[i].Status1 % 10);  //Get the actual status, just in case it's a Y-wing turned B-wing.
                if (status == 1)
                {
                    status = 0;  //In X-wing this is for No Warheads, but in TIE it double's warheads.  Take away warheads instead.
                    tie.FlightGroups[i].Missile = 0;
                }
                tie.FlightGroups[i].Status1 = status;
                if (tie.FlightGroups[i].Status1 >= 5) tie.FlightGroups[i].Status1 = 0; //Replace unknown/unused with default status.

                tie.FlightGroups[i].IFF = miss.FlightGroups[i].GetTIEIFF();
                tie.FlightGroups[i].AI = miss.FlightGroups[i].AI;
                tie.FlightGroups[i].Markings = miss.FlightGroups[i].Markings;
                if (miss.FlightGroups[i].IsFlightGroup())
                {
                    if (miss.FlightGroups[i].Formation > 12) throw flightException(1, i, Xwing.Strings.Formation[miss.FlightGroups[i].Formation]);
                    else tie.FlightGroups[i].Formation = miss.FlightGroups[i].Formation;
                }
                else
                {
                    tie.FlightGroups[i].Formation = (byte)(miss.FlightGroups[i].Formation & 0x3);  //Bits 1 and 2 are formation.
                }
                tie.FlightGroups[i].NumberOfWaves = Convert.ToByte(miss.FlightGroups[i].NumberOfWaves + 1);  //XWING95 editor was modified to use raw data, TIE editor does not
                tie.FlightGroups[i].PlayerCraft = miss.FlightGroups[i].PlayerCraft;
                //tie.FlightGroups[i].Yaw = miss.FlightGroups[i].Yaw;      //Handled with objects
                //tie.FlightGroups[i].Pitch = miss.FlightGroups[i].Pitch;
                //tie.FlightGroups[i].Roll = miss.FlightGroups[i].Roll;
                #endregion Craft
                #region ArrDep
                tie.FlightGroups[i].Difficulty = 0;  //All

                Tie.Mission.Trigger arr = tie.FlightGroups[i].ArrDepTriggers[0];
                arr.Amount = 0;
                arr.Condition = Convert.ToByte(miss.FlightGroups[i].ArrivalEvent);
                if (arr.Condition == 6) //"Disabled" condition has a different ID in TIE
                    arr.Condition = 8;

                if (miss.FlightGroups[i].ArrivalFG >= 0)
                {
                    arr.VariableType = 1;
                    arr.Variable = (byte)miss.FlightGroups[i].ArrivalFG;
                }
                else
                {
                    arr.VariableType = 0;
                    arr.Variable = 0;
                    arr.Condition = 0;
                }
                if (arr.VariableType == 1 && arr.Condition == 0 && arr.Variable == 0)  //Replace with always (TRUE)
                    arr.VariableType = 0;

                if (arr.Condition != 2)   //All conditions except Destroyed trigger when "at least one" fulfills the condition.  Destroyed requires at least one, and the rest to "come and go" (like XvT and XWA) but TIE doesn't have that.
                {
                    arr.Amount = 4;  //At least one

                    int fg = miss.FlightGroups[i].ArrivalFG;
                    if (arr.VariableType == 1 && fg >= 0 && fg < miss.FlightGroups.Count)
                        if (miss.FlightGroups[fg].NumberOfWaves == 0 && miss.FlightGroups[fg].NumberOfCraft == 1)
                            arr.Amount = 0;   //But if the craft only has 1 wave of 1 ship, go ahead and call it 100% to make it more intuitive.
                }


                tie.FlightGroups[i].ArrivalDelayMinutes = (byte)miss.FlightGroups[i].GetArrivalMinutes();
                tie.FlightGroups[i].ArrivalDelaySeconds = (byte)miss.FlightGroups[i].GetArrivalSeconds();
                tie.FlightGroups[i].DepartureTimerMinutes = 0;
                tie.FlightGroups[i].DepartureTimerSeconds = 0;
                tie.FlightGroups[i].AbortTrigger = 0;
                tie.FlightGroups[i].ArrivalCraft1 = (byte)(miss.FlightGroups[i].Mothership >= 0 ? miss.FlightGroups[i].Mothership : 0);
                tie.FlightGroups[i].ArrivalMethod1 = !(miss.FlightGroups[i].ArrivalHyperspace == 1);  //Note: X-wing uses true to arrive from hyperspace, TIE uses false
                tie.FlightGroups[i].ArrivalCraft2 = 0;
                tie.FlightGroups[i].ArrivalMethod2 = false;
                tie.FlightGroups[i].DepartureCraft1 = (byte)(miss.FlightGroups[i].Mothership >= 0 ? miss.FlightGroups[i].Mothership : 0);
                tie.FlightGroups[i].DepartureMethod1 = !(miss.FlightGroups[i].DepartureHyperspace == 1);
                tie.FlightGroups[i].DepartureCraft2 = 0;
                tie.FlightGroups[i].DepartureMethod2 = false;
                if (miss.FlightGroups[i].Mothership == -1)   //Set defaults to Hyperspace if no mothership is set
                {
                    tie.FlightGroups[i].ArrivalMethod1 = false;
                    tie.FlightGroups[i].DepartureMethod1 = false;
                }
                //Fighters in TIE will auto abort at 25% hull, like X-wing.
                #endregion ArrDep
                #region Goals
                if (miss.FlightGroups[i].IsFlightGroup())
                {
                    if (miss.FlightGroups[i].Objective != 0)
                    {
                        byte cond = 0;
                        byte amount = 0;
                        switch (miss.FlightGroups[i].Objective)
                        {
                            // Reminder: Goal amounts for TIE doesn't use normal trigger values
                            case 0: cond = 0; amount = 0; break; //none   (XWING95 goal conditions)
                            case 1: cond = 2; amount = 0; break; //100% destroyed
                            case 2: cond = 12; amount = 0; break; //100% complete mission
                            case 3: cond = 4; amount = 0; break; //100% captured
                            case 4: cond = 6; amount = 0; break; //100% be boarded
                            case 5: cond = 2; amount = 4; break; //special craft destroyed
                            case 6: cond = 12; amount = 4; break; //special craft complete mission
                            case 7: cond = 4; amount = 4; break; //special craft captured
                            case 8: cond = 6; amount = 4; break; //special craft be boarded
                            case 9: cond = 2; amount = 1; break; //50% destroyed
                            case 10: cond = 12; amount = 1; break; //50% complete mission
                            case 11: cond = 4; amount = 1; break; //50% captured
                            case 12: cond = 6; amount = 1; break; //50% be boarded
                            case 13: cond = 5; amount = 0; break; //100% inspected
                            case 14: cond = 5; amount = 4; break; //special craft inspected
                            case 15: cond = 5; amount = 1; break; //50% inspected
                            case 16: cond = 1; amount = 0; break; //100% arrive
                        }
                        tie.FlightGroups[i].Goals.PrimaryCondition = cond;
                        tie.FlightGroups[i].Goals.PrimaryAmount = amount;
                    }
                }
                else  //For objects...
                {
                    if (!miss.FlightGroups[i].IsTrainingPlatform())
                    {
                        byte cond = 0;
                        if ((miss.FlightGroups[i].Formation & 0x4) > 0) cond = 2;       //must be destroyed
                        else if ((miss.FlightGroups[i].Formation & 0x8) > 0) cond = 9;  //must survive
                        tie.FlightGroups[i].Goals.PrimaryCondition = cond;
                        tie.FlightGroups[i].Goals.PrimaryAmount = 0;  //100%
                    }
                }
                #endregion Goals

                #region Orders
                int thisIFF = tie.FlightGroups[i].IFF;
                byte oppositeIFF = (byte)((thisIFF % 2 == 0) ? thisIFF + 1 : thisIFF - 1);

                if (miss.FlightGroups[i].IsFlightGroup())
                {
                    int targPri = miss.FlightGroups[i].TargetPrimary;
                    int targSec = miss.FlightGroups[i].TargetSecondary;

                    convertXwingOrderToTIE(miss.FlightGroups[i].Order, targPri, targSec, oppositeIFF, tie.FlightGroups[i].Orders[0], tie.FlightGroups[i].Orders[1]);
                    moveOrderUp(tie.FlightGroups[i].Orders[0]);

                    if (miss.FlightGroups[i].Order >= 13 && miss.FlightGroups[i].Order <= 17)  //The X-wing boarding commands (Give, Take, Exchange, Capture, Destroy)
                    {
                        int time = miss.FlightGroups[i].DockTimeThrottle * 60 / 5;  //Convert minutes to increments of 5 seconds.
                        if (time < 0) time = 0;
                        else if (time > 255) time = 255;
                        tie.FlightGroups[i].Orders[0].Throttle = 10;  //Full throttle
                        tie.FlightGroups[i].Orders[0].Variable1 = (byte)time;
                        tie.FlightGroups[i].Orders[0].Variable2 = 1;  //Docking count
                    }
                    else if (miss.FlightGroups[i].Order == 26)  //More for SS Hold Position
                    {
                        tie.FlightGroups[i].Orders[0].Throttle = 0;

                        BaseFlightGroup.BaseOrder order3 = tie.FlightGroups[i].Orders[2];
                        order3.Command = 20;    //Set an additional SS Wait order.  TIE orders don't have an equivalent command that sits still and autotargets.
                        order3.Target1Type = 0x5; order3.Target1 = oppositeIFF; order3.T1AndOrT2 = true;
                        order3.Variable1 = 255; //Maximum wait time, 21:15
                        order3.Throttle = 0;
                    }
                    else
                    {
                        int throttle = miss.FlightGroups[i].DockTimeThrottle;
                        if (throttle == 0) throttle = 10;                       //100%
                        else if (throttle >= 1 && throttle <= 9) throttle += 1; //20% to 100%
                        else if (throttle == 10) throttle = 0;                  //0%
                        else throttle = 10;                                     //Unrecognized, full throttle for anything else.
                        tie.FlightGroups[i].Orders[0].Throttle = (byte)throttle;
                    }
                }
                else  //For objects...
                {
                    if (tie.FlightGroups[i].CraftType == 0x4B)  //Special case for mine.
                    {
                        Tie.FlightGroup.Order order = new Tie.FlightGroup.Order
                        {
                            Command = 7,           //Attack targets
                            Target1Type = 0x5,     //IFF
                            Target1 = oppositeIFF,
                            T1AndOrT2 = true      //OR   (none)
                        };
                        tie.FlightGroups[i].Orders[0] = order;
                    }
                }
                #endregion Orders

                //XWING95 waypoints: 0=Start1, 1=WayPt1, 2=WayPt2, 3=WayPt3, 4=Start2, 5=Start3, 6=Hyper
                tie.FlightGroups[i].Waypoints[0] = miss.FlightGroups[i].Waypoints[0]; //Start1
                tie.FlightGroups[i].Waypoints[1] = miss.FlightGroups[i].Waypoints[4]; //Start2
                tie.FlightGroups[i].Waypoints[2] = miss.FlightGroups[i].Waypoints[5]; //Start3

                tie.FlightGroups[i].Waypoints[4] = miss.FlightGroups[i].Waypoints[1]; //WayPt1
                tie.FlightGroups[i].Waypoints[5] = miss.FlightGroups[i].Waypoints[2]; //WayPt2
                tie.FlightGroups[i].Waypoints[6] = miss.FlightGroups[i].Waypoints[3]; //WayPt3

                tie.FlightGroups[i].Waypoints[13] = miss.FlightGroups[i].Waypoints[6]; //Hyper
            }
            #endregion FGs
            #region Briefing

            //Briefing flightgroups are separate from normal flightgroups.  They aren't necessarily the same, nor do they share the same indexes.  Try to detect if the FG exists by craft and position.  If the craft doesn't exist, create new briefing-only FGs if there's space to function as icons.
            Dictionary<int, int> BRFtoReal = new Dictionary<int, int>();  //Maps BRF FlightGroups to their actual XWI FlightGroup, or a new dummy FG to be used as a briefing icon.

            Xwing.BriefingPage pg = miss.Briefing.GetBriefingPage(0);
            int cs = pg.CoordSet;
            int wpIndex = 0; //Default to Start1
            if (cs >= 1 && cs <= 3) //If not Start1, transform into the waypoint index of the virtualized coordinate
                wpIndex = 7 + cs - 1;
            for (int i = 0; i < miss.FlightGroupsBriefing.Count; i++)
            {
                Xwing.FlightGroup bfg = miss.FlightGroupsBriefing[i];
                int found = miss.GetMatchingXWIFlightGroup(i);
                if (found >= 0)
                {
                    tie.FlightGroups[found].Waypoints[14] = bfg.Waypoints[wpIndex];
                    tie.FlightGroups[found].Waypoints[14].RawY *= -1;  //Axis inversion?
                    tie.FlightGroups[found].Waypoints[14].Enabled = true;
                }
                else if (tie.FlightGroups.Count < Tie.Mission.FlightGroupLimit)   //Create a new FG solely for a briefing icon, if there's space.
                {
                    Tie.FlightGroup newFG = new Tie.FlightGroup
                    {
                        Name = bfg.Name,
                        Cargo = "BRIEF_ICON",
                        CraftType = (byte)bfg.GetTIECraftType(),
                        NumberOfCraft = 1,
                        NumberOfWaves = 1,
                        IFF = bfg.GetTIEIFF(),
                        Difficulty = 0
                    };
                    newFG.ArrDepTriggers[0].Condition = 10; //False so it never arrives.
                    newFG.ArrDepTriggers[1].Condition = 10; //False

                    newFG.Waypoints[14] = bfg.Waypoints[wpIndex];
                    newFG.Waypoints[14].RawY *= -1;   //Axis inversion?
                    newFG.Waypoints[14].Enabled = true;

                    tie.FlightGroups.Add(newFG);
                    found = tie.FlightGroups.Count - 1;
                }
                BRFtoReal[i] = found;
            }

            for (int i = 0; i < miss.Briefing.BriefingTag.Length; i++)
            {
                string t = miss.Briefing.BriefingTag[i];
                if (t.Length >= 40) t = t.Substring(0, 38) + "|";     //Limit tag length.  Attempting to render a text tag >= 40 characters long will crash the game.
                tie.Briefing.BriefingTag[i] = t;
            }
            for (int i = 0; i < miss.Briefing.BriefingString.Length; i++)
            {
                string t = miss.Briefing.BriefingString[i];
                if (t.Length >= 158) t = t.Substring(0, 157) + "|";   //Limit strings in TIE Fighter and prevent sporadic crashes when loading the briefing.
                tie.Briefing.BriefingString[i] = t;
            }
            //tie.Briefing.Unknown1 = miss.Briefings[0].Unknown1;
            tie.Briefing.Length = (short)(((float)pg.Length / Xwing.Briefing.TicksPerSecond) * Tie.Briefing.TicksPerSecond);
            int rpos = 0;
            int wpos = 0;
            int length = miss.Briefing.GetEventsLength(0);
            while (rpos < length)
            {
                short[] xwevt = miss.Briefing.ReadBriefingEvent(0, rpos);
                /*if (rpos > 0 && xwevt[1] == (short)Xwing.Briefing.EventType.ClearText)  //Insert a PageBreak before the text (Title/Caption) is cleared, if it's not the first event in the list.
                {
                    tie.Briefing.Events[wpos++] = (short)(((float)xwevt[0] / Xwing.Briefing.TicksPerSecond) * Tie.Briefing.TicksPerSecond);
                    tie.Briefing.Events[wpos++] = (short)BaseBriefing.EventType.PageBreak;
                }*/
                short[] tieevt = miss.Briefing.TranslateBriefingEvent(xwevt);
                if (tieevt[1] >= (short)BaseBriefing.EventType.FGTag1 && tieevt[1] <= (short)BaseBriefing.EventType.FGTag8)
                    tieevt[2] = (short)(BRFtoReal.ContainsKey(tieevt[2]) ? BRFtoReal[tieevt[2]] : 0);  //Replace the BRF FlightGroup with the mapped FlightGroup as detected, or replace with zero.
                else if (tieevt[1] >= (short)BaseBriefing.EventType.TextTag1 && tieevt[1] <= (short)BaseBriefing.EventType.TextTag8)
                    tieevt[5] = 7;  //Color White

                if (tieevt.Length < 2)
                    break;
                tieevt[0] = (short)(((float)tieevt[0] / Xwing.Briefing.TicksPerSecond) * Tie.Briefing.TicksPerSecond);
                if (tieevt[1] != (short)BaseBriefing.EventType.None)
                    for (int i = 0; i < tieevt.Length; i++)
                        tie.Briefing.Events[wpos++] = tieevt[i];
                rpos += xwevt.Length;
            }
            tie.Briefing.Events[wpos++] = 9999;
            tie.Briefing.Events[wpos++] = 0x22;

            #endregion Briefing
            #region Description
            //Extract the mission description from the briefing's text pages.  Anything before the hints page goes into the pre-mission questions.  The hints pages go into the post-mission failure questions. 
            List<string> preText = new List<string>();
            List<string> failText = new List<string>();

            List<string> captionText;
            bool hintPage = false;
            for (int i = 0; i < miss.Briefing.Pages.Count; i++)
            {
                if (!miss.Briefing.IsMapPage(i))
                {
                    miss.Briefing.GetCaptionText(i, out captionText);
                    foreach (string s in captionText)
                    {
                        if (s == "") continue;
                        bool isHintMsg = miss.Briefing.ContainsHintText(s);
                        hintPage |= isHintMsg;  //All of the hint pages are in order.
                        if (isHintMsg || s.StartsWith(">")) continue;
                        List<string> list = hintPage ? failText : preText;
                        if (list.Count == 0)
                        {
                            list.Add(s);
                            continue;
                        }
                        int curLen = list[list.Count - 1].Length;
                        if (curLen + s.Length <= 990)  //Maximum total size of Question + NewLine + Answer + NullTerminator (+post mission trigger bytes) is about 1024, so ensure we have some space to work with.  Append to the current page if we have enough space, otherwise add another.
                            list[list.Count - 1] += (curLen > 0 ? "$" : "") + s;
                        else if (list.Count < 5)  //No more than 5 questions total.
                            list.Add(s);
                    }
                }
            }
            for (int i = 0; i < preText.Count; i++)
            {
                tie.BriefingQuestions.PreMissQuestions[i] = (i == 0) ? "What are the mission details?" : "Any more details?";    //If this text length is changed, check the maximum total size above.
                tie.BriefingQuestions.PreMissAnswers[i] = preText[i];
            }
            for (int i = 0; i < failText.Count; i++)
            {
                tie.BriefingQuestions.PostMissQuestions[i] = (i == 0) ? "Any special instructions?" : "Any more instructions?";  //If this text length is changed, check the maximum total size above.
                tie.BriefingQuestions.PostMissAnswers[i] = failText[i];
                tie.BriefingQuestions.PostTrigType[i] = 1; //Primary goal
                tie.BriefingQuestions.PostTrigger[i] = 5;  //Failed
            }
            #endregion Description

            tie.MissionPath = miss.MissionPath.ToUpper().Replace(".XWI", "_TIE.tie");
            return tie;
        }

        /// <summary>Upgrades XWING95 missions to XvT/BoP</summary>
        /// <remarks>Attempts to convert Name, Cargo, and SpecialCargo strings from uppercase to initial-case only.<br/>
        /// Maximum FG.Formation value of 12 allowed.<br/>
        /// XWING95 has 3 EndOfMissionMessages, but XvT/BoP only supports 2.  The last is converted into a Message.<br/>
        /// XWING95's TIE Bombers have both torpedoes and missiles.  Its targets are examined to give it missiles or torpedoes as appropriate.<br/>
        /// Some orders perform "autotargeting" based on craft type.  This aspect isn't easily replicated, but is simulated as best it can with additional target slots, plus additional order slots.<br/>
        /// SS Hold Position does not have an equivalent, since it autotargets and does not wait to return fire.  Tries to simulate as best it can with SS Await Launch (works for capital ships) and follows up with two SS Wait orders (with maximum time).<br/>
        /// Attempts to allow converting of custom missions, does not assume the player is IFF Rebel.<br/>
        /// Radio is automatically assigned to same-IFF fighters, transports, shuttles, and tugs.<br/>
        /// Mission description is generated from text-only pages in the briefing.  Failure description is based off hint pages.<br/>
        /// Briefing waypoints are estimated by scanning the separate "Briefing FG" list for the most likely match.  If none are found, dummy FGs are inserted to provide icons for the briefing map.<br/>
        /// Briefing text tags do not have colors.  Converted to yellow since it's the closest match to light blue.<br/>
        /// Filename will end in "_xvt.tie"</remarks>
        /// <param name="miss">XWING95 mission to convert</param>
		/// <param name="bop">If the mission is to be XvT or BoP</param>
        /// <returns>Upgraded mission</returns>
        /// <exception cref="ArgumentException">Properties incompatable with XvT were detected in <paramref name="miss"/></exception>
        public static Xvt.Mission XwingToXvtBop(Xwing.Mission miss, bool bop)
        {
            Xvt.Mission xvt = new Xvt.Mission
            {
                IsBop = bop,
                FlightGroups = new Xvt.FlightGroupCollection(miss.FlightGroups.Count)
            };
            #region Mission
            byte[] teamMap = new byte[2] { 0, 1 };

            int playerIFF = 0;  //Need to find the player to determine how to set up radio orders.  We'll do it this way if for some reason a custom mission is set up otherwise.
            int playerMothership = -1;  //We'll use this to set a craft role later, if applicable.
            foreach (var fg in miss.FlightGroups)
            {
                if (fg.PlayerCraft > 0)
                {
                    playerIFF = fg.GetTIEIFF();
                    playerMothership = fg.Mothership;
                    break;
                }
            }
            if (playerIFF == 1)
            {
                teamMap = new byte[2] { 1, 0 };
                xvt.Teams[0].Name = "Imperial"; xvt.Teams[1].Name = "Rebel";
            }
            else
            {
                xvt.Teams[0].Name = "Rebel"; xvt.Teams[1].Name = "Imperial";
            }

            byte color = (byte)playerIFF;
            if (color == 0) color = 1;  //In XvT, IFF Rebel is 0 but Color 0 is Red.
            else if (color == 1) color = 0;
            xvt.Teams[0].EndOfMissionMessages[0] = (color > 0 ? color.ToString() : "") + miss.EndOfMissionMessages[0];  //X-wing has 3 messages, XvT has two.  Prefix with color codes.
            xvt.Teams[0].EndOfMissionMessages[1] = (color > 0 ? color.ToString() : "") + miss.EndOfMissionMessages[1];
            if (miss.EndOfMissionMessages[2] != "")
            {
                Xvt.Message msg = new Xvt.Message
                {
                    MessageString = miss.EndOfMissionMessages[2],
                    Color = color,
                    Delay = 1   //5 sec
                };
                msg.Triggers[0].Amount = 0;
                msg.Triggers[0].VariableType = 12; //Team
                msg.Triggers[0].Variable = 0;
                msg.Triggers[0].Condition = 13; //complete primary mission
                xvt.Messages.Add(msg);
            }
            xvt.MissionType = bop ? Xvt.Mission.MissionTypeEnum.MPTraining : Xvt.Mission.MissionTypeEnum.Training;
            //Not doing mission time.
            #endregion Mission
            #region FGs
            //List<XwingGlobalGroup> ggList = new List<XwingGlobalGroup>();
            for (int i = 0; i < miss.FlightGroups.Count; i++)
            {
                #region Craft
                xvt.FlightGroups[i].Name = xwingCaseConversion(miss.FlightGroups[i].Name);
                xvt.FlightGroups[i].Cargo = xwingCaseConversion(miss.FlightGroups[i].Cargo);
                xvt.FlightGroups[i].SpecialCargo = xwingCaseConversion(miss.FlightGroups[i].SpecialCargo);
                int spec = miss.FlightGroups[i].SpecialCargoCraft + 1;  //X-wing special craft number is zero-based.  Out of bounds indicating no craft.
                if (spec < 0 || xvt.FlightGroups[i].SpecialCargo == "") spec = 0;
                else if (spec > miss.FlightGroups[i].NumberOfCraft)
                    spec = 0;
                xvt.FlightGroups[i].SpecialCargoCraft = (byte)spec;
                xvt.FlightGroups[i].RandSpecCargo = miss.FlightGroups[i].RandSpecCargo;
                xvt.FlightGroups[i].CraftType = (byte)miss.FlightGroups[i].GetTIECraftType();
                if (xvt.FlightGroups[i].CraftType == 255) throw flightException(4, i, Xwing.Strings.CraftType[miss.FlightGroups[i].CraftType]);
                if (miss.FlightGroups[i].IsObjectGroup())
                {
                    xvt.FlightGroups[i].Pitch = miss.FlightGroups[i].Pitch;
                    xvt.FlightGroups[i].Yaw = miss.FlightGroups[i].Yaw;
                    xvt.FlightGroups[i].Roll = miss.FlightGroups[i].Roll;
                }
                xvt.FlightGroups[i].NumberOfCraft = miss.FlightGroups[i].NumberOfCraft;
                if (xvt.FlightGroups[i].NumberOfCraft > 9)
                    xvt.FlightGroups[i].NumberOfCraft = 9;

                xvt.FlightGroups[i].Radio = (byte)(xwingPlayerCommand(miss.FlightGroups[i].CraftType) && miss.FlightGroups[i].GetTIEIFF() == playerIFF ? 1 : 0);

                byte warhead = 0;
                switch (miss.FlightGroups[i].CraftType)
                {
                    case 1:   //X-W
                    case 2:   //Y-W/B-W
                    case 6:   //T/B
                    case 8:   //TRN
                        warhead = 4;  //Torpedo
                        break;
                    case 3:   //A-W
                    case 7:   //GUN
                        warhead = 3;  //Missile
                        break;
                }
                //Special case for the T/B.  In X-wing they carry both missiles and torpedoes.  We need to check their orders and targets to see if they'll be attacking fighters or larger craft, and choose accordingly.
                if (miss.FlightGroups[i].CraftType == 6)
                {
                    int tPri = miss.FlightGroups[i].TargetPrimary;
                    int tSec = miss.FlightGroups[i].TargetSecondary;
                    int tcPri = (tPri >= 0 && tPri < miss.FlightGroups.Count) ? miss.FlightGroups[tPri].CraftType : 0;
                    int tcSec = (tSec >= 0 && tSec < miss.FlightGroups.Count) ? miss.FlightGroups[tSec].CraftType : 0;
                    bool tfPri = (tcPri >= 0 && tcPri <= 7) || (tcPri == 17);  //Include None as a fighter for the sake of simplicity
                    bool tfSec = (tcSec >= 0 && tcSec <= 7) || (tcSec == 17);
                    if (!tfPri || !tfSec)
                        warhead = 4;
                    else
                    {
                        int orders = miss.FlightGroups[i].Order;
                        if (orders >= 0x14 && orders <= 0x19)  //Attack Transports, Freighters, Starship, Disable Transports, Freighters, Starship
                            warhead = 4;
                        else //Everything else, assuming fighters as default targets since no non-fighters specifically targeted
                            warhead = 3;
                    }
                }

                xvt.FlightGroups[i].Missile = warhead;

                byte status = (byte)(miss.FlightGroups[i].Status1 % 10);  //Get the actual status, just in case it's a Y-wing turned B-wing.
                if (status == 1)
                {
                    status = 0;  //In X-wing this is for No Warheads, but in TIE it double's warheads.  Take away warheads instead.
                    xvt.FlightGroups[i].Missile = 0;
                }
                xvt.FlightGroups[i].Status1 = status;  //Get the actual status, just in case it's a Y-wing turned B-wing.
                if (xvt.FlightGroups[i].Status1 >= 5) xvt.FlightGroups[i].Status1 = 0; //Replace unknown/unused with default status.

                xvt.FlightGroups[i].IFF = miss.FlightGroups[i].GetTIEIFF();
                byte team = miss.FlightGroups[i].GetTIEIFF();
                if (team >= 0 && team <= 1) team = teamMap[team];
                xvt.FlightGroups[i].Team = team;

                xvt.FlightGroups[i].AI = miss.FlightGroups[i].AI;
                xvt.FlightGroups[i].Markings = miss.FlightGroups[i].Markings;
                if (miss.FlightGroups[i].IsFlightGroup())
                {
                    if (miss.FlightGroups[i].Formation > 12) throw flightException(1, i, Xwing.Strings.Formation[miss.FlightGroups[i].Formation]);
                    else xvt.FlightGroups[i].Formation = miss.FlightGroups[i].Formation;
                }
                else
                {
                    xvt.FlightGroups[i].Formation = (byte)(miss.FlightGroups[i].Formation & 0x3);  //Bits 1 and 2 are formation.
                }
                xvt.FlightGroups[i].NumberOfWaves = Convert.ToByte(miss.FlightGroups[i].NumberOfWaves + 1);  //XWING95 editor was modified to use raw data, TIE editor does not
                xvt.FlightGroups[i].PlayerCraft = miss.FlightGroups[i].PlayerCraft;
                if (xvt.FlightGroups[i].PlayerCraft > 0) xvt.FlightGroups[i].PlayerCraft--;
                if (miss.FlightGroups[i].PlayerCraft > 0)
                    xvt.FlightGroups[i].PlayerNumber = 1;  //Player slot
                //xvt.FlightGroups[i].Yaw = miss.FlightGroups[i].Yaw;     //Handled with objects
                //xvt.FlightGroups[i].Pitch = miss.FlightGroups[i].Pitch;
                //xvt.FlightGroups[i].Roll = miss.FlightGroups[i].Roll;
                #endregion Craft
                #region ArrDep
                xvt.FlightGroups[i].Difficulty = 0;  //All

                Xvt.Mission.Trigger arr = xvt.FlightGroups[i].ArrDepTriggers[0];
                arr.Amount = 0;
                arr.Condition = Convert.ToByte(miss.FlightGroups[i].ArrivalEvent);
                if (arr.Condition == 6) //"Disabled" condition has a different ID in TIE
                    arr.Condition = 8;

                if (miss.FlightGroups[i].ArrivalFG >= 0)
                {
                    arr.VariableType = 1;
                    arr.Variable = (byte)miss.FlightGroups[i].ArrivalFG;
                }
                else
                {
                    arr.VariableType = 0;
                    arr.Variable = 0;
                    arr.Condition = 0;
                }
                if (arr.VariableType == 1 && arr.Condition == 0 && arr.Variable == 0)  //Replace with always (TRUE)
                    arr.VariableType = 0;

                //All conditions except Destroyed trigger when "at least one" fulfills the condition.  Destroyed requires at least one, and the rest to "come and go" (like XvT and XWA) but TIE doesn't have that.
                arr.Amount = 4;  //At least one

                int fg = miss.FlightGroups[i].ArrivalFG;
                if (arr.VariableType == 1 && fg >= 0 && fg < miss.FlightGroups.Count)
                    if (miss.FlightGroups[fg].NumberOfWaves == 0 && miss.FlightGroups[fg].NumberOfCraft == 1)
                        arr.Amount = 0;   //But if the craft only has 1 wave of 1 ship, go ahead and call it 100% to make it more intuitive.

                if (arr.Condition == 2)
                {
                    Xvt.Mission.Trigger arr2 = xvt.FlightGroups[i].ArrDepTriggers[1];
                    arr2.Amount = 0;
                    arr2.VariableType = 1;
                    arr2.Variable = arr.Variable;
                    arr2.Condition = 43;  //come and go
                    xvt.FlightGroups[i].ArrDepAO[0] = false;  //AND
                }

                xvt.FlightGroups[i].ArrivalDelayMinutes = (byte)miss.FlightGroups[i].GetArrivalMinutes();
                xvt.FlightGroups[i].ArrivalDelaySeconds = (byte)miss.FlightGroups[i].GetArrivalSeconds();
                xvt.FlightGroups[i].DepartureTimerMinutes = 0;
                xvt.FlightGroups[i].DepartureTimerSeconds = 0;
                xvt.FlightGroups[i].AbortTrigger = 0;
                xvt.FlightGroups[i].ArrivalCraft1 = (byte)(miss.FlightGroups[i].Mothership >= 0 ? miss.FlightGroups[i].Mothership : 0);
                xvt.FlightGroups[i].ArrivalMethod1 = !(miss.FlightGroups[i].ArrivalHyperspace == 1);  //Note: X-wing uses true to arrive from hyperspace, TIE uses false
                xvt.FlightGroups[i].ArrivalCraft2 = 0;
                xvt.FlightGroups[i].ArrivalMethod2 = false;
                xvt.FlightGroups[i].DepartureCraft1 = (byte)(miss.FlightGroups[i].Mothership >= 0 ? miss.FlightGroups[i].Mothership : 0);
                xvt.FlightGroups[i].DepartureMethod1 = !(miss.FlightGroups[i].DepartureHyperspace == 1);
                xvt.FlightGroups[i].DepartureCraft2 = 0;
                xvt.FlightGroups[i].DepartureMethod2 = false;
                if (miss.FlightGroups[i].Mothership == -1)   //Set defaults to Hyperspace if no mothership is set
                {
                    xvt.FlightGroups[i].ArrivalMethod1 = false;
                    xvt.FlightGroups[i].DepartureMethod1 = false;
                }
                if (xwingCanWithdraw(miss.FlightGroups[i].CraftType))
                    xvt.FlightGroups[i].AbortTrigger = 9;  //25% hull
                #endregion ArrDep
                #region Goals
                if (miss.FlightGroups[i].IsFlightGroup())
                {
                    if (miss.FlightGroups[i].Objective != 0)
                    {
                        byte cond = 0;
                        byte amount = 0;
                        string role = "";
                        switch (miss.FlightGroups[i].Objective)
                        {
                            case 0: cond = 0; amount = 0; break; //none   (XWING95 goal conditions)
                            case 1: cond = 2; amount = 0; role = "1PRI"; break; //100% destroyed
                            case 2: cond = 12; amount = 0; role = "1MIS"; break; //100% complete mission
                            case 3: cond = 4; amount = 0; role = "1PRI"; break; //100% captured
                            case 4: cond = 6; amount = 0; role = "1PRI"; break; //100% be boarded
                            case 5: cond = 2; amount = 6; role = "1PRI"; break; //special craft destroyed
                            case 6: cond = 12; amount = 6; role = "1MIS"; break; //special craft complete mission
                            case 7: cond = 4; amount = 6; role = "1PRI"; break; //special craft captured
                            case 8: cond = 6; amount = 6; role = "1PRI"; break; //special craft be boarded
                            case 9: cond = 2; amount = 2; role = "1PRI"; break; //50% destroyed
                            case 10: cond = 12; amount = 2; role = "1MIS"; break; //50% complete mission
                            case 11: cond = 4; amount = 2; role = "1PRI"; break; //50% captured
                            case 12: cond = 6; amount = 2; role = "1PRI"; break; //50% be boarded
                            case 13: cond = 5; amount = 0; role = "1PRI"; break; //100% inspected
                            case 14: cond = 5; amount = 6; role = "1PRI"; break; //special craft inspected
                            case 15: cond = 5; amount = 2; role = "1PRI"; break; //50% inspected
                            case 16: cond = 1; amount = 0; break; //100% arrive
                        }
                        xvt.FlightGroups[i].Goals[0].Condition = cond;
                        xvt.FlightGroups[i].Goals[0].Argument = 0;  //Must be completed
                        xvt.FlightGroups[i].Goals[0].Amount = amount;
                        xvt.FlightGroups[i].Goals[0].SetEnabledForTeam(0, true);

                        if (i == playerMothership)
                            role = "1COM";
                        if (role != "")
                            xvt.FlightGroups[i].Roles[0] = role;
                    }
                }
                else  //For objects...
                {
                    if (!miss.FlightGroups[i].IsTrainingPlatform())
                    {
                        byte cond = 0;
                        byte argument = 0;
                        string role = "";
                        if ((miss.FlightGroups[i].Formation & 0x4) > 0) { cond = 2; }                                   //must be destroyed, probably better not to set a role for this.
                        else if ((miss.FlightGroups[i].Formation & 0x8) > 0) { cond = 2; role = "1MIS"; argument = 1; }  //must survive, converts as destruction must be prevented
                        if (cond != 0)
                        {
                            xvt.FlightGroups[i].Goals[0].Condition = cond;
                            xvt.FlightGroups[i].Goals[0].Argument = argument;
                            xvt.FlightGroups[i].Goals[0].Amount = 0;  //100%
                            xvt.FlightGroups[i].Goals[0].SetEnabledForTeam(0, true);

                            if (role != "")
                                xvt.FlightGroups[i].Roles[0] = role;
                        }
                    }
                }
                #endregion Goals

                #region Orders
                int thisIFF = xvt.FlightGroups[i].IFF;
                byte oppositeIFF = (byte)((thisIFF % 2 == 0) ? thisIFF + 1 : thisIFF - 1);

                if (miss.FlightGroups[i].IsFlightGroup())
                {
                    int targPri = miss.FlightGroups[i].TargetPrimary;
                    int targSec = miss.FlightGroups[i].TargetSecondary;

                    convertXwingOrderToTIE(miss.FlightGroups[i].Order, targPri, targSec, oppositeIFF, xvt.FlightGroups[i].Orders[0], xvt.FlightGroups[i].Orders[1]);
                    moveOrderUp(xvt.FlightGroups[i].Orders[0]);

                    if (miss.FlightGroups[i].Order >= 13 && miss.FlightGroups[i].Order <= 17)  //The X-wing boarding commands (Give, Take, Exchange, Capture, Destroy)
                    {
                        int time = miss.FlightGroups[i].DockTimeThrottle * 60 / 5;  //Convert minutes to increments of 5 seconds.
                        if (time < 0) time = 0;
                        else if (time > 255) time = 255;
                        xvt.FlightGroups[i].Orders[0].Throttle = 10;  //Full throttle
                        xvt.FlightGroups[i].Orders[0].Variable1 = (byte)time;
                        xvt.FlightGroups[i].Orders[0].Variable2 = 1;  //Docking count
                    }
                    else if (miss.FlightGroups[i].Order == 26)  //More for SS Hold Position
                    {
                        xvt.FlightGroups[i].Orders[0].Throttle = 0;

                        BaseFlightGroup.BaseOrder order3 = xvt.FlightGroups[i].Orders[2];
                        order3.Command = 20;  //Set an additional SS Wait order.  TIE orders don't have an equivalent command that sits still and autotargets.
                        order3.Target1Type = 0x5; order3.Target1 = oppositeIFF; order3.T1AndOrT2 = true;
                        order3.Variable1 = 255; //Maximum wait time, 21:15
                        order3.Throttle = 0;
                    }
                    else
                    {
                        int throttle = miss.FlightGroups[i].DockTimeThrottle;
                        if (throttle == 0) throttle = 10;                       //100%
                        else if (throttle >= 1 && throttle <= 9) throttle += 1; //20% to 100%
                        else if (throttle == 10) throttle = 0;                  //0%
                        else throttle = 10;                                     //Unrecognized, full throttle for anything else.
                        xvt.FlightGroups[i].Orders[0].Throttle = (byte)throttle;
                    }
                }
                else  //For objects...
                {
                    if (xvt.FlightGroups[i].CraftType == 0x4B)  //Special case for mine.
                    {
                        Xvt.FlightGroup.Order order = new Xvt.FlightGroup.Order
                        {
                            Command = 7,           //Attack targets
                            Target1Type = 0x5,     //IFF
                            Target1 = oppositeIFF,
                            T1AndOrT2 = true      //OR   (none)
                        };
                        xvt.FlightGroups[i].Orders[0] = order;
                    }
                }

                if (miss.FlightGroups[i].PlayerCraft > 0)   //If a player, set the roster role.
                {
                    string playerRole = "General";
                    int xorder = miss.FlightGroups[i].Order;
                    if ((xorder >= 0x08 && xorder <= 0x0A) || (xorder >= 0x14 && xorder <= 0x16))
                        playerRole = "Attack";
                    else if ((xorder == 0x12 || xorder == 0x13) || (xorder >= 0x17 && xorder <= 0x19))
                        playerRole = "Disable";
                    xvt.FlightGroups[i].Orders[0].Designation = playerRole;
                }
                #endregion Orders

                //XWING95 waypoints: 0=Start1, 1=WayPt1, 2=WayPt2, 3=WayPt3, 4=Start2, 5=Start3, 6=Hyper
                xvt.FlightGroups[i].Waypoints[0] = miss.FlightGroups[i].Waypoints[0]; //Start1
                xvt.FlightGroups[i].Waypoints[1] = miss.FlightGroups[i].Waypoints[4]; //Start2
                xvt.FlightGroups[i].Waypoints[2] = miss.FlightGroups[i].Waypoints[5]; //Start3

                xvt.FlightGroups[i].Waypoints[4] = miss.FlightGroups[i].Waypoints[1]; //WayPt1
                xvt.FlightGroups[i].Waypoints[5] = miss.FlightGroups[i].Waypoints[2]; //WayPt2
                xvt.FlightGroups[i].Waypoints[6] = miss.FlightGroups[i].Waypoints[3]; //WayPt3

                xvt.FlightGroups[i].Waypoints[13] = miss.FlightGroups[i].Waypoints[6]; //Hyper
            }
            #endregion FGs
            #region Briefing

            //Briefing flightgroups are separate from normal flightgroups.  They aren't necessarily the same, nor do they share the same indexes.  Try to detect if the FG exists by craft and position.  If the craft doesn't exist, create new briefing-only FGs if there's space to function as icons.
            Dictionary<int, int> BRFtoReal = new Dictionary<int, int>();  //Maps BRF FlightGroups to their actual XWI FlightGroup, or a new dummy FG to be used as a briefing icon.

            Xwing.BriefingPage pg = miss.Briefing.GetBriefingPage(0);
            int cs = pg.CoordSet;
            int wpIndex = 0; //Default to Start1
            if (cs >= 1 && cs <= 3) //If not Start1, transform into the waypoint index of the virtualized coordinate
                wpIndex = 7 + cs - 1;
            for (int i = 0; i < miss.FlightGroupsBriefing.Count; i++)
            {
                Xwing.FlightGroup bfg = miss.FlightGroupsBriefing[i];
                int found = miss.GetMatchingXWIFlightGroup(i);
                if (found >= 0)
                {
                    xvt.FlightGroups[found].Waypoints[14] = bfg.Waypoints[wpIndex];
                    xvt.FlightGroups[found].Waypoints[14].RawY *= -1;  //Axis inversion?
                    xvt.FlightGroups[found].Waypoints[14].Enabled = true;
                }
                else if (xvt.FlightGroups.Count < Xvt.Mission.FlightGroupLimit)   //Create a new FG solely for a briefing icon, if there's space.
                {
                    Xvt.FlightGroup newFG = new Xvt.FlightGroup
                    {
                        Name = bfg.Name,
                        Cargo = "BRIEF_ICON",
                        CraftType = (byte)bfg.GetTIECraftType(),
                        NumberOfCraft = 1,
                        NumberOfWaves = 1,
                        IFF = bfg.GetTIEIFF(),
                        Difficulty = 0
                    };
                    newFG.ArrDepTriggers[0].Condition = 10; //False so it never arrives.
                    newFG.ArrDepTriggers[1].Condition = 10; //False

                    newFG.Waypoints[14] = bfg.Waypoints[wpIndex];
                    newFG.Waypoints[14].RawY *= -1;   //Axis inversion?
                    newFG.Waypoints[14].Enabled = true;

                    xvt.FlightGroups.Add(newFG);
                    found = xvt.FlightGroups.Count - 1;
                }
                BRFtoReal[i] = found;
            }

            for (int i = 0; i < miss.Briefing.BriefingTag.Length; i++)
            {
                string t = miss.Briefing.BriefingTag[i];
                if (t.Length >= 40) t = t.Substring(0, 38) + "|";     //Limit tag length.  Attempting to render a text tag >= 40 characters long will crash the game.
                xvt.Briefings[0].BriefingTag[i] = t;
            }
            for (int i = 0; i < miss.Briefing.BriefingString.Length; i++)
            {
                string t = miss.Briefing.BriefingString[i];
                if (t.Length >= 318) t = t.Substring(0, 317) + "|";   //Limit strings in XvT.  The game's memory capacity for captions appears to be doubled from TIE (now 320 bytes).
                xvt.Briefings[0].BriefingString[i] = t;
            }
            //xvt.Briefing.Unknown1 = miss.Briefings[0].Unknown1;
            xvt.Briefings[0].Length = (short)(((float)pg.Length / Xwing.Briefing.TicksPerSecond) * Xvt.Briefing.TicksPerSecond);
            int rpos = 0;
            int wpos = 0;
            int length = miss.Briefing.GetEventsLength(0);
            while (rpos < length)
            {
                short[] xwevt = miss.Briefing.ReadBriefingEvent(0, rpos);
                /*if (rpos > 0 && xwevt[1] == (short)Xwing.Briefing.EventType.ClearText)  //Insert a PageBreak before the text (Title/Caption) is cleared, if it's not the first event in the list.
                {
                    xvt.Briefings[0].Events[wpos++] = (short)(((float)xwevt[0] / Xwing.Briefing.TicksPerSecond) * Xvt.Briefing.TicksPerSecond);
                    xvt.Briefings[0].Events[wpos++] = (short)BaseBriefing.EventType.PageBreak;
                }*/
                short[] tieevt = miss.Briefing.TranslateBriefingEvent(xwevt);
                if (tieevt[1] >= (short)BaseBriefing.EventType.FGTag1 && tieevt[1] <= (short)BaseBriefing.EventType.FGTag8)
                    tieevt[2] = (short)(BRFtoReal.ContainsKey(tieevt[2]) ? BRFtoReal[tieevt[2]] : 0);  //Replace the BRF FlightGroup with the mapped FlightGroup as detected, or replace with zero.
                else if (tieevt[1] >= (short)BaseBriefing.EventType.TextTag1 && tieevt[1] <= (short)BaseBriefing.EventType.TextTag8)
                    tieevt[5] = 2;  //Color Yellow.  Blue (3) is too dark to read clearly, and there's no other bright neutral color.
                else if (tieevt[1] == (short)BaseBriefing.EventType.ZoomMap)
                {
                    //A multiplier of 2.0 is closer to the original viewport's size, but text tags are distorted too much.  1.5 seems to be a much better fit for text, but it's zoomed out further.  1.75 is a decent middle ground.  Some text tag distortion but not zoomed out too far.
                    tieevt[2] = (short)(tieevt[2] * 1.75);
                    tieevt[3] = (short)(tieevt[3] * 1.75);
                }
                if (tieevt.Length < 2)
                    break;
                tieevt[0] = (short)(((float)tieevt[0] / Xwing.Briefing.TicksPerSecond) * Xvt.Briefing.TicksPerSecond);
                if (tieevt[1] != (short)BaseBriefing.EventType.None)
                    for (int i = 0; i < tieevt.Length; i++)
                        xvt.Briefings[0].Events[wpos++] = tieevt[i];
                rpos += xwevt.Length;
            }
            xvt.Briefings[0].Events[wpos++] = 9999;
            xvt.Briefings[0].Events[wpos++] = 0x22;
            xvt.Briefings[0].Team[0] = true;
            #endregion Briefing
            #region Description
            //Extract the mission description from the briefing's text pages.
            string preText = "";
            string failText = "";
            List<string> captionText;
            bool hintPage = false;
            for (int i = 0; i < miss.Briefing.Pages.Count; i++)
            {
                if (!miss.Briefing.IsMapPage(i))
                {
                    miss.Briefing.GetCaptionText(i, out captionText);
                    foreach (string s in captionText)
                    {
                        if (s == "") continue;
                        bool isHintMsg = miss.Briefing.ContainsHintText(s);
                        hintPage |= isHintMsg;  //All of the hint pages are in order.
                        if (s.StartsWith(">")) continue;
                        if (hintPage && !isHintMsg)
                        {
                            if (failText.Length > 0) failText += "$";
                            failText += s;
                        }
                        else if (!hintPage)
                        {
                            if (preText.Length > 0) preText += "$";
                            preText += s;
                        }
                    }
                }
            }
            preText = preText.Replace("[", "");    //Get rid of the highlight codes since XvT can't use them.
            preText = preText.Replace("]", "");
            failText = failText.Replace("[", "");
            failText = failText.Replace("]", "");
            xvt.MissionDescription = preText;
            xvt.MissionFailed = failText;
            #endregion Description

            xvt.MissionPath = miss.MissionPath.ToUpper().Replace(".XWI", "_xvt.tie");
            return xvt;
        }

        /// <summary>Upgrades XWING95 missions to XWA</summary>
        /// <remarks>Attempts to convert Name, Cargo, and SpecialCargo strings from uppercase to initial-case only.<br/>
        /// Maximum FG.Formation value of 12 allowed.<br/>
        /// XWING95 has 3 EndOfMissionMessages, but XWA only supports 2.  The last is converted into a Message.<br/>
        /// XWING95's TIE Bombers have both torpedoes and missiles.  Its targets are examined to give it missiles or torpedoes as appropriate.<br/>
        /// Some orders perform "autotargeting" based on craft type.  This aspect isn't easily replicated, but is simulated as best it can with additional target slots, plus additional order slots.<br/>
        /// SS Hold Position does not have an equivalent, since it autotargets and does not wait to return fire.  Tries to simulate as best it can with SS Await Launch (works for capital ships) and follows up with two SS Wait orders (with maximum time).<br/>
        /// Attempts to allow converting of custom missions, does not assume the player is IFF Rebel.<br/>
        /// Radio is automatically assigned to same-IFF fighters, transports, shuttles, and tugs.<br/>
        /// Mission description is generated from text-only pages in the briefing.  Failure description is based off hint pages.<br/>
        /// Briefing waypoints are estimated by scanning the separate "Briefing FG" list for the most likely match.  If none are found, dummy FGs are inserted to provide icons for the briefing map.<br/>
        /// Briefing text tags do not have colors.  Converted to yellow since it's the closest match to light blue.<br/>
        /// Filename will end in "_xvt.tie"</remarks>
        /// <param name="miss">XWING95 mission to convert</param>
        /// <returns>Upgraded mission</returns>
        /// <exception cref="ArgumentException">Properties incompatable with XWA were detected in <paramref name="miss"/></exception>
        public static Xwa.Mission XwingToXwa(Xwing.Mission miss)
        {
            Xwa.Mission xwa = new Xwa.Mission
            {
                FlightGroups = new Xwa.FlightGroupCollection(miss.FlightGroups.Count)
            };
            #region Mission
            byte[] teamMap = new byte[2] { 0, 1 };

            int playerIFF = 0;  //Need to find the player to determine how to set up radio orders.  We'll do it this way if for some reason a custom mission is set up otherwise.
            int playerMothership = -1;  //We'll use this to set a craft role later, if applicable.
            foreach (var fg in miss.FlightGroups)
            {
                if (fg.PlayerCraft > 0)
                {
                    playerIFF = fg.GetTIEIFF();
                    playerMothership = fg.Mothership;
                    break;
                }
            }
            if (playerIFF == 1)
            {
                teamMap = new byte[2] { 1, 0 };
                xwa.Teams[0].Name = "Imperial"; xwa.Teams[1].Name = "Rebel";
            }
            else
            {
                xwa.Teams[0].Name = "Rebel"; xwa.Teams[1].Name = "Imperial";
            }

            xwa.Teams[0].EndOfMissionMessages[0] = miss.EndOfMissionMessages[0];  //X-wing has 3 messages, XWA has two.  Message color automatically matches player IFF.
            xwa.Teams[0].EndOfMissionMessages[1] = miss.EndOfMissionMessages[1];
            if (miss.EndOfMissionMessages[2] != "")
            {
                Xwa.Message msg = new Xwa.Message
                {
                    MessageString = miss.EndOfMissionMessages[2],
                    Color = (byte)playerIFF,
                    Delay = 5
                };
                msg.Triggers[0].Amount = 0;
                msg.Triggers[0].VariableType = 12; //Team
                msg.Triggers[0].Variable = 0;
                msg.Triggers[0].Condition = 13; //complete primary mission
                xwa.Messages.Add(msg);
            }
            xwa.MissionType = Xwa.Mission.HangarEnum.MonCalCruiser;
            //Not doing mission time.
            #endregion Mission
            #region FGs

            byte curGU = 1;
            for (int i = 0; i < miss.FlightGroups.Count; i++)
            {
                #region Craft
                xwa.FlightGroups[i].Name = xwingCaseConversion(miss.FlightGroups[i].Name);
                xwa.FlightGroups[i].Cargo = xwingCaseConversion(miss.FlightGroups[i].Cargo);
                xwa.FlightGroups[i].SpecialCargo = xwingCaseConversion(miss.FlightGroups[i].SpecialCargo);
                int spec = miss.FlightGroups[i].SpecialCargoCraft + 1;  //X-wing special craft number is zero-based.  Out of bounds indicating no craft.
                if (spec < 0 || xwa.FlightGroups[i].SpecialCargo == "") spec = 0;
                else if (spec > miss.FlightGroups[i].NumberOfCraft)
                    spec = 0;
                xwa.FlightGroups[i].SpecialCargoCraft = (byte)spec;
                xwa.FlightGroups[i].RandSpecCargo = miss.FlightGroups[i].RandSpecCargo;
                xwa.FlightGroups[i].CraftType = (byte)miss.FlightGroups[i].GetTIECraftType();
                if (xwa.FlightGroups[i].CraftType == 255) throw flightException(4, i, Xwing.Strings.CraftType[miss.FlightGroups[i].CraftType]);
                if (miss.FlightGroups[i].IsObjectGroup())
                {
                    xwa.FlightGroups[i].Pitch = miss.FlightGroups[i].Pitch;
                    xwa.FlightGroups[i].Yaw = miss.FlightGroups[i].Yaw;
                    xwa.FlightGroups[i].Roll = miss.FlightGroups[i].Roll;
                }
                xwa.FlightGroups[i].NumberOfCraft = miss.FlightGroups[i].NumberOfCraft;
                if (xwa.FlightGroups[i].NumberOfCraft > 9)
                    xwa.FlightGroups[i].NumberOfCraft = 9;
                if (miss.FlightGroups[i].NumberOfCraft > 1 && miss.FlightGroups[i].IsFlightGroup() && curGU <= 31)  //XWA has a GU and GG limit which can cause buffer overflow issues.  Normally this wouldn't exceed 16 FGs in X-wing anyway.
                    xwa.FlightGroups[i].GlobalUnit = curGU++;  //XWA doesn't count craft by default, so give each FG a global unit if it has multiple craft.

                xwa.FlightGroups[i].Radio = (byte)(xwingPlayerCommand(miss.FlightGroups[i].CraftType) && miss.FlightGroups[i].GetTIEIFF() == playerIFF ? 1 : 0);

                byte warhead = 0;
                switch (miss.FlightGroups[i].CraftType)
                {
                    case 1:   //X-W
                    case 2:   //Y-W/B-W
                    case 6:   //T/B
                    case 8:   //TRN
                        warhead = 4;  //Torpedo
                        break;
                    case 3:   //A-W
                    case 7:   //GUN
                        warhead = 3;  //Missile
                        break;
                }
                //Special case for the T/B.  In X-wing they carry both missiles and torpedoes.  We need to check their orders and targets to see if they'll be attacking fighters or larger craft, and choose accordingly.
                if (miss.FlightGroups[i].CraftType == 6)
                {
                    int tPri = miss.FlightGroups[i].TargetPrimary;
                    int tSec = miss.FlightGroups[i].TargetSecondary;
                    int tcPri = (tPri >= 0 && tPri < miss.FlightGroups.Count) ? miss.FlightGroups[tPri].CraftType : 0;
                    int tcSec = (tSec >= 0 && tSec < miss.FlightGroups.Count) ? miss.FlightGroups[tSec].CraftType : 0;
                    bool tfPri = (tcPri >= 0 && tcPri <= 7) || (tcPri == 17);  //Include None as a fighter for the sake of simplicity
                    bool tfSec = (tcSec >= 0 && tcSec <= 7) || (tcSec == 17);
                    if (!tfPri || !tfSec)
                        warhead = 4;
                    else
                    {
                        int orders = miss.FlightGroups[i].Order;
                        if (orders >= 0x14 && orders <= 0x19)  //Attack Transports, Freighters, Starship, Disable Transports, Freighters, Starship
                            warhead = 4;
                        else //Everything else, assuming fighters as default targets since no non-fighters specifically targeted
                            warhead = 3;
                    }
                }

                xwa.FlightGroups[i].Missile = warhead;

                byte status = (byte)(miss.FlightGroups[i].Status1 % 10);  //Get the actual status, just in case it's a Y-wing turned B-wing.
                if (status == 1)
                {
                    status = 0;  //In X-wing this is for No Warheads, but in TIE it double's warheads.  Take away warheads instead.
                    xwa.FlightGroups[i].Missile = 0;
                }
                xwa.FlightGroups[i].Status1 = status;  //Get the actual status, just in case it's a Y-wing turned B-wing.
                if (xwa.FlightGroups[i].Status1 >= 5) xwa.FlightGroups[i].Status1 = 0; //Replace unknown/unused with default status.

                xwa.FlightGroups[i].IFF = miss.FlightGroups[i].GetTIEIFF();
                byte team = miss.FlightGroups[i].GetTIEIFF();
                if (team >= 0 && team <= 1) team = teamMap[team];
                xwa.FlightGroups[i].Team = team;

                xwa.FlightGroups[i].AI = miss.FlightGroups[i].AI;
                xwa.FlightGroups[i].Markings = miss.FlightGroups[i].Markings;
                if (miss.FlightGroups[i].IsFlightGroup())
                {
                    if (miss.FlightGroups[i].Formation > 12) throw flightException(1, i, Xwing.Strings.Formation[miss.FlightGroups[i].Formation]);
                    else xwa.FlightGroups[i].Formation = miss.FlightGroups[i].Formation;
                }
                else
                {
                    xwa.FlightGroups[i].Formation = (byte)(miss.FlightGroups[i].Formation & 0x3);  //Bits 1 and 2 are formation.
                }
                xwa.FlightGroups[i].NumberOfWaves = Convert.ToByte(miss.FlightGroups[i].NumberOfWaves + 1);  //XWING95 editor was modified to use raw data, TIE editor does not
                xwa.FlightGroups[i].PlayerCraft = miss.FlightGroups[i].PlayerCraft;
                if (xwa.FlightGroups[i].PlayerCraft > 0) xwa.FlightGroups[i].PlayerCraft--;
                if (miss.FlightGroups[i].PlayerCraft > 0)
                    xwa.FlightGroups[i].PlayerNumber = 1;  //Player slot
                //xwa.FlightGroups[i].Yaw = miss.FlightGroups[i].Yaw;     //Handled with objects
                //xwa.FlightGroups[i].Pitch = miss.FlightGroups[i].Pitch;
                //xwa.FlightGroups[i].Roll = miss.FlightGroups[i].Roll;
                #endregion Craft
                #region ArrDep
                xwa.FlightGroups[i].Difficulty = 0;  //All

                Xwa.Mission.Trigger arr = xwa.FlightGroups[i].ArrDepTriggers[0];
                arr.Amount = 0;
                arr.Condition = Convert.ToByte(miss.FlightGroups[i].ArrivalEvent);
                if (arr.Condition == 6) //"Disabled" condition has a different ID in TIE
                    arr.Condition = 8;

                if (miss.FlightGroups[i].ArrivalFG >= 0)
                {
                    arr.VariableType = 1;
                    arr.Variable = (byte)miss.FlightGroups[i].ArrivalFG;
                }
                else
                {
                    arr.VariableType = 0;
                    arr.Variable = 0;
                    arr.Condition = 0;
                }
                if (arr.VariableType == 1 && arr.Condition == 0 && arr.Variable == 0)  //Replace with always (TRUE)
                    arr.VariableType = 0;

                //All conditions except Destroyed trigger when "at least one" fulfills the condition.  Destroyed requires at least one, and the rest to "come and go" (like XvT and XWA) but TIE doesn't have that.
                arr.Amount = 4;  //At least one

                int fg = miss.FlightGroups[i].ArrivalFG;
                if (arr.VariableType == 1 && fg >= 0 && fg < miss.FlightGroups.Count)
                    if (miss.FlightGroups[fg].NumberOfWaves == 0 && miss.FlightGroups[fg].NumberOfCraft == 1)
                        arr.Amount = 0;   //But if the craft only has 1 wave of 1 ship, go ahead and call it 100% to make it more intuitive.

                if (arr.Condition == 2)
                {
                    Xwa.Mission.Trigger arr2 = xwa.FlightGroups[i].ArrDepTriggers[1];
                    arr2.Amount = 0;
                    arr2.VariableType = 1;
                    arr2.Variable = arr.Variable;
                    arr2.Condition = 43;  //come and go
                    xwa.FlightGroups[i].ArrDepAndOr[0] = false;  //AND
                }

                xwa.FlightGroups[i].ArrivalDelayMinutes = (byte)miss.FlightGroups[i].GetArrivalMinutes();
                xwa.FlightGroups[i].ArrivalDelaySeconds = (byte)miss.FlightGroups[i].GetArrivalSeconds();
                xwa.FlightGroups[i].DepartureTimerMinutes = 0;
                xwa.FlightGroups[i].DepartureTimerSeconds = 0;
                xwa.FlightGroups[i].AbortTrigger = 0;
                xwa.FlightGroups[i].ArrivalCraft1 = (byte)(miss.FlightGroups[i].Mothership >= 0 ? miss.FlightGroups[i].Mothership : 0);
                xwa.FlightGroups[i].ArrivalMethod1 = !(miss.FlightGroups[i].ArrivalHyperspace == 1);  //Note: X-wing uses true to arrive from hyperspace, TIE uses false
                xwa.FlightGroups[i].ArrivalCraft2 = 0;
                xwa.FlightGroups[i].ArrivalMethod2 = false;
                xwa.FlightGroups[i].DepartureCraft1 = (byte)(miss.FlightGroups[i].Mothership >= 0 ? miss.FlightGroups[i].Mothership : 0);
                xwa.FlightGroups[i].DepartureMethod1 = !(miss.FlightGroups[i].DepartureHyperspace == 1);
                xwa.FlightGroups[i].DepartureCraft2 = 0;
                xwa.FlightGroups[i].DepartureMethod2 = false;
                if (miss.FlightGroups[i].Mothership == -1)   //Set defaults to Hyperspace if no mothership is set
                {
                    xwa.FlightGroups[i].ArrivalMethod1 = false;
                    xwa.FlightGroups[i].DepartureMethod1 = false;
                }
                if (xwingCanWithdraw(miss.FlightGroups[i].CraftType))
                    xwa.FlightGroups[i].AbortTrigger = 9;  //25% hull
                #endregion ArrDep
                #region Goals
                if (miss.FlightGroups[i].IsFlightGroup())
                {
                    if (miss.FlightGroups[i].Objective != 0)
                    {
                        byte cond = 0;
                        byte amount = 0;
                        byte role = 0;
                        switch (miss.FlightGroups[i].Objective)
                        {
                            case 0: cond = 0; amount = 0; break; //none   (XWING95 goal conditions)
                            case 1: cond = 2; amount = 0; role = 7; break; //100% destroyed
                            case 2: cond = 12; amount = 0; role = 3; break; //100% complete mission
                            case 3: cond = 4; amount = 0; role = 7; break; //100% captured
                            case 4: cond = 6; amount = 0; role = 7; break; //100% be boarded
                            case 5: cond = 2; amount = 6; role = 7; break; //special craft destroyed
                            case 6: cond = 12; amount = 6; role = 3; break; //special craft complete mission
                            case 7: cond = 4; amount = 6; role = 7; break; //special craft captured
                            case 8: cond = 6; amount = 6; role = 7; break; //special craft be boarded
                            case 9: cond = 2; amount = 2; role = 7; break; //50% destroyed
                            case 10: cond = 12; amount = 2; role = 3; break; //50% complete mission
                            case 11: cond = 4; amount = 2; role = 7; break; //50% captured
                            case 12: cond = 6; amount = 2; role = 7; break; //50% be boarded
                            case 13: cond = 5; amount = 0; role = 7; break; //100% inspected
                            case 14: cond = 5; amount = 6; role = 7; break; //special craft inspected
                            case 15: cond = 5; amount = 2; role = 7; break; //50% inspected
                            case 16: cond = 1; amount = 0; break; //100% arrive
                        }
                        xwa.FlightGroups[i].Goals[0].Condition = cond;
                        xwa.FlightGroups[i].Goals[0].Argument = 0;  //Must be completed
                        xwa.FlightGroups[i].Goals[0].Amount = amount;
                        xwa.FlightGroups[i].Goals[0].Team = 0;
                        xwa.FlightGroups[i].Goals[0].Enabled = true;

                        if (i == playerMothership || role > 0)   //Mothership is role 0.
                        {
                            xwa.FlightGroups[i].EnableDesignation1 = 0;
                            xwa.FlightGroups[i].Designation1 = role;
                        }
                    }
                }
                else  //For objects...
                {
                    if (!miss.FlightGroups[i].IsTrainingPlatform())
                    {
                        byte cond = 0;
                        byte argument = 0;
                        byte role = 0;
                        if ((miss.FlightGroups[i].Formation & 0x4) > 0) { cond = 2; }                                   //must be destroyed, probably better not to set a role for this.
                        else if ((miss.FlightGroups[i].Formation & 0x8) > 0) { cond = 2; role = 3; argument = 1; }  //must survive, converts as destruction must be prevented
                        if (cond != 0)
                        {
                            xwa.FlightGroups[i].Goals[0].Condition = cond;
                            xwa.FlightGroups[i].Goals[0].Argument = argument;
                            xwa.FlightGroups[i].Goals[0].Amount = 0;  //100%
                            xwa.FlightGroups[i].Goals[0].Team = 0;
                            xwa.FlightGroups[i].Goals[0].Enabled = true;

                            if (role > 0)   //Mothership is role 0.
                            {
                                xwa.FlightGroups[i].EnableDesignation1 = 0;
                                xwa.FlightGroups[i].Designation1 = role;
                            }
                        }
                    }
                }
                #endregion Goals

                #region Orders
                int thisIFF = xwa.FlightGroups[i].IFF;
                byte oppositeIFF = (byte)((thisIFF % 2 == 0) ? thisIFF + 1 : thisIFF - 1);

                if (miss.FlightGroups[i].IsFlightGroup())
                {
                    int targPri = miss.FlightGroups[i].TargetPrimary;
                    int targSec = miss.FlightGroups[i].TargetSecondary;

                    convertXwingOrderToTIE(miss.FlightGroups[i].Order, targPri, targSec, oppositeIFF, xwa.FlightGroups[i].Orders[0, 0], xwa.FlightGroups[i].Orders[0, 1]);
                    moveOrderUp(xwa.FlightGroups[i].Orders[0, 0]);

                    if (miss.FlightGroups[i].Order >= 13 && miss.FlightGroups[i].Order <= 17)  //The X-wing boarding commands (Give, Take, Exchange, Capture, Destroy)
                    {
                        int time = miss.FlightGroups[i].DockTimeThrottle * 60 / 5;  //Convert minutes to increments of 5 seconds.
                        if (time < 0) time = 0;
                        else if (time > 255) time = 255;
                        xwa.FlightGroups[i].Orders[0, 0].Throttle = 10;  //Full throttle
                        xwa.FlightGroups[i].Orders[0, 0].Variable1 = (byte)time;
                        xwa.FlightGroups[i].Orders[0, 0].Variable2 = 1;  //Docking count
                    }
                    else if (miss.FlightGroups[i].Order == 26)  //More for SS Hold Position
                    {
                        xwa.FlightGroups[i].Orders[0, 0].Throttle = 0;

                        BaseFlightGroup.BaseOrder order3 = xwa.FlightGroups[i].Orders[0, 2];
                        order3.Command = 20;  //Set an additional SS Wait order.  TIE orders don't have an equivalent command that sits still and autotargets.
                        order3.Target1Type = 0x5; order3.Target1 = oppositeIFF; order3.T1AndOrT2 = true;
                        order3.Variable1 = 255; //Maximum wait time, 21:15
                        order3.Throttle = 0;
                    }
                    else
                    {
                        int throttle = miss.FlightGroups[i].DockTimeThrottle;
                        if (throttle == 0) throttle = 10;                       //100%
                        else if (throttle >= 1 && throttle <= 9) throttle += 1; //20% to 100%
                        else if (throttle == 10) throttle = 0;                  //0%
                        else throttle = 10;                                     //Unrecognized, full throttle for anything else.
                        xwa.FlightGroups[i].Orders[0, 0].Throttle = (byte)throttle;
                    }
                }
                else  //For objects...
                {
                    if (xwa.FlightGroups[i].CraftType == 0x4B)  //Special case for mine.
                    {
                        Xwa.FlightGroup.Order order = new Xwa.FlightGroup.Order
                        {
                            Command = 7,           //Attack targets
                            Target1Type = 0x5,     //IFF
                            Target1 = oppositeIFF,
                            T1AndOrT2 = true      //OR   (none)
                        };
                        xwa.FlightGroups[i].Orders[0, 0] = order;
                    }
                }
                #endregion Orders

                //XWING95 waypoints: 0=Start1, 1=WayPt1, 2=WayPt2, 3=WayPt3, 4=Start2, 5=Start3, 6=Hyper
                xwa.FlightGroups[i].Waypoints[0] = miss.FlightGroups[i].Waypoints[0]; //Start1
                xwa.FlightGroups[i].Waypoints[1] = miss.FlightGroups[i].Waypoints[4]; //Start2
                xwa.FlightGroups[i].Waypoints[2] = miss.FlightGroups[i].Waypoints[5]; //Start3

                xwa.FlightGroups[i].Orders[0, 0].Waypoints[0] = miss.FlightGroups[i].Waypoints[1]; //WayPt1
                xwa.FlightGroups[i].Orders[0, 0].Waypoints[1] = miss.FlightGroups[i].Waypoints[2]; //WayPt2
                xwa.FlightGroups[i].Orders[0, 0].Waypoints[2] = miss.FlightGroups[i].Waypoints[3]; //WayPt3

                xwa.FlightGroups[i].Waypoints[3] = miss.FlightGroups[i].Waypoints[6]; //Hyper
            }
            #endregion FGs
            #region Briefing

            //Briefing flightgroups are separate from normal flightgroups.  They aren't necessarily the same, nor do they share the same indexes.  The good thing about XWA's briefing is the icon system is independent from FGs, so briefing flightgroups can be directly ported without any need to cross reference with normal flightgroups.
            for (int i = 0; i < miss.Briefing.BriefingTag.Length; i++)
            {
                string t = miss.Briefing.BriefingTag[i];
                if (t.Length >= 40) t = t.Substring(0, 38) + "|";     //Limit tag length.  Attempting to render a text tag >= 40 characters long will crash the game.
                xwa.Briefings[0].BriefingTag[i] = t;
            }
            for (int i = 0; i < miss.Briefing.BriefingString.Length; i++)
            {
                string t = miss.Briefing.BriefingString[i];
                if (t.Length >= 318) t = t.Substring(0, 317) + "|";   //Limit strings in xwa.  The game's memory capacity for captions appears to be doubled from TIE (now 320 bytes).
                xwa.Briefings[0].BriefingString[i] = t;
            }
            //xwa.Briefing.Unknown1 = miss.Briefings[0].Unknown1;
            int rpos = 0;
            int wpos = 0;
            Xwing.BriefingPage pg = miss.Briefing.GetBriefingPage(0);
            int length = miss.Briefing.GetEventsLength(0);
            xwa.Briefings[0].Length = (short)(((float)pg.Length / Xwing.Briefing.TicksPerSecond) * Xwa.Briefing.TicksPerSecond);
            int cs = pg.CoordSet;
            int wpIndex = 0; //Default to Start1
            if (cs >= 1 && cs <= 3) //If not Start1, transform into the waypoint index of the virtualized coordinate
                wpIndex = 7 + cs - 1;
            short[] evt = new short[5];
            for (int i = 0; i < miss.FlightGroupsBriefing.Count; i++)
            {
                Xwing.FlightGroup bfg = miss.FlightGroupsBriefing[i];
                int x = bfg.Waypoints[wpIndex].RawX;
                int y = bfg.Waypoints[wpIndex].RawY;
                evt[0] = 0;            //Time
                evt[1] = 0x1A;         //New Icon
                evt[2] = (short)i;     //Icon #
                evt[3] = (short)bfg.GetTIECraftType();
                evt[4] = (short)bfg.GetTIEIFF();
                if (evt[3] == 0x56)   //Icon Hack for asteroids (which don't seem to have an icon) to display as the asteroid R&D base.
                    evt[3] = 0x42;
                for (int j = 0; j < 5; j++)
                    xwa.Briefings[0].Events[wpos++] = evt[j];
                evt[0] = 0;            //Time
                evt[1] = 0x1C;         //Move Icon
                evt[2] = (short)i;     //Icon #
                evt[3] = (short)x;
                evt[4] = (short)y;
                for (int j = 0; j < 5; j++)
                    xwa.Briefings[0].Events[wpos++] = evt[j];
            }

            while (rpos < length)
            {
                short[] xwevt = miss.Briefing.ReadBriefingEvent(0, rpos);
                /*if (rpos > 0 && xwevt[1] == (short)Xwing.Briefing.EventType.ClearText)  //Insert a PageBreak before the text (Title/Caption) is cleared, if it's not the first event in the list.
                {
                    xwa.Briefings[0].Events[wpos++] = (short)(((float)xwevt[0] / Xwing.Briefing.TicksPerSecond) * Xwa.Briefing.TicksPerSecond);
                    xwa.Briefings[0].Events[wpos++] = (short)BaseBriefing.EventType.PageBreak;
                }*/
                short[] tieevt = miss.Briefing.TranslateBriefingEvent(xwevt);
                if (tieevt[1] >= (short)BaseBriefing.EventType.TextTag1 && tieevt[1] <= (short)BaseBriefing.EventType.TextTag8)
                    tieevt[5] = 2;  //Color Yellow.  Blue (3) is too dark to read clearly, and there's no other bright neutral color.
                else if (tieevt[1] == (short)BaseBriefing.EventType.ZoomMap)
                {
                    //XWING size: 212, 165    XWA size: 510, 294
                    //If we zoom too much to compensate for the increased viewport, the position of text tags will become distorted.
                    tieevt[2] = (short)(tieevt[2] * 2.4);
                    tieevt[3] = (short)(tieevt[3] * 2.4);
                }
                /*else if (tieevt[1] == (short)BaseBriefing.EventType.MoveMap) //~MG This "half move" might be related to an issue in YOGEME itself, once that was corrected this needed to be removed
                {
                    tieevt[2] = (short)(tieevt[2] * 0.5);  //[JB] For reasons I haven't investigated, XWA needs a half move to be positioned correctly.  Or at least for the few missions I tested.
                    tieevt[3] = (short)(tieevt[3] * 0.5);
                }*/
                if (tieevt.Length < 2)
                    break;
                tieevt[0] = (short)(((float)tieevt[0] / Xwing.Briefing.TicksPerSecond) * Xwa.Briefing.TicksPerSecond);
                if (tieevt[1] != (short)BaseBriefing.EventType.None)
                    for (int i = 0; i < tieevt.Length; i++)
                        xwa.Briefings[0].Events[wpos++] = tieevt[i];
                rpos += xwevt.Length;
            }
            xwa.Briefings[0].Events[wpos++] = 9999;
            xwa.Briefings[0].Events[wpos++] = 0x22;
            xwa.Briefings[0].Team[0] = true;
            #endregion Briefing
            #region Description
            //Extract the mission description from the briefing's text pages.
            string preText = "";
            string failText = "";
            List<string> captionText;
            bool hintPage = false;
            for (int i = 0; i < miss.Briefing.Pages.Count; i++)
            {
                if (!miss.Briefing.IsMapPage(i))
                {
                    miss.Briefing.GetCaptionText(i, out captionText);
                    foreach (string s in captionText)
                    {
                        if (s == "") continue;
                        bool isHintMsg = miss.Briefing.ContainsHintText(s);
                        hintPage |= isHintMsg;  //All of the hint pages are in order.
                        if (s.StartsWith(">")) continue;
                        if (hintPage && !isHintMsg)
                        {
                            if (failText.Length > 0) failText += "$";
                            failText += s;
                        }
                        else if (!hintPage)
                        {
                            if (preText.Length > 0) preText += "$";
                            preText += s;
                        }
                    }
                }
            }
            preText = preText.Replace("[", "");    //Get rid of the highlight codes since XvT can't use them.
            preText = preText.Replace("]", "");
            failText = failText.Replace("[", "");
            failText = failText.Replace("]", "");
            xwa.MissionDescription = preText;
            xwa.MissionFailed = failText;
            #endregion Description
            #region Backdrops
            //Two backdrops with randomized type and location for lighting, similar to how skirmish files are generated.
            Random rnd = new Random();
            int backdrop1 = rnd.Next(1, 60);   //Select a planet (randomizer always returns 1 less than maximum)
            if (backdrop1 == 24 || backdrop1 == 54) backdrop1++;  //These seem to be empty, skip over them.
            int backdrop2 = rnd.Next(83, 92);  //Select a star
            short[] coord1 = { 0, 0, 0, 1 };
            short[] coord2 = { 0, 0, 0, 1 };
            coord1[rnd.Next(3)] = 1;   //Randomize one of the coordinates
            coord2[rnd.Next(3)] = -1;

            Xwa.FlightGroup bd = new Xwa.FlightGroup
            {
                Name = "1.0 1.0 1.0",
                Cargo = Math.Round(0.5 + (rnd.NextDouble() * 0.5), 1).ToString(),         //Brightness randomized from 0.5 to 1.0
                SpecialCargo = Math.Round(1.7 + (rnd.NextDouble() * 0.9), 1).ToString(),  //Size randomized from 1.7 to 2.6
                CraftType = 183,  //Backdrop
                NumberOfCraft = 1,
                GlobalCargo = (byte)rnd.Next(7),  //Shadow randomized from 0 to 6
                IFF = 4,  //IFF Red
                Team = 9,
                GlobalGroup = 31,
                Backdrop = (byte)backdrop1
            };
            for (int i = 0; i < 4; i++) bd.Waypoints[0][i] = coord1[i];
            xwa.FlightGroups.Add(bd);

            bd = new Xwa.FlightGroup
            {
                Name = "1.0 1.0 1.0",
                Cargo = Math.Round(0.5 + (rnd.NextDouble() * 0.5), 1).ToString(),         //Brightness randomized from 0.5 to 1.0
                SpecialCargo = "1.0",   //Size
                CraftType = 183,  //Backdrop
                NumberOfCraft = 1,
                GlobalCargo = 1,   //Shadow  (stars don't have shadows)
                IFF = 4,  //IFF Red
                Team = 9,
                GlobalGroup = 31,
                Backdrop = (byte)backdrop2
            };
            for (int i = 0; i < 4; i++) bd.Waypoints[0][i] = coord2[i];
            xwa.FlightGroups.Add(bd);
            #endregion Backdrops

            string path = miss.MissionPath.ToUpper().Replace(".XWI", "_xwa.tie");
            path = path.Insert(path.LastIndexOf('\\') + 1, "B0M1_");  //Appends after the last slash, or at the start of the filename if none found.  XWA might crash if the mission doesn't have the proper mission prefix.
            xwa.MissionPath = path;
            return xwa;
        }

        #region helper functions
        /// <summary>Determines if the player can issue orders to this craft in XWING95.</summary>
        /// <param name="xwingCraftType">Must be an XWING95 craft type ID.</param>
        /// <returns>Returns <b>true</b> if the player is capable of issuing orders to this craft in XWING95.</returns>
        static bool xwingPlayerCommand(int xwingCraftType)
        {
            return (xwingCraftType >= 1 && xwingCraftType <= 10) || (xwingCraftType == 17);  //(X-W, Y-W/B-W, A-W, T/F, T/I, T/B, GUN, TRN, SHU, TUG) || (T/A)
        }

        /// <summary>Converts XWING95 primary and secondary targets into a default BaseOrder.</summary>
        /// <remarks>Does not handle the order command itself.</remarks>
        /// <param name="order">Order object to modify.</param>
        /// <param name="targPri">Flight Group index, -1 for none.</param>
        /// <param name="targSec">Flight Group index, -1 for none.</param>
        static void xwingSetOrderPriSec(BaseFlightGroup.BaseOrder order, int targPri, int targSec)
        {
            if (targPri >= 0)
            {
                order.Target1Type = 1;
                order.Target1 = (byte)targPri;
            }
            if (targSec >= 0)
            {
                order.Target2Type = 1;
                order.Target2 = (byte)targSec;
            }
            order.T1AndOrT2 = true;  //OR
        }

        /// <summary> Determine if an XWING95 CraftType is a fighter.</summary>
        /// <remarks> In XWING95, fighter craft always withdraw when hull damaged.</remarks>
        /// <param name="xwingCraftType">Must be an XWING95 craft type ID.</param>
        /// <returns><b>true</b> if a fighter.</returns>
        static bool xwingCanWithdraw(int xwingCraftType)
        {
            return (xwingCraftType >= 1 && xwingCraftType <= 7) || (xwingCraftType == 17);    //(X-W, Y-W/B-W, A-W, T/F, T/I, T/B, GUN) || (T/A)
        }

        /// <summary>Moves order target slots up if there is space to do so.</summary>
        /// <remarks>When converting from XWING95 to TIE95, some capital ship orders don't seem to use Target3 or Target 4.
        /// This function checks if both Target1 and Target2 are empty.  If so, it moves Target3 and Target4 into Target1 and Target2.
        /// Since XWING95 autotarget orders usually translate into "Craft X (AND) IFF Y" then we need to keep T1/T2 and T3/T4 paired up together.</remarks>
        /// <param name="order">Order to modify.</param>
        static void moveOrderUp(BaseFlightGroup.BaseOrder order)
        {
            if (order.Target1Type != 0 || order.Target1 != 0) return;
            if (order.Target2Type != 0 || order.Target2 != 0) return;
            order.Target1Type = order.Target3Type; order.Target1 = order.Target3;
            order.Target2Type = order.Target4Type; order.Target2 = order.Target4;
            order.T1AndOrT2 = order.T3AndOrT4;
            order.Target3Type = 0; order.Target3 = 0;
            order.Target4Type = 0; order.Target4 = 0;
            order.T3AndOrT4 = true;  //Reset to OR
        }

        /// <summary>Returns text converted to lowercase, if applicable.  Each word's initial letter will remain capitalized.</summary>
        /// <remarks>XWING95 FlightGroup names are usually always in ALL CAPS which doesn't look nice in the later games.
        /// Strings not recognized as words (no vowels detected) will not be converted.</remarks>
        /// <param name="text">String to convert.</param>
        /// <returns>Converted string, or same string if not converted.</returns>
        static string xwingCaseConversion(string text)
        {
            string vowels = "AEIOUaeiouüéâäàåêëèïîìÄÅÉôöòûùÖÜáíóú"; //Vowels in code page 437.
            bool hasVowel = (text.IndexOfAny(vowels.ToCharArray()) >= 0);

            if (!hasVowel)  //Most likely not a word.
                return text;

            char[] carr = text.ToCharArray();
            bool firstCase = true;
            for (int i = 0; i < carr.Length; i++)
            {
                char c = carr[i];
                if (firstCase)
                {
                    if (char.IsLetter(c))
                        firstCase = false;
                    continue;
                }
                else if (char.IsWhiteSpace(c))
                {
                    firstCase = true;
                    continue;
                }
                else if (!firstCase && char.IsLetter(c))
                {
                    carr[i] = char.ToLower(carr[i]);
                }
            }
            return new string(carr);
        }

        /// <summary>Converts an XWING95 order to the platform BaseOrder format.</summary>
        /// <remarks>For complex orders, additional BaseOrder objects may be required.
        /// NOTE: This function does not handle docking time or boarding counts.
        /// NOTE: In TIE Fighter, the variable for waypoint loop count is interpreted as a signed char.  Negative values (>= 128) will jump to hyperspace.  This does not apply to XvT.
        /// NOTE: Some commands require additional processing not performed here.</remarks>
        /// <param name="command">XWING95 order command</param>
        /// <param name="targPri">Flight Group index, or -1 for none.</param>
        /// <param name="targSec">Flight Group index, or -1 for none.</param>
        /// <param name="iff">IFF to target.</param>
        /// <param name="order1">Order object to receive the conversion.</param>
        /// <param name="order2">Order object to receive the conversion.</param>
        static void convertXwingOrderToTIE(int command, int targPri, int targSec, byte iff, BaseFlightGroup.BaseOrder order1, BaseFlightGroup.BaseOrder order2)
        {
            xwingSetOrderPriSec(order1, targPri, targSec);  //Set the default primary and secondary target FGs, which are used by most orders.  Additional information will be patched in depending on a case basis, if needed.
            switch (command)
            {
                case 0: order1.Command = 0; order1.Throttle = 0; break;  //Hold steady (XW) -> Hold Steady (TIE)
                case 1: order1.Command = 1; break;  //Go home -> Go Home
                case 2: order1.Command = 2; order1.Variable1 = 127; break;  //Circle and Ignore -> Circle (infinite loop)
                case 3: order1.Command = 2; order1.Variable1 = 1; break;  //Fly Once and Ignore -> Circle (1 loop)
                case 4: order1.Command = 3; order1.Variable1 = 127; break;  //Circle and Evade -> Circle and Evade (infinite loop)
                case 5: order1.Command = 3; order1.Variable1 = 1; break;  //Fly Once and Evade -> Circle and Evade (1 loop)
                case 6: order1.Command = 10; break;  //Close Escort -> Escort
                case 7: order1.Command = 10; break;  //Loose Escort -> Escort
                case 8: order1.Command = 8; break;  //Attack Escorts -> Attack Escorts
                case 9: order1.Command = 7; break;  //Attack Targets (Primary, secondary)
                case 10:
                    //Attack enemies (Fighter, TRN, SHU)
                    order1.Command = 7;
                    order1.Target3Type = 0x3; order1.Target3 = 0x0;  //Fighters
                    order1.Target4Type = 0x5; order1.Target4 = iff; order1.T3AndOrT4 = false;

                    order2.Command = 7;
                    order2.Target1Type = 0x3; order2.Target1 = 0x1;  //Transports
                    order2.Target2Type = 0x5; order2.Target2 = iff; order2.T1AndOrT2 = false;
                    order2.Target3Type = 0x3; order2.Target3 = 0x0;  //Fighters
                    order2.Target4Type = 0x5; order2.Target4 = iff; order2.T3AndOrT4 = false;
                    break;
                case 11: order1.Command = 4; order1.Variable1 = 1; break; //Rendezvous (1 docking)
                case 12: order1.Command = 5; break; //Disabled
                case 13: order1.Command = 12; break; //Board to Give Cargo
                case 14: order1.Command = 13; break; //Board to Take cargo
                case 15: order1.Command = 14; break; //Board to Exchange cargo
                case 16: order1.Command = 15; break; //Board to Capture
                case 17: order1.Command = 16; break; //Board to Destroy cargo
                case 18: order1.Command = 11; break; //Disable targets (primary, secondary)
                case 19:
                    //Disable all enemies (Fighter, TRN, SHU)
                    order1.Command = 11;
                    order1.Target3Type = 0x3; order1.Target3 = 0x0;  //Fighters
                    order1.Target4Type = 0x5; order1.Target4 = iff; order1.T3AndOrT4 = false;

                    order2.Command = 11;
                    order2.Target1Type = 0x3; order2.Target1 = 0x1;  //Transports
                    order2.Target2Type = 0x5; order2.Target2 = iff; order2.T1AndOrT2 = false;
                    order2.Target3Type = 0x3; order2.Target3 = 0x0;  //Fighters
                    order2.Target4Type = 0x5; order2.Target4 = iff; order2.T3AndOrT4 = false;
                    break;
                case 20:
                    //Attack Transports
                    order1.Command = 7;
                    order1.Target3Type = 0x3; order1.Target3 = 0x1;  //Transports
                    order1.Target4Type = 0x5; order1.Target4 = iff; order1.T3AndOrT4 = false;
                    break;
                case 21:
                    //Attack Freighters (and CRV)
                    order1.Command = 7;
                    order2.Command = 7;
                    order2.Target1Type = 0x3; order2.Target1 = 0x2;  //Freighters
                    order2.Target2Type = 0x5; order2.Target2 = iff; order2.T1AndOrT2 = false;
                    order2.Target3Type = 0x2; order2.Target3 = 0x27;  //CRV
                    order2.Target4Type = 0x5; order2.Target4 = iff; order2.T3AndOrT4 = false;
                    break;
                case 22:
                    //Attack Starships
                    order1.Command = 7;
                    order1.Target3Type = 0x3; order1.Target3 = 0x3;  //Starships
                    order1.Target4Type = 0x5; order1.Target4 = iff; order1.T3AndOrT4 = false;
                    break;
                case 23:
                    //Disable Transports
                    order1.Command = 11;
                    order1.Target3Type = 0x3; order1.Target3 = 0x1;  //Transports
                    order1.Target4Type = 0x5; order1.Target4 = iff; order1.T3AndOrT4 = false;
                    break;
                case 24:
                    //Disable Freighters (and CRV)
                    order1.Command = 11;
                    order2.Command = 11;
                    order2.Target1Type = 0x3; order2.Target1 = 0x2;  //Freighters
                    order2.Target2Type = 0x5; order2.Target2 = iff; order2.T1AndOrT2 = false;
                    order2.Target3Type = 0x2; order2.Target3 = 0x27;  //CRV
                    order2.Target4Type = 0x5; order2.Target4 = iff; order2.T3AndOrT4 = false;
                    break;
                case 25:
                    //Disable Starships
                    order1.Command = 11;
                    order1.Target3Type = 0x3; order1.Target3 = 0x3;  //Starships
                    order1.Target4Type = 0x5; order1.Target4 = iff; order1.T3AndOrT4 = false;
                    break;
                case 26:
                    order1.Command = 22;    //SS Hold Position -> SS Await Return            [JB] Attempted workaround.  TIE format doesn't seem to have a proper SS Hold order that autotargets enemies, and patrol doesn't always play nice with orientation depending on how the mission waypoints are set up.  So do SS Await Return for autotargeting (probably works if it's a starship), then transition into SS Wait with maximum time.
                    order1.Target3Type = 0x5; order1.Target3 = iff; order1.T3AndOrT4 = true;
                    order1.Variable1 = 1;  //One loop So it doesn't head home.
                    order1.Throttle = 0;   //Zero throttle so it doesn't move.
                    order2.Command = 20;
                    order2.Target1Type = 0x5; order1.Target1 = iff; order1.T1AndOrT2 = true;
                    order2.Variable1 = 255; //Maximum wait time, 21:15
                    order2.Throttle = 0;    //Zero throttle so it doesn't move.
                    break;
                case 27:
                    order1.Command = 21; order1.Variable1 = 1; //SS Fly Once -> SS Patrol waypoints
                    assignIFFTarget(order1, iff);
                    break;
                case 28:
                    order1.Command = 21; order1.Variable1 = 127; //SS Circle -> SS Patrol waypoints
                    assignIFFTarget(order1, iff);
                    break;
                case 29:
                    order1.Command = 22; //SS Await Return
                    assignIFFTarget(order1, iff);
                    break;
                case 30:
                    order1.Command = 23; //SS Await Launch
                    assignIFFTarget(order1, iff);
                    break;
                case 31:
                    order1.Command = 39; //SS Await Boarding
                    assignIFFTarget(order1, iff);
                    break;
                case 32:
                    order1.Command = 39; //Wait for arrival of
                    assignIFFTarget(order1, iff);
                    break;
            }
        }

        /// <summary>Generates targets to attack an IFF.  Helper function used when setting orders.</summary>
        /// <remarks>XWING95 starships usually autotarget the opposite IFF, this implements such targeting.
        /// Attempts to assign to Target1 or Target2 if they are empty.</remarks>
        /// <param name="order1">Order object to modify.</param>
        /// <param name="iff">IFF to target.</param>
        static void assignIFFTarget(BaseFlightGroup.BaseOrder order1, byte iff)
        {
            if (order1.Target1Type == 0) { order1.Target1Type = 0x5; order1.Target1 = iff; order1.T1AndOrT2 = true; }      //There's a bug in TIE Fighter where Targets 3 and 4 will not function for SS Patrol orders.
            else if (order1.Target2Type == 0) { order1.Target2Type = 0x5; order1.Target2 = iff; order1.T1AndOrT2 = true; }
            else { order1.Target3Type = 0x5; order1.Target3 = iff; order1.T3AndOrT4 = true; }
        }

        /// <summary>Validates FlightGroup.Goals for TIE</summary>
		/// <remarks>Converts 75% to 100%, 25% to 50%</remarks>
		/// <param name="label">Identifier used in error message</param>
		/// <param name="goals">The Goal object to check</param>
		/// <exception cref="ArgumentException">Invalid Goal.Amount detected</exception>
		static void tieGoalsCheck(string label, Tie.FlightGroup.FGGoals goals)
        {
            for (int i = 0; i < 8; i += 2)
            {
                if (i == 4) continue;   // Secret goal, not converted
                if (goals[i] > 24) throw triggerException(0, label + " Goal " + i, Xwa.Strings.Trigger[goals[i]]);
                if (goals[i + 1] > 6) throw triggerException(3, label + " Goal " + i, Xwa.Strings.Amount[goals[i + 1]]);
                else if (goals[i + 1] == 1) goals[i + 1] = 0;   // 75 to 100
                else if (goals[i + 1] > 1) goals[i + 1] -= 2;   // 25 to 50, slide everything after
            }
        }

        /// <summary>Returns an ArgumentException formatted for MissionLimits based on the inputs</summary>
        /// <param name="toTie"><b>true</b> for TIE95, <b>false</b> for XvT</param>
        /// <param name="isFG"><b>true</b> for FlightGroups, <b>false</b> for Messages</param>
        /// <param name="limit">The appropriate Mission Limit value</param>
        static ArgumentException maxException(bool toTie, bool isFG, int limit)
        {
            string s = (isFG ? "FlightGroups" : "In-Flight Messages");
            return new ArgumentException("Number of " + s + " exceeds " + (toTie ? "TIE95" : "XvT")
                + " maximum (" + limit + "). Remove " + s + " before converting");
        }

        /// <summary>Returns an ArgumentException formatted for Triggers based on the inputs</summary>
        /// <param name="index">0 for Trigger condition, 1 for Trigger Type, 2 for Trigger Craft Type, 3 for Amount</param>
        /// <param name="label">Trigger indentifier string</param>
        /// <param name="id">String for the invalid value</param>
        static ArgumentException triggerException(byte index, string label, string id)
        {
            return new ArgumentException("Invalid Trigger "
                + (index == 0 ? "Condition" : (index == 1 ? "VariableType" : (index == 2 ? "Craft" : "Amount")))
                + " detected (" + id + "). " + label);
        }

        /// <summary>Returns an ArgumentException formatted for FlightGroups based on the inputs</summary>
        /// <param name="mode">0 for Status, 1 for Formation, 2 for Abort, 3 for Order, 4 for CraftType</param>
        /// <param name="index">FG index</param>
        /// <param name="id">String for the invalid value</param>
        static ArgumentException flightException(byte mode, int index, string id)
        {
            return new ArgumentException("Invalid FlightGroup "
            + (mode == 0 ? "Status" : (mode == 1 ? "Formation" : (mode == 2 ? "Abort condition" : (mode == 3 ? "Order" : "CraftType"))))
            + " detected. FG " + index + ", " + (mode == 3 ? "Order: " : "") + id);
        }
        #endregion
    }
}