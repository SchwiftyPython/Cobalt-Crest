using System.Collections.Generic;
using UnityEngine;

namespace Audio
{
    public static class ScaleQuantizer
    {
        public enum Scale
        {
            Pentatonic,
            Dorian,
            Lydian
        }

        private static readonly Dictionary<Scale, int[]> PC = new()
        {
            { Scale.Pentatonic, new[] { 0, 2, 4, 7, 9 } }, // major pentatonic
            { Scale.Dorian, new[] { 0, 2, 3, 5, 7, 9, 10 } },
            { Scale.Lydian, new[] { 0, 2, 4, 6, 7, 9, 11 } }
        };

        private static bool Allowed(int pc, Scale s)
        {
            var set = PC.TryGetValue(s, out var arr) ? arr : PC[Scale.Pentatonic];
            
            if (arr != null)
            {
                for (var i = 0; i < arr.Length; i++)
                {
                    if (arr[i] == pc)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int WrapMod(int a, int m)
        {
            var r = a % m;
            return r < 0 ? r + m : r;
        }

        /// Quantize any absolute semitone to nearest pitch class in the scale.
        public static int Quantize(int absSemitone, Scale scale)
        {
            var pc = WrapMod(absSemitone, 12);
            if (Allowed(pc, scale))
            {
                return absSemitone;
            }

            // search nearest ±n semitones
            for (var step = 1; step <= 6; step++)
            {
                int up = pc + step, dn = pc - step;
                if (Allowed(WrapMod(up, 12), scale))
                {
                    return absSemitone + step;
                }

                if (Allowed(WrapMod(dn, 12), scale))
                {
                    return absSemitone - step;
                }
            }

            return absSemitone;
        }

        /// Clamp to [minOct,maxOct] range (inclusive) after quantization.
        public static int QuantizeAndClamp(int absSemitone, Scale scale, int minOct, int maxOct)
        {
            var q = Quantize(absSemitone, scale);
            var min = Mathf.Clamp(minOct, 0, 8) * 12;
            var max = Mathf.Clamp(maxOct, 0, 8) * 12 + 11;
            return Mathf.Clamp(q, min, max);
        }
    }
}