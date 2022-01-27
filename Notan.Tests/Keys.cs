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
        Compare("_őőőőőőőőőőőőőőőő");
        Compare("😀");
        Assert.IsTrue(new Key(Encoding.UTF8, new byte[] { 0xC3, 0x28 }) != "");
        Assert.IsTrue(new Key(Encoding.Unicode, new byte[] { 0xC3, 0x28 }) != "");
    }

    private static void Compare(string str)
    {
        Assert.IsTrue(new Key(Encoding.UTF8, Encoding.UTF8.GetBytes(str)) == str);
        Assert.IsTrue(new Key(Encoding.Unicode, Encoding.Unicode.GetBytes(str)) == str);
    }
}
