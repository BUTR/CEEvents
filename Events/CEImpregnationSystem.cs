﻿#define V170
using CaptivityEvents.CampaignBehaviors;
using CaptivityEvents.Config;
using CaptivityEvents.Helper;
using Helpers;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CaptivityEvents.Events
{
    public class CEImpregnationSystem
    {

        // Random Version
        public void ImpregnationChance(Hero targetHero, int modifier = 0, bool forcePreg = false, Hero senderHero = null)
        {
            ScoresCalculation score = new ScoresCalculation();

            if (targetHero != null && targetHero.IsFemale && !targetHero.IsPregnant && !CECampaignBehavior.CheckIfPregnancyExists(targetHero))
            {
                if (CESettings.Instance != null && !forcePreg && (IsHeroAgeSuitableForPregnancy(targetHero) && CESettings.Instance.PregnancyToggle))
                {
                    if (!CESettings.Instance.UsePregnancyModifiers) modifier = 0;

                    if (MBRandom.Random.Next(100)
                        >= (CESettings.Instance.AttractivenessSkill
                            ? score.AttractivenessScore(targetHero) / 20 + modifier
                            : CESettings.Instance.PregnancyChance + modifier))
                    {
                        return;
                    }

                    Hero randomSoldier;

                    if (senderHero != null)
                    {
                        if (!senderHero.IsFemale) randomSoldier = senderHero;
                        else return;
                    }
                    else if (targetHero.CurrentSettlement?.Party != null && !targetHero.CurrentSettlement.Party.MemberRoster.GetTroopRoster().IsEmpty())
                    {
                        Settlement settlementCurrent = targetHero.CurrentSettlement;
                        IEnumerable<TroopRosterElement> maleMembers = settlementCurrent.Party.MemberRoster.GetTroopRoster().Where(characterObject => characterObject.Character.IsFemale == false);
                        List<TroopRosterElement> troopRosterElements = maleMembers.ToList();

                        if (!troopRosterElements.Any()) return;

                        CharacterObject m = troopRosterElements.GetRandomElement().Character;
                        randomSoldier = HeroCreator.CreateSpecialHero(m, settlementCurrent, CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(20) + 20);
                    }
                    else if (targetHero.PartyBelongedTo != null)
                    {
                        IEnumerable<TroopRosterElement> maleMembers = targetHero.PartyBelongedTo.MemberRoster.GetTroopRoster().Where(characterObject => characterObject.Character.IsFemale == false);
                        List<TroopRosterElement> troopRosterElements = maleMembers.ToList();

                        if (!troopRosterElements.Any()) return;

                        CharacterObject m = troopRosterElements.GetRandomElement().Character;
                        randomSoldier = HeroCreator.CreateSpecialHero(m, targetHero.PartyBelongedTo.HomeSettlement, CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(20) + 20);
                    }
                    else
                    {
#if V165
                        CharacterObject m = CharacterObject.Templates.GetRandomElementWithPredicate(characterObject => characterObject.IsFemale == false && characterObject.Occupation == Occupation.Wanderer);
#else
                        CharacterObject m = CharacterObject.PlayerCharacter.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate(characterObject => characterObject.IsFemale == false && characterObject.Occupation == Occupation.Wanderer);
#endif
                        randomSoldier = HeroCreator.CreateSpecialHero(m, SettlementHelper.FindRandomSettlement(x => x.IsTown && x.Culture == m.Culture), CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(20) + 20);
                    }

                    TextObject textObject3 = GameTexts.FindText("str_CE_impregnated");
                    textObject3.SetTextVariable("HERO", targetHero.Name);
                    textObject3.SetTextVariable("SPOUSE", randomSoldier.Name);
                    InformationManager.DisplayMessage(new InformationMessage(textObject3.ToString(), Colors.Magenta));

                    CEHelper.spouseOne = randomSoldier;
                    CEHelper.spouseTwo = targetHero;
                    MakePregnantAction.Apply(targetHero);
                    CEHelper.spouseOne = CEHelper.spouseTwo = null;

                    //RelationsModifier(randomSoldier, 50, targetHero);
                }
                else if (forcePreg)
                {
#if V165
                    CharacterObject m = CharacterObject.Templates.GetRandomElementWithPredicate(characterObject => characterObject.IsFemale == false);
#else
                    CharacterObject m = CharacterObject.PlayerCharacter.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate(characterObject => characterObject.IsFemale == false && characterObject.Occupation == Occupation.Wanderer);
#endif
                    Hero randomSoldier = HeroCreator.CreateSpecialHero(m, targetHero.BornSettlement, CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(20) + 20);
                    CEHelper.spouseOne = randomSoldier;
                    CEHelper.spouseTwo = targetHero;
                    MakePregnantAction.Apply(targetHero);
                    CEHelper.spouseOne = CEHelper.spouseTwo = null;
                    TextObject textObject4 = new TextObject("{PLAYER_HERO} forced impregnated by {PLAYER_SPOUSE}.");
                    textObject4.SetTextVariable("PLAYER_HERO", targetHero.Name);
                    textObject4.SetTextVariable("PLAYER_SPOUSE", randomSoldier.Name);
                    InformationManager.DisplayMessage(new InformationMessage(textObject4.ToString(), Colors.Magenta));
                }
            }
            else if (targetHero != null && !targetHero.IsFemale)
            {
                if (CESettings.Instance != null && !CESettings.Instance.PregnancyToggle) return;
                if (CESettings.Instance != null && !CESettings.Instance.UsePregnancyModifiers) modifier = 0;

                if (CESettings.Instance != null
                    && !forcePreg && MBRandom.Random.Next(100)
                    >= (CESettings.Instance.AttractivenessSkill
                        ? score.AttractivenessScore(targetHero) / 20 + modifier
                        : CESettings.Instance.PregnancyChance + modifier))
                {
                    return;
                }

                Hero randomSoldier;

                if (senderHero != null)
                {
                    if (senderHero.IsFemale && !senderHero.IsPregnant && !CECampaignBehavior.CheckIfPregnancyExists(senderHero) && IsHeroAgeSuitableForPregnancy(senderHero)) randomSoldier = senderHero;
                    else return;

                }
                else if (targetHero.CurrentSettlement?.Party != null && !targetHero.CurrentSettlement.Party.MemberRoster.GetTroopRoster().IsEmpty())
                {
                    Settlement settlementCurrent = targetHero.CurrentSettlement;
                    IEnumerable<TroopRosterElement> femaleMembers = settlementCurrent.Party.MemberRoster.GetTroopRoster().Where(characterObject => characterObject.Character.IsFemale);
                    List<TroopRosterElement> troopRosterElements = femaleMembers.ToList();

                    if (!troopRosterElements.Any()) return;

                    do
                    {
                        CharacterObject m = troopRosterElements.GetRandomElement().Character;
                        randomSoldier = HeroCreator.CreateSpecialHero(m, settlementCurrent, CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(15) + 18);
                    } while (!IsHeroAgeSuitableForPregnancy(randomSoldier));
                }
                else if (targetHero.PartyBelongedTo != null)
                {
                    IEnumerable<TroopRosterElement> femaleMembers = targetHero.PartyBelongedTo.MemberRoster.GetTroopRoster().Where(characterObject => characterObject.Character.IsFemale);
                    List<TroopRosterElement> troopRosterElements = femaleMembers.ToList();

                    if (!troopRosterElements.Any()) return;

                    do
                    {
                        CharacterObject m = troopRosterElements.GetRandomElement().Character;
                        randomSoldier = HeroCreator.CreateSpecialHero(m, targetHero.PartyBelongedTo.HomeSettlement, CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(15) + 18);
                    } while (!IsHeroAgeSuitableForPregnancy(randomSoldier));
                }
                else
                {
#if V165
                    CharacterObject m = CharacterObject.Templates.Where(characterObject => characterObject.IsFemale && characterObject.Occupation == Occupation.Wanderer).GetRandomElementInefficiently();
#else
                    CharacterObject m = CharacterObject.PlayerCharacter.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate(characterObject => characterObject.IsFemale && characterObject.Occupation == Occupation.Wanderer);
#endif
                    randomSoldier = HeroCreator.CreateSpecialHero(m, SettlementHelper.FindRandomSettlement(x => x.IsTown && x.Culture == m.Culture), CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(15) + 18);
                }

                TextObject textObject3 = GameTexts.FindText("str_CE_impregnated");
                textObject3.SetTextVariable("HERO", randomSoldier.Name);
                textObject3.SetTextVariable("SPOUSE", targetHero.Name);
                InformationManager.DisplayMessage(new InformationMessage(textObject3.ToString(), Colors.Magenta));

                CEHelper.spouseOne = randomSoldier;
                CEHelper.spouseTwo = targetHero;
                MakePregnantAction.Apply(randomSoldier);
                CEHelper.spouseOne = CEHelper.spouseTwo = null;

                //RelationsModifier(randomSoldier, 50, targetHero);
            }
        }

        // Captor / Captive Version
        public void CaptivityImpregnationChance(Hero targetHero, int modifier = 0, bool forcePreg = false, bool lord = true, Hero captorHero = null)
        {
            ScoresCalculation scoresCalculation = new ScoresCalculation();


            if (targetHero != null && targetHero.IsFemale && !targetHero.IsPregnant && !CECampaignBehavior.CheckIfPregnancyExists(targetHero))
            {
                if (CESettings.Instance != null && IsHeroAgeSuitableForPregnancy(targetHero) && CESettings.Instance.PregnancyToggle)
                {
                    if (!CESettings.Instance.UsePregnancyModifiers) modifier = 0;

                    if (MBRandom.Random.Next(100)
                        >= (CESettings.Instance.AttractivenessSkill
                            ? scoresCalculation.AttractivenessScore(targetHero) / 20 + modifier
                            : CESettings.Instance.PregnancyChance + modifier))
                    {
                        return;
                    }

                    Hero randomSoldier;

                    if (captorHero != null)
                    {
                        if (!captorHero.IsFemale) randomSoldier = captorHero;
                        else return;
                    }
                    else if (lord && CECampaignBehavior.ExtraProps.Owner != null)
                    {
                        randomSoldier = CECampaignBehavior.ExtraProps.Owner;
                    }
                    else if (lord && targetHero.PartyBelongedToAsPrisoner != null && targetHero.PartyBelongedToAsPrisoner.IsMobile && targetHero.PartyBelongedToAsPrisoner.MobileParty?.LeaderHero != null && !targetHero.PartyBelongedToAsPrisoner.MobileParty.LeaderHero.IsFemale)
                    {
                        randomSoldier = targetHero.PartyBelongedToAsPrisoner.MobileParty.LeaderHero;
                    }
                    else if (targetHero.PartyBelongedToAsPrisoner != null && targetHero.PartyBelongedToAsPrisoner.IsMobile && targetHero.PartyBelongedToAsPrisoner.MobileParty != null)
                    {
                        IEnumerable<TroopRosterElement> maleMembers = targetHero.PartyBelongedToAsPrisoner.MobileParty.MemberRoster.GetTroopRoster().Where(characterObject => characterObject.Character.IsFemale == false);
                        List<TroopRosterElement> troopRosterElements = maleMembers.ToList();

                        if (!troopRosterElements.Any()) return;

                        CharacterObject m = troopRosterElements.GetRandomElement().Character;
                        randomSoldier = HeroCreator.CreateSpecialHero(m, targetHero.PartyBelongedToAsPrisoner.MobileParty.HomeSettlement, CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(20) + 20);
                    }
                    else if (targetHero.PartyBelongedToAsPrisoner != null && targetHero.PartyBelongedToAsPrisoner.IsSettlement && targetHero.PartyBelongedToAsPrisoner.Settlement.Party != null && !targetHero.PartyBelongedToAsPrisoner.Settlement.Party.MemberRoster.GetTroopRoster().IsEmpty())
                    {
                        Settlement playerCaptor = targetHero.PartyBelongedToAsPrisoner.Settlement;
                        IEnumerable<TroopRosterElement> maleMembers = playerCaptor.Party.MemberRoster.GetTroopRoster().Where(characterObject => characterObject.Character.IsFemale == false);
                        List<TroopRosterElement> troopRosterElements = maleMembers.ToList();

                        if (!troopRosterElements.Any()) return;

                        CharacterObject m = troopRosterElements.GetRandomElement().Character;
                        randomSoldier = HeroCreator.CreateSpecialHero(m, playerCaptor, CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(20) + 20);
                    }
                    else
                    {
#if V165
                        CharacterObject m = CharacterObject.Templates.GetRandomElementWithPredicate(characterObject => characterObject.IsFemale == false && characterObject.Occupation == Occupation.Wanderer);
#else
                        CharacterObject m = CharacterObject.PlayerCharacter.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate(characterObject => characterObject.IsFemale == false && characterObject.Occupation == Occupation.Wanderer);
#endif
                        randomSoldier = HeroCreator.CreateSpecialHero(m, SettlementHelper.FindRandomSettlement(x => x.IsTown && x.Culture == m.Culture), CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(20) + 20);
                    }

                    TextObject textObject3 = GameTexts.FindText("str_CE_impregnated");
                    textObject3.SetTextVariable("HERO", targetHero.Name);
                    textObject3.SetTextVariable("SPOUSE", randomSoldier.Name);
                    InformationManager.DisplayMessage(new InformationMessage(textObject3.ToString(), Colors.Magenta));

                    CEHelper.spouseOne = randomSoldier;
                    CEHelper.spouseTwo = targetHero;
                    MakePregnantAction.Apply(targetHero);
                    CEHelper.spouseOne = CEHelper.spouseTwo = null;

                    //RelationsModifier(randomSoldier, 50, targetHero);
                }
                else if (forcePreg)
                {
#if V165
                    CharacterObject m = CharacterObject.Templates.GetRandomElementWithPredicate(characterObject => characterObject.IsFemale == false && characterObject.Occupation == Occupation.Wanderer);
#else
                    CharacterObject m = CharacterObject.PlayerCharacter.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate(characterObject => characterObject.IsFemale == false && characterObject.Occupation == Occupation.Wanderer);
#endif
                    Hero randomSoldier = HeroCreator.CreateSpecialHero(m, targetHero.BornSettlement, CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(20) + 20);
                    CEHelper.spouseOne = randomSoldier;
                    CEHelper.spouseTwo = targetHero;
                    MakePregnantAction.Apply(targetHero);
                    CEHelper.spouseOne = CEHelper.spouseTwo = null;
                    TextObject textObject4 = new TextObject("{PLAYER_HERO} forced impregnated by {PLAYER_SPOUSE}.");
                    textObject4.SetTextVariable("PLAYER_HERO", targetHero.Name);
                    textObject4.SetTextVariable("PLAYER_SPOUSE", randomSoldier.Name);
                    InformationManager.DisplayMessage(new InformationMessage(textObject4.ToString(), Colors.Magenta));
                }
            }
            else if (targetHero != null && !targetHero.IsFemale)
            {
                if (CESettings.Instance != null && !CESettings.Instance.PregnancyToggle) return;
                if (CESettings.Instance != null && !CESettings.Instance.UsePregnancyModifiers) modifier = 0;

                if (CESettings.Instance != null
                    && MBRandom.Random.Next(100)
                    >= (CESettings.Instance.AttractivenessSkill
                        ? scoresCalculation.AttractivenessScore(targetHero) / 20 + modifier
                        : CESettings.Instance.PregnancyChance + modifier))
                {
                    return;
                }

                Hero randomSoldier = null;

                if (captorHero != null)
                {
                    randomSoldier = captorHero;
                    if (!(randomSoldier.IsFemale && !randomSoldier.IsPregnant && !CECampaignBehavior.CheckIfPregnancyExists(randomSoldier))) return;
                }
                else if (lord && targetHero.PartyBelongedToAsPrisoner != null && targetHero.PartyBelongedToAsPrisoner.IsMobile && targetHero.PartyBelongedToAsPrisoner.MobileParty?.LeaderHero != null)
                {
                    randomSoldier = targetHero.PartyBelongedToAsPrisoner.MobileParty.LeaderHero;
                    if (!(randomSoldier.IsFemale && !randomSoldier.IsPregnant && !CECampaignBehavior.CheckIfPregnancyExists(randomSoldier))) return;
                }
                else if (targetHero.PartyBelongedToAsPrisoner != null && targetHero.PartyBelongedToAsPrisoner.IsMobile && targetHero.PartyBelongedToAsPrisoner.MobileParty != null)
                {
                    IEnumerable<TroopRosterElement> femaleMembers = targetHero.PartyBelongedToAsPrisoner.MobileParty.MemberRoster.GetTroopRoster().Where(characterObject => characterObject.Character.IsFemale);
                    List<TroopRosterElement> troopRosterElements = femaleMembers.ToList();

                    if (!troopRosterElements.Any()) return;

                    do
                    {
                        CharacterObject m = troopRosterElements.GetRandomElement().Character;
                        randomSoldier = HeroCreator.CreateSpecialHero(m, targetHero.PartyBelongedToAsPrisoner.MobileParty.HomeSettlement, CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(15) + 18);
                    } while (!IsHeroAgeSuitableForPregnancy(randomSoldier));
                }
                else if (targetHero.PartyBelongedToAsPrisoner != null && targetHero.PartyBelongedToAsPrisoner.IsSettlement && targetHero.PartyBelongedToAsPrisoner.Settlement.Party != null && !targetHero.PartyBelongedToAsPrisoner.Settlement.Party.MemberRoster.GetTroopRoster().IsEmpty())
                {
                    Settlement playerCaptor = targetHero.PartyBelongedToAsPrisoner.Settlement;
                    IEnumerable<TroopRosterElement> femaleMembers = playerCaptor.Party.MemberRoster.GetTroopRoster().Where(characterObject => characterObject.Character.IsFemale);
                    List<TroopRosterElement> troopRosterElements = femaleMembers.ToList();

                    if (!troopRosterElements.Any()) return;

                    do
                    {
                        CharacterObject m = troopRosterElements.GetRandomElement().Character;
                        if (targetHero.PartyBelongedToAsPrisoner.MobileParty != null) randomSoldier = HeroCreator.CreateSpecialHero(m, targetHero.PartyBelongedToAsPrisoner.MobileParty.HomeSettlement, CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(15) + 18);
                    } while (!IsHeroAgeSuitableForPregnancy(randomSoldier));
                }
                else
                {
#if V165
                    CharacterObject m = CharacterObject.Templates.Where(characterObject => characterObject.IsFemale && characterObject.Occupation == Occupation.Wanderer).GetRandomElementInefficiently();
#else
                    CharacterObject m = CharacterObject.PlayerCharacter.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate(characterObject => characterObject.IsFemale && characterObject.Occupation == Occupation.Wanderer);
#endif
                    randomSoldier = HeroCreator.CreateSpecialHero(m, SettlementHelper.FindRandomSettlement(x => x.IsTown && x.Culture == m.Culture), CampaignData.NeutralFaction, CampaignData.NeutralFaction, MBRandom.Random.Next(15) + 18);
                }

                TextObject textObject3 = GameTexts.FindText("str_CE_impregnated");

                if (randomSoldier != null)
                {
                    textObject3.SetTextVariable("HERO", randomSoldier.Name);
                    textObject3.SetTextVariable("SPOUSE", targetHero.Name);
                    InformationManager.DisplayMessage(new InformationMessage(textObject3.ToString(), Colors.Magenta));

                    CEHelper.spouseOne = randomSoldier;
                }

                CEHelper.spouseTwo = targetHero;
                MakePregnantAction.Apply(randomSoldier);
                CEHelper.spouseOne = CEHelper.spouseTwo = null;

                //RelationsModifier(randomSoldier, 50, targetHero);
            }
        }

        private bool IsHeroAgeSuitableForPregnancy(Hero hero) => hero != null && hero.Age >= 18f && hero.Age <= 45f && !CECampaignBehavior.CheckIfPregnancyExists(hero);
    }
}