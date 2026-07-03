namespace Wevear;

public static class ContainerBuilderExtension
{
    public static IContainerBuilder RegisterSingleton(this IContainerBuilder containerBuilder, in Type serviceType, in Type implementation)
    {
        containerBuilder.Register(new TypeBasedServiceDescriptor()
        {
            ImplementationType = implementation,
            ServiceType = serviceType,
            LifeTime = LifeTime.Single
        });

        return containerBuilder;
    }

    public static IContainerBuilder RegisterSingleton<TService, TImplementation>(this IContainerBuilder containerBuilder) where TImplementation : TService
    {
        containerBuilder.Register(new TypeBasedServiceDescriptor()
        {
            ImplementationType = typeof(TImplementation),
            ServiceType = typeof(TService),
            LifeTime = LifeTime.Single
        });

        return containerBuilder;
    }

    public static IContainerBuilder RegisterSingleton(this IContainerBuilder containerBuilder, in Type serviceType, in object instance)
        => containerBuilder.RegisterInstance(serviceType, instance);

    public static IContainerBuilder RegisterSingleton<TService>(this IContainerBuilder containerBuilder, in object instance)
        => containerBuilder.RegisterInstance(typeof(TService), instance);

    public static IContainerBuilder RegisterTransient(this IContainerBuilder containerBuilder, in Type serviceType, in Type implementation)
    {
        containerBuilder.Register(new TypeBasedServiceDescriptor()
        {
            ImplementationType = implementation,
            ServiceType = serviceType,
            LifeTime = LifeTime.Transient
        });

        return containerBuilder;
    }

    public static IContainerBuilder RegisterTransient<TService, TImplementation>(this IContainerBuilder containerBuilder) where TImplementation : TService
    {
        containerBuilder.Register(new TypeBasedServiceDescriptor()
        {
            ImplementationType = typeof(TImplementation),
            ServiceType = typeof(TService),
            LifeTime = LifeTime.Transient
        });

        return containerBuilder;
    }

    public static IContainerBuilder RegisterScoped(this IContainerBuilder containerBuilder, in Type serviceType, in Type implementation)
    {
        containerBuilder.Register(new TypeBasedServiceDescriptor()
        {
            ImplementationType = implementation,
            ServiceType = serviceType,
            LifeTime = LifeTime.Scoped
        });

        return containerBuilder;
    }

    public static IContainerBuilder RegisterScoped<TService, TImplementation>(this IContainerBuilder containerBuilder) where TImplementation : TService
    {
        containerBuilder.Register(new TypeBasedServiceDescriptor()
        {
            ImplementationType = typeof(TImplementation),
            ServiceType = typeof(TService),
            LifeTime = LifeTime.Scoped
        });

        return containerBuilder;
    }

    public static IContainerBuilder RegisterSingleton(this IContainerBuilder containerBuilder, in Type serviceType, in Func<IScope, object> factory)
        => containerBuilder.RegisterFactory(serviceType, factory, LifeTime.Single);

    public static IContainerBuilder RegisterTransient(this IContainerBuilder containerBuilder, in Type serviceType, in Func<IScope, object> factory)
        => containerBuilder.RegisterFactory(serviceType, factory, LifeTime.Transient);

    public static IContainerBuilder RegisterScoped(this IContainerBuilder containerBuilder, in Type serviceType, in Func<IScope, object> factory)
        => containerBuilder.RegisterFactory(serviceType, factory, LifeTime.Scoped);

    private static IContainerBuilder RegisterFactory(this IContainerBuilder containerBuilder, in Type serviceType, in Func<IScope, object> factory, in LifeTime lifeTime)
    {
        containerBuilder.Register(new FactoryBasedServiceDescriptor()
        {
            LifeTime = lifeTime,
            Factory = factory,
            ServiceType = serviceType,
        });

        return containerBuilder;
    }

    private static IContainerBuilder RegisterInstance(this IContainerBuilder containerBuilder, in Type serviceType, object instance)
    {
        containerBuilder.Register(new InstanceBasedServiceDescriptor(serviceType, instance));

        return containerBuilder;
    }
}
