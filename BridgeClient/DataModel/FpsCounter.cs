using System;
using System.Collections.Generic;
using System.Linq;

namespace BridgeClient.DataModel
{
    public class FpsCounter
    {
        public DateTime LastFrameTime = DateTime.Now;
        public double Fps;

        List<DateTime> Frames = new List<DateTime>();

        public void CalculateFps()
        {
            var oneSecondAgo = DateTime.Now.AddSeconds(-2);

            var toRemove = new List<DateTime>();

            foreach (var frame in Frames.Where(f => f < oneSecondAgo)) toRemove.Add(frame);

            try
            {
                foreach (var f in toRemove) Frames.Remove(f);
            }
            catch (Exception) { }

            Fps = Frames.Count / 2;
        }

        public void GotFrame()
        {
            var frameTime = DateTime.Now;
            LastFrameTime = frameTime;
            Frames.Add(frameTime);

            CalculateFps();
        }

        public int LastFrameMsAgo
        {
            get
            {
                return (int)Math.Round((DateTime.Now - LastFrameTime).TotalMilliseconds);
            }
        }
    }
}
