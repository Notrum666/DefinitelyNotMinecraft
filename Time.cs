using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DefinitelyNotMinecraft
{
    public static class Time
    {
        
        public static double DeltaTime { get; internal set; }
        public static double TotalTime { get; internal set; }
        internal static readonly double FixedDeltaTime = 1.0 / 100.0;
        //private static Stopwatch time;
        //internal static void Init()
        //{
        //    time = new Stopwatch();
        //    time.Start();
        //}
        //internal static void Update()
        //{
        //    DeltaTime = time.ElapsedMilliseconds / 1000.0;
        //    time.Restart();
        //}
    }
}
