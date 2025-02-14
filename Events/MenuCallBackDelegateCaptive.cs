﻿using CaptivityEvents.CampaignBehaviors;
using CaptivityEvents.Custom;
using CaptivityEvents.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CaptivityEvents.Events
{
    public class MenuCallBackDelegateCaptive
    {
        private readonly CEEvent _listedEvent;
        private readonly List<CEEvent> _eventList;
        private readonly Option _option;
        private readonly SharedCallBackHelper _sharedCallBackHelper;
        private readonly CECompanionSystem _companionSystem;
        private readonly Dynamics _dynamics = new Dynamics();
        private readonly ScoresCalculation _score = new ScoresCalculation();
        private readonly CEImpregnationSystem _impregnation = new CEImpregnationSystem();
        private readonly CEVariablesLoader _variableLoader = new CEVariablesLoader();

        private float _timer = 0;
        private float _max = 0;

        private readonly CaptiveSpecifics _captive = new CaptiveSpecifics();

        internal MenuCallBackDelegateCaptive(CEEvent listedEvent, List<CEEvent> eventList)
        {
            _listedEvent = listedEvent;
            _eventList = eventList;
            _sharedCallBackHelper = new SharedCallBackHelper(listedEvent, null, eventList);
            _companionSystem = new CECompanionSystem(listedEvent, null, eventList);
        }

        internal MenuCallBackDelegateCaptive(CEEvent listedEvent, Option option, List<CEEvent> eventList)
        {
            _listedEvent = listedEvent;
            _option = option;
            _eventList = eventList;
            _sharedCallBackHelper = new SharedCallBackHelper(listedEvent, option, eventList);
            _companionSystem = new CECompanionSystem(listedEvent, option, eventList);
        }

        #region Progress Event
        internal void CaptiveProgressInitWaitGameMenu(MenuCallbackArgs args)
        {
            if (args.MenuContext != null)
            {
                args.MenuContext.SetBackgroundMeshName(Hero.MainHero.IsFemale
                                           ? "wait_captive_female"
                                           : "wait_captive_male");
            }

            _sharedCallBackHelper.LoadBackgroundImage("default_random");
            _sharedCallBackHelper.ConsequencePlaySound(true);

            InitCaptiveTextVariables(ref args);

            if (_listedEvent.ProgressEvent != null)
            {
                _max = _variableLoader.GetFloatFromXML(_listedEvent.ProgressEvent.TimeToTake);
                _timer = 0f;

                CEHelper.progressEventExists = true;
                CEHelper.notificationCaptorExists = false;
                CEHelper.notificationEventExists = false;
            }
            else
            {
                CECustomHandler.ForceLogToFile("Missing Progress Event Settings in " + _listedEvent.Name);
            }
        }
        internal bool CaptiveProgressConditionWaitGameMenu(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            return true;
        }
        internal void CaptiveProgressConsequenceWaitGameMenu(MenuCallbackArgs args)
        {
            if (_listedEvent.ProgressEvent.TriggerEvents != null && _listedEvent.ProgressEvent.TriggerEvents.Length > 0)
            {
                ConsequenceRandomEventTriggerProgress(ref args);
            }
            else if (!string.IsNullOrEmpty(_listedEvent.ProgressEvent.TriggerEventName))
            {
                ConsequenceSingleEventTriggerProgress(ref args);
            }
        }
        internal void CaptiveProgressTickWaitGameMenu(MenuCallbackArgs args, CampaignTime dt)
        {
            _timer += dt.CurrentHourInDay;

            if (PlayerCaptivity.CaptorParty.IsMobile)
            {
                PlayerCaptivity.CaptorParty.MobileParty.SetMoveModeHold();
            }

            if (_timer / _max == 1)
            {
                CEHelper.progressEventExists = false;
                PlayerCaptivity.CaptorParty.MobileParty.RecalculateShortTermAi();
            }

            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(_timer / _max);

        }
        #endregion

        #region Wait Menu
        internal void CaptiveInitWaitGameMenu(MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.IsSettlement && args.MenuContext != null)
            {
                args.MenuContext.SetBackgroundMeshName(Hero.MainHero.IsFemale
                                                           ? "wait_prisoner_female"
                                                           : "wait_prisoner_male");
                CEHelper.waitMenuCheck = 1;
            }
            else if (PlayerCaptivity.CaptorParty.IsMobile && args.MenuContext != null)
            {
                args.MenuContext.SetBackgroundMeshName(Hero.MainHero.IsFemale
                                           ? "wait_captive_female"
                                           : "wait_captive_male");
                CEHelper.waitMenuCheck = 2;
            }

            _sharedCallBackHelper.LoadBackgroundImage("default");
            _sharedCallBackHelper.ConsequencePlaySound(true);

            if (PlayerCaptivity.IsCaptive) InitCaptiveTextVariables(ref args);

            if (args.MenuContext != null) args.MenuContext.GameMenu.StartWait();
        }
        internal bool CaptiveConditionWaitGameMenu(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            return true;
        }
        internal void CaptiveConsequenceWaitGameMenu(MenuCallbackArgs args) { }
        internal void CaptiveTickWaitGameMenu(MenuCallbackArgs args, CampaignTime dt)
        {
            int captiveTimeInDays = PlayerCaptivity.CaptiveTimeInDays;
            TextObject text = args.MenuContext.GameMenu.GetText();

            InitCaptiveTimeInDays(captiveTimeInDays, ref text);

            if (!PlayerCaptivity.IsCaptive) return;

            text = CEHelper.ShouldChangeMenu(text);

            try
            {
                if (_listedEvent.SavedCompanions != null)
                {
                    foreach (KeyValuePair<string, Hero> item in _listedEvent.SavedCompanions)
                    {
                        text.SetTextVariable("COMPANION_NAME_" + item.Key, item.Value?.Name);
                        text.SetTextVariable("COMPANIONISFEMALE_" + item.Key, item.Value.IsFemale ? 1 : 0);
                    }
                }
            }
            catch (Exception)
            {
                CECustomHandler.ForceLogToFile("Failed to CaptiveTickWaitGameMenu for " + _listedEvent.Name);
            }

            if (PlayerCaptivity.CaptorParty.IsMobile && PlayerCaptivity.CaptorParty.MobileParty.IsActive) PartyBase.MainParty.MobileParty.Position2D = PlayerCaptivity.CaptorParty.MobileParty.Position2D;
            else if (PlayerCaptivity.CaptorParty.IsSettlement) PartyBase.MainParty.MobileParty.Position2D = PlayerCaptivity.CaptorParty.Settlement.GatePosition;
            PlayerCaptivity.CaptorParty.SetAsCameraFollowParty();

            string eventToRun = Campaign.Current.Models.PlayerCaptivityModel.CheckCaptivityChange(Campaign.Current.CampaignDt);
            if (!string.IsNullOrWhiteSpace(eventToRun)) GameMenu.SwitchToMenu(eventToRun);
        }
        #endregion

        #region Regular Event
        internal void CaptiveEventGameMenu(MenuCallbackArgs args)
        {
            _sharedCallBackHelper.LoadBackgroundImage();
            _sharedCallBackHelper.ConsequencePlaySound(true);
            InitCaptiveTextVariables(ref args);
        }
        internal bool CaptiveEventOptionGameMenu(MenuCallbackArgs args)
        {
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.AttemptEscape) || _option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.Escape)) args.optionLeaveType = GameMenuOption.LeaveType.Escape;
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.Leave)) args.optionLeaveType = GameMenuOption.LeaveType.Leave;

            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.GiveGold)) InitGiveGold();
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.ChangeGold)) InitChangeGold();
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.ChangeCaptorGold)) InitChangeCaptorGold();
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.SoldToSettlement)) InitSoldToSettlement();
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.SoldToCaravan)) InitSoldToCaravan();
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.SoldToLordParty)) InitSoldToLordParty();

            InitLeaveType(ref args);

            ReqMorale(ref args);
            ReqTroops(ref args);
            ReqMaleTroops(ref args);
            ReqFemaleTroops(ref args);
            ReqCaptives(ref args);
            ReqMaleCaptives(ref args);
            ReqFemaleCaptives(ref args);
            ReqHeroCaptorRelation(ref args);
            ReqHeroHealthPercentage(ref args);
            ReqSlavery(ref args);
            ReqProstitute(ref args);
            ReqTrait(ref args);
            ReqCaptorTrait(ref args);
            ReqHeroSkill(ref args);
            ReqCaptorSkill(ref args);
            ReqHeroSkills(ref args);
            ReqCaptorSkills(ref args);
            ReqGold(ref args);

            return true;
        }
        internal void CaptiveEventOptionConsequenceGameMenu(MenuCallbackArgs args)
        {
            // For Captive and Random Similarity
            _sharedCallBackHelper.ConsequenceXP();
            _sharedCallBackHelper.ConsequenceLeaveSpouse();
            _sharedCallBackHelper.ConsequenceGold();
            _sharedCallBackHelper.ConsequenceChangeGold();
            _sharedCallBackHelper.ConsequenceChangeTrait();
            _sharedCallBackHelper.ConsequenceChangeSkill();
            _sharedCallBackHelper.ConsequenceSlaveryLevel();
            _sharedCallBackHelper.ConsequenceSlaveryFlags();
            _sharedCallBackHelper.ConsequenceProstitutionLevel();
            _sharedCallBackHelper.ConsequenceProstitutionFlags();
            _sharedCallBackHelper.ConsequenceRenown();
            _sharedCallBackHelper.ConsequenceChangeHealth();
            _sharedCallBackHelper.ConsequenceChangeMorale();
            _sharedCallBackHelper.ConsequenceStripPlayer();
            _sharedCallBackHelper.ConsequencePlaySound();

            ConsequenceCompanions();
            ConsequenceSpawnTroop();
            ConsequenceSpawnHero();
            ConsequenceChangeClan();
            ConsequenceForceMarry();
            ConsequenceChangeKingdom();
            ConsequenceImpregnationByLeader();
            ConsequenceImpregnation();

            ConsequenceSpecificCaptor();
            ConsequenceSoldEvents(ref args);
            ConsequenceGainRandomPrisoners();

            ConsequenceWoundTroops();
            ConsequenceKillTroops();

            _sharedCallBackHelper.ConsequenceMission();
            _sharedCallBackHelper.ConsequenceTeleportPlayer();


            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.KillCaptor) && PlayerCaptivity.CaptorParty.NumberOfAllMembers == 1)
            {
                ConsequenceKillCaptor();
            } 
            else if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.KillPrisoner))
            {
                _dynamics.CEKillPlayer(PlayerCaptivity.CaptorParty.LeaderHero);
            }
            else if (_option.TriggerEvents != null && _option.TriggerEvents.Length > 0)
            {
                ConsequenceRandomEventTrigger(ref args);
            }
            else if (!string.IsNullOrEmpty(_option.TriggerEventName))
            {
                ConsequenceSingleEventTrigger(ref args);
            }
            else if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.StartBattle))
            {
                _sharedCallBackHelper.ConsequenceStartBattle(() => { _captive.CECaptivityContinue(ref args); }, 0);
            }
            else if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.AttemptEscape))
            {
                ConsequenceEscapeEventTrigger(ref args);
            }
            else if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.Escape))
            {
                _captive.CECaptivityEscape(ref args);
            }
            else if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.Leave))
            {
                _captive.CECaptivityLeave(ref args);
            }
            else
            {
                _captive.CECaptivityContinue(ref args);
            }
        }
        #endregion

        #region Consequences
        private void ConsequenceCompanions()
        {
            try
            {
                _companionSystem.ConsequenceCompanions(CharacterObject.PlayerCharacter, PlayerCaptivity.CaptorParty);
            }
            catch (Exception e)
            {
                CECustomHandler.ForceLogToFile("ConsequenceCaptiveCompanions. Failed" + e.ToString());
            }
        }
        private void ConsequenceKillCaptor()
        {

            if (!_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.KillCaptor) || PlayerCaptivity.CaptorParty.NumberOfAllMembers <= 1) return;

            try
            {
                if (PlayerCaptivity.CaptorParty.LeaderHero != null) KillCharacterAction.ApplyByMurder(PlayerCaptivity.CaptorParty.LeaderHero, Hero.MainHero);
#if V165
                else PlayerCaptivity.CaptorParty.MemberRoster.AddToCounts(PlayerCaptivity.CaptorParty.Leader, -1);
#else
#endif

                if (PlayerCaptivity.CaptorParty != null && PlayerCaptivity.CaptorParty.IsMobile && PlayerCaptivity.CaptorParty.MemberRoster.Count == 0 && PlayerCaptivity.CaptorParty.MobileParty.ActualClan != null)
                {
                   DestroyPartyAction.Apply(null, PlayerCaptivity.CaptorParty.MobileParty);
                }
            }
            catch (Exception e)
            {
                CECustomHandler.ForceLogToFile("ConsequenceKillCaptor. Failed" + e.ToString());
            }
        }
        private void ConsequenceRandomEventTriggerProgress(ref MenuCallbackArgs args)
        {
            List<CEEvent> eventNames = new List<CEEvent>();

            try
            {
                foreach (TriggerEvent triggerEvent in _listedEvent.ProgressEvent.TriggerEvents)
                {
                    CEEvent triggeredEvent = _eventList.Find(item => item.Name == triggerEvent.EventName);

                    if (triggeredEvent == null)
                    {
                        CECustomHandler.ForceLogToFile("Couldn't find " + triggerEvent.EventName + " in events.");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(triggerEvent.EventUseConditions) && triggerEvent.EventUseConditions.ToLower() == "true")
                    {
                        string conditionMatched = null;
                        if (triggeredEvent.MultipleRestrictedListOfFlags.Contains(RestrictedListOfFlags.Captive))
                        {
                            conditionMatched = new CEEventChecker(triggeredEvent).FlagsDoMatchEventConditions(CharacterObject.PlayerCharacter, PlayerCaptivity.CaptorParty);
                        }
                        else if (triggeredEvent.MultipleRestrictedListOfFlags.Contains(RestrictedListOfFlags.Random))
                        {
                            conditionMatched = new CEEventChecker(triggeredEvent).FlagsDoMatchEventConditions(CharacterObject.PlayerCharacter);
                        }

                        if (conditionMatched != null)
                        {
                            CECustomHandler.LogToFile(conditionMatched);
                            continue;
                        }
                    }

                    int weightedChance = 0;

                    try
                    {
                        weightedChance = new CEVariablesLoader().GetIntFromXML(!string.IsNullOrWhiteSpace(triggerEvent.EventWeight)
                                                                      ? triggerEvent.EventWeight
                                                                      : triggeredEvent.WeightedChanceOfOccuring);
                    }
                    catch (Exception) { CECustomHandler.LogToFile("Missing EventWeight"); }

                    if (weightedChance == 0) weightedChance = 1;

                    for (int a = weightedChance; a > 0; a--) eventNames.Add(triggeredEvent);
                }

                if (eventNames.Count > 0)
                {
                    int number = MBRandom.Random.Next(0, eventNames.Count);

                    try
                    {
                        CEEvent triggeredEvent = eventNames[number];
                        triggeredEvent.Captive = CharacterObject.PlayerCharacter;
                        triggeredEvent.SavedCompanions = _listedEvent.SavedCompanions;
                        GameMenu.ActivateGameMenu(triggeredEvent.Name);
                    }
                    catch (Exception)
                    {
                        CECustomHandler.ForceLogToFile("Couldn't find " + eventNames[number] + " in events.");
                        _captive.CECaptivityContinue(ref args);
                    }
                }
                else { _captive.CECaptivityContinue(ref args); }
            }
            catch (Exception)
            {
                CECustomHandler.LogToFile("MBRandom.Random in events Failed.");
                _captive.CECaptivityContinue(ref args);
            }
        }
        private void ConsequenceSingleEventTriggerProgress(ref MenuCallbackArgs args)
        {

            try
            {
                CEEvent triggeredEvent = _eventList.Find(item => item.Name == _listedEvent.ProgressEvent.TriggerEventName);
                triggeredEvent.Captive = CharacterObject.PlayerCharacter;
                triggeredEvent.SavedCompanions = _listedEvent.SavedCompanions;
                GameMenu.SwitchToMenu(triggeredEvent.Name);
            }
            catch (Exception)
            {
                CECustomHandler.ForceLogToFile("Couldn't find " + _listedEvent.ProgressEvent.TriggerEventName + " in events.");
                _captive.CECaptivityContinue(ref args);
            }
        }
        private void ConsequenceRandomEventTrigger(ref MenuCallbackArgs args)
        {
            List<CEEvent> eventNames = new List<CEEvent>();

            try
            {
                foreach (TriggerEvent triggerEvent in _option.TriggerEvents)
                {
                    CEEvent triggeredEvent = _eventList.Find(item => item.Name == triggerEvent.EventName);

                    if (triggeredEvent == null)
                    {
                        CECustomHandler.ForceLogToFile("Couldn't find " + triggerEvent.EventName + " in events.");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(triggerEvent.EventUseConditions) && triggerEvent.EventUseConditions.ToLower() == "true")
                    {
                        string conditionMatched = null;
                        if (triggeredEvent.MultipleRestrictedListOfFlags.Contains(RestrictedListOfFlags.Captive))
                        {
                            conditionMatched = new CEEventChecker(triggeredEvent).FlagsDoMatchEventConditions(CharacterObject.PlayerCharacter, PlayerCaptivity.CaptorParty);
                        }
                        else if (triggeredEvent.MultipleRestrictedListOfFlags.Contains(RestrictedListOfFlags.Random))
                        {
                            conditionMatched = new CEEventChecker(triggeredEvent).FlagsDoMatchEventConditions(CharacterObject.PlayerCharacter);
                        }

                        if (conditionMatched != null)
                        {
                            CECustomHandler.LogToFile(conditionMatched);
                            continue;
                        }
                    }

                    int weightedChance = 0;

                    try
                    {
                        weightedChance = new CEVariablesLoader().GetIntFromXML(!string.IsNullOrWhiteSpace(triggerEvent.EventWeight)
                                                                      ? triggerEvent.EventWeight
                                                                      : triggeredEvent.WeightedChanceOfOccuring);
                    }
                    catch (Exception) { CECustomHandler.LogToFile("Missing EventWeight"); }

                    if (weightedChance == 0) weightedChance = 1;

                    for (int a = weightedChance; a > 0; a--) eventNames.Add(triggeredEvent);
                }

                if (eventNames.Count > 0)
                {
                    int number = MBRandom.Random.Next(0, eventNames.Count);

                    try
                    {
                        CEEvent triggeredEvent = eventNames[number];
                        triggeredEvent.Captive = CharacterObject.PlayerCharacter;
                        triggeredEvent.SavedCompanions = _listedEvent.SavedCompanions;
                        GameMenu.ActivateGameMenu(triggeredEvent.Name);
                    }
                    catch (Exception)
                    {
                        CECustomHandler.ForceLogToFile("Couldn't find " + eventNames[number] + " in events.");
                        _captive.CECaptivityContinue(ref args);
                    }
                }
                else { _captive.CECaptivityContinue(ref args); }
            }
            catch (Exception)
            {
                CECustomHandler.LogToFile("MBRandom.Random in events Failed.");
                _captive.CECaptivityContinue(ref args);
            }
        }
        private void ConsequenceSingleEventTrigger(ref MenuCallbackArgs args)
        {

            try
            {
                CEEvent triggeredEvent = _eventList.Find(item => item.Name == _option.TriggerEventName);
                triggeredEvent.Captive = CharacterObject.PlayerCharacter;
                triggeredEvent.SavedCompanions = _listedEvent.SavedCompanions;
                GameMenu.SwitchToMenu(triggeredEvent.Name);
            }
            catch (Exception)
            {
                CECustomHandler.ForceLogToFile("Couldn't find " + _option.TriggerEventName + " in events.");
                _captive.CECaptivityContinue(ref args);
            }
        }
        private void ConsequenceEscapeEventTrigger(ref MenuCallbackArgs args)
        {
            try
            {
                _captive.CECaptivityEscapeAttempt(ref args, !string.IsNullOrEmpty(_option.EscapeChance)
                                                      ? new CEVariablesLoader().GetIntFromXML(_option.EscapeChance)
                                                      : new CEVariablesLoader().GetIntFromXML(_listedEvent.EscapeChance));
            }
            catch (Exception)
            {
                CECustomHandler.LogToFile("Missing EscapeChance");
                _captive.CECaptivityEscapeAttempt(ref args);
            }
        }
        private void ConsequenceGainRandomPrisoners()
        {
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.GainRandomPrisoners)) _dynamics.CEGainRandomPrisoners(PlayerCaptivity.CaptorParty);
        }
        private void ConsequenceWoundTroops()
        {
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.WoundRandomTroops)) _dynamics.CEWoundTroops(PlayerCaptivity.CaptorParty);
        }
        private void ConsequenceKillTroops()
        {
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.KillRandomTroops)) _dynamics.CEKillTroops(PlayerCaptivity.CaptorParty);
        }
        private void ConsequenceSoldEvents(ref MenuCallbackArgs args)
        {
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.SoldToCaravan)) ConsequenceSoldToCaravan(ref args);
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.SoldToSettlement)) ConsequenceSoldToSettlement(ref args);
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.SoldToLordParty)) ConsequenceSoldToLordParty(ref args);
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.SoldToNotable)) ConsequenceSoldToNotable(ref args);
        }
        private void ConsequenceSoldToNotable(ref MenuCallbackArgs args)
        {
            try
            {
                Settlement settlement = PlayerCaptivity.CaptorParty.IsSettlement
                    ? PlayerCaptivity.CaptorParty.Settlement
                    : PlayerCaptivity.CaptorParty.MobileParty.CurrentSettlement;

                Hero notable = settlement.Notables.GetRandomElementWithPredicate(findFirstNotable => !findFirstNotable.IsFemale);
                //Hero notable = settlement.Notables.Where(findFirstNotable => !findFirstNotable.IsFemale).GetRandomElement();
                CECampaignBehavior.ExtraProps.Owner = notable;
                _captive.CECaptivityChange(ref args, settlement.Party);
            }
            catch (Exception) { CECustomHandler.LogToFile("Failed to get Settlement"); }
        }
        private void ConsequenceSoldToLordParty(ref MenuCallbackArgs args)
        {
            try
            {
                MobileParty party = null;

                party = PlayerCaptivity.CaptorParty.IsSettlement
                    ? PlayerCaptivity.CaptorParty.Settlement.Parties.FirstOrDefault(mobileParty => mobileParty.IsLordParty && !mobileParty.IsMainParty)
                    : PlayerCaptivity.CaptorParty.MobileParty.CurrentSettlement.Parties.FirstOrDefault(mobileParty => mobileParty.IsLordParty && !mobileParty.IsMainParty);

                if (party == null) return;

                _captive.CECaptivityChange(ref args, party.Party);
                CECampaignBehavior.ExtraProps.Owner = party.LeaderHero;
            }
            catch (Exception) { CECustomHandler.LogToFile("Failed to get Lord"); }
        }
        private void ConsequenceSoldToSettlement(ref MenuCallbackArgs args)
        {
            try
            {
                PartyBase party = !PlayerCaptivity.CaptorParty.IsSettlement
                    ? PlayerCaptivity.CaptorParty.MobileParty.CurrentSettlement.Party
                    : PlayerCaptivity.CaptorParty;

                CECampaignBehavior.ExtraProps.Owner = null;
                _captive.CECaptivityChange(ref args, party);
            }
            catch (Exception) { CECustomHandler.LogToFile("Failed to get Settlement"); }
        }
        private void ConsequenceSoldToCaravan(ref MenuCallbackArgs args)
        {
            try
            {
                MobileParty party = null;

                if (PlayerCaptivity.CaptorParty.IsSettlement)
                {
                    CECampaignBehavior.ExtraProps.Owner = null;
                    party = PlayerCaptivity.CaptorParty.Settlement.Parties.FirstOrDefault(mobileParty => mobileParty.IsCaravan);
                }
                else
                {
                    CECampaignBehavior.ExtraProps.Owner = null;
                    party = PlayerCaptivity.CaptorParty.MobileParty.CurrentSettlement.Parties.FirstOrDefault(mobileParty => mobileParty.IsCaravan);
                }

                if (party != null) _captive.CECaptivityChange(ref args, party.Party);
            }
            catch (Exception) { CECustomHandler.LogToFile("Failed to get Caravan"); }
        }
        private void ConsequenceSpecificCaptor()
        {
            if (PlayerCaptivity.CaptorParty == null)
            {
                CECustomHandler.ForceLogToFile("Using Wrong Category for the Event of " + _listedEvent.Name.ToString());
                InformationManager.DisplayMessage(new InformationMessage("Using Wrong Category for the Event of " + _listedEvent.Name.ToString(), Colors.Red));
                GameMenu.ExitToLast();
                return;
            }
            if (CECampaignBehavior.ExtraProps.Owner == null && (PlayerCaptivity.CaptorParty.IsSettlement || !PlayerCaptivity.CaptorParty.IsMobile || PlayerCaptivity.CaptorParty.MobileParty.LeaderHero == null)) return;

            Hero hero = CECampaignBehavior.ExtraProps.Owner ?? PlayerCaptivity.CaptorParty.MobileParty.LeaderHero;

            ConsequenceSpecificCaptorRelations(hero);
            ConsequenceSpecificCaptorGold(hero);
            ConsequenceSpecificCaptorChangeGold(hero);
            ConsequenceSpecificCaptorSkill(hero);
            ConsequenceSpecificCaptorTrait(hero);
            ConsequenceSpecificCaptorRenown(hero);
        }
        private void ConsequenceSpecificCaptorRenown(Hero hero)
        {
            if (!_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.ChangeCaptorRenown)) return;

            try
            {
                _dynamics.RenownModifier(!string.IsNullOrEmpty(_option.RenownTotal)
                                             ? new CEVariablesLoader().GetIntFromXML(_option.RenownTotal)
                                             : new CEVariablesLoader().GetIntFromXML(_listedEvent.RenownTotal), hero);
            }
            catch (Exception)
            {
                CECustomHandler.LogToFile("Missing RenownTotal");
                _dynamics.RenownModifier(MBRandom.RandomInt(-5, 5), hero);
            }
        }
        private void ConsequenceSpecificCaptorSkill(Hero hero)
        {
            try
            {
                if (_option.SkillsToLevel != null && _option.SkillsToLevel.Count(SkillToLevel => SkillToLevel.Ref.ToLower() == "captor") != 0)
                {
                    foreach (SkillToLevel skillToLevel in _option.SkillsToLevel)
                    {
                        int level = 0;
                        int xp = 0;

                        if (skillToLevel.Ref.ToLower() != "captor") continue;
                        if (!string.IsNullOrWhiteSpace(skillToLevel.ByLevel)) level = new CEVariablesLoader().GetIntFromXML(skillToLevel.ByLevel);
                        else if (!string.IsNullOrWhiteSpace(skillToLevel.ByXP)) xp = new CEVariablesLoader().GetIntFromXML(skillToLevel.ByXP);


                        new Dynamics().SkillModifier(hero, skillToLevel.Id, level, xp, !skillToLevel.HideNotification, skillToLevel.Color);
                    }
                }
                else if (_listedEvent.SkillsToLevel != null && _listedEvent.SkillsToLevel.Count(SkillToLevel => SkillToLevel.Ref.ToLower() == "captor") != 0)
                {
                    foreach (SkillToLevel skillToLevel in _listedEvent.SkillsToLevel)
                    {
                        int level = 0;
                        int xp = 0;

                        if (skillToLevel.Ref.ToLower() != "captor") continue;
                        if (!string.IsNullOrWhiteSpace(skillToLevel.ByLevel)) level = new CEVariablesLoader().GetIntFromXML(skillToLevel.ByLevel);
                        else if (!string.IsNullOrWhiteSpace(skillToLevel.ByXP)) xp = new CEVariablesLoader().GetIntFromXML(skillToLevel.ByXP);

                        new Dynamics().SkillModifier(hero, skillToLevel.Id, level, xp, !skillToLevel.HideNotification, skillToLevel.Color);
                    }
                }
                else
                {
                    if (!_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.ChangeCaptorSkill)) return;

                    int level = 0;
                    int xp = 0;

                    if (!string.IsNullOrWhiteSpace(_option.SkillTotal)) level = new CEVariablesLoader().GetIntFromXML(_option.SkillTotal);
                    else if (!string.IsNullOrWhiteSpace(_option.SkillXPTotal)) xp = new CEVariablesLoader().GetIntFromXML(_option.SkillXPTotal);
                    else if (!string.IsNullOrWhiteSpace(_listedEvent.SkillTotal)) level = new CEVariablesLoader().GetIntFromXML(_listedEvent.SkillTotal);
                    else if (!string.IsNullOrWhiteSpace(_listedEvent.SkillXPTotal)) xp = new CEVariablesLoader().GetIntFromXML(_listedEvent.SkillXPTotal);
                    else CECustomHandler.LogToFile("Missing Skill SkillTotal");

                    if (!string.IsNullOrWhiteSpace(_option.SkillToLevel)) new Dynamics().SkillModifier(hero, _option.SkillToLevel, level, xp);
                    else if (!string.IsNullOrWhiteSpace(_listedEvent.SkillToLevel)) new Dynamics().SkillModifier(hero, _listedEvent.SkillToLevel, level, xp);
                    else CECustomHandler.LogToFile("Missing SkillToLevel");
                }

            }
            catch (Exception) { CECustomHandler.LogToFile("Invalid Skill Flags"); }
        }
        private void ConsequenceSpecificCaptorTrait(Hero hero)
        {
            try
            {
                if (_option.TraitsToLevel != null && _option.TraitsToLevel.Count(TraitToLevel => TraitToLevel.Ref.ToLower() == "captor") != 0)
                {
                    foreach (TraitToLevel traitToLevel in _option.TraitsToLevel)
                    {
                        int level = 0;
                        int xp = 0;

                        if (traitToLevel.Ref.ToLower() != "captor") continue;
                        if (!string.IsNullOrWhiteSpace(traitToLevel.ByLevel)) level = new CEVariablesLoader().GetIntFromXML(traitToLevel.ByLevel);
                        else if (!string.IsNullOrWhiteSpace(traitToLevel.ByXP)) xp = new CEVariablesLoader().GetIntFromXML(traitToLevel.ByXP);

                        _dynamics.TraitModifier(hero, traitToLevel.Id, level, xp, !traitToLevel.HideNotification, traitToLevel.Color);
                    }
                }
                else if (_listedEvent.TraitsToLevel != null && _listedEvent.TraitsToLevel.Count(TraitsToLevel => TraitsToLevel.Ref.ToLower() == "captor") != 0)
                {
                    foreach (TraitToLevel traitToLevel in _listedEvent.TraitsToLevel)
                    {
                        int level = 0;
                        int xp = 0;

                        if (traitToLevel.Ref.ToLower() != "captor") continue;
                        if (!string.IsNullOrWhiteSpace(traitToLevel.ByLevel)) level = new CEVariablesLoader().GetIntFromXML(traitToLevel.ByLevel);
                        else if (!string.IsNullOrWhiteSpace(traitToLevel.ByXP)) xp = new CEVariablesLoader().GetIntFromXML(traitToLevel.ByXP);

                        _dynamics.TraitModifier(hero, traitToLevel.Id, level, xp, !traitToLevel.HideNotification, traitToLevel.Color);
                    }
                }
                else
                {
                    if (!_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.ChangeCaptorTrait)) return;

                    int level = 0;
                    int xp = 0;

                    if (!string.IsNullOrEmpty(_option.TraitTotal)) level = new CEVariablesLoader().GetIntFromXML(_option.TraitTotal);
                    else if (!string.IsNullOrEmpty(_option.TraitXPTotal)) xp = new CEVariablesLoader().GetIntFromXML(_option.TraitXPTotal);
                    else if (!string.IsNullOrEmpty(_listedEvent.TraitTotal)) level = new CEVariablesLoader().GetIntFromXML(_listedEvent.TraitTotal);
                    else if (!string.IsNullOrEmpty(_listedEvent.TraitXPTotal)) xp = new CEVariablesLoader().GetIntFromXML(_listedEvent.TraitXPTotal);
                    else CECustomHandler.LogToFile("Missing Trait TraitTotal");

                    if (!string.IsNullOrEmpty(_option.TraitToLevel)) _dynamics.TraitModifier(hero, _option.TraitToLevel, level, xp);
                    else if (!string.IsNullOrEmpty(_listedEvent.TraitToLevel)) _dynamics.TraitModifier(hero, _listedEvent.TraitToLevel, level, xp);
                    else CECustomHandler.LogToFile("Missing TraitToLevel");
                }
            }
            catch (Exception) { CECustomHandler.LogToFile("Invalid Trait Flags"); }
        }
        private void ConsequenceSpecificCaptorChangeGold(Hero hero)
        {
            if (!_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.ChangeCaptorGold)) return;

            try
            {
                int level = 0;

                if (!string.IsNullOrEmpty(_option.CaptorGoldTotal)) level = new CEVariablesLoader().GetIntFromXML(_option.CaptorGoldTotal);
                else if (!string.IsNullOrEmpty(_listedEvent.CaptorGoldTotal)) level = new CEVariablesLoader().GetIntFromXML(_listedEvent.CaptorGoldTotal);
                else CECustomHandler.LogToFile("Missing CaptorGoldTotal");

                GiveGoldAction.ApplyBetweenCharacters(null, hero, level);
            }
            catch (Exception) { CECustomHandler.LogToFile("Invalid CaptorGoldTotal"); }
        }
        private void ConsequenceSpecificCaptorGold(Hero hero)
        {
            if (!_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.GiveCaptorGold)) return;

            int content = _score.AttractivenessScore(Hero.MainHero);
            int currentValue = Hero.MainHero.GetSkillValue(CESkills.Prostitution);
            content += currentValue / 2;
            content *= _option.MultipleRestrictedListOfConsequences.Count(consequence => consequence == RestrictedListOfConsequences.GiveCaptorGold);
            GiveGoldAction.ApplyBetweenCharacters(null, hero, content);
        }
        private void ConsequenceSpecificCaptorRelations(Hero hero)
        {
            if (!_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.ChangeRelation)) return;
            bool InformationMessage = !_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.NoInformationMessage);
            bool NoMessages = _option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.NoMessages);


            try
            {
                _dynamics.RelationsModifier(hero, !string.IsNullOrEmpty(_option.RelationTotal)
                                                ? new CEVariablesLoader().GetIntFromXML(_option.RelationTotal)
                                                : new CEVariablesLoader().GetIntFromXML(_listedEvent.RelationTotal), null, InformationMessage && !NoMessages, !InformationMessage && !NoMessages);
            }
            catch (Exception)
            {
                CECustomHandler.LogToFile("Missing RelationTotal");
            }
        }
        private void ConsequenceSpawnTroop()
        {
            if (_option.SpawnTroops != null)
            {
                new CESpawnSystem().SpawnTheTroops(_option.SpawnTroops, PlayerCaptivity.CaptorParty);
            }
        }
        private void ConsequenceSpawnHero()
        {
            if (_option.SpawnHeroes != null)
            {
                new CESpawnSystem().SpawnTheHero(_option.SpawnHeroes, PlayerCaptivity.CaptorParty);
            }
        }
        private void ConsequenceImpregnation()
        {
            if (!_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.ImpregnationRisk)) return;

            try
            {
                if (!string.IsNullOrEmpty(_option.PregnancyRiskModifier)) { _impregnation.CaptivityImpregnationChance(Hero.MainHero, new CEVariablesLoader().GetIntFromXML(_option.PregnancyRiskModifier), false, false); }
                else if (!string.IsNullOrEmpty(_listedEvent.PregnancyRiskModifier)) { _impregnation.CaptivityImpregnationChance(Hero.MainHero, new CEVariablesLoader().GetIntFromXML(_listedEvent.PregnancyRiskModifier), false, false); }
                else
                {
                    CECustomHandler.LogToFile("Missing PregnancyRiskModifier");
                    _impregnation.CaptivityImpregnationChance(Hero.MainHero, 30, false, false);
                }
            }
            catch (Exception) { CECustomHandler.LogToFile("Invalid PregnancyRiskModifier"); }
        }
        private void ConsequenceImpregnationByLeader()
        {
            if (!_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.ImpregnationHero)) return;

            try
            {
                if (!string.IsNullOrEmpty(_option.PregnancyRiskModifier)) { _impregnation.CaptivityImpregnationChance(Hero.MainHero, new CEVariablesLoader().GetIntFromXML(_option.PregnancyRiskModifier)); }
                else if (!string.IsNullOrEmpty(_listedEvent.PregnancyRiskModifier)) { _impregnation.CaptivityImpregnationChance(Hero.MainHero, new CEVariablesLoader().GetIntFromXML(_listedEvent.PregnancyRiskModifier)); }
                else
                {
                    CECustomHandler.LogToFile("Missing PregnancyRiskModifier");
                    _impregnation.CaptivityImpregnationChance(Hero.MainHero, 30);
                }
            }
            catch (Exception) { CECustomHandler.LogToFile("Invalid PregnancyRiskModifier"); }
        }
        private void ConsequenceChangeClan()
        {
            if (_option.ClanOptions == null) return;

            if (PlayerCaptivity.CaptorParty != null && PlayerCaptivity.CaptorParty.LeaderHero != null) _dynamics.ClanChange(_option.ClanOptions, Hero.MainHero, PlayerCaptivity.CaptorParty.LeaderHero);
            else if (CECampaignBehavior.ExtraProps.Owner != null) _dynamics.ClanChange(_option.ClanOptions, Hero.MainHero, CECampaignBehavior.ExtraProps.Owner);
            else _dynamics.ClanChange(_option.ClanOptions, Hero.MainHero, null);
        }
        private void ConsequenceChangeKingdom()
        {
            if (_option.KingdomOptions == null) return;

            if (PlayerCaptivity.CaptorParty != null && PlayerCaptivity.CaptorParty.LeaderHero != null) _dynamics.KingdomChange(_option.KingdomOptions, Hero.MainHero, PlayerCaptivity.CaptorParty.LeaderHero);
            else if (CECampaignBehavior.ExtraProps.Owner != null) _dynamics.KingdomChange(_option.KingdomOptions, Hero.MainHero, CECampaignBehavior.ExtraProps.Owner);
            else _dynamics.KingdomChange(_option.KingdomOptions, Hero.MainHero, null);
        }
        private void ConsequenceForceMarry()
        {
            if (!_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.CaptiveMarryCaptor)) return;

            if (PlayerCaptivity.CaptorParty != null && PlayerCaptivity.CaptorParty.LeaderHero != null) _dynamics.ChangeSpouse(Hero.MainHero, PlayerCaptivity.CaptorParty.LeaderHero);
            else if (PlayerCaptivity.CaptorParty != null && CECampaignBehavior.ExtraProps.Owner != null) _dynamics.ChangeSpouse(Hero.MainHero, CECampaignBehavior.ExtraProps.Owner);
        }
