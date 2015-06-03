namespace FluentSecurity.ServiceLocation.LifeCycles
{
	public class NullObjectCache : IObjectCache
	{
		public object Get(object key)
		{
			return null;
		}

		public void Add(object key, object instance) {}

		public void Set(object key, object instance) {}
		
		public void Clear() {}
	}
}