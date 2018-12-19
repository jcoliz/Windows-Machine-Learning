﻿using Mono.Options;
using System;
using System.Collections.Generic;

namespace SampleModule
{
    public class AppOptions: OptionSet
    {
        public bool Help { get; private set; }
/*
        public string DeviceId { get; private set; }
        public bool ShowList { get; private set; }
        public bool ShowConfig { get; private set; }
        public bool UseEdge { get; private set; }
        */
        public string ModelPath { get; private set; }
        public string ImagePath { get; private set; }
        public bool Exit { get; private set; } = false;

        public AppOptions()
        {
            Add( "h|help", "show this message and exit", v => Help = v != null );
/*
            Add( "l|list", "list available devices and exit", v => ShowList = v != null);
            Add( "d|device=", "the {ID} of device to connect", v => DeviceId = v);
            Add( "e|edge", "transmit through azure edge", v => UseEdge = v != null);
            Add( "c|config", "display device configuration", v => ShowConfig = v != null);
            */
            Add( "m|model=", "path to model {FILE}", v => ModelPath = v);
            Add( "i|image=", "path to image {FILE}", v => ImagePath = v);
        }

        public new List<string> Parse(IEnumerable<string> args)
        {
            var result = base.Parse(args);

            if (Help || string.IsNullOrEmpty(ModelPath) || string.IsNullOrEmpty(ImagePath) )
            {
                Console.WriteLine($"{AppName} {AppVersion}");
                WriteOptionDescriptions(Console.Out);
                Exit = true;
            }

            return result;
        }

        static private string AppName => typeof(AppOptions).Assembly.GetName().Name;
        static private string AppVersion => typeof(AppOptions).Assembly.GetName().Version.ToString();
    }
}
