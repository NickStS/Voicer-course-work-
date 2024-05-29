using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Voicer.Service
{
    public class SystemControlService
    {
        public void IncreaseVolume()
        {
            const int APPCOMMAND_VOLUME_UP = 0x0a0000;
            const int FAPPCOMMAND_KEYDOWN = 0x0100;

            unchecked
            {
                keybd_event((byte)APPCOMMAND_VOLUME_UP, 0, FAPPCOMMAND_KEYDOWN, 0);
            }
        }

        public void DecreaseVolume()
        {
            const int APPCOMMAND_VOLUME_DOWN = 0x090000;
            const int FAPPCOMMAND_KEYDOWN = 0x0100;

            unchecked
            {
                keybd_event((byte)APPCOMMAND_VOLUME_DOWN, 0, FAPPCOMMAND_KEYDOWN, 0);
            }
        }

        public void ShutdownComputer()
        {
            Process.Start("shutdown", "/s /t 0");
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
    }
}