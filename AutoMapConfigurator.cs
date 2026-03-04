using Bindito.Core;

namespace Calloatti.AutoTools
{
  [Context("Game")]
  public class AutoMapConfigurator : Configurator
  {
    protected override void Configure()
    {
      Bind<AutoMapInputService>().AsSingleton();
      Bind<AutoMapService>().AsSingleton();
      Bind<AutoMapHoverService>().AsSingleton(); // <-- Add this line
    }
  }
}