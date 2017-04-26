using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneBusAway.Model
{
  public class XmlUtilities
  {
    public static string SafeGetValue(XElement element)
    {
      var value = element?.Value;
      if (value == null)
      {
        value = string.Empty;
      }
      return value;
    }
  }
}
