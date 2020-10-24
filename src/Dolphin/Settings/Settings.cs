﻿using Dolphin.Enum;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WK.Libraries.HotkeyListenerNS;

namespace Dolphin
{
    public static class InitialSettings
    {
        private static IList<ActionName> _nonMacroableActions = new List<ActionName> { ActionName.None, ActionName.OpenRift, ActionName.AcceptGriftPopup, ActionName.StartGame };

        public static uint UpdateInterval => 100;

        public static IList<SkillName> EnabledSkills => new List<SkillName>();

        public static IDictionary<ActionName, Hotkey> Hotkeys
        {
            get
            {
                var dict = new Dictionary<ActionName, Hotkey>();
                foreach (var @enum in System.Enum.GetValues(typeof(ActionName)).Cast<ActionName>())
                {
                    if (!_nonMacroableActions.Contains(@enum))
                    {
                        dict[@enum] = @enum == ActionName.Pause ? new Hotkey(Keys.Control, Keys.C) : null;
                    }
                }
                return dict;
            }
        }

        public static MacroSettings MacroSettings => new MacroSettings
        {
            ConvertingSpeed = ConvertingSpeed.Normal,
            SelectedGambleItem = ItemType.OneHandedWeapon,
            SpareColumns = 1,
            SwapItemsAmount = 3,
            Poolspots = new List<Waypoint>
            {
                new Waypoint{Act = 1, Name = "CemetryOfTheForsaken" },
                new Waypoint{Act = 2, Name = "HowlingPlateau"}
            }
        };

        public static IDictionary<Command, Keys> OtherKeybindings
        {
            get
            {
                return new Dictionary<Command, Keys>
                {
                    { Command.TeleportToTown, Keys.T },
                    { Command.OpenMap, Keys.M },
                    { Command.OpenInventory, Keys.C }
                };
            }
        }

        public static IList<Keys> SkillKeybindigns => new Keys[] { Keys.D1, Keys.D2, Keys.D3, Keys.D4 };

        public static UiSettings UiSettings => new UiSettings
        {
            DisplayLogLevel = LogLevel.Warning,
            LogPaused = false,
            IsDark = false
        };
    }

    public class Settings
    {
        public IList<SkillName> EnabledSkills { get; set; } = InitialSettings.EnabledSkills;

        public IDictionary<Enum.ActionName, Hotkey> Hotkeys { get; set; } = InitialSettings.Hotkeys;

        public bool IsPaused { get; set; }

        public MacroSettings MacroSettings { get; set; } = InitialSettings.MacroSettings;

        public IDictionary<Command, Keys> OtherKeybindings { get; set; } = InitialSettings.OtherKeybindings;

        public IList<Keys> SkillKeybindings { get; set; } = InitialSettings.SkillKeybindigns;

        public UiSettings UiSettings { get; set; } = InitialSettings.UiSettings;

        public uint UpdateInterval { get; set; } = InitialSettings.UpdateInterval;
    }
}