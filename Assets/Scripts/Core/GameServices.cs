using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameServices
{
    private static readonly Dictionary<Type, object> services = new();

    public static void Register<T>(T service)
    {
        var type = typeof(T);

        if (services.ContainsKey(type))
        {
            Debug.LogWarning($"[GameServices] Service {type.Name} déjà enregistré. Il sera remplacé.");
            services[type] = service;
        }
        else
        {
            services.Add(type, service);
        }
    }

    public static void Unregister<T>(T service)
    {
        var type = typeof(T);

        if (services.TryGetValue(type, out var existingService) && ReferenceEquals(existingService, service))
        {
            services.Remove(type);
        }
        else
        {
            Debug.LogWarning($"[GameServices] Impossible de désenregistrer le service {type.Name}. Il n'est pas enregistré ou ne correspond pas à l'instance.");
        }
    }

    public static T Get<T>() where T : class
    {
        var type = typeof(T);

        if (services.TryGetValue(type, out var service))
        {
            return service as T;
        }

        Debug.LogError($"[GameServices] Service {type.Name} introuvable !");
        return null;
    }

    public static bool IsRegistered<T>()
    {
        return services.ContainsKey(typeof(T));
    }

    public static void ClearAll()
    {
        services.Clear();
    }
}