#endregion

#region Requirements

#region ReqGold
        private void ReqGold(ref MenuCallbackArgs args)
        {
            try
            {
                if (!string.IsNullOrEmpty(_option.ReqGoldAbove)) ReqGoldAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqGoldAbove / Failed "); }

            try
            {
                if (!string.IsNullOrEmpty(_option.ReqGoldBelow)) ReqGoldBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqGoldBelow / Failed "); }
        }
        private void ReqGoldBelow(ref MenuCallbackArgs args)
        {
            if (Hero.MainHero.Gold <= new CEVariablesLoader().GetIntFromXML(_option.ReqGoldBelow)) return;

            args.Tooltip = GameTexts.FindText("str_CE_gold_level", "high");
            args.IsEnabled = false;
        }
        private void ReqGoldAbove(ref MenuCallbackArgs args)
        {
            if (Hero.MainHero.Gold >= new CEVariablesLoader().GetIntFromXML(_option.ReqGoldAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_gold_level", "low");
            args.IsEnabled = false;
        }
#endregion

#region ReqSkills
        private void ReqCaptorSkills(ref MenuCallbackArgs args)
        {
            if (_option.SkillsRequired == null) return;

            foreach (SkillRequired skillRequired in _option.SkillsRequired)
            {
                if (skillRequired.Ref == "Hero") continue;

                if (PlayerCaptivity.CaptorParty.LeaderHero == null)
                {
                    args.IsEnabled = false;
                    return;
                }

                SkillObject foundSkill = CESkills.FindSkill(skillRequired.Id);

                if (foundSkill == null)
                {
                    CECustomHandler.ForceLogToFile("Could not find " + skillRequired.Id);
                    return;
                }

                int skillLevel = PlayerCaptivity.CaptorParty.LeaderHero.GetSkillValue(foundSkill);

                try
                {
                    if (ReqSkillsLevelAbove(ref args, foundSkill, skillLevel, skillRequired.Min, "str_CE_skill_captor_level")) break;
                }
                catch (Exception) { CECustomHandler.LogToFile("Invalid SkillRequiredAbove"); }

                try
                {
                    if (ReqSkillsLevelBelow(ref args, foundSkill, skillLevel, skillRequired.Max, "str_CE_skill_captor_level")) break;
                }
                catch (Exception) { CECustomHandler.LogToFile("Invalid SkillRequiredBelow"); }

            }
        }
        private void ReqHeroSkills(ref MenuCallbackArgs args)
        {
            if (_option.SkillsRequired == null) return;

            foreach (SkillRequired skillRequired in _option.SkillsRequired)
            {
                if (skillRequired.Ref == "Captor") continue;

                SkillObject foundSkill = CESkills.FindSkill(skillRequired.Id);

                if (foundSkill == null)
                {
                    CECustomHandler.ForceLogToFile("Could not find " + skillRequired.Id);
                    return;
                }

                int skillLevel = Hero.MainHero.GetSkillValue(foundSkill);

                try
                {
                    if (ReqSkillsLevelAbove(ref args, foundSkill, skillLevel, skillRequired.Min, "str_CE_skill_level")) break;
                }
                catch (Exception) { CECustomHandler.LogToFile("Invalid SkillRequiredAbove"); }

                try
                {
                    if (ReqSkillsLevelBelow(ref args, foundSkill, skillLevel, skillRequired.Max, "str_CE_skill_level")) break;
                }
                catch (Exception) { CECustomHandler.LogToFile("Invalid SkillRequiredBelow"); }

            }
        }
        private bool ReqSkillsLevelBelow(ref MenuCallbackArgs args, SkillObject skillRequired, int skillLevel, string max, string type)
        {
            if (string.IsNullOrWhiteSpace(max)) return false;
            if (skillLevel <= new CEVariablesLoader().GetIntFromXML(max)) return false;

            TextObject text = GameTexts.FindText(type, "high");
            text.SetTextVariable("SKILL", skillRequired.Name);
            args.Tooltip = text;
            args.IsEnabled = false;

            return true;
        }
        private bool ReqSkillsLevelAbove(ref MenuCallbackArgs args, SkillObject skillRequired, int skillLevel, string min, string type)
        {
            if (string.IsNullOrWhiteSpace(min)) return false;
            if (skillLevel >= new CEVariablesLoader().GetIntFromXML(min)) return false;

            TextObject text = GameTexts.FindText(type, "low");
            text.SetTextVariable("SKILL", skillRequired.Name);
            args.Tooltip = text;
            args.IsEnabled = false;

            return true;
        }
#endregion

#region ReqCaptorSkill
        private void ReqCaptorSkill(ref MenuCallbackArgs args)
        {
            if (string.IsNullOrWhiteSpace(_option.ReqCaptorSkill)) return;

            if (PlayerCaptivity.CaptorParty.LeaderHero == null) args.IsEnabled = false;
            int skillLevel = 0;

            try
            {
                SkillObject foundSkill = CESkills.FindSkill(_option.ReqCaptorSkill);
                if (foundSkill == null)
                    CECustomHandler.LogToFile("Invalid Skill Captor");
                else
                    skillLevel = PlayerCaptivity.CaptorParty.LeaderHero.GetSkillValue(foundSkill);
            }
            catch (Exception)
            {
                CECustomHandler.LogToFile("Invalid Skill Captor");
                skillLevel = 0;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqCaptorSkillLevelAbove)) ReqCaptorSkillLevelAbove(ref args, skillLevel);
            }
            catch (Exception) { CECustomHandler.LogToFile("Missing ReqCaptorSkillLevelAbove"); }

            try
            {
                if (string.IsNullOrWhiteSpace(_option.ReqCaptorSkillLevelBelow)) ReqCaptorSkillLevelBelow(ref args, skillLevel);
            }
            catch (Exception) { CECustomHandler.LogToFile("Missing ReqCaptorSkillLevelBelow"); }
        }
        private void ReqCaptorSkillLevelBelow(ref MenuCallbackArgs args, int skillLevel)
        {
            if (skillLevel <= new CEVariablesLoader().GetIntFromXML(_option.ReqCaptorSkillLevelBelow)) return;

            TextObject text = GameTexts.FindText("str_CE_skill_captor_level", "high");
            text.SetTextVariable("SKILL", _option.ReqCaptorSkill);
            args.Tooltip = text;
            args.IsEnabled = false;
        }
        private void ReqCaptorSkillLevelAbove(ref MenuCallbackArgs args, int skillLevel)
        {
            if (skillLevel >= new CEVariablesLoader().GetIntFromXML(_option.ReqCaptorSkillLevelAbove)) return;

            TextObject text = GameTexts.FindText("str_CE_skill_captor_level", "low");
            text.SetTextVariable("SKILL", _option.ReqCaptorSkill);
            args.Tooltip = text;
            args.IsEnabled = false;
        }
#endregion

#region ReqHeroSkill
        private void ReqHeroSkill(ref MenuCallbackArgs args)
        {
            if (string.IsNullOrWhiteSpace(_option.ReqHeroSkill)) return;

            int skillLevel = 0;

            try
            {
                SkillObject foundSkill = CESkills.FindSkill(_option.ReqHeroSkill);
                if (foundSkill == null)
                    CECustomHandler.LogToFile("Invalid Skill Captive");
                else
                    skillLevel = Hero.MainHero.GetSkillValue(foundSkill);
            }
            catch (Exception)
            {
                CECustomHandler.LogToFile("Invalid Skill Captive");
                skillLevel = 0;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroSkillLevelAbove)) ReqHeroSkillLevelAbove(ref args, skillLevel);
            }
            catch (Exception) { CECustomHandler.LogToFile("Invalid ReqHeroSkillLevelAbove"); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroSkillLevelBelow)) ReqHeroSkillLevelBelow(ref args, skillLevel);
            }
            catch (Exception) { CECustomHandler.LogToFile("Invalid ReqHeroSkillLevelBelow"); }
        }
        private void ReqHeroSkillLevelBelow(ref MenuCallbackArgs args, int skillLevel)
        {
            if (skillLevel <= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroSkillLevelBelow)) return;

            TextObject text = GameTexts.FindText("str_CE_skill_level", "high");
            text.SetTextVariable("SKILL", _option.ReqHeroSkill);
            args.Tooltip = text;
            args.IsEnabled = false;
        }
        private void ReqHeroSkillLevelAbove(ref MenuCallbackArgs args, int skillLevel)
        {
            if (skillLevel >= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroSkillLevelAbove)) return;

            TextObject text = GameTexts.FindText("str_CE_skill_level", "low");
            text.SetTextVariable("SKILL", _option.ReqHeroSkill);
            args.Tooltip = text;
            args.IsEnabled = false;
        }
