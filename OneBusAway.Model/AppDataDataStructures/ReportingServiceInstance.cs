using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneBusAway.Model.AppDataDataStructures
{
  public enum ServiceType
  {
    OneBusAway
  }

  public class ReportingServiceInstance
  {
    public Uri EndpointUrl { get; private set; }
    public ServiceType Kind { get; private set; }
    public string Name { get; private set; }
    public string Id { get; private set; }
    public bool Experimental { get; private set; }
    public bool SupportsRealtime { get; private set; }
    public string ContactEmail { get; private set; }
    public Uri StopInfoUrl { get; private set; }
    public Uri TwitterUrl { get; private set; }
    public Uri FacebookUrl { get; private set; }
  }
}
