using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoomListv2
{

    public class RoomInputValues
    {
        public List<VideoSource> Displays;
        public List<VideoSource> Cameras;
        public uint AudioValue;

        public RoomInputValues()
        {
            Displays = new List<VideoSource>();
            Cameras = new List<VideoSource>();
            AudioValue = 0;
            for (int i = 0; i < 4; i++)
            { 
                Displays.Add(new VideoSource());
                Cameras.Add(new VideoSource());
            }
        }

        public void Reset()
        {
            foreach (VideoSource source in Displays)
            {
                source.reset();
            }
            foreach (VideoSource source in Cameras)
            {
                source.reset();
            }
            AudioValue = 0;
        }
    }
}
