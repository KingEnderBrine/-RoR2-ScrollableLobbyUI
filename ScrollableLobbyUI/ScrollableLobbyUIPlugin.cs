using BepInEx;
using BepInEx.Configuration;
using System.Reflection;
using System.Security.Permissions;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion(ScrollableLobbyUI.ScrollableLobbyUIPlugin.Version)]
namespace ScrollableLobbyUI
{
    [BepInPlugin(GUID, Name, Version)]
    public class ScrollableLobbyUIPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.KingEnderBrine.ScrollableLobbyUI";
        public const string Name = "Scrollable lobby UI";
        public const string Version = "1.9.1";

        internal static ConfigEntry<int> CharacterSelectRows { get; private set; }
        internal static ConfigEntry<bool> InlinePageArrows { get; private set; }
        internal static ConfigEntry<bool> ScrollRowsWithMouseWheel { get; private set; }
        internal static ConfigEntry<int> SkillRowHeight { get; private set; }

        private void Awake()
        {
            CharacterSelectRows = Config.Bind("Main", "CharacterSelectRows", 2, new ConfigDescription("The amount of rows that should be displayed in character select screen", new AcceptableValueRange<int>(1, 50)));
            InlinePageArrows = Config.Bind("Main", "CharacterPageArrowsInline", false, new ConfigDescription("Sets if the arrows should be placed to the side of the character list (false) or if they should be placed in a survivor square (true)."));
            ScrollRowsWithMouseWheel = Config.Bind("Main", "SkillsSkinsWheelScrolling", false, new ConfigDescription("Sets if rows of skills or skins with too many entries should be scrollable with mouse wheel."));
            SkillRowHeight = Config.Bind("Main", "SkillsSkinsRowHeight", 96, new ConfigDescription("The height of a single skill/skin row.", new AcceptableValueRange<int>(1, 320)));

            //Editing skills overview UI to prevent auto resizing and add scrolling
            IL.RoR2.UI.CharacterSelectController.RebuildLocal += UIHooks.CharacterSelectControllerRebuildLocal;

            //Editing lobby UI to add scrolling for skill and loadout
            On.RoR2.UI.LoadoutPanelController.Awake += UIHooks.LoadoutPanelControllerAwake;
            On.RoR2.UI.LoadoutPanelController.Row.ctor += UIHooks.LoadoutPanelControllerRowCtor;
            On.RoR2.UI.LoadoutPanelController.Row.FinishSetup += UIHooks.LoadoutPanelControllerRowFinishSetup;
            On.RoR2.UI.LoadoutPanelController.OnDestroy += UIHooks.LoadoutPanelControllerOnDestroy;

            On.RoR2.CharacterSelectBarController.Awake += UIHooks.CharacterSelectBarControllerAwake;
            On.RoR2.CharacterSelectBarController.Build += UIHooks.CharacterSelectBarControllerBuild;
            On.RoR2.CharacterSelectBarController.EnforceValidChoice += UIHooks.CharacterSelectBarControllerEnforceValidChoice;
            On.RoR2.CharacterSelectBarController.PickIconBySurvivorDef += UIHooks.CharacterSelectBarControllerPickIconBySurvivorDef;

            On.RoR2.UI.RuleBookViewer.Awake += UIHooks.RuleBookViewerAwake;
            On.RoR2.UI.RuleCategoryController.SetData += UIHooks.RuleCategoryControllerSetData;
            On.RoR2.UI.RuleBookViewerStrip.Update += UIHooks.RuleBookViewerStripUpdate;
            On.RoR2.UI.RuleBookViewerStrip.SetData += UIHooks.RuleBookViewerStripSetData;
        }

        private void Start()
        {
            InLobbyConfigIntegration.OnStart();
        }
    }
}