#endregion

#region ReqCaptorTrait
        private void ReqCaptorTrait(ref MenuCallbackArgs args)
        {
            if (string.IsNullOrWhiteSpace(_option.ReqCaptorTrait)) return;

            if (PlayerCaptivity.CaptorParty.LeaderHero == null) args.IsEnabled = false;
            int traitLevel;

            try
            {
                traitLevel = PlayerCaptivity.CaptorParty.LeaderHero.GetTraitLevel(TraitObject.All.Single((TraitObject traitObject) => traitObject.StringId == _option.ReqCaptorTrait));
            }
            catch (Exception)
            {
                CECustomHandler.LogToFile("Invalid Trait Captor");
                traitLevel = 0;
            }

            try
            {
                if (!string.IsNullOrEmpty(_option.ReqCaptorTraitLevelAbove)) ReqCaptorTraitLevelAbove(ref args, traitLevel);
            }
            catch (Exception) { CECustomHandler.LogToFile("Missing ReqCaptorTraitLevelAbove"); }

            try
            {
                if (!string.IsNullOrEmpty(_option.ReqCaptorTraitLevelBelow)) ReqCaptorTraitLevelBelow(ref args, traitLevel);
            }
            catch (Exception) { CECustomHandler.LogToFile("Missing ReqCaptorTraitLevelBelow"); }
        }
        private void ReqCaptorTraitLevelBelow(ref MenuCallbackArgs args, int traitLevel)
        {
            if (traitLevel <= new CEVariablesLoader().GetIntFromXML(_option.ReqCaptorTraitLevelBelow)) return;

            TextObject text = GameTexts.FindText("str_CE_trait_captor_level", "high");
            text.SetTextVariable("TRAIT", CEStrings.FetchTraitString(_option.ReqCaptorTrait));
            args.Tooltip = text;
            args.IsEnabled = false;
        }
        private void ReqCaptorTraitLevelAbove(ref MenuCallbackArgs args, int traitLevel)
        {
            if (traitLevel >= new CEVariablesLoader().GetIntFromXML(_option.ReqCaptorTraitLevelAbove)) return;

            TextObject text = GameTexts.FindText("str_CE_trait_captor_level", "low");
            text.SetTextVariable("TRAIT", CEStrings.FetchTraitString(_option.ReqCaptorTrait));
            args.Tooltip = text;
            args.IsEnabled = false;
        }
