﻿using System;
using System.Text;
using NUnit.Framework;
using OpenFileSystem.IO;
using OpenWrap.IO;
using OpenWrap.Testing;
using Tests.contexts;

namespace Tests.Configuration.dictionaries
{
    class writing : configuration<ConfigurationDictionary<writing.Config>>
    {
        public writing()
        {
            Entry = new ConfigurationDictionary<Config> { { "sauron", new Config { Evil = "great" } } };
            when_saving_configuration("sauron");
        }

        [Test]
        public void values_are_persisted()
        {
            ConfigurationDirectory.FindFile("sauron")
                .ShouldNotBeNull()
                .OpenRead().ReadString(Encoding.UTF8)
                .ShouldContain(@"[config sauron]")
                .ShouldContain("evil: great");
        }

        public class Config
        {
            public string Evil { get; set; }
        }
    }
}