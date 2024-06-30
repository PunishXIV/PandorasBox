using Dalamud.Game;
using Dalamud.Utility;
using ECommons.DalamudServices;
using Lumina.Excel;

namespace PandorasBox.Helpers
{
    public static class LazyRowExtensions
    {
        public static LazyRow<T> GetDifferentLanguage<T>(this LazyRow<T> row, ClientLanguage language) where T : ExcelRow
        {
            return new LazyRow<T>(Svc.Data.GameData, row.Row, language.ToLumina());
        }
    }
}