#endregion

#region ReqTrait
        private void ReqTrait(ref MenuCallbackArgs args)
        {
            if (string.IsNullOrWhiteSpace(_option.ReqHeroTrait)) return;

            int traitLevel;
            try
            {
                traitLevel = Hero.MainHero.GetTraitLevel(TraitObject.All.Single((TraitObject traitObject) => traitObject.StringId == _option.ReqCaptorTrait));
            }
            catch (Exception)
            {
                CECustomHandler.LogToFile("Invalid Trait Captive");
                traitLevel = 0;
            }

            try
            {
                if (!string.IsNullOrEmpty(_option.ReqHeroTraitLevelAbove)) ReqHeroTraitLevelAbove(ref args, traitLevel);
            }
            catch (Exception) { CECustomHandler.LogToFile("Invalid ReqHeroTraitLevelAbove"); }

            try
            {
                if (!string.IsNullOrEmpty(_option.ReqHeroTraitLevelBelow)) ReqHeroTraitLevelBelow(ref args, traitLevel);
            }
            catch (Exception) { CECustomHandler.LogToFile("Invalid ReqHeroTraitLevelBelow"); }
        }
        private void ReqHeroTraitLevelBelow(ref MenuCallbackArgs args, int traitLevel)
        {
            if (traitLevel <= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroTraitLevelBelow)) return;

            TextObject text = GameTexts.FindText("str_CE_trait_level", "high");
            text.SetTextVariable("TRAIT", CEStrings.FetchTraitString(_option.ReqHeroTrait));
            args.Tooltip = text;
            args.IsEnabled = false;
        }
        private void ReqHeroTraitLevelAbove(ref MenuCallbackArgs args, int traitLevel)
        {
            if (traitLevel >= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroTraitLevelAbove)) return;

            TextObject text = GameTexts.FindText("str_CE_trait_level", "low");
            text.SetTextVariable("TRAIT", CEStrings.FetchTraitString(_option.ReqHeroTrait));
            args.Tooltip = text;
            args.IsEnabled = false;
        }
