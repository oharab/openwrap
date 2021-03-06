﻿using NUnit.Framework;
using OpenWrap.Configuration;
using OpenWrap.Testing;
using Tests.contexts;

namespace Tests.Configuration.dictionaries
{
    class two_dictionary_values_with_same_name_are_present : configuration<ConfigurationDictionary<reading_no_file.Config>>
    {
        public two_dictionary_values_with_same_name_are_present()
        {
            given_configuration_file("sauron",
                                     "[config openwrap]\r\nfetch:http://wraps.openwrap.org\r\n[config openwrap]\r\nfetch:http://default.openwrap.org");

            when_loading_configuration("sauron");
        }

        [Test]
        public void an_error_is_triggered()
        {
            Error.ShouldBeOfType<InvalidConfigurationException>();
        }

        class Config
        {
            public string Fetch { get; set; }
        }
    }
}