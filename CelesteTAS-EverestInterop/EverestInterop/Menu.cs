﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste;
using Celeste.Mod;
using Monocle;
using TAS.EverestInterop.Hitboxes;
using TAS.EverestInterop.InfoHUD;
using TAS.Utils;

namespace TAS.EverestInterop {
    internal static class Menu {
        private static readonly MethodInfo CreateKeyboardConfigUi = typeof(EverestModule).GetMethodInfo("CreateKeyboardConfigUI");
        private static List<TextMenu.Item> options;
        private static TextMenu.Item keyConfigButton;
        private static CelesteTasModuleSettings Settings => CelesteTasModule.Settings;

        internal static string ToDialogText(this string input) => Dialog.Clean("TAS_" + input.Replace(" ", "_"));

        private static void CreateOptions(EverestModule everestModule, TextMenu menu, bool inGame) {
            options = new List<TextMenu.Item> {
                HitboxTweak.CreateSubMenu(menu, inGame),
                SimplifiedGraphicsFeature.CreateSubMenu(),
                InfoHud.CreateSubMenu(),

                new TextMenuExt.SubMenu("Relaunch Required".ToDialogText(), false).Apply(subMenu => {
                    subMenu.Add(new TextMenu.OnOff("Launch Studio At Boot".ToDialogText(), Settings.LaunchStudioAtBoot).Change(value =>
                        Settings.LaunchStudioAtBoot = value));
                    subMenu.Add(new TextMenu.OnOff("Auto Extract New Studio".ToDialogText(), Settings.AutoExtractNewStudio).Change(value =>
                        Settings.AutoExtractNewStudio = value));
                    subMenu.Add(new TextMenu.OnOff("Unix RTC".ToDialogText(), Settings.UnixRtc).Change(value => Settings.UnixRtc = value));
                }),

                new TextMenuExt.SubMenu("More Options".ToDialogText(), false).Apply(subMenu => {
                    subMenu.Add(new TextMenu.OnOff("Center Camera".ToDialogText(), Settings.CenterCamera).Change(value =>
                        Settings.CenterCamera = value));
                    subMenu.Add(new TextMenu.OnOff("Round Position".ToDialogText(), Settings.RoundPosition).Change(value =>
                        Settings.RoundPosition = value));
                    subMenu.Add(new TextMenu.OnOff("Round Speed".ToDialogText(), Settings.RoundSpeed).Change(value =>
                        Settings.RoundSpeed = value));
                    subMenu.Add(new TextMenu.OnOff("Round Velocity".ToDialogText(), Settings.RoundVelocity).Change(value =>
                        Settings.RoundVelocity = value));
                    subMenu.Add(new TextMenu.OnOff("Pause After Load State".ToDialogText(), Settings.PauseAfterLoadState).Change(value =>
                        Settings.PauseAfterLoadState = value));
                    subMenu.Add(new TextMenu.OnOff("Restore Settings".ToDialogText(), Settings.RestoreSettings).Change(value =>
                        Settings.RestoreSettings = value));
                    subMenu.Add(
                        new TextMenu.OnOff("Disable Achievements".ToDialogText(), Settings.DisableAchievements).Change(value =>
                            Settings.DisableAchievements = value));
                    subMenu.Add(new TextMenu.OnOff("Mod 9D Lighting".ToDialogText(), Settings.Mod9DLighting).Change(value =>
                        Settings.Mod9DLighting = value));
                }),
                new TextMenu.Button(Dialog.Clean("options_keyconfig")).Pressed(() => {
                    menu.Focused = false;
                    Entity keyboardConfig;
                    if (CreateKeyboardConfigUi != null) {
                        keyboardConfig = CreateKeyboardConfigUi.Invoke(everestModule, new object[] {menu}) as Entity;
                    } else {
                        keyboardConfig = new ModuleSettingsKeyboardConfigUI(everestModule) {OnClose = () => menu.Focused = true};
                    }

                    Engine.Scene.Add(keyboardConfig);
                    Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
                }).Apply(item => keyConfigButton = item)
            };
        }

        public static void CreateMenu(EverestModule everestModule, TextMenu menu, bool inGame) {
            menu.Add(new TextMenu.OnOff("Enabled".ToDialogText(), Settings.Enabled).Change((value) => {
                Settings.Enabled = value;
                foreach (TextMenu.Item item in options) {
                    item.Visible = value;
                }
            }));
            CreateOptions(everestModule, menu, inGame);
            foreach (TextMenu.Item item in options) {
                menu.Add(item);
                item.Visible = Settings.Enabled;
            }

            HitboxTweak.AddSubMenuDescription(menu, inGame);
            InfoHud.AddSubMenuDescription(menu);
            keyConfigButton.AddDescription(menu, "Key Config Description".ToDialogText());
        }

        public static IEnumerable<KeyValuePair<int?, string>> CreateSliderOptions(int start, int end, Func<int, string> formatter = null) {
            if (formatter == null) {
                formatter = i => i.ToString();
            }

            List<KeyValuePair<int?, string>> result = new List<KeyValuePair<int?, string>>();

            if (start <= end) {
                for (int current = start; current <= end; current++) {
                    result.Add(new KeyValuePair<int?, string>(current, formatter(current)));
                }

                result.Insert(0, new KeyValuePair<int?, string>(null, "Default".ToDialogText()));
            } else {
                for (int current = start; current >= end; current--) {
                    result.Add(new KeyValuePair<int?, string>(current, formatter(current)));
                }

                result.Insert(0, new KeyValuePair<int?, string>(null, "Default".ToDialogText()));
            }

            return result;
        }

        public static IEnumerable<KeyValuePair<bool, string>> CreateDefaultHideOptions() {
            return new List<KeyValuePair<bool, string>> {
                new KeyValuePair<bool, string>(false, "Default".ToDialogText()),
                new KeyValuePair<bool, string>(true, "Hide".ToDialogText()),
            };
        }

        public static IEnumerable<KeyValuePair<bool, string>> CreateSimplifyOptions() {
            return new List<KeyValuePair<bool, string>> {
                new KeyValuePair<bool, string>(false, "Default".ToDialogText()),
                new KeyValuePair<bool, string>(true, "Simplify".ToDialogText()),
            };
        }
    }
}