#endregion

#region ReqProstitute
        private void ReqProstitute(ref MenuCallbackArgs args)
        {
            int prostitute = Hero.MainHero.GetSkillValue(CESkills.Prostitution);

            try
            {
                if (!string.IsNullOrEmpty(_option.ReqHeroProstituteLevelAbove)) ReqHeroProstituteLevelAbove(ref args, prostitute);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroProstituteLevelAbove / Failed "); }

            try
            {
                if (!string.IsNullOrEmpty(_option.ReqHeroProstituteLevelBelow)) ReqHeroProstituteLevelBelow(ref args, prostitute);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroProstituteLevelBelow / Failed "); }
        }
        private void ReqHeroProstituteLevelBelow(ref MenuCallbackArgs args, int prostitute)
        {
            if (prostitute <= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroProstituteLevelBelow)) return;

            args.Tooltip = GameTexts.FindText("str_CE_prostitution_level", "high");
            args.IsEnabled = false;
        }
        private void ReqHeroProstituteLevelAbove(ref MenuCallbackArgs args, int prostitute)
        {
            if (prostitute >= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroProstituteLevelAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_prostitution_level", "low");
            args.IsEnabled = false;
        }
#endregion

#region ReqSlavery
        private void ReqSlavery(ref MenuCallbackArgs args)
        {
            int slave = Hero.MainHero.GetSkillValue(CESkills.Slavery);

            try
            {
                if (!string.IsNullOrEmpty(_option.ReqHeroSlaveLevelAbove)) SetReqHeroSlaveLevelAbove(ref args, slave);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroSlaveLevelAbove / Failed "); }

            try
            {
                if (!string.IsNullOrEmpty(_option.ReqHeroSlaveLevelBelow)) SetReqHeroSlaveLevelBelow(ref args, slave);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroSlaveLevelBelow / Failed "); }
        }
        private void SetReqHeroSlaveLevelBelow(ref MenuCallbackArgs args, int slave)
        {
            if (slave <= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroSlaveLevelBelow)) return;

            args.Tooltip = GameTexts.FindText("str_CE_slavery_level", "high");
            args.IsEnabled = false;
        }
        private void SetReqHeroSlaveLevelAbove(ref MenuCallbackArgs args, int slave)
        {
            if (slave >= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroSlaveLevelAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_slavery_level", "low");
            args.IsEnabled = false;
        }
#endregion

#region ReqHeroHealthPercentage
        private void ReqHeroHealthPercentage(ref MenuCallbackArgs args)
        {
            try
            {
                if (!string.IsNullOrEmpty(_option.ReqHeroHealthAbovePercentage)) ReqHeroHealthAbovePercentage(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroHealthAbovePercentage / Failed "); }

            try
            {
                if (!string.IsNullOrEmpty(_option.ReqHeroHealthBelowPercentage)) ReqHeroHealthBelowPercentage(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroHealthBelowPercentage / Failed "); }
        }
        private void ReqHeroHealthBelowPercentage(ref MenuCallbackArgs args)
        {
            if (Hero.MainHero.HitPoints <= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroHealthBelowPercentage)) return;

            args.Tooltip = GameTexts.FindText("str_CE_health", "high");
            args.IsEnabled = false;
        }
        private void ReqHeroHealthAbovePercentage(ref MenuCallbackArgs args)
        {
            if (Hero.MainHero.HitPoints >= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroHealthAbovePercentage)) return;

            args.Tooltip = GameTexts.FindText("str_CE_health", "low");
            args.IsEnabled = false;
        }
#endregion

#region ReqHeroCaptorRelation
        private void ReqHeroCaptorRelation(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty?.LeaderHero == null) return;

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroCaptorRelationAbove)) ReqHeroCaptorRelationAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroCaptorRelationAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroCaptorRelationBelow)) ReqHeroCaptorRelationBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroCaptorRelationBelow / Failed "); }
        }
        private void ReqHeroCaptorRelationBelow(ref MenuCallbackArgs args)
        {
            if (!(PlayerCaptivity.CaptorParty.LeaderHero.GetRelationWithPlayer() > new CEVariablesLoader().GetFloatFromXML(_option.ReqHeroCaptorRelationBelow))) return;

            TextObject textResponse3 = GameTexts.FindText("str_CE_relationship", "high");
            textResponse3.SetTextVariable("HERO", PlayerCaptivity.CaptorParty.LeaderHero.Name.ToString());
            args.Tooltip = textResponse3;
            args.IsEnabled = false;
        }
        private void ReqHeroCaptorRelationAbove(ref MenuCallbackArgs args)
        {
            if (!(PlayerCaptivity.CaptorParty.LeaderHero.GetRelationWithPlayer() < new CEVariablesLoader().GetFloatFromXML(_option.ReqHeroCaptorRelationAbove))) return;

            TextObject textResponse4 = GameTexts.FindText("str_CE_relationship", "low");
            textResponse4.SetTextVariable("HERO", PlayerCaptivity.CaptorParty.LeaderHero.Name.ToString());
            args.Tooltip = textResponse4;
            args.IsEnabled = false;
        }
#endregion

#region ReqCaptives
        private void ReqCaptives(ref MenuCallbackArgs args)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqCaptivesAbove)) SetReqCaptivesAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqCaptivesAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroCaptivesAbove)) SetReqHeroCaptivesAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroCaptivesAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqCaptivesBelow)) SetReqCaptivesBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqCaptivesBelow / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroCaptivesBelow)) SetReqHeroCaptivesBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroCaptivesBelow / Failed "); }
        }
        private void ReqFemaleCaptives(ref MenuCallbackArgs args)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqFemaleCaptivesAbove)) SetReqFemaleCaptivesAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqFemaleCaptivesAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroFemaleCaptivesAbove)) SetReqHeroFemaleCaptivesAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroFemaleCaptivesAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqFemaleCaptivesBelow)) SetReqFemaleCaptivesBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqFemaleCaptivesBelow / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroFemaleCaptivesBelow)) SetReqHeroFemaleCaptivesBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroFemaleCaptivesBelow / Failed "); }
        }
        private void ReqMaleCaptives(ref MenuCallbackArgs args)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqMaleCaptivesAbove)) SetReqMaleCaptivesAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqMaleCaptivesAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroMaleCaptivesAbove)) SetReqHeroMaleCaptivesAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroMaleCaptivesAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqMaleCaptivesBelow)) SetReqMaleCaptivesBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqMaleCaptivesBelow / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroMaleCaptivesBelow)) SetReqHeroMaleCaptivesBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroMaleCaptivesBelow / Failed "); }
        }
