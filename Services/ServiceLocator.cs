using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocationSampling.Services;
public static class ServiceLocator
{
    private static IServiceProvider _serviceProvider;

    // Static property to hold the SharedMainConfigViewModel instance
    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static T GetService<T>() where T : notnull
    {
        return _serviceProvider.GetService<T>();
    }
}
