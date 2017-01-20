using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port {
    public static class Settings {

        static Settings(){

            string[] settinglines;

            Dictionary<string, string> settings = new Dictionary<string, string>();

            settinglines = Properties.Resources.usersettings.Split('\n');
            int linenumber = 0;
            foreach(string settingline in settinglines) {
                linenumber++;
                if(settingline.Count(c => c == '=') != 1) {
                    Console.WriteLine("Invalid Line: " + linenumber + ".");
                    continue;
                }
                string[] pair = settingline.Split('=');
                settings.Add(pair[0].Trim(), pair[1].Trim());
            }
            UserSettings = new __UserSettings(settings);
        }

        public static readonly __UserSettings UserSettings;

        public class __UserSettings {

            public readonly bool StartFullscreen;
            public readonly Size2 WindowResolution;
            public readonly Size2 FullscreenResolution;

            private Dictionary<string, string> __settings;

            public string this[string index] {
                get {
                    if (__settings.ContainsKey(index))
                        return __settings[index];
                    return null;
                }
            }

            protected internal __UserSettings(Dictionary<string, string> settings) {
                __settings = settings;
                string reswinx = __settings["Resolution_Windowed"].Split(new char[] { 'x', 'X' })[0];
                string reswiny = __settings["Resolution_Windowed"].Split(new char[] { 'x', 'X' })[1];
                string resfullx = __settings["Resolution_Fullscreen"].Split(new char[] { 'x', 'X' })[0];
                string resfully = __settings["Resolution_Fullscreen"].Split(new char[] { 'x', 'X' })[1];

                WindowResolution = new Size2(int.Parse(reswinx), int.Parse(reswiny));
                FullscreenResolution = new Size2(int.Parse(resfullx), int.Parse(resfully));

                StartFullscreen = bool.Parse(__settings["StartInFullscreen"]);
            }
        }
    }
}
