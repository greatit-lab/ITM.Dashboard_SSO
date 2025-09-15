// ITM.Dashboard.Web.Client/Themes/MainTheme.cs
using MudBlazor;

namespace ITM.Dashboard.Web.Client.Themes
{
    public class MainTheme
    {
        public static MudTheme DarkTheme = new MudTheme()
        {
            PaletteDark = new PaletteDark()
            {
                Primary = "#009688",
                Secondary = "#2196F3",
                Background = "#1E2125",
                AppbarBackground = "#1E2125",
                DrawerBackground = "#25292D",
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

            // [최종 수정] 숫자 값을 모두 문자열로 변경하고, new 키워드를 제거했습니다.
            Typography = new Typography()
            {
                // BaseTypography는 추상 클래스이므로 직접 인스턴스화할 수 없습니다.
                // Typography의 각 속성(H4, H5, H6, Subtitle1 등)에 맞는 구체적인 타입을 사용해야 합니다.
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
