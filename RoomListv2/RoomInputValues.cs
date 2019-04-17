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
            AudioValue = 41;
            for (int i = 0; i < 4; i++)
            { 
                Displays.Add(new VideoSource());
                Cameras.Add(new VideoSource());
            }
        }

        public RoomInputValues(RoomInputValues obj)
        {
            AudioValue = obj.AudioValue;
            Displays = new List<VideoSource>();
            Cameras = new List<VideoSource>();
            for (int i = 0; i < 4; i++)
            {
                Displays.Add(new VideoSource(obj.Displays[i]));
                Cameras.Add(new VideoSource(obj.Cameras[i]));
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
            AudioValue = 41;
        }

        public override string ToString()
        {

            return String.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n", Displays[0].ToString(), Displays[1].ToString(), Displays[2].ToString(), Displays[3].ToString(),
                                                                        Cameras[0].ToString(), Cameras[1].ToString(), Cameras[2].ToString());
        }


    }
}
