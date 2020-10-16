﻿using Dolphin.Enum;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Dolphin
{
    public class Settings
    {
        public IList<SkillName> EnabledSkills { get; set; } = new List<SkillName>();

        public IList<Keys> SkillKeybindings { get; set; } = new Keys[] { Keys.D1, Keys.D2, Keys.D3, Keys.D4 };
    }
}