#endregion

#region ReqTroops
        private void ReqTroops(ref MenuCallbackArgs args)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqTroopsAbove)) SetReqTroopsAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqTroopsAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroTroopsAbove)) SetReqHeroTroopsAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqTroopsAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqTroopsBelow)) SetReqTroopsBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqTroopsBelow / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroTroopsBelow)) SetReqHeroTroopsBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqTroopsBelow / Failed "); }
        }
        private void ReqFemaleTroops(ref MenuCallbackArgs args)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqFemaleTroopsAbove)) SetReqFemaleTroopsAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqFemaleTroopsAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroFemaleTroopsAbove)) SetReqHeroFemaleTroopsAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroFemaleTroopsAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqFemaleTroopsBelow)) SetReqFemaleTroopsBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqFemaleTroopsBelow / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroFemaleTroopsBelow)) SetReqHeroFemaleTroopsBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroFemaleTroopsBelow / Failed "); }
        }
        private void ReqMaleTroops(ref MenuCallbackArgs args)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqMaleTroopsAbove)) SetReqMaleTroopsAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqMaleTroopsAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroMaleTroopsAbove)) SetReqHeroMaleTroopsAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroMaleTroopsAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqMaleTroopsBelow)) SetReqMaleTroopsBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqMaleTroopsBelow / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqHeroMaleTroopsBelow)) SetReqHeroMaleTroopsBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqHeroMaleTroopsBelow / Failed "); }
        }
