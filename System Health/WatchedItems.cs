using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;

namespace OuterBanks.software.SystemHealth
{
    class WatchedItems
    {
        private List<WatchedItem> items = new List<WatchedItem>();
        public List<WatchedItem> Items
        {
            get
            {
                return this.items;
            }
            protected set
            {
                this.items = value;
            }
        }

        private StringBuilder log = new StringBuilder();
        public StringBuilder Log
        {
            get
            {
                return this.log;
            }
            protected set
            {
                this.log = value;
            }
        }

        public WatchedItems()
        {
            try
            {
                XmlSchemaSet schema = new XmlSchemaSet();
                schema.Add(@"http://outerbanks.software/config.xsd", @"config.xsd");

                XDocument watchedItemsXML = XDocument.Load(@"config.xml");

                watchedItemsXML.Validate(schema, (o, e) =>
                        {
                            Log.AppendLine(e.Message);
                        });

                foreach(XElement el in watchedItemsXML.Descendants("WatchedItem"))
                {
                    WatchedItem wi = new WatchedItem();

                    wi.Type = el.Attribute("type").Value;
                    wi.Identifier = el.Attribute("identifier").Value;
                    wi.DisplayName = el.Attribute("displayname").Value;

                    Items.Add(wi);
                }
            }
            catch (System.Xml.XmlException e)
            {
                //TODO: handle exceptions
            }
        }


    }
}
