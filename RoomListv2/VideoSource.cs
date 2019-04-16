using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoomListv2
{

    public class VideoSource
    {
            public uint IconValue { get; set; }
            public uint InputValue { get; set; }
            public string InputName {get; set; }
            public string OutputName { get; set; }
            public bool enabled { get; set; }

            public VideoSource ()
	        {
                IconValue = 0;
                InputValue = 0;
                OutputName = "N/A";
                InputName = "No Name";
                enabled = false;
	        }

            public VideoSource(VideoSource obj)
            {
                IconValue = obj.IconValue;
                InputValue = obj.InputValue;
                OutputName = obj.OutputName;
                InputName = obj.InputName;
                enabled = obj.enabled;
            }

            public void reset()
            {
                IconValue = 0;
                InputValue = 0;
                InputName = "No Name";
                OutputName = "N/A";
                enabled = false;
            }

            public override string ToString()
            {
                return String.Format("Input Name: {0} || InputValue {1} || OutputName: {2} ||  IconValue: {3} || Enabled: {4}", InputName, InputValue, OutputName, IconValue, enabled);
            }

    }
}