#endregion

#region ReqMorale
        private void ReqMorale(ref MenuCallbackArgs args)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqMoraleAbove)) ReqMoraleAbove(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqMoralAbove / Failed "); }

            try
            {
                if (!string.IsNullOrWhiteSpace(_option.ReqMoraleBelow)) ReqMoraleBelow(ref args);
            }
            catch (Exception) { CECustomHandler.LogToFile("Incorrect ReqMoralBelow / Failed "); }
        }
        private void ReqMoraleBelow(ref MenuCallbackArgs args)
        {
            if (!PlayerCaptivity.CaptorParty.IsMobile || !(PlayerCaptivity.CaptorParty.MobileParty.Morale > new CEVariablesLoader().GetIntFromXML(_option.ReqMoraleBelow))) return;

            args.Tooltip = GameTexts.FindText("str_CE_morale_level", "high");
            args.IsEnabled = false;
        }
        private void ReqMoraleAbove(ref MenuCallbackArgs args)
        {
            if (!PlayerCaptivity.CaptorParty.IsMobile || !(PlayerCaptivity.CaptorParty.MobileParty.Morale < new CEVariablesLoader().GetIntFromXML(_option.ReqMoraleAbove))) return;

            args.Tooltip = GameTexts.FindText("str_CE_morale_level", "low");
            args.IsEnabled = false;
        }
#endregion

