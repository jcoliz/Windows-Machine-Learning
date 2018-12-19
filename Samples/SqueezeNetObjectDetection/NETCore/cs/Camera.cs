using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using static Helpers.AsyncHelper;
using Log = System.Console;

namespace SampleModule
{
    static class Camera
    {
        public static async Task<Tuple<MediaFrameSourceGroup, MediaFrameSourceInfo>> EnumFrameSourcesAsync()
        {
            MediaFrameSourceInfo result_info = null;
            MediaFrameSourceGroup result_group = null;
            var sourcegroups = await AsAsync(MediaFrameSourceGroup.FindAllAsync());
            Log.WriteLine("found {0} Source Groups", sourcegroups.Count);
            foreach (var g in sourcegroups)
            {
                var sourceinfos = g.SourceInfos;
                Log.WriteLine("Source Group {0}", g.Id);
                Log.WriteLine("             {0}", g.DisplayName);
                Log.WriteLine("             with {0} Sources:", sourceinfos.Count);
                foreach (var s in sourceinfos)
                {
                    var d = s.DeviceInformation;
                    Log.WriteLine("\t{0}", s.Id);
                    Log.WriteLine("\t\tKind {0}", s.SourceKind);
                    Log.WriteLine("\t\tDevice {0}", d.Id);
                    Log.WriteLine("\t\t       {0}", d.Name);
                    Log.WriteLine("\t\t       Kind {0}", d.Kind);
                    if (result_info == null)
                    {
                        result_info = s; // for now just pick the first thing we find
                    }
                }
                Log.WriteLine("\r\n");
                if (result_group == null)
                {
                    result_group = g; // for now just pick the first thing we find
                }
            }
            return new Tuple<MediaFrameSourceGroup, MediaFrameSourceInfo>(result_group, result_info);
        }
    }
}