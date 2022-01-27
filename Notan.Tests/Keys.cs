using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Serialization;
using System.Text;

namespace Notan.Tests;

[TestClass]
public class Keys
{
    [TestMethod]
    public void Key()
    {
        Compare("");
        Compare("ascii");
        Compare("_ő_á_ű_");
        Compare("😀");
        Assert.IsTrue(new Key(new byte[] { 0xC3, 0x28 }) != "");
    }

    private static void Compare(string str)
    {
        Assert.IsTrue(new Key(Encoding.UTF8.GetBytes(str)) == str);
    }
}
