﻿using CaptivityEvents.Brothel;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;

namespace CaptivityEvents.Patches
{
    [HarmonyPatch(typeof(ClanIncomeVM), "RefreshList")]
    internal class CEPatchClanIncomeVM
    {

        public MethodInfo GetDefaultIncome = AccessTools.Method(typeof(ClanIncomeVM), "GetDefaultIncome");
        public MethodInfo OnIncomeSelection = AccessTools.Method(typeof(ClanIncomeVM), "OnIncomeSelection");
        internal CEBrothelSession Session { get; set; }


        public CEPatchClanIncomeVM(CEBrothelSession session)
        {
            Session = session;
        }


        [HarmonyPrepare]
        private bool ShouldPatch()
        {
            return CESettings.Instance != null && CESettings.Instance.ProstitutionControl;
        }

        [HarmonyPostfix]
        public void RefreshList(ClanIncomeVM __instance)
        {
            foreach (var brothel in Session.GetPlayerBrothels())
            {
                __instance.Incomes.Add(new CEBrothelClanFinanceItemVM(brothel, new Action<ClanFinanceIncomeItemBaseVM>((ClanFinanceIncomeItemBaseVM brothelIncome) =>
                {
                    OnIncomeSelection.Invoke(__instance, new object[] { brothelIncome });
                }), new Action(__instance.OnRefresh)));
            }
            __instance.RefreshTotalIncome();
            OnIncomeSelection.Invoke(__instance, new object[] { GetDefaultIncome.Invoke(__instance, null) });
            __instance.RefreshValues();
        }
    }
}
