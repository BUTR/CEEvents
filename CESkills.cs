﻿using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace CaptivityEvents
{
    internal static class CESkills
    {
        public static SkillObject Prostitution => CustomSkills[_StartDefaultSkillNode];

        public static SkillObject Slavery => CustomSkills[_StartDefaultSkillNode + 1];

        public static SkillObject IsProstitute => CustomSkills[_StartDefaultSkillNode + 2];

        public static SkillObject IsSlave => CustomSkills[_StartDefaultSkillNode + 3];

        public static bool IsInitialized => _Initialized;

        internal static List<SkillObject> CustomSkills { get; private set; }

        internal static CharacterAttribute CEAttribute { get; private set; }

        private static int _StartDefaultSkillNode = 0;

        private static List<CESkillNode> _Skills = new List<CESkillNode>();

        private static bool _Initialized;


        public static void AddCustomSkill(CESkillNode skillNode)
        {
            _Skills.Add(skillNode);
        }

        public static SkillObject FindSkill(string skill)
        {

            foreach (SkillObject skillObjectCustom in CustomSkills)
            {
                if (skillObjectCustom.Name.ToString().Equals(skill, StringComparison.InvariantCultureIgnoreCase) || skillObjectCustom.StringId == skill)
                {
                    return skillObjectCustom;
                }
            }

            foreach (SkillObject skillObject in SkillObject.All)
            {
                if (skillObject.Name.ToString().Equals(skill, StringComparison.InvariantCultureIgnoreCase) || skillObject.StringId == skill)
                {
                    return skillObject;
                }
            }

            return null;
        }


        public static void RegisterAll(Game game)
        {
            CustomSkills = new List<SkillObject>();

            CEAttribute = game.ObjectManager.RegisterPresumedObject(new CharacterAttribute("CEAttribute"));

            _StartDefaultSkillNode = _Skills.Count;

            _Skills.Add(new CESkillNode("Prostitution", "{=CEEVENTS1106}Prostitution"));
            _Skills.Add(new CESkillNode("Slavery", "{=CEEVENTS1105}Slavery"));
            _Skills.Add(new CESkillNode("IsProstitute", "{=CEEVENTS1104}prostitute"));
            _Skills.Add(new CESkillNode("IsSlave", "{=CEEVENTS1103}slave"));

            foreach (CESkillNode skill in _Skills)
            {
                CustomSkills.Add(game.ObjectManager.RegisterPresumedObject(new SkillObject(skill.Id)));
            }

            InitializeAll();
        }

        public static void InitializeAll()
        {
            CEAttribute.Initialize(new TextObject("CE"), new TextObject("CE represents the ability to move with speed and force."), new TextObject("CE"), CharacterAttributesEnum.Social);

            for (int i = 0; i < CustomSkills.Count; i++)
            {
                CustomSkills[i].Initialize(new TextObject(_Skills[i].Name), new TextObject(_Skills[i].Name), SkillObject.SkillTypeEnum.Personal).SetAttribute(CEAttribute);
            }

            _Initialized = true;
        }
    }
}