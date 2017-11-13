﻿using System;
using System.Linq;
using DryIoc;
using Prism.AppModel;
using Prism.Behaviors;
using Prism.Common;
using Prism.DryIoc.Modularity;
using Prism.DryIoc.Navigation;
using Prism.Events;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Services;
using Xamarin.Forms;
using DependencyService = Prism.Services.DependencyService;

namespace Prism.DryIoc
{
    /// <summary>
    /// Application base class using DryIoc
    /// </summary>
    public abstract class PrismApplication : PrismApplicationBase<IContainer>
    {
        /// <summary>
        /// Service key used when registering the <see cref="DryIocPageNavigationService"/> with the container
        /// </summary>

        /// <summary>
        /// Create a new instance of <see cref="PrismApplication"/>
        /// </summary>
        /// <param name="platformInitializer">Class to initialize platform instances</param>
        /// <remarks>
        /// The method <see cref="IPlatformInitializer.RegisterTypes(IContainer)"/> will be called after <see cref="PrismApplication.RegisterTypes()"/> 
        /// to allow for registering platform specific instances.
        /// </remarks>
        protected PrismApplication(IPlatformInitializer platformInitializer = null)
            : base(platformInitializer)
        {

        }

        /// <summary>
        /// Create a default instance of <see cref="IContainer" /> with <see cref="Rules" /> created in
        /// <see cref="CreateContainerRules" />
        /// </summary>
        /// <returns>An instance of <see cref="IContainer" /></returns>
        protected override IContainer CreateContainer()
        {
            var rules = CreateContainerRules();
            return new Container(rules);
        }

        protected override IModuleManager CreateModuleManager()
        {
            return Container.Resolve<IModuleManager>();
        }

        /// <summary>
        /// Create <see cref="Rules" /> to alter behavior of <see cref="IContainer" />
        /// </summary>
        /// <remarks>
        /// Default rule is to consult <see cref="Xamarin.Forms.DependencyService" /> if the requested type cannot be inferred from
        /// <see cref="Container" />
        /// </remarks>
        /// <returns>An instance of <see cref="Rules" /></returns>
        protected virtual Rules CreateContainerRules() => 
            Rules.Default.WithAutoConcreteTypeResolution();

        protected override void ConfigureContainer()
        {
            Container.UseInstance(Logger);
            Container.UseInstance(ModuleCatalog);
            Container.UseInstance(Container);
            Container.Register<INavigationService, DryIocPageNavigationService>();
            Container.Register<INavigationService>(
                made: Made.Of(() => SetPage(Arg.Of<INavigationService>(), Arg.Of<Page>())),
                setup: Setup.Decorator);
            Container.Register<IApplicationProvider, ApplicationProvider>(Reuse.Singleton);
            Container.Register<IApplicationStore, ApplicationStore>(Reuse.Singleton);
            Container.Register<IModuleManager, ModuleManager>(Reuse.Singleton);
            Container.Register<IModuleInitializer, DryIocModuleInitializer>(Reuse.Singleton);
            Container.Register<IEventAggregator, EventAggregator>(Reuse.Singleton);
            Container.Register<IDependencyService, DependencyService>(Reuse.Singleton);
            Container.Register<IPageDialogService, PageDialogService>(Reuse.Singleton);
            Container.Register<IDeviceService, DeviceService>(Reuse.Singleton);
            Container.Register<IPageBehaviorFactory, PageBehaviorFactory>(Reuse.Singleton);            
        }

        protected override void InitializeModules()
        {
            if (ModuleCatalog.Modules.Any())
            {
                var manager = Container.Resolve<IModuleManager>();
                manager.Run();
            }
        }

        /// <summary>
        /// Create instance of <see cref="INavigationService"/>
        /// </summary>
        /// <remarks>
        /// The <see cref="_navigationServiceKey"/> is used as service key when resolving
        /// </remarks>
        /// <returns>Instance of <see cref="INavigationService"/></returns>
        protected override INavigationService CreateNavigationService()
        {
            return Container.Resolve<INavigationService>();
        }

        protected override void ConfigureViewModelLocator()
        {
            ViewModelLocationProvider.SetDefaultViewModelFactory((view, type) =>
            {
                switch(view)
                {
                    case Page page:
                        var getVM = Container.Resolve<Func<Page, object>>(type);
                        return getVM(page);
                    default:
                        return Container.Resolve(type);
                }
            });
        }

        internal static INavigationService SetPage(INavigationService navigationService, Page page)
        {
            if(navigationService is IPageAware pageAware)
            {
                pageAware.Page = page;
            }

            return navigationService;
        }
    }
}