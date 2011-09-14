﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenFileSystem.IO;
using OpenFileSystem.IO.FileSystems.InMemory;
using OpenWrap.IO.Packaging;
using OpenWrap.PackageModel;
using OpenWrap.Repositories;
using OpenWrap.Runtime;

namespace OpenWrap.PackageManagement.Packages
{
    public class InMemoryPackage : IPackageInfo, IPackage
    {
        public InMemoryPackage()
        {
            Created = DateTime.Now;
            Dependencies = new List<PackageDependency>();
            Descriptor = new PackageDescriptor();
        }

        public bool Anchored { get; set; }
        public DateTimeOffset Created { get; private set; }
        public ICollection<PackageDependency> Dependencies { get; set; }

        public string Description { get; set; }

        public string FullName
        {
            get { return Name + "-" + Version; }
        }

        public PackageIdentifier Identifier
        {
            get { return new PackageIdentifier(Name, Version); }
        }

        public string Name { get; set; }
        public bool Nuked { get; set; }

        public bool IsValid
        {
            get { return true; }
        }

        public IPackageRepository Source { get; set; }
        public Version Version { get; set; }
        Dictionary<string, List<Exports.IFile>> _content = new Dictionary<string, List<Exports.IFile>>();
        public ICollection<Exports.IFile> this[string exportName]
        {
            get
            { 
                List<Exports.IFile> outValue;
                if (!_content.TryGetValue(exportName, out outValue))
                    _content[exportName] = outValue = new List<Exports.IFile>();
                return outValue;
            }
        }
        public IEnumerable<IGrouping<string, Exports.IFile>> Content
        {
            get { return _content.SelectMany(x => x.Value.GroupBy(_ => x.Key)); }
        }

        public IPackageDescriptor Descriptor { get; private set; }

        public string Namespace { get; set; }

        public Stream OpenStream()
        {
            var package = new InMemoryFile(@"c:\test.wrap");
            Packager.NewWithDescriptor(package, Name, Version.ToString(), Dependencies.Select(x => "depends: " + x).ToArray());
            return package.OpenRead();
        }

        public IPackage Load()
        {
            return this;
        }
    }
}