using System.IO;
using HuaShang.DocImport;
using HuaShang.Rules.Config;

namespace HuaShang.Tests
{
    /// <summary>
    /// 测试直接从 docs/ 读配置，预期值也从这里取，不在测试里写第二套数字（docs/26 §2）。
    /// Unity 编辑器的工作目录是工程根，向上查找 docs/04 即可。
    /// </summary>
    public static class Fixture
    {
        static ConfigSnapshot cached;

        public static string ProjectRoot => DocsConfigReader.FindProjectRoot(Directory.GetCurrentDirectory());

        public static ConfigSnapshot Config
        {
            get
            {
                if (cached == null) cached = DocsConfigReader.ReadAll(ProjectRoot);
                return cached;
            }
        }
    }
}
