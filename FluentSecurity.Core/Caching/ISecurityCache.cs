using FluentSecurity.ServiceLocation;

namespace FluentSecurity.Caching
{
	public interface ISecurityCache
	{
		T Get<T>(string cacheKey, Lifecycle lifecycle);
		void Set<T>(T item, string cacheKey, Lifecycle lifecycle);
		void Clear(Lifecycle lifecycle);
	}
}