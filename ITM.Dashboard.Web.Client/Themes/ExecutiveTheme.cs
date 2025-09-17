// ITM.Dashboard.Web.Client/Themes/ExecutiveTheme.cs
using MudBlazor;

namespace ITM.Dashboard.Web.Client.Themes
{
    public class ExecutiveTheme
    {
        public static MudTheme ExecITMTheme = new MudTheme()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = "#00796B",
                AppbarBackground = "#00796B",
                Background = "#F7F9FC",
                Surface = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#24292e",
                Divider = "rgba(0,0,0, 0.08)",
                ActionDefault = "#586069"
            },
            PaletteDark = new PaletteDark()
            {
                Primary = "#26A69A",
                // ▼▼▼ [수정] 배경과 카드 색상을 미세하게 조정하여 깊이감 부여 ▼▼▼
                Background = "#18191C", // 더 깊고 어두운 배경
                Surface = "#242529",    // 배경보다 살짝 밝은 카드
                AppbarBackground = "#18191C",
                DrawerBackground = "#18191C",
                Divider = "rgba(255,255,255, 0.12)",
                ActionDefault = "#adbac7"
            },
            LayoutProperties = new LayoutProperties()
            {
                DrawerWidthLeft = "260px"
            }
        };
    }
}
