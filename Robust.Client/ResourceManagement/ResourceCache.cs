﻿using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;

namespace Robust.Client.ResourceManagement
{
    public partial class ResourceCache : ResourceManager, IResourceCacheInternal, IDisposable
    {
        private Dictionary<(ResourcePath, Type), BaseResource> CachedResources = new Dictionary<(ResourcePath, Type), BaseResource>();

        public T GetResource<T>(string path, bool useFallback = true) where T : BaseResource, new()
        {
            return GetResource<T>(new ResourcePath(path), useFallback);
        }

        public T GetResource<T>(ResourcePath path, bool useFallback = true) where T : BaseResource, new()
        {
            if (CachedResources.TryGetValue((path, typeof(T)), out var cached))
            {
                return (T)cached;
            }

            var _resource = new T();
            try
            {
                _resource.Load(this, path);
                CachedResources[(path, typeof(T))] = _resource;
                return _resource;
            }
            catch (Exception e)
            {
                if (useFallback && _resource.Fallback != null)
                {
                    Logger.Error($"Exception while loading resource {typeof(T)} at '{path}', resorting to fallback.\n{Environment.StackTrace}\n{e}");
                    return GetResource<T>(_resource.Fallback, false);
                }
                else
                {
                    Logger.Error($"Exception while loading resource {typeof(T)} at '{path}', no fallback available\n{Environment.StackTrace}\n{e}");
                    throw;
                }
            }
        }

        public bool TryGetResource<T>(string path, out T resource) where T : BaseResource, new()
        {
            return TryGetResource(new ResourcePath(path), out resource);
        }

        public bool TryGetResource<T>(ResourcePath path, out T resource) where T : BaseResource, new()
        {
            if (CachedResources.TryGetValue((path, typeof(T)), out var cached))
            {
                resource = (T)cached;
                return true;
            }
            var _resource = new T();
            try
            {
                _resource.Load(this, path);
                resource = _resource;
                CachedResources[(path, typeof(T))] = resource;
                return true;
            }
            catch
            {
                resource = null;
                return false;
            }
        }

        public void ReloadResource<T>(string path) where T : BaseResource, new()
        {
            ReloadResource<T>(new ResourcePath(path));
        }

        public void ReloadResource<T>(ResourcePath path) where T : BaseResource, new()
        {
            var resource = new T();
            try
            {
                resource.Load(this, path);
                CachedResources[(path, typeof(T))] = resource;
            }
            catch (Exception e)
            {
                Logger.Error($"Exception while reloading resource {typeof(T)} at '{path}'\n{e}");
                throw;
            }
        }

        public bool HasResource<T>(string path) where T : BaseResource, new()
        {
            return HasResource<T>(new ResourcePath(path));
        }

        public bool HasResource<T>(ResourcePath path) where T : BaseResource, new()
        {
            return TryGetResource<T>(path, out var _);
        }

        public void CacheResource<T>(string path, T resource) where T : BaseResource, new()
        {
            CacheResource(new ResourcePath(path), resource);
        }

        public void CacheResource<T>(ResourcePath path, T resource) where T : BaseResource, new()
        {
            CachedResources[(path, typeof(T))] = resource;
        }

        public T GetFallback<T>() where T : BaseResource, new()
        {
            var res = new T();
            if (res.Fallback == null)
            {
                throw new InvalidOperationException($"Resource of type '{typeof(T)}' has no fallback.");
            }
            return GetResource<T>(res.Fallback, useFallback: false);
        }

        #region IDisposable Members

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var res in CachedResources.Values)
                {
                    res.Dispose();
                }
            }

            disposed = true;
        }

        ~ResourceCache()
        {
            Dispose(false);
        }

        #endregion IDisposable Members
    }
}