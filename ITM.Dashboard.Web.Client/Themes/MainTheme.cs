// ITM.Dashboard.Web.Client/Themes/MainTheme.cs
using MudBlazor;
using MudBlazor.Utilities;

namespace ITM.Dashboard.Web.Client.Themes
{
    public class MainTheme
    {
        public static MudTheme MainITMTheme = new MudTheme()
        {
            // 1. 라이트 모드 팔레트 정의
            PaletteLight = new PaletteLight()
            {
                Primary = "#009688",
                AppbarBackground = "#009688",
                DrawerBackground = "#FFFFFF",
                DrawerText = "rgba(0,0,0, 0.7)",
                DrawerIcon = "rgba(0,0,0, 0.7)",
                Success = "#00C853",
            },
            // 2. 다크 모드 팔레트 정의
            PaletteDark = new PaletteDark()
            {
                Primary = "#009688",
                Background = "#1E2125",
                AppbarBackground = "#25292D",
                DrawerBackground = "#1E2125",
                Surface = "#2C3035",
                Success = "#00C853",
                Info = "#29B6F6",
            },
            // 3. 타이포그래피 정의 (오류 수정된 버전)
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
            }
        };
    }
}
