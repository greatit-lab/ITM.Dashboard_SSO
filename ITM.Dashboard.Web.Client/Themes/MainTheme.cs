// ITM.Dashboard.Web.Client/Themes/MainTheme.cs
using MudBlazor;
using MudBlazor.Utilities;

namespace ITM.Dashboard.Web.Client.Themes
{
    public class MainTheme
    {
        public static MudTheme MainITMTheme = new MudTheme()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = "#009688",
                AppbarBackground = "#009688",
                DrawerBackground = "#FFFFFF",
                DrawerText = "rgba(0,0,0, 0.7)",
                DrawerIcon = "rgba(0,0,0, 0.7)",
                Success = "#00C853",
            },
            PaletteDark = new PaletteDark()
            {
                Primary = "#009688",
                Secondary = "#2196F3",
                Background = "#1E2125",
                AppbarBackground = "#25292D",
                DrawerBackground = "#1E2125",
                Surface = "#2C3035",
                TextPrimary = "rgba(255,255,255, 0.87)",
                TextSecondary = "rgba(255,255,255, 0.60)",
                ActionDefault = "#ADADAD",
                ActionDisabled = "rgba(255,255,255, 0.26)",
                DrawerIcon = "rgba(255,255,255, 0.87)",
                DrawerText = "rgba(255,255,255, 0.87)",
                Success = "#00C853",
                Info = "#29B6F6",
                Warning = "#FFAB00",
                Error = "#F44336"
            },
            
            // ▼▼▼ 고객님께서 찾아주신 정확한 코드를 반영합니다. ▼▼▼
            Typography = new Typography()
            {
                Default = new DefaultTypography()
                {
                    FontFamily = new[] { "Roboto", "Helvetica", "Arial", "sans-serif" },
                    FontWeight = "400",
                    LineHeight = "1.6"
                },
                H4 = new H4Typography() { FontWeight = "300" },
                H5 = new H5Typography() { FontWeight = "400" },
                H6 = new H6Typography() { FontWeight = "500" },
                Subtitle1 = new Subtitle1Typography() { FontSize = "1.1rem" }
            },
            LayoutProperties = new LayoutProperties()
            {
                DrawerWidthLeft = "260px"
            }
        };
    }
}
