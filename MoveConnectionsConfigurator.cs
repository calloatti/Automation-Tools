using Bindito.Core;
using Timberborn.EntityPanelSystem;

namespace Calloatti.MoveConnections
{
  [Context("Game")]
  internal class MoveConnectionsConfigurator : Configurator
  {
    private class EntityPanelModuleProvider : IProvider<EntityPanelModule>
    {
      private readonly MoveConnectionsFragment _moveConnectionsFragment;

      public EntityPanelModuleProvider(MoveConnectionsFragment moveConnectionsFragment)
      {
        _moveConnectionsFragment = moveConnectionsFragment;
      }

      public EntityPanelModule Get()
      {
        EntityPanelModule.Builder builder = new EntityPanelModule.Builder();
        // Inject the button into the Left Header container (alongside Delete and Copy Settings).
        // Order '99' ensures it appears at the end of that specific icon row.
        builder.AddLeftHeaderFragment(_moveConnectionsFragment, 99);
        return builder.Build();
      }
    }

    protected override void Configure()
    {
      Bind<MoveConnectionsTool>().AsSingleton();
      Bind<MoveConnectionsFragment>().AsSingleton();

      MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
    }
  }
}