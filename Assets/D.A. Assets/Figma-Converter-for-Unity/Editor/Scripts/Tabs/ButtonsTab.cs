using DA_Assets.DAI;
using DA_Assets.FCU.Extensions;

namespace DA_Assets.FCU
{
    internal class ButtonsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity>
    {
        internal void Draw()
        {
            gui.TabHeader(FcuLocKey.label_buttons_tab.Localize(), FcuLocKey.tooltip_buttons_tab.Localize());
            gui.Space15();

            gui.DrawObjectFields(monoBeh.Settings.ButtonSettings);

            gui.DrawObjectFields(monoBeh.Settings.ButtonSettings.UnityButtonSettings);

#if DABUTTON_EXISTS
            if (monoBeh.UsingDAButton())
            {
                gui.Space15();
                this.DAB_Section.Draw();
            }
#endif
        }

#if DABUTTON_EXISTS
        private DAB_Section dabSection;
        internal DAB_Section DAB_Section => monoBeh.Link(ref dabSection, scriptableObject);
#endif
    }
}