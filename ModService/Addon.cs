using System;

namespace arma_launcher.ModService
{
    public struct Addon : IEquatable<Addon>
    {
        public string Md5 { get; set; }
        public string Path { get; set; }
        public string Pbo { get; set; }
        public long Size { get; set; }
        public int Time { get; set; }
        public string Url { get; set; }

        public bool Equals(Addon addon) => Path == addon.Path && Pbo == addon.Pbo;
    }
}