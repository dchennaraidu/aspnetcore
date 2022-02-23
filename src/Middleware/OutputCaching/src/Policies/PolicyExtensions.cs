// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OutputCaching.Policies;
public static class PolicyExtensions
{
    public static PredicatePolicy When(this IOutputCachingPolicy policy, Func<IOutputCachingContext, Task<bool>> predicate)
    {
        return new PredicatePolicy(predicate, policy);
    }

    public static PredicatePolicy Map(this IOutputCachingPolicy policy, PathString pathBase)
    {
        return new PredicatePolicy(context =>
        {
            var match = context.HttpContext.Request.Path.StartsWithSegments(pathBase);
            return Task.FromResult(match);
        }, policy);
    }

    public static PredicatePolicy Map(this IOutputCachingPolicy policy, params PathString[] pathBases)
    {
        return new PredicatePolicy(context =>
        {
            var match = pathBases.Any(x => context.HttpContext.Request.Path.StartsWithSegments(x));
            return Task.FromResult(match);
        }, policy);
    }

    public static PredicatePolicy Methods(this IOutputCachingPolicy policy, string method)
    {
        return new PredicatePolicy(context =>
        {
            var upperMethod = method.ToUpperInvariant();
            var match = context.HttpContext.Request.Method.ToUpperInvariant() == upperMethod;
            return Task.FromResult(match);
        }, policy);
    }

    public static PredicatePolicy Methods(this IOutputCachingPolicy policy, params string[] methods)
    {
        return new PredicatePolicy(context =>
        {
            var upperMethods = methods.Select(m => m.ToUpperInvariant()).ToArray();
            var match = methods.Any(m => context.HttpContext.Request.Method.ToUpperInvariant() == m);
            return Task.FromResult(match);
        }, policy);
    }

    public static TBuilder WithOutputCachingPolicy<TBuilder>(this TBuilder builder, params IOutputCachingPolicy[] items) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(items, nameof(items));

        var policiesMetadata = new PoliciesMetadata();
        policiesMetadata.Policies.AddRange(items);

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(policiesMetadata);
        });
        return builder;
    }

    public static TBuilder OutputCacheVaryByQuery<TBuilder>(this TBuilder builder, params string[] queryKeys) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(queryKeys, nameof(queryKeys));

        return builder.WithOutputCachingPolicy(queryKeys.Select( q => new VaryByQueryPolicy(q)).ToArray());
    }

    public static TBuilder OutputCacheProfile<TBuilder>(this TBuilder builder, string profileName) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(profileName, nameof(profileName));

        return builder.WithOutputCachingPolicy(new ProfilePolicy(profileName));
    }
}
