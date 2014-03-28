using System;
using System.Collections;
using System.Text;
using System.Configuration;
using System.Xml;

namespace ReportingServicesSourceControl
{
    public class ServerConfiguration : ConfigurationSection
    {

        [ConfigurationProperty("servers")]
        public ServerCollection Servers
        {
            get
            {
                return (ServerCollection)base["servers"];
            }
        }

    }

    [ConfigurationCollection(typeof(ServerElement))]
    public class ServerCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {

            return new ServerElement();

        }



        protected override object GetElementKey(ConfigurationElement element)
        {

            return ((ServerElement)(element)).Url;

        }



        public ServerElement this[int idx]
        {

            get
            {

                return (ServerElement)BaseGet(idx);

            }

        }
    }
    
    public class ServerElement : ConfigurationElement 
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get
            {
                return this["name"].ToString();
            }
            set
            {
                this["name"] = value;
            }
        }

        [ConfigurationProperty("url", IsRequired = true)]
        public string Url
        {
            get
            {
                return this["url"].ToString();
            }
            set
            {
                this["url"] = value;
            }
        }

        [ConfigurationProperty("useDefaultCredentials", DefaultValue = "false", IsRequired = false)]
        public bool UseDefaultCredentials
        {
            get
            {
                return bool.Parse(this["useDefaultCredentials"].ToString());
            }
            set
            {
                this["useDefaultCredentials"] = value;
            }
        }

        [ConfigurationProperty("username", DefaultValue = "", IsRequired = false)]
        public string Username
        {
            get
            {
                return this["username"].ToString();
            }
            set
            {
                this["username"] = value;
            }
        }

        [ConfigurationProperty("password", DefaultValue = "", IsRequired = false)]
        public string Password
        {
            get
            {
                return this["password"].ToString();
            }
            set
            {
                this["password"] = value;
            }
        }

        [ConfigurationProperty("domain", DefaultValue = "", IsRequired = false)]
        public string Domain
        {
            get
            {
                return this["domain"].ToString();
            }
            set
            {
                this["domain"] = value;
            }
        }
    }
}