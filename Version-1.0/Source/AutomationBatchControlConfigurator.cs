using Bindito.Core;
using Timberborn.BatchControl;

namespace Calloatti.AutomationUI
{
  [Context("Game")]
  public class AutomationBatchControlConfigurator : Configurator
  {
    protected override void Configure()
    {
      // Bind the tab itself as a singleton so Dependency Injection can build it
      Bind<AutomationBatchControlTab>().AsSingleton();

      // Inject it into the game's Batch Control Module
      MultiBind<BatchControlModule>().ToProvider<BatchControlModuleProvider>().AsSingleton();
    }

    private class BatchControlModuleProvider : IProvider<BatchControlModule>
    {
      private readonly AutomationBatchControlTab _automationTab;

      public BatchControlModuleProvider(AutomationBatchControlTab automationTab)
      {
        _automationTab = automationTab;
      }

      public BatchControlModule Get()
      {
        BatchControlModule.Builder builder = new BatchControlModule.Builder();

        // Add our custom tab. '8' puts it at the far right of the top menu tabs.
        builder.AddTab(_automationTab, 99);

        return builder.Build();
      }
    }
  }
}