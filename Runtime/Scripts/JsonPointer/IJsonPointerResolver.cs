namespace UnityGLTF.JsonPointer
{
	public interface IJsonPointerResolver
	{
		bool TryResolve(object target, ref string path);
	}
}
