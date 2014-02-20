using System;
using System.Collections.Generic;
using System.Drawing;
using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;

namespace RotatingWheelDemo
{
    public class RotatingWheelView : UIView
    {
        private float _deltaAngle;
        private const float _minAlphaValue = 0.6f;
        private const float _maxAlphaValue = 1.0f;
        private static CGAffineTransform _startTransform;
        private int _currentSliceValue;

        private readonly RotatingWheelSettings _settings;
        private UIView _container;
        private List<WheelSlice> _slices;

        public RotatingWheelView(RectangleF frame, RotatingWheelSettings settings)
            : base(frame)
        {
            _settings = settings;
            InitWheel();
        }

        private void InitWheel()
        {
            var sectionNumber = _settings.Slices.Count;
            _container = new UIView(Frame);
            _slices = new List<WheelSlice>(sectionNumber);

            var angleSize = (float)(2 * Math.PI / sectionNumber);
            for (int i = 0; i < sectionNumber; i++)
            {
                var imageView = new UIImageView(_settings.SegmentImage);
                imageView.Layer.AnchorPoint = new PointF(1.0f, 0.5f);
                var slicePosX = (float)(_container.Bounds.Size.Width / (2.0 - _container.Frame.X));
                var slicePosY = (float)(_container.Bounds.Size.Height / 2.0 - _container.Frame.Y);
                imageView.Layer.Position = new PointF(slicePosX, slicePosY);
                imageView.Transform = CGAffineTransform.MakeRotation(angleSize * i);
                imageView.Alpha = _minAlphaValue;
                imageView.Tag = i;

                if (i == 0)
                {
                    imageView.Alpha = _maxAlphaValue;
                }
                var sliceImageView = new UIImageView(new RectangleF(12, 15, 40, 40));
                var sliceImage = _settings.Slices[i].Image;
                sliceImageView.Image = sliceImage;
                imageView.Add(sliceImageView);
                _container.Add(imageView);
            }

            _container.UserInteractionEnabled = false;
            Add(_container);

            var backgroundView = new UIImageView(Frame) { Image = _settings.BackgroundImage };
            Add(backgroundView);

            var mask = new UIImageView(new RectangleF(0, 0, 58, 58))
                {
                    Image = _settings.CenterButtonImage,
                    Center = Center
                };
            mask.Center = new PointF(mask.Center.X, mask.Center.Y + 3);
            Add(mask);

            if (sectionNumber % 2 == 0)
            {
                BuildSlicesEvenly();
            }
            else
            {
                BuildSlicesUnEvenly();
            }
            //SliceDidChanged(this, new SliceDidChangedEventArgs { Value = _settings.Slices[0].Value });
        }

        private void BuildSlicesEvenly()
        {
            var sectionNumber = _settings.Slices.Count;
            var fanWidth = (float)Math.PI * 2 / sectionNumber;
            var mid = 0.0f;
            for (var i = 0; i < sectionNumber; i++)
            {
                var slice = new WheelSlice()
                    {
                        MidValue = mid,
                        MinValue = mid - (fanWidth / 2.0f),
                        MaxValue = mid + (fanWidth / 2.0f),
                        Value = i,
                    };

                if (slice.MaxValue - fanWidth < -Math.PI)
                {
                    mid = 3.14f;
                    slice.MidValue = mid;
                    slice.MinValue = Math.Abs(slice.MaxValue);
                }
                mid -= fanWidth;
                _slices.Add(slice);
            }
        }

        private void BuildSlicesUnEvenly()
        {
            var sectionNumber = _settings.Slices.Count;
            var fanWidth = (float)Math.PI * 2 / sectionNumber;
            var mid = 0.0f;
            for (var i = 0; i < sectionNumber; i++)
            {
                var slice = new WheelSlice()
                {
                    MidValue = mid,
                    MinValue = mid - (fanWidth / 2.0f),
                    MaxValue = mid + (fanWidth / 2.0f),
                    Value = i,
                };
                mid -= fanWidth;
                if (slice.MinValue < -Math.PI)
                {
                    mid = -mid;
                    mid -= fanWidth;
                }
                _slices.Add(slice);
            }
        }

        public event EventHandler<SliceDidChangedEventArgs> SliceDidChanged;
        public class SliceDidChangedEventArgs : EventArgs
        {
            public string Value { get; set; }
        }


        public override void TouchesBegan(MonoTouch.Foundation.NSSet touches, UIEvent evt)
        {
            var touch = (UITouch)touches.AnyObject;
            var point = touch.LocationInView(this);
            var distance = CalculateDistanceFromCenter(point);
            if (distance < 40 || distance > 100)
            {
                return;
            }
            _startTransform = _container.Transform;

            var label = GetLabelByValue(_currentSliceValue);
            label.Alpha = _minAlphaValue;

            var dx = point.X - _container.Center.X;
            var dy = point.Y - _container.Center.Y;
            _deltaAngle = (float)Math.Atan2(dx, dy);
        }

        public override void TouchesMoved(MonoTouch.Foundation.NSSet touches, UIEvent evt)
        {
            var touch = (UITouch)touches.AnyObject;
            var point = touch.LocationInView(this);

            var dx = point.X - _container.Center.X;
            var dy = point.Y - _container.Center.Y;
            var angle = Math.Atan2(dy, dx);
            var angleDiff = (float)(_deltaAngle - angle);

            _startTransform.Rotate(-angleDiff);
            _container.Transform = CGAffineTransform.MakeRotation(-angleDiff);
        }

        public override void TouchesEnded(MonoTouch.Foundation.NSSet touches, UIEvent evt)
        {
            var radians = (float)Math.Atan2(_container.Transform.xx, _container.Transform.yx);
            var newVal = 0.0f;
            foreach (var slice in _slices)
            {
                if (slice.MinValue > 0 && slice.MaxValue < 0)
                {
                    if (slice.MaxValue > radians || slice.MinValue < radians)
                    {
                        if (radians > 0)
                        {
                            newVal = radians - (float)Math.PI;
                        }
                        else
                        {
                            newVal = (float)(Math.PI + radians);
                        }
                        _currentSliceValue = slice.Value;
                    }
                }
                if (radians > slice.MinValue && radians < slice.MaxValue)
                {
                    newVal = radians - slice.MidValue;
                    _currentSliceValue = slice.Value;
                }
            }

            BeginAnimations(null, new IntPtr());
            SetAnimationDuration(0.2);
            var t = CGAffineTransform.MakeRotation(-newVal);
            _container.Transform = t;
            CommitAnimations();

            var label = GetLabelByValue(_currentSliceValue);
            label.Alpha = _maxAlphaValue;
        }

        private float CalculateDistanceFromCenter(PointF point)
        {
            var center = new PointF(Bounds.Size.Width / 2.0f, Bounds.Size.Height / 2.0f);
            var dx = (point.X - center.X);
            var dy = (point.Y - center.Y);
            var distance = (float)Math.Sqrt(dx * dx + dy * dy);
            return distance;
        }

        private UIImageView GetLabelByValue(int value)
        {
            var currentLabel = new UIImageView();

            var labels = _container.Subviews;
            foreach (UIImageView label in labels)
            {
                if (label.Tag == value)
                    currentLabel = label;
            }
            return currentLabel;
        }
    }
}
