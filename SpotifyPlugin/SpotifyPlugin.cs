using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using Controll.Common;

namespace SpotifyPlugin
{
    [ActivityAttribute("27611FAD-17CD-463B-A179-796F3E3B1120")]
    public class SpotifyPlugin : IPlugin
    {
        public SpotifyPlugin()
        {
            Name = "Spotfy Plugin";
        }

        public Guid Key { get; private set; }
        public string Name { get; private set; }
        public void Execute(IPluginContext context)
        {
            context.Started();
            
            Process [] spotifies =Process.GetProcessesByName("spotify");
            if(spotifies.Length==0)
            {
                context.Error("Spotify is not running");
            }

            var spotify = spotifies[0];

            string action;
            if (!context.Parameters.TryGetValue("Command", out action))
            {
                context.Error("Key 'Command' (case sensitive) not found in parameters.\nPassed parameters: " +
                    string.Join("\n", context.Parameters.Select(p => "\"" + p.Key + "\" = \"" + p.Value + "\"")));
                return;
            }

            string findTrackName = context.Parameters["name"];

            var uri = GetURIOfBestMatchingTrack(findTrackName, "track", context);

            Process.Start(new ProcessStartInfo(uri));


            #region gamla
            //string action;
            //if (!context.Parameters.TryGetValue("Command", out action))
            //{
            //    context.Error("Key 'Command' (case sensitive) not found in parameters.\nPassed parameters: " + 
            //        string.Join("\n", context.Parameters.Select(p => "\"" + p.Key + "\" = \"" + p.Value + "\"")));
            //    return;
            //}


            //switch (action.ToString())
            //{
            //    case "sleep":
            //        context.Notify("Sleeping 5 sec");
            //        Thread.Sleep(5000);
            //        context.Notify("Done - sleeping 1 more");
            //        Thread.Sleep(1000);
            //        context.Notify("Done - sleeping 1 more");
            //        Thread.Sleep(1000);
            //        context.Notify("Done - sleeping 1 more");
            //        Thread.Sleep(1000);
            //        context.Notify("Done - sleeping 1 more");
            //        Thread.Sleep(1000);
            //        context.Notify("Done - sleeping 1 more");
            //        Thread.Sleep(1000);
            //        break;
            //    case "playpause":
            //        Win32.SendMessage(spotify.MainWindowHandle, Win32.Constants.WM_APPCOMMAND, IntPtr.Zero, new IntPtr((long)SpotifyAction.PlayPause));
            //        break;
            //    case "next":
            //        Win32.SendMessage(spotify.MainWindowHandle, Win32.Constants.WM_APPCOMMAND, IntPtr.Zero, new IntPtr((long)SpotifyAction.NextTrack));
            //        break;
            //    case "prev":
            //        Win32.SendMessage(spotify.MainWindowHandle, Win32.Constants.WM_APPCOMMAND, IntPtr.Zero, new IntPtr((long)SpotifyAction.PreviousTrack));
            //        break;
            //    default:
            //        {
            //            if (action.ToString().StartsWith("spotify:"))
            //            {
            //                Process.Start(new ProcessStartInfo(action.ToString()));
            //            }
            //            else if (action.ToString().StartsWith("find:"))
            //            {
            //                var splitted = action.ToString().Split(':');
            //                string uri = GetURIOfBestMatchingTrack(splitted[2], splitted[1], context);
            //                Process.Start(new ProcessStartInfo(uri));
            //            }else
            //            {
            //                context.Error("Unkown command");
            //                return;
            //            }
            //        }
            //        break;
            //}
            #endregion


            context.Finish("OK :)");
        }

        public string GetURIOfBestMatchingTrack(string searchFor, string type, IPluginContext context)
        {
            var jss = new JavaScriptSerializer();

            var webClient = new WebClient();
            var s = webClient.OpenRead("http://ws.spotify.com/search/1/" + type + ".json?q=" + HttpUtility.HtmlEncode(searchFor));
            string json = new StreamReader(s).ReadToEnd();

            var d = jss.Deserialize<dynamic>(json);

            string trackName = d["tracks"][0]["name"];
            string artistName = d["tracks"][0]["artists"][0]["name"];
            string albumName = d["tracks"][0]["album"]["name"];
            context.Notify(string.Format("Found track {0} by {1}, {2}", trackName, artistName, albumName));

            string uri = d["tracks"][0]["href"];
            return uri;

        }

        internal class Win32
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
            internal static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            internal class Constants
            {
                internal const uint WM_APPCOMMAND = 0x0319;
            }
        }


        public enum SpotifyAction : long
        {
            PlayPause = 917504,
            Mute = 524288,
            VolumeDown = 589824,
            VolumeUp = 655360,
            Stop = 851968,
            PreviousTrack = 786432,
            NextTrack = 720896
        }

    }
}
