using System;
using System.Runtime.InteropServices;

namespace Visualiser_Hub
{
    internal static class CapHelper
    {
        public static IPin GetPin(this IBaseFilter filter, PinDirection dir, int num)
        {
            IPin[] pin = new IPin[1];
            IEnumPins pinsEnum = null;

            try
            {
                if (filter.EnumPins(out pinsEnum) == 0)
                {
                    PinDirection pinDir;
                    int n = 0;

                    while (pinsEnum.Next(1, pin, out n) == 0)
                    {
                        pin[0].QueryDirection(out pinDir);

                        if (pinDir == dir)
                        {
                            if (num == 0)
                            {
                                return pin[0];
                            }
                            num--;
                        }

                        Marshal.ReleaseComObject(pin[0]);
                        pin[0] = null;
                    }
                }

            }
            catch (NullReferenceException exNull)
            {
            }

            return null;
        }
    }
}
