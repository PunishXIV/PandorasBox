using Dalamud;
using ECommons.DalamudServices;
using Lumina.Excel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PandorasBox.Helpers
{
    public static class LazyRowExtensions
    {
        public static LazyRow<T> GetDifferentLanguage<T>(this LazyRow<T> row, ClientLanguage language) where T : ExcelRow
        {
            var sheet = Svc.Data.Excel.GetSheet<T>();
            return new LazyRow<T>(Svc.Data.GameData, row.Row, language.ToLumina());
        }
    }
}
