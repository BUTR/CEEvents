﻿using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Localization;

namespace CaptivityEvents.Brothel
{
    internal static class CEBrothelToolTip
    {
        public static void BrothelTypeTooltipAction(this TooltipVM tooltipVM, object[] args) => UpdateTooltip(tooltipVM, args[0] as CEBrothel);

        public static void UpdateTooltip(this TooltipVM tooltipVM, CEBrothel brothel)
        {
            tooltipVM.Mode = 1;
            tooltipVM.AddProperty("", new TextObject("{=CEEVENTS1099}Brothel").ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
            tooltipVM.AddProperty(new TextObject("{=qRqnrtdX}Owner").ToString(), brothel.Owner.Name.ToString());
            tooltipVM.AddProperty(new TextObject("{=CEBROTHEL0994}Notable Prostitutes").ToString(), "None");
        }
    }
}