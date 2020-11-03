using BepInEx;
using R2API;
using R2API.Utils;

namespace ScrollableLobbyUI
{
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    [R2APISubmoduleDependency(nameof(PrefabAPI))]
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.KingEnderBrine.ScrollableLobbyUI", "Scrollable lobby UI", "1.3.3")]

    public class ScrollableLobbyUIPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            //Edditing skills overview UI to prevent auto resizing and add scrolling
            IL.RoR2.UI.CharacterSelectController.RebuildLocal += UIHooks.CharacterSelectControllerRebuildLocal;

            //Edditing lobby UI to add scrolling for skill and loadout
            On.RoR2.UI.LoadoutPanelController.Awake += UIHooks.LoadoutPanelControllerAwake;
            On.RoR2.UI.LoadoutPanelController.Row.ctor += UIHooks.LoadoutPanelControllerRowCtor;
            On.RoR2.UI.LoadoutPanelController.Row.FinishSetup += UIHooks.LoadoutPanelControllerRowFinishSetup;
            On.RoR2.UI.LoadoutPanelController.OnDestroy += UIHooks.LoadoutPanelControllerOnDestroy;

            On.RoR2.CharacterSelectBarController.Start += UIHooks.CharacterSelectBarControllerStart;
            On.RoR2.CharacterSelectBarController.Update += UIHooks.CharacterSelectBarControllerUpdate;
        }
    }
}