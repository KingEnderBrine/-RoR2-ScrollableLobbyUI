using BepInEx;

namespace ScrollableLobbyUI
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.KingEnderBrine.ScrollableLobbyUI", "Scrollable lobby UI", "1.2.0")]

    public class ScrollableLobbyUIPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            //Edditing skills overview UI to prevent auto resizig
            IL.RoR2.UI.CharacterSelectController.RebuildLocal += UIHooks.CharacterSelectControllerRebuildLocal;

            //Edditing lobby UI to add scrolling for skill and loadout
            On.RoR2.UI.LoadoutPanelController.Awake += UIHooks.LoadoutPanelControllerAwake;
            On.RoR2.UI.LoadoutPanelController.Row.ctor += UIHooks.LoadoutPanelControllerRowCtor;
            On.RoR2.UI.LoadoutPanelController.Row.FinishSetup += UIHooks.LoadoutPanelControllerRowFinishSetup;

            On.RoR2.CharacterSelectBarController.Awake += (orig, self) =>
            {
                self.gameObject.AddComponent<CharacterSelectBarControllerReplacement>();
            };
        }
    }
}