using System;

namespace PandorasBox.Helpers
{
    public static class NumberHelper
    {
        public static int RoundOff(this int i, int sliderIncrement)
        {
            var sliderAsDouble = Convert.ToDouble(sliderIncrement);
            return ((int)Math.Round(i / sliderAsDouble)) * (int)sliderIncrement;
        }

        public static float RoundOff(this float i, float sliderIncrement)
        {
            return (float)Math.Round(i / sliderIncrement) * sliderIncrement;
        }
    }
}
