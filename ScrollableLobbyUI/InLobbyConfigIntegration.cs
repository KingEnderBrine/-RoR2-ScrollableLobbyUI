using BepInEx.Bootstrap;
using InLobbyConfig;
using InLobbyConfig.Fields;
using System.Runtime.CompilerServices;

namespace ScrollableLobbyUI
{
    public static class InLobbyConfigIntegration
    {
        public const string GUID = "com.KingEnderBrine.InLobbyConfig";
        private static bool Enabled => Chainloader.PluginInfos.ContainsKey(GUID);
        private static object ModConfig { get; set; }

        public static void OnStart()
        {
            if (Enabled)
            {
                OnStartInternal();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnStartInternal()
        {
            var modConfig = new ModConfigEntry
            {
                DisplayName = ScrollableLobbyUIPlugin.Name,
                SectionFields = 
                {
                    [ScrollableLobbyUIPlugin.CharacterSelectRows.Definition.Section] = new IConfigField[]
                    {
                        new IntConfigField(ScrollableLobbyUIPlugin.CharacterSelectRows.Definition.Key, ScrollableLobbyUIPlugin.CharacterSelectRows.Description.Description, () => ScrollableLobbyUIPlugin.CharacterSelectRows.Value, null, (newValue) => ScrollableLobbyUIPlugin.CharacterSelectRows.Value = newValue, 1, 50),
                        new BooleanConfigField(ScrollableLobbyUIPlugin.InlinePageArrows.Definition.Key, ScrollableLobbyUIPlugin.InlinePageArrows.Description.Description, () => ScrollableLobbyUIPlugin.InlinePageArrows.Value, (newValue) => ScrollableLobbyUIPlugin.InlinePageArrows.Value = newValue),
                        new BooleanConfigField(ScrollableLobbyUIPlugin.ScrollRowsWithMouseWheel.Definition.Key, ScrollableLobbyUIPlugin.ScrollRowsWithMouseWheel.Description.Description, () => ScrollableLobbyUIPlugin.ScrollRowsWithMouseWheel.Value, (newValue) => ScrollableLobbyUIPlugin.ScrollRowsWithMouseWheel.Value = newValue),
                        new IntConfigField(ScrollableLobbyUIPlugin.SkillRowHeight.Definition.Key, ScrollableLobbyUIPlugin.SkillRowHeight.Description.Description, () => ScrollableLobbyUIPlugin.SkillRowHeight.Value, null, (newValue) => ScrollableLobbyUIPlugin.SkillRowHeight.Value = newValue, 1, 320),
                    }
                }
            };

            ModConfigCatalog.Add(modConfig);
            ModConfig = modConfig;
        }
    }
}
