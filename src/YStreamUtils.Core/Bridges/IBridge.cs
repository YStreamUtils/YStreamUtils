using Jint;
using Jint.Native;

namespace YStreamUtils.Core.Bridges;

public interface IBridge
{
    void Register(Engine vm, JsObject hostObj);
}