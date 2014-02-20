using System.Collections.Generic;
using MonoTouch.UIKit;

namespace RotatingWheelDemo
{
    public class RotatingWheelSettings
    {
        public UIImage SegmentImage { get; set; }
        public UIImage BackgroundImage { get; set; }
        public UIImage CenterButtonImage { get; set; }
        public List<WheelSliceValue> Slices { get; set; }

        public RotatingWheelSettings()
        {
            Slices = new List<WheelSliceValue>();
        }

        public class WheelSliceValue
        {
            public UIImage Image { get; set; }
            public string Value { get; set; }
        }
    }
}
