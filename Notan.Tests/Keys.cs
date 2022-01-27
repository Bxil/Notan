using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
using Notan.Serialization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

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
    }

    private static void Compare(string str)
    {
        Assert.IsTrue(new Key(Encoding.UTF8.GetBytes(str)) == str);
    }
}