#region ReqFemaleCaptives

        private void SetReqFemaleCaptivesBelow(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.PrisonRoster.Sum(troopRosterElement => { return (troopRosterElement.Character.IsFemale) ? troopRosterElement.Number : 0; }) <= new CEVariablesLoader().GetIntFromXML(_option.ReqFemaleCaptivesBelow)) return;

            args.Tooltip = GameTexts.FindText("str_CE_captives_level", "high");
            args.IsEnabled = false;
        }

        private void SetReqHeroFemaleCaptivesBelow(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.PrisonRoster.Sum(troopRosterElement => { return (troopRosterElement.Character.IsFemale && troopRosterElement.Character.IsHero) ? troopRosterElement.Number : 0; }) <= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroFemaleCaptivesBelow)) return;

            args.Tooltip = GameTexts.FindText("str_CE_captives_level", "high");
            args.IsEnabled = false;
        }

        private void SetReqFemaleCaptivesAbove(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.PrisonRoster.Sum(troopRosterElement => { return (troopRosterElement.Character.IsFemale) ? troopRosterElement.Number : 0; }) >= new CEVariablesLoader().GetIntFromXML(_option.ReqFemaleCaptivesAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_captives_level", "low");
            args.IsEnabled = false;
        }

        private void SetReqHeroFemaleCaptivesAbove(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.PrisonRoster.Sum(troopRosterElement => { return (troopRosterElement.Character.IsFemale && troopRosterElement.Character.IsHero) ? troopRosterElement.Number : 0; }) >= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroFemaleCaptivesAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_captives_level", "low");
            args.IsEnabled = false;
        }

#endregion

#region ReqMaleCaptives

        private void SetReqMaleCaptivesBelow(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.PrisonRoster.Sum(troopRosterElement => { return (!troopRosterElement.Character.IsFemale) ? troopRosterElement.Number : 0; }) <= new CEVariablesLoader().GetIntFromXML(_option.ReqMaleCaptivesBelow)) return;

            args.Tooltip = GameTexts.FindText("str_CE_captives_level", "high");
            args.IsEnabled = false;
        }

        private void SetReqHeroMaleCaptivesBelow(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.PrisonRoster.Sum(troopRosterElement => { return (!troopRosterElement.Character.IsFemale && troopRosterElement.Character.IsHero) ? troopRosterElement.Number : 0; }) <= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroMaleCaptivesBelow)) return;

            args.Tooltip = GameTexts.FindText("str_CE_captives_level", "high");
            args.IsEnabled = false;
        }

        private void SetReqMaleCaptivesAbove(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.PrisonRoster.Sum(troopRosterElement => { return (!troopRosterElement.Character.IsFemale) ? troopRosterElement.Number : 0; }) >= new CEVariablesLoader().GetIntFromXML(_option.ReqMaleCaptivesAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_captives_level", "low");
            args.IsEnabled = false;
        }

        private void SetReqHeroMaleCaptivesAbove(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.PrisonRoster.Sum(troopRosterElement => { return (!troopRosterElement.Character.IsFemale && troopRosterElement.Character.IsHero) ? troopRosterElement.Number : 0; }) >= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroMaleCaptivesAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_captives_level", "low");
            args.IsEnabled = false;
        }


#endregion

#region ReqCaptives

        private void SetReqCaptivesBelow(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.PrisonRoster.Sum(troopRosterElement => { return troopRosterElement.Number; }) <= new CEVariablesLoader().GetIntFromXML(_option.ReqCaptivesBelow)) return;

            args.Tooltip = GameTexts.FindText("str_CE_captives_level", "high");
            args.IsEnabled = false;
        }

        private void SetReqHeroCaptivesBelow(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.PrisonRoster.Sum(troopRosterElement => { return (troopRosterElement.Character.IsHero) ? troopRosterElement.Number : 0; }) <= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroCaptivesBelow)) return;

            args.Tooltip = GameTexts.FindText("str_CE_captives_level", "high");
            args.IsEnabled = false;
        }

        private void SetReqCaptivesAbove(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.PrisonRoster.Sum(troopRosterElement => { return troopRosterElement.Number; }) >= new CEVariablesLoader().GetIntFromXML(_option.ReqCaptivesAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_captives_level", "low");
            args.IsEnabled = false;
        }

        private void SetReqHeroCaptivesAbove(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.PrisonRoster.Sum(troopRosterElement => { return (troopRosterElement.Character.IsHero) ? troopRosterElement.Number : 0; }) >= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroCaptivesAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_captives_level", "low");
            args.IsEnabled = false;
        }
#endregion

#region ReqFemaleTroops

        private void SetReqFemaleTroopsBelow(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.MemberRoster.Sum(troopRosterElement => { return (troopRosterElement.Character.IsFemale) ? troopRosterElement.Number : 0; }) <= new CEVariablesLoader().GetIntFromXML(_option.ReqFemaleTroopsBelow)) return;

            args.Tooltip = GameTexts.FindText("str_CE_member_level", "high");
            args.IsEnabled = false;
        }

        private void SetReqHeroFemaleTroopsBelow(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.MemberRoster.Sum(troopRosterElement => { return (troopRosterElement.Character.IsFemale && troopRosterElement.Character.IsHero) ? troopRosterElement.Number : 0; }) <= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroFemaleTroopsBelow)) return;

            args.Tooltip = GameTexts.FindText("str_CE_member_level", "high");
            args.IsEnabled = false;
        }

        private void SetReqFemaleTroopsAbove(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.MemberRoster.Sum(troopRosterElement => { return (troopRosterElement.Character.IsFemale) ? troopRosterElement.Number : 0; }) >= new CEVariablesLoader().GetIntFromXML(_option.ReqFemaleTroopsAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_member_level", "low");
            args.IsEnabled = false;
        }

        private void SetReqHeroFemaleTroopsAbove(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.MemberRoster.Sum(troopRosterElement => { return (troopRosterElement.Character.IsFemale && troopRosterElement.Character.IsHero) ? troopRosterElement.Number : 0; }) >= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroFemaleTroopsAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_member_level", "low");
            args.IsEnabled = false;
        }

#endregion

#region ReqMaleTroops

        private void SetReqMaleTroopsBelow(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.MemberRoster.Sum(troopRosterElement => { return (!troopRosterElement.Character.IsFemale) ? troopRosterElement.Number : 0; }) <= new CEVariablesLoader().GetIntFromXML(_option.ReqMaleTroopsBelow)) return;
            args.Tooltip = GameTexts.FindText("str_CE_member_level", "high");
            args.IsEnabled = false;
        }

        private void SetReqHeroMaleTroopsBelow(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.MemberRoster.Sum(troopRosterElement => { return (!troopRosterElement.Character.IsFemale && troopRosterElement.Character.IsHero) ? troopRosterElement.Number : 0; }) <= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroMaleTroopsBelow)) return;
            args.Tooltip = GameTexts.FindText("str_CE_member_level", "high");
            args.IsEnabled = false;
        }

        private void SetReqMaleTroopsAbove(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.MemberRoster.Sum(troopRosterElement => { return (!troopRosterElement.Character.IsFemale) ? troopRosterElement.Number : 0; }) >= new CEVariablesLoader().GetIntFromXML(_option.ReqMaleTroopsAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_member_level", "low");
            args.IsEnabled = false;
        }

        private void SetReqHeroMaleTroopsAbove(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.MemberRoster.Sum(troopRosterElement => { return (!troopRosterElement.Character.IsFemale && troopRosterElement.Character.IsHero) ? troopRosterElement.Number : 0; }) >= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroMaleTroopsAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_member_level", "low");
            args.IsEnabled = false;
        }

#endregion

#region ReqTroops

        private void SetReqTroopsBelow(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.MemberRoster.Sum(troopRosterElement => { return troopRosterElement.Number; }) <= new CEVariablesLoader().GetIntFromXML(_option.ReqTroopsBelow)) return;

            args.Tooltip = GameTexts.FindText("str_CE_member_level", "high");
            args.IsEnabled = false;
        }

        private void SetReqHeroTroopsBelow(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.MemberRoster.Sum(troopRosterElement => { return (troopRosterElement.Character.IsHero) ? troopRosterElement.Number : 0; }) <= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroTroopsBelow)) return;

            args.Tooltip = GameTexts.FindText("str_CE_member_level", "high");
            args.IsEnabled = false;
        }

        private void SetReqTroopsAbove(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.MemberRoster.Sum(troopRosterElement => { return troopRosterElement.Number; }) >= new CEVariablesLoader().GetIntFromXML(_option.ReqTroopsAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_member_level", "low");
            args.IsEnabled = false;
        }

        private void SetReqHeroTroopsAbove(ref MenuCallbackArgs args)
        {
            if (PlayerCaptivity.CaptorParty.MemberRoster.Sum(troopRosterElement => { return (troopRosterElement.Character.IsHero) ? troopRosterElement.Number : 0; }) >= new CEVariablesLoader().GetIntFromXML(_option.ReqHeroTroopsAbove)) return;

            args.Tooltip = GameTexts.FindText("str_CE_member_level", "low");
            args.IsEnabled = false;
        }

#endregion

#endregion

#region Init Options
        private void InitGiveGold()
        {
            int content = new ScoresCalculation().AttractivenessScore(Hero.MainHero);
            content *= _option.MultipleRestrictedListOfConsequences.Count(consequence => consequence == RestrictedListOfConsequences.GiveGold);
            MBTextManager.SetTextVariable("MONEY_AMOUNT", content);
        }
        private void InitChangeGold()
        {
            try
            {
                int level = 0;

                if (!string.IsNullOrEmpty(_option.GoldTotal)) level = new CEVariablesLoader().GetIntFromXML(_option.GoldTotal);
                else if (!string.IsNullOrEmpty(_listedEvent.GoldTotal)) level = new CEVariablesLoader().GetIntFromXML(_listedEvent.GoldTotal);
                else CECustomHandler.LogToFile("Missing GoldTotal");
                MBTextManager.SetTextVariable("MONEY_AMOUNT", level);
            }
            catch (Exception) { CECustomHandler.LogToFile("Invalid GoldTotal"); }
        }
        private void InitChangeCaptorGold()
        {
            try
            {
                int level = 0;

                if (!string.IsNullOrEmpty(_option.CaptorGoldTotal)) level = new CEVariablesLoader().GetIntFromXML(_option.CaptorGoldTotal);
                else if (!string.IsNullOrEmpty(_listedEvent.CaptorGoldTotal)) level = new CEVariablesLoader().GetIntFromXML(_listedEvent.CaptorGoldTotal);
                else CECustomHandler.LogToFile("Missing CaptorGoldTotal");
                MBTextManager.SetTextVariable("CAPTOR_MONEY_AMOUNT", level);
            }
            catch (Exception) { CECustomHandler.LogToFile("Invalid CaptorGoldTotal"); }
        }
        private static void InitSoldToSettlement()
        {
            try
            {
                PartyBase party = !PlayerCaptivity.CaptorParty.IsSettlement
                    ? PlayerCaptivity.CaptorParty.MobileParty.CurrentSettlement.Party
                    : PlayerCaptivity.CaptorParty;

                MBTextManager.SetTextVariable("BUYERSETTLEMENT", party.Name);
            }
            catch (Exception) { CECustomHandler.LogToFile("Failed to get Settlement"); }
        }
        private static void InitSoldToCaravan()
        {
            try
            {
                PartyBase party = PlayerCaptivity.CaptorParty.IsSettlement
                    ? PlayerCaptivity.CaptorParty.Settlement.Parties.First(mobileParty => mobileParty.IsCaravan).Party
                    : PlayerCaptivity.CaptorParty.MobileParty.CurrentSettlement.Parties.First(mobileParty => mobileParty.IsCaravan).Party;

                MBTextManager.SetTextVariable("BUYERCARAVAN", party.Name);
            }
            catch (Exception) { CECustomHandler.LogToFile("Failed to get Caravan"); }
        }
        private static void InitSoldToLordParty()
        {
            try
            {
                PartyBase party = PlayerCaptivity.CaptorParty.IsSettlement
                    ? PlayerCaptivity.CaptorParty.Settlement.Parties.First(mobileParty => mobileParty.IsLordParty && !mobileParty.IsMainParty).Party
                    : PlayerCaptivity.CaptorParty.MobileParty.CurrentSettlement.Parties.First(mobileParty => mobileParty.IsLordParty && !mobileParty.IsMainParty).Party;

                MBTextManager.SetTextVariable("BUYERLORDPARTY", party.Name);
            }
            catch (Exception) { CECustomHandler.LogToFile("Failed to get Lord"); }
        }
        private void InitLeaveType(ref MenuCallbackArgs args)
        {
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.Wait)) args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.Trade)) args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.RansomAndBribe)) args.optionLeaveType = GameMenuOption.LeaveType.RansomAndBribe;
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.BribeAndEscape)) args.optionLeaveType = GameMenuOption.LeaveType.BribeAndEscape;
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.Submenu)) args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.Continue)) args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            if (_option.MultipleRestrictedListOfConsequences.Contains(RestrictedListOfConsequences.EmptyIcon)) args.optionLeaveType = GameMenuOption.LeaveType.Default;
        }
        private void InitCaptiveTimeInDays(int captiveTimeInDays, ref TextObject text)
        {
            if (captiveTimeInDays != 0)
            {
                text.SetTextVariable("DAYS", 1);
                text.SetTextVariable("NUMBER_OF_DAYS", captiveTimeInDays);

                text.SetTextVariable("PLURAL", captiveTimeInDays > 1
                                         ? 1
                                         : 0);
            }
            else { text.SetTextVariable("DAYS", 0); }
        }
        private void InitCaptiveTextVariables(ref MenuCallbackArgs args)
        {
            if (!PlayerCaptivity.IsCaptive) return;

            int captiveTimeInDays = PlayerCaptivity.CaptiveTimeInDays;
            TextObject text = args.MenuContext.GameMenu.GetText();

            text.SetTextVariable("ISFEMALE", Hero.MainHero.IsFemale
                         ? 1
                         : 0);

#if V165
            if (PlayerCaptivity.CaptorParty.Leader != null)
            {
                text.SetTextVariable("CAPTOR_NAME", PlayerCaptivity.CaptorParty.Leader.Name);

                text.SetTextVariable("ISCAPTORFEMALE", PlayerCaptivity.CaptorParty.Leader.IsFemale
                                         ? 1
                                         : 0);
            }
#else
            if (PlayerCaptivity.CaptorParty.LeaderHero != null)
            {
                text.SetTextVariable("CAPTOR_NAME", PlayerCaptivity.CaptorParty.LeaderHero.Name);

                text.SetTextVariable("ISCAPTORFEMALE", PlayerCaptivity.CaptorParty.LeaderHero.IsFemale
                                         ? 1
                                         : 0);
            }
#endif
            else
            {
                text.SetTextVariable("CAPTOR_NAME", new TextObject("{=CESETTINGS0099}captor"));
                text.SetTextVariable("ISCAPTORFEMALE", 0);
            }

            try
            {
                if (_listedEvent.SavedCompanions != null)
                {
                    foreach (KeyValuePair<string, Hero> item in _listedEvent.SavedCompanions)
                    {
                        text.SetTextVariable("COMPANION_NAME_" + item.Key, item.Value?.Name);
                        text.SetTextVariable("COMPANIONISFEMALE_" + item.Key, item.Value.IsFemale ? 1 : 0);
                    }
                }
            }
            catch (Exception)
            {
                CECustomHandler.ForceLogToFile("Failed to SetCaptiveTextVariables for " + _listedEvent.Name);
            }

            if (CECampaignBehavior.ExtraProps.Owner != null) text.SetTextVariable("OWNER_NAME", CECampaignBehavior.ExtraProps.Owner.Name);

            if (PlayerCaptivity.CaptorParty.IsMobile && PlayerCaptivity.CaptorParty.MobileParty.CurrentSettlement != null) text.SetTextVariable("SETTLEMENT_NAME", PlayerCaptivity.CaptorParty.MobileParty.CurrentSettlement.Name);
            else if (PlayerCaptivity.CaptorParty.IsSettlement) text.SetTextVariable("SETTLEMENT_NAME", PlayerCaptivity.CaptorParty.Settlement.Name);

            if (PlayerCaptivity.CaptorParty.IsMobile)
            {
                text.SetTextVariable("ISCARAVAN", PlayerCaptivity.CaptorParty.MobileParty.IsCaravan
                                         ? 1
                                         : 0);

                text.SetTextVariable("ISBANDITS", PlayerCaptivity.CaptorParty.MobileParty.IsBandit || PlayerCaptivity.CaptorParty.MobileParty.IsBanditBossParty
                                         ? 1
                                         : 0);

                text.SetTextVariable("ISLORDPARTY", PlayerCaptivity.CaptorParty.MobileParty.IsLordParty
                                         ? 1
                                         : 0);

                text.SetTextVariable("PARTY_NAME", PlayerCaptivity.CaptorParty.Name);
            }

            InitCaptiveTimeInDays(captiveTimeInDays, ref text);
        }
#endregion
    }
}