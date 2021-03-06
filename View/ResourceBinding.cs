﻿using System;
using System.Reflection;
using System.Windows.Media;

namespace arma_launcher.View
{
    public static class ResourceBinding
    {
        private static readonly Random Random = new Random();

        public static ImageSource RandomImage
        {
            get
            {
                var id = Random.Next(0, 11);
                var path = "pack://application:,,,/images/backgrounds/" + id + ".jpg";
                return new ImageSourceConverter().ConvertFromString(path) as ImageSource;
            }
        }

        public static string GetTitle =>
            Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
    }
}