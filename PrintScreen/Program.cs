using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConfigurationData;
using llc = LowLevelControls;

namespace PrintScreen
{
    class Program
    {
        static llc.KeyboardHook kbdHook = new llc.KeyboardHook();
        static Config config = new Config(Path.Combine(AssemblyDirectory, "config"));
        static volatile bool loop = true;
        static bool prscDown = false;
        static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("PrintScreen (made by j3soon)");
            Console.WriteLine("1. Press PrintScreen to save the entire screen.");
            Console.WriteLine("2. Press Alt+PrintScreen to save the current window.");
            Console.WriteLine("3. Press Ctrl+C to exit.");
            config.DefaultConfigEvent += () => { config["dir"] = @"%UserProfile%\Desktop\Screenshots\"; };
            config.Load();
            config.Save();
            Console.WriteLine("4. The captured screens will be saved in: " + config["dir"]);
            kbdHook.KeyDownEvent += kbdHook_KeyDownEvent;
            kbdHook.KeyUpEvent += kbdHook_KeyUpEvent;
            kbdHook.InstallGlobalHook();
            Console.CancelKeyPress += Console_CancelKeyPress;
            while (loop)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            kbdHook.UninstallGlobalHook();
            config.Save();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            loop = false;
        }

        private static bool kbdHook_KeyDownEvent(llc.KeyboardHook sender, uint vkCode, bool injected)
        {
            if (vkCode == (uint)Keys.PrintScreen && !prscDown)
            {
                if (llc.Keyboard.IsKeyDown((int)Keys.Menu))
                    Console.WriteLine("Saved Current Window - " + PrintWindow());
                else
                    Console.WriteLine("Saved Current Screen - " + PrintScreen());
                prscDown = true;
                return true;
            }
            return false;
        }

        private static bool kbdHook_KeyUpEvent(llc.KeyboardHook sender, uint vkCode, bool injected)
        {
            if (vkCode == (uint)Keys.PrintScreen)
                prscDown = false;
            return false;
        }

        static String GetScreenshotName()
        {
            String path = Environment.ExpandEnvironmentVariables(config["dir"]);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = Path.Combine(path, DateTime.Now.ToString("yyyyMMdd_HH-mm-ss"));
            int num = 0;
            for (; File.Exists(Path.Combine(path, num == 0 ? "" : ("_" + num)) + ".png"); num++) ;
            return path + (num == 0 ? "" : ("_" + num)) + ".png";
        }

        static String PrintScreen()
        {
            Rectangle bounds = SystemInformation.VirtualScreen;
            Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bmp))
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            String name = GetScreenshotName();
            bmp.Save(name, ImageFormat.Png);
            return name;
        }

        static String PrintWindow()
        {
            llc.Natives.RECT rect = llc.Window.GetWindowBounds(llc.Window.GetForegroundWindow());
            Rectangle bounds = new Rectangle
            {
                X = rect.left,
                Y = rect.top,
                Width = rect.right - rect.left,
                Height = rect.bottom - rect.top
            };
            Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bmp))
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            String name = GetScreenshotName();
            bmp.Save(name, ImageFormat.Png);
            return name;
        }
    }
}
