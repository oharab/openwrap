using System;
using System.Collections.Generic;
using OpenWrap.IO;
using OpenWrap.Repositories;

namespace OpenWrap.Dependencies
{
    public class WrapDescriptor : IPackageInfo
    {
        public WrapDescriptor()
        {
            Dependencies = new List<WrapDependency>();
            Description = "";
        }
        public ICollection<WrapDependency> Dependencies { get; set; }

        public string Name { get; set; }

        public Version Version { get; set; }
        public IFile File { get; set; }
        public string Description { get; set; }
        public IPackage Load()
        {
            return null;
        }

        public IPackageRepository Source
        {
            get { return null; }
        }

        public string FullName
        {
            get { return Name + "-" + Version; }
        }

        public bool IsCompatibleWith(Version version)
        {
            return false;
        }

